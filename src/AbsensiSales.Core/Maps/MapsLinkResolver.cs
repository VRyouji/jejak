using System.Net.Http.Headers;

namespace AbsensiSales.Core.Maps;

/// <summary>Hasil pengambilan koordinat. Tidak pernah melempar exception.</summary>
public sealed class HasilKoordinat
{
    public bool Sukses { get; private init; }
    public Koordinat Koordinat { get; private init; }
    public string Pesan { get; private init; } = string.Empty;

    public static HasilKoordinat Berhasil(Koordinat k, string pesan = "")
        => new() { Sukses = true, Koordinat = k, Pesan = pesan };

    public static HasilKoordinat Gagal(string pesan)
        => new() { Sukses = false, Pesan = pesan };
}

/// <summary>
/// Mengubah link Google Maps (panjang, pendek, atau koordinat polos) menjadi
/// latitude/longitude. Link pendek (maps.app.goo.gl) diikuti redirect-nya
/// secara manual, maksimal <see cref="MaksRedirect"/> kali, dan hanya ke
/// domain Google.
///
/// Gratis, tanpa API key. Yang dibaca hanya alamat tujuan redirect (header
/// Location), bukan isi halaman Google Maps.
/// </summary>
public sealed class MapsLinkResolver : IDisposable
{
    private const int MaksRedirect = 6;

    private const string UserAgentBrowser =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

    private readonly HttpClient _http;
    private readonly bool _memilikiHttpClient;

    public MapsLinkResolver() : this(null)
    {
    }

    /// <param name="httpClient">
    /// Kalau diisi, handler-nya HARUS memakai AllowAutoRedirect = false.
    /// </param>
    public MapsLinkResolver(HttpClient? httpClient)
    {
        if (httpClient is null)
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = false };
            _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
            _memilikiHttpClient = true;
        }
        else
        {
            _http = httpClient;
            _memilikiHttpClient = false;
        }

        _http.DefaultRequestHeaders.Remove("User-Agent");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgentBrowser);
        _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "id-ID,id;q=0.9,en-US;q=0.8");
    }

    public async Task<HasilKoordinat> AmbilAsync(string? masukan, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(masukan))
        {
            return HasilKoordinat.Gagal("Link atau koordinat masih kosong.");
        }

        var teks = masukan.Trim();

        // Link panjang / koordinat polos: cukup dibaca, tidak perlu internet.
        if (MapsUrlParser.TryParse(teks, out var langsung))
        {
            return HasilKoordinat.Berhasil(langsung);
        }

        if (!Uri.TryCreate(teks, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return HasilKoordinat.Gagal(
                "Format tidak dikenali. Tempel link Google Maps atau koordinat seperti -6.2088, 106.8456.");
        }

        if (!DomainGoogle(uri))
        {
            return HasilKoordinat.Gagal("Hanya link Google Maps yang didukung.");
        }

        try
        {
            for (var i = 0; i < MaksRedirect; i++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                using var response = await _http.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead, ct);

                var status = (int)response.StatusCode;
                var tujuan = response.Headers.Location;

                if (status is < 300 or >= 400 || tujuan is null)
                {
                    break;
                }

                // Location bisa relatif ("/maps/...").
                uri = tujuan.IsAbsoluteUri ? tujuan : new Uri(uri, tujuan);

                if (MapsUrlParser.TryParse(uri.ToString(), out var hasil))
                {
                    return HasilKoordinat.Berhasil(hasil);
                }

                if (!DomainGoogle(uri))
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return HasilKoordinat.Gagal("Dibatalkan.");
        }
        catch (OperationCanceledException)
        {
            return HasilKoordinat.Gagal("Google Maps tidak merespons. Periksa koneksi internet.");
        }
        catch (HttpRequestException)
        {
            return HasilKoordinat.Gagal("Tidak bisa membuka link. Periksa koneksi internet.");
        }
        catch (Exception ex)
        {
            return HasilKoordinat.Gagal("Gagal membaca link: " + ex.Message);
        }

        return HasilKoordinat.Gagal(
            "Koordinat tidak ditemukan di link ini. Minta sales membagikan ulang lokasi " +
            "(pin) toko, atau isi latitude dan longitude secara manual.");
    }

    private static bool DomainGoogle(Uri uri)
    {
        var host = uri.Host.ToLowerInvariant();
        return host == "goo.gl"
            || host.EndsWith(".goo.gl", StringComparison.Ordinal)
            || host == "g.co"
            || host == "google.com"
            || host.EndsWith(".google.com", StringComparison.Ordinal)
            || host == "google.co.id"
            || host.EndsWith(".google.co.id", StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (_memilikiHttpClient)
        {
            _http.Dispose();
        }
    }
}

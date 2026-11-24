using AbsensiSales.Core.Api;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AbsensiSales.HR.ViewModels;

[QueryProperty(nameof(KunjunganId), "id")]
public partial class DetailKunjunganViewModel : ObservableObject
{
    private readonly ApiService _api;

    public DetailKunjunganViewModel(ApiService api)
    {
        _api = api;
    }

    [ObservableProperty]
    private int _kunjunganId;

    [ObservableProperty]
    private string _judul = "Detail kunjungan";

    [ObservableProperty]
    private string _namaSales = string.Empty;

    [ObservableProperty]
    private string _infoSales = string.Empty;

    [ObservableProperty]
    private string _inisial = "?";

    [ObservableProperty]
    private string _namaToko = string.Empty;

    [ObservableProperty]
    private string _infoToko = string.Empty;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _waktuAwal = string.Empty;

    [ObservableProperty]
    private string _waktuCatat = string.Empty;

    [ObservableProperty]
    private string _lokasi = string.Empty;

    [ObservableProperty]
    private string _tujuan = string.Empty;

    [ObservableProperty]
    private string _catatan = string.Empty;

    [ObservableProperty]
    private string _infoWajah = string.Empty;

    [ObservableProperty]
    private string _infoFoto = string.Empty;

    [ObservableProperty]
    private string? _fotoUrl;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TidakAdaFoto))]
    private bool _adaFoto;

    public bool TidakAdaFoto => !AdaFoto;

    [ObservableProperty]
    private string _pesanError = string.Empty;

    [ObservableProperty]
    private bool _adaError;

    public async Task MuatAsync()
    {
        var hasil = await _api.GetKunjunganDetailAsync(KunjunganId);
        if (!hasil.Sukses || hasil.Data is null)
        {
            PesanError = hasil.Pesan;
            AdaError = true;
            return;
        }

        var kunjungan = hasil.Data;
        PesanError = string.Empty;
        AdaError = false;

        var namaToko = string.IsNullOrWhiteSpace(kunjungan.NamaToko)
            ? "Toko"
            : kunjungan.NamaToko!;
        var kodeToko = string.IsNullOrWhiteSpace(kunjungan.KodeToko)
            ? $"TK-{kunjungan.IdToko}"
            : kunjungan.KodeToko!;

        Judul = $"Detail kunjungan - {namaToko}";
        NamaSales = string.IsNullOrWhiteSpace(kunjungan.NamaSales)
            ? "-"
            : kunjungan.NamaSales!;
        InfoSales = string.IsNullOrWhiteSpace(kunjungan.WilayahSales)
            ? ApiMapper.AtauDash(kunjungan.NomorSales)
            : $"{ApiMapper.AtauDash(kunjungan.NomorSales)} - {kunjungan.WilayahSales}";
        Inisial = ApiMapper.Inisial(kunjungan.NamaSales);
        NamaToko = namaToko;
        InfoToko = $"{kodeToko} - {ApiMapper.AtauDash(kunjungan.AlamatToko)}";
        Status = ApiMapper.StatusKunjungan(kunjungan.StatusVerifikasi);
        WaktuAwal = ApiMapper.Jam(kunjungan.WaktuCheckin);
        WaktuCatat = ApiMapper.Jam(kunjungan.WaktuCheckout);

        var koordinat = kunjungan.LatitudeCheckin.HasValue && kunjungan.LongitudeCheckin.HasValue
            ? $"{kunjungan.LatitudeCheckin.Value:F4}, {kunjungan.LongitudeCheckin.Value:F4}"
            : "-";

        Lokasi = kunjungan.JarakMeter.HasValue
            ? $"{koordinat} - +/-{kunjungan.JarakMeter.Value:F0} m"
            : koordinat;

        Tujuan = string.IsNullOrWhiteSpace(kunjungan.TujuanKunjungan)
            ? "-"
            : kunjungan.TujuanKunjungan!;
        Catatan = string.IsNullOrWhiteSpace(kunjungan.Catatan)
            ? "Tidak ada catatan tambahan."
            : kunjungan.Catatan!;

        InfoWajah = (kunjungan.SkorWajah ?? 0) > 0
            ? $"Skor wajah {kunjungan.SkorWajah!.Value:F2}"
            : "Tidak ada data skor wajah.";

        var foto = kunjungan.FotoBukti?.Trim();
        if (string.IsNullOrWhiteSpace(foto))
        {
            FotoUrl = null;
            AdaFoto = false;
            InfoFoto = "Tidak ada foto bukti.";
        }
        else
        {
            var url = $"{ApiConfig.BaseUrl.TrimEnd('/')}/{foto.TrimStart('/')}";
            var tersedia = await FotoBisaDimuatAsync(url);

            // Jangan tampilkan kotak gambar rusak kalau berkasnya tidak ada di server.
            FotoUrl = tersedia ? url : null;
            AdaFoto = tersedia;
            InfoFoto = tersedia
                ? "Foto bukti tersimpan di server."
                : "Berkas foto tidak ditemukan di server.";
        }
    }

    private static readonly HttpClient FotoClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    private static async Task<bool> FotoBisaDimuatAsync(string url)
    {
        try
        {
            using var respons = await FotoClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!respons.IsSuccessStatusCode)
            {
                return false;
            }

            var tipe = respons.Content.Headers.ContentType?.MediaType ?? string.Empty;
            return tipe.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

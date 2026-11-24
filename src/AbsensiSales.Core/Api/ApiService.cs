using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AbsensiSales.Core.Api;

/// <summary>
/// Satu-satunya kelas yang memanggil backend PHP.
///
/// Pemakaian dasar:
///   var api = new ApiService();
///   var cek = await api.CekKoneksiAsync();
///   var login = await api.LoginAsync("andi@example.com", "rahasia123");
///   if (login.Sukses) { /* api.Token sudah terisi otomatis */ }
///   var toko = await api.GetTokoAsync();
///
/// Semua method mengembalikan <see cref="ApiResult{T}"/> dan TIDAK melempar
/// exception untuk masalah jaringan, jadi aman dipanggil langsung dari ViewModel.
/// </summary>
public sealed class ApiService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Beberapa hosting gratis menutup koneksi kalau User-Agent bukan browser.
    /// Nilai ini membuat request terlihat seperti browser biasa.
    /// </summary>
    private const string UserAgentBrowser =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

    /// <summary>Jumlah percobaan ulang setelah tantangan JavaScript hosting.</summary>
    private const int MaksPercobaanTantangan = 2;

    private readonly HttpClient _http;
    private readonly bool _memilikiHttpClient;

    /// <summary>Cookie <c>__test</c> hasil menyelesaikan tantangan hosting (kalau ada).</summary>
    private string? _cookieTantangan;

    public ApiService() : this(null)
    {
    }

    public ApiService(HttpClient? httpClient)
    {
        if (httpClient is null)
        {
            // UseCookies = false: cookie tantangan dikelola sendiri lewat header,
            // supaya perilakunya sama di Windows dan Android.
            var handler = new HttpClientHandler { UseCookies = false };
            _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            _memilikiHttpClient = true;
        }
        else
        {
            _http = httpClient;
            _memilikiHttpClient = false;
        }

        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.1));

        _http.DefaultRequestHeaders.Remove("User-Agent");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgentBrowser);
        _http.DefaultRequestHeaders.TryAddWithoutValidation(
            "Accept-Language", "id-ID,id;q=0.9,en-US;q=0.8");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("Cache-Control", "no-cache");
    }

    /// <summary>Token Bearer hasil login. Diisi otomatis saat login berhasil.</summary>
    public string? Token { get; private set; }

    public bool SudahLogin => !string.IsNullOrWhiteSpace(Token);

    public void SetToken(string? token) => Token = string.IsNullOrWhiteSpace(token) ? null : token.Trim();

    public void LogoutLokal() => Token = null;

    /* ============================================================
     * CEK KONEKSI
     * ============================================================ */

    /// <summary>
    /// Memanggil health.php. Ini cara paling cepat memastikan
    /// URL, hosting, dan database sudah benar.
    /// </summary>
    public Task<ApiResult<KesehatanData>> CekKoneksiAsync(CancellationToken ct = default)
        => KirimAsync<KesehatanData>(HttpMethod.Get, "health.php", null, butuhToken: false, ct);

    /// <summary>True kalau backend + database merespons dengan sukses.</summary>
    public async Task<bool> TerhubungAsync(CancellationToken ct = default)
    {
        var hasil = await CekKoneksiAsync(ct);
        return hasil.Sukses && hasil.Terhubung;
    }

    /* ============================================================
     * AUTENTIKASI
     * ============================================================ */

    /// <summary>
    /// POST login.php. Parameter pertama boleh email atau username.
    /// Token otomatis disimpan ke properti <see cref="Token"/>.
    /// </summary>
    public async Task<ApiResult<LoginData>> LoginAsync(string emailAtauUsername, string password, CancellationToken ct = default)
    {
        var hasil = await KirimAsync<LoginData>(
            HttpMethod.Post,
            "login.php",
            new { email = emailAtauUsername, password },
            butuhToken: false,
            ct);

        if (hasil.Sukses && !string.IsNullOrWhiteSpace(hasil.Data?.Token))
        {
            SetToken(hasil.Data!.Token);
        }

        return hasil;
    }

    /// <summary>POST register.php — membuat tbl_pengguna + tbl_sales.</summary>
    public Task<ApiResult<RegisterData>> RegisterAsync(RegisterRequest permintaan, CancellationToken ct = default)
        => KirimAsync<RegisterData>(HttpMethod.Post, "register.php", permintaan, butuhToken: false, ct);

    /// <summary>POST logout.php — hapus token di server, lalu bersihkan token lokal.</summary>
    public async Task<ApiResult<object>> LogoutAsync(CancellationToken ct = default)
    {
        var hasil = await KirimAsync<object>(HttpMethod.Post, "logout.php", null, butuhToken: true, ct);
        LogoutLokal();
        return hasil;
    }

    /* ============================================================
     * TOKO
     * ============================================================ */

    public Task<ApiResult<List<TokoDto>>> GetTokoAsync(string? cari = null, CancellationToken ct = default)
    {
        var path = "toko.php";
        if (!string.IsNullOrWhiteSpace(cari))
        {
            path += "?q=" + Uri.EscapeDataString(cari.Trim());
        }
        return KirimAsync<List<TokoDto>>(HttpMethod.Get, path, null, butuhToken: true, ct);
    }

    public Task<ApiResult<TokoDto>> GetTokoAsync(int idToko, CancellationToken ct = default)
        => KirimAsync<TokoDto>(HttpMethod.Get, $"toko.php?id_toko={idToko}", null, butuhToken: true, ct);

    /// <summary>Khusus HR: tambah / ubah / hapus toko.</summary>
    public Task<ApiResult<TokoDto>> SimpanTokoAsync(TokoRequest permintaan, CancellationToken ct = default)
        => KirimAsync<TokoDto>(HttpMethod.Post, "toko.php", permintaan, butuhToken: true, ct);

    /// <summary>Khusus HR: hapus toko beserta kunjungan yang tercatat di sana.</summary>
    public Task<ApiResult<TokoDto>> HapusTokoAsync(int idToko, CancellationToken ct = default)
        => KirimAsync<TokoDto>(
            HttpMethod.Post,
            "toko.php",
            new TokoRequest { Action = "delete", IdToko = idToko },
            butuhToken: true,
            ct);

    /* ============================================================
     * SALES (khusus HR)
     * ============================================================ */

    public Task<ApiResult<List<SalesAdminDto>>> GetSalesAdminAsync(
        string? cari = null,
        CancellationToken ct = default)
    {
        var path = "sales.php";
        if (!string.IsNullOrWhiteSpace(cari))
        {
            path += "?q=" + Uri.EscapeDataString(cari.Trim());
        }
        return KirimAsync<List<SalesAdminDto>>(HttpMethod.Get, path, null, butuhToken: true, ct);
    }

    public Task<ApiResult<SalesAdminDto>> TambahSalesAsync(
        SalesCreateRequest permintaan,
        CancellationToken ct = default)
        => KirimAsync<SalesAdminDto>(HttpMethod.Post, "sales.php", permintaan, butuhToken: true, ct);

    /// <summary>Khusus HR: ubah data akun sales (password opsional).</summary>
    public Task<ApiResult<SalesAdminDto>> UpdateSalesAsync(
        SalesUpdateRequest permintaan,
        CancellationToken ct = default)
        => KirimAsync<SalesAdminDto>(HttpMethod.Post, "sales.php", permintaan, butuhToken: true, ct);

    /// <summary>Khusus HR: hapus akun sales beserta riwayat terkait.</summary>
    public Task<ApiResult<SalesAdminDto>> HapusSalesAsync(
        int idSales,
        CancellationToken ct = default)
        => KirimAsync<SalesAdminDto>(
            HttpMethod.Post,
            "sales.php",
            new SalesActionRequest { Action = "delete", IdSales = idSales },
            butuhToken: true,
            ct);

    /* ============================================================
     * ABSEN / KUNJUNGAN
     * ============================================================ */

    /// <summary>
    /// Unggah foto bukti kunjungan (Base64) dan kembalikan nama file yang
    /// disimpan server. Nama file itulah yang dikirim pada check-in.
    /// </summary>
    public Task<ApiResult<UploadFotoData>> UploadFotoAsync(byte[] foto, CancellationToken ct = default)
    {
        var permintaan = new { data = Convert.ToBase64String(foto) };
        return KirimAsync<UploadFotoData>(HttpMethod.Post, "upload.php", permintaan, butuhToken: true, ct);
    }

    public Task<ApiResult<CheckInData>> CheckInAsync(CheckInRequest permintaan, CancellationToken ct = default)
        => KirimAsync<CheckInData>(HttpMethod.Post, "absen.php", permintaan, butuhToken: true, ct);

    public Task<ApiResult<CheckOutData>> CheckOutAsync(CheckOutRequest permintaan, CancellationToken ct = default)
        => KirimAsync<CheckOutData>(HttpMethod.Post, "absen.php", permintaan, butuhToken: true, ct);

    public Task<ApiResult<List<KunjunganDto>>> GetKunjunganAsync(
        string? tanggal = null,
        string? periode = null,
        string? status = null,
        int? idKunjungan = null,
        int? idSales = null,
        CancellationToken ct = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(tanggal)) query.Add("tanggal=" + Uri.EscapeDataString(tanggal.Trim()));
        if (!string.IsNullOrWhiteSpace(periode)) query.Add("periode=" + Uri.EscapeDataString(periode.Trim()));
        if (!string.IsNullOrWhiteSpace(status)) query.Add("status=" + Uri.EscapeDataString(status.Trim()));
        if (idKunjungan is > 0) query.Add("id_kunjungan=" + idKunjungan.Value);
        if (idSales is > 0) query.Add("id_sales=" + idSales.Value);

        var path = "kunjungan.php" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
        return KirimAsync<List<KunjunganDto>>(HttpMethod.Get, path, null, butuhToken: true, ct);
    }

    /// <summary>Ambil satu kunjungan berdasarkan id (dipakai halaman detail HR).</summary>
    public async Task<ApiResult<KunjunganDto>> GetKunjunganDetailAsync(int idKunjungan, CancellationToken ct = default)
    {
        var hasil = await GetKunjunganAsync(idKunjungan: idKunjungan, ct: ct);
        if (!hasil.Sukses)
        {
            return ApiResult<KunjunganDto>.Gagal(hasil.Pesan, hasil.Kode, hasil.StatusCode);
        }

        var item = hasil.Data?.FirstOrDefault();
        return item is null
            ? ApiResult<KunjunganDto>.Gagal("Kunjungan tidak ditemukan.", "KUNJUNGAN_NOT_FOUND", 404)
            : ApiResult<KunjunganDto>.Berhasil(item, hasil.Pesan, hasil.StatusCode);
    }

    /* ============================================================
     * PERMOHONAN (IZIN / CUTI)
     * ============================================================ */

    public Task<ApiResult<PermohonanDto>> AjukanPermohonanAsync(PermohonanRequest permintaan, CancellationToken ct = default)
        => KirimAsync<PermohonanDto>(HttpMethod.Post, "permohonan.php", permintaan, butuhToken: true, ct);

    public Task<ApiResult<List<PermohonanDto>>> GetPermohonanAsync(string? status = null, CancellationToken ct = default)
    {
        var path = "permohonan.php";
        if (!string.IsNullOrWhiteSpace(status))
        {
            path += "?status=" + Uri.EscapeDataString(status.Trim());
        }
        return KirimAsync<List<PermohonanDto>>(HttpMethod.Get, path, null, butuhToken: true, ct);
    }

    /// <summary>Khusus HR: setujui / tolak permohonan.</summary>
    public Task<ApiResult<PermohonanDto>> SetujuiPermohonanAsync(
        int idPermohonan,
        bool setujui,
        string? catatan = null,
        CancellationToken ct = default)
    {
        var permintaan = new PersetujuanPermohonanRequest
        {
            Action = setujui ? "approve" : "reject",
            IdPermohonan = idPermohonan,
            Catatan = catatan
        };
        return KirimAsync<PermohonanDto>(HttpMethod.Post, "permohonan.php", permintaan, butuhToken: true, ct);
    }

    /* ============================================================
     * LAPORAN (HR)
     * ============================================================ */

    public Task<ApiResult<LaporanKunjunganData>> GetLaporanKunjunganAsync(
        string periode,
        string? tanggal = null,
        CancellationToken ct = default)
    {
        var path = $"laporan.php?jenis=kunjungan&periode={Uri.EscapeDataString(periode)}";
        if (!string.IsNullOrWhiteSpace(tanggal))
        {
            path += "&tanggal=" + Uri.EscapeDataString(tanggal.Trim());
        }
        return KirimAsync<LaporanKunjunganData>(HttpMethod.Get, path, null, butuhToken: true, ct);
    }

    public Task<ApiResult<LaporanPermohonanData>> GetLaporanPermohonanAsync(string periode, CancellationToken ct = default)
        => KirimAsync<LaporanPermohonanData>(
            HttpMethod.Get,
            $"laporan.php?jenis=permohonan&periode={Uri.EscapeDataString(periode)}",
            null,
            butuhToken: true,
            ct);

    public Task<ApiResult<RekapSalesData>> GetRekapSalesAsync(string periode, CancellationToken ct = default)
        => KirimAsync<RekapSalesData>(
            HttpMethod.Get,
            $"laporan.php?jenis=sales&periode={Uri.EscapeDataString(periode)}",
            null,
            butuhToken: true,
            ct);

    /* ============================================================
     * PENGGAJIAN (HR)
     * ============================================================ */

    /// <summary>Rekap gaji terhitung untuk periode YYYY-MM.</summary>
    public Task<ApiResult<PenggajianData>> GetPenggajianAsync(
        string periode,
        CancellationToken ct = default)
        => KirimAsync<PenggajianData>(
            HttpMethod.Get,
            $"penggajian.php?periode={Uri.EscapeDataString(periode)}",
            null,
            butuhToken: true,
            ct);

    /// <summary>Hitung ulang lalu simpan rekap gaji ke tbl_penggajian.</summary>
    public Task<ApiResult<PenggajianData>> ProsesPenggajianAsync(
        string periode,
        CancellationToken ct = default)
        => KirimAsync<PenggajianData>(
            HttpMethod.Post,
            "penggajian.php",
            new { action = "process", periode },
            butuhToken: true,
            ct);

    /* ============================================================
     * INTI: kirim request dan baca response envelope
     * ============================================================ */

    private async Task<ApiResult<T>> KirimAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        bool butuhToken,
        CancellationToken ct)
    {
        if (ApiConfig.BelumDikonfigurasi)
        {
            return ApiResult<T>.TidakTerhubung(
                "Alamat backend belum diisi. Buka src/AbsensiSales.Core/Api/ApiConfig.cs " +
                "dan ganti DefaultBaseUrl dengan domain InfinityFree kamu.");
        }

        if (butuhToken && !SudahLogin)
        {
            return ApiResult<T>.Gagal(
                "Belum login. Panggil LoginAsync() terlebih dahulu.",
                "TOKEN_KOSONG",
                401);
        }

        var url = ApiConfig.Url(path);
        var json = body is null ? null : JsonSerializer.Serialize(body, JsonOptions);

        try
        {
            var percobaanTantangan = 0;

            while (true)
            {
                using var request = new HttpRequestMessage(method, url);

                if (butuhToken)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                }

                if (json is not null)
                {
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                if (_cookieTantangan is not null)
                {
                    request.Headers.TryAddWithoutValidation("Cookie", _cookieTantangan);
                }

                using var response = await _http.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    ct);

                var teks = await response.Content.ReadAsStringAsync(ct);
                var status = (int)response.StatusCode;
                var tipeKonten = response.Content.Headers.ContentType?.MediaType ?? "tidak diketahui";

                // Hosting gratis (InfinityFree) membalas halaman JavaScript challenge,
                // bukan JSON. Hitung cookie __test lalu ulangi request yang sama.
                if (TantanganHosting.Terdeteksi(teks))
                {
                    if (percobaanTantangan >= MaksPercobaanTantangan)
                    {
                        return ApiResult<T>.TidakTerhubung(
                            "Hosting terus mengirim halaman verifikasi JavaScript. " +
                            "Matikan proteksi browser-check di panel hosting, " +
                            "atau pindahkan API ke hosting lain.");
                    }

                    if (!TantanganHosting.CobaSelesaikan(teks, out var cookie))
                    {
                        return ApiResult<T>.TidakTerhubung(
                            "Hosting mengirim halaman verifikasi JavaScript yang tidak bisa " +
                            "diselesaikan otomatis. Buka domain API di browser untuk melewati " +
                            "verifikasi, atau pindahkan API ke hosting tanpa proteksi tersebut.");
                    }

                    _cookieTantangan = "__test=" + cookie;
                    percobaanTantangan++;
                    continue;
                }

                // InfinityFree mengalihkan file yang tidak ada ke halaman error mereka.
                if (HalamanErrorHosting(response, teks))
                {
                    return ApiResult<T>.Gagal(
                        $"Endpoint '{path}' tidak ditemukan di server (404). " +
                        "Pastikan semua file backend/api sudah di-upload ke folder " +
                        "htdocs/api di hosting (termasuk health.php).",
                        "ENDPOINT_TIDAK_ADA",
                        404);
                }

                if (string.IsNullOrWhiteSpace(teks))
                {
                    return ApiResult<T>.Gagal(
                        $"Server tidak mengirim data (HTTP {status}). Periksa file PHP di hosting.",
                        "RESPONSE_KOSONG",
                        status);
                }

                ApiEnvelope<T>? envelope;
                try
                {
                    envelope = JsonSerializer.Deserialize<ApiEnvelope<T>>(teks, JsonOptions);
                }
                catch (JsonException)
                {
                    return ApiResult<T>.Gagal(
                        $"Response bukan JSON (HTTP {status}, {tipeKonten}). " +
                        "Server mengembalikan halaman HTML, biasanya karena file PHP belum " +
                        "di-upload, ada error PHP, atau URL API salah.",
                        "BUKAN_JSON",
                        status);
                }

                if (envelope is null)
                {
                    return ApiResult<T>.Gagal(
                        $"Response tidak dikenali (HTTP {status}).",
                        "RESPONSE_TIDAK_DIKENAL",
                        status);
                }

                if (envelope.Success)
                {
                    return ApiResult<T>.Berhasil(envelope.Data, envelope.Message, status);
                }

                return ApiResult<T>.Gagal(
                    string.IsNullOrWhiteSpace(envelope.Message) ? "Permintaan gagal." : envelope.Message,
                    envelope.Code,
                    status);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return ApiResult<T>.Gagal("Permintaan dibatalkan.", "DIBATALKAN");
        }
        catch (OperationCanceledException)
        {
            return ApiResult<T>.TidakTerhubung(
                $"Server tidak merespons dalam {_http.Timeout.TotalSeconds:F0} detik. " +
                $"Periksa koneksi internet dan URL: {url}");
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<T>.TidakTerhubung(
                $"Tidak bisa menghubungi server di {url}. " +
                "Periksa internet, domain, dan HTTPS/SSL. Detail: " + ex.Message);
        }
        catch (Exception ex)
        {
            return ApiResult<T>.TidakTerhubung(
                "Terjadi kesalahan saat menghubungi server: " + ex.Message);
        }
    }

    /// <summary>True kalau respons berasal dari halaman error hosting (file tidak ada).</summary>
    private static bool HalamanErrorHosting(HttpResponseMessage response, string teks)
    {
        var uriKembali = response.RequestMessage?.RequestUri;
        if (uriKembali is not null &&
            uriKembali.Host.Contains("infinityfree.net", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var lokasi = response.Headers.Location?.ToString();
        if (!string.IsNullOrEmpty(lokasi) &&
            lokasi.Contains("infinityfree.net", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return teks.Contains("errors.infinityfree.net", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (_memilikiHttpClient)
        {
            _http.Dispose();
        }
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace AbsensiSales.Core.Api;

/* ============================================================
 * Bentuk response standar dari semua endpoint PHP:
 *   sukses : { "success": true,  "message": "...", "data": ... }
 *   gagal  : { "success": false, "message": "...", "code": "..." }
 * ============================================================ */
public sealed class ApiEnvelope<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

/* ============================================================
 * health.php
 * ============================================================ */
public sealed class KesehatanData
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("database")]
    public string Database { get; set; } = string.Empty;

    [JsonPropertyName("server_time")]
    public string ServerTime { get; set; } = string.Empty;

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("php_version")]
    public string PhpVersion { get; set; } = string.Empty;

    [JsonPropertyName("mysql_version")]
    public string MysqlVersion { get; set; } = string.Empty;

    [JsonPropertyName("tabel")]
    public Dictionary<string, bool> Tabel { get; set; } = new();

    [JsonPropertyName("tabel_lengkap")]
    public bool TabelLengkap { get; set; }
}

/* ============================================================
 * login.php / register.php
 * ============================================================ */
public sealed class PenggunaDto
{
    [JsonPropertyName("id_pengguna")]
    public int IdPengguna { get; set; }

    [JsonPropertyName("nama")]
    public string Nama { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}

public sealed class SalesDto
{
    [JsonPropertyName("id_sales")]
    public int IdSales { get; set; }

    [JsonPropertyName("nomor_sales")]
    public string NomorSales { get; set; } = string.Empty;

    [JsonPropertyName("nomor_telepon")]
    public string NomorTelepon { get; set; } = string.Empty;

    [JsonPropertyName("face_data")]
    public string? FaceData { get; set; }
}

public sealed class HrDto
{
    [JsonPropertyName("id_hr")]
    public int IdHr { get; set; }
}

public sealed class LoginData
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public string ExpiresAt { get; set; } = string.Empty;

    [JsonPropertyName("pengguna")]
    public PenggunaDto? Pengguna { get; set; }

    [JsonPropertyName("sales")]
    public SalesDto? Sales { get; set; }

    [JsonPropertyName("hr")]
    public HrDto? Hr { get; set; }
}

public sealed class RegisterRequest
{
    [JsonPropertyName("nama")]
    public string Nama { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("nomor_sales")]
    public string NomorSales { get; set; } = string.Empty;

    [JsonPropertyName("nomor_telepon")]
    public string? NomorTelepon { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }
}

public sealed class RegisterData
{
    [JsonPropertyName("pengguna")]
    public PenggunaDto? Pengguna { get; set; }

    [JsonPropertyName("sales")]
    public SalesDto? Sales { get; set; }
}

/* ============================================================
 * toko.php
 * ============================================================ */
public sealed class TokoDto
{
    [JsonPropertyName("id_toko")]
    public int IdToko { get; set; }

    /// <summary>Kode toko; PHP memakai fallback "TK-{id}" bila kolom belum ada.</summary>
    [JsonPropertyName("kode_toko")]
    public string? KodeToko { get; set; }

    [JsonPropertyName("nama_toko")]
    public string NamaToko { get; set; } = string.Empty;

    [JsonPropertyName("pemilik")]
    public string? Pemilik { get; set; }

    [JsonPropertyName("telepon")]
    public string? Telepon { get; set; }

    [JsonPropertyName("alamat")]
    public string? Alamat { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    /// <summary>Nama kolom di ERD: radius.</summary>
    [JsonPropertyName("radius")]
    public double? Radius { get; set; }

    /// <summary>Alias dari PHP supaya cocok dengan properti aplikasi.</summary>
    [JsonPropertyName("radius_meter")]
    public double RadiusMeter
    {
        get => Radius ?? 100;
        set => Radius = value;
    }
}

/* ============================================================
 * absen.php
 * ============================================================ */
public sealed class CheckInRequest
{
    public CheckInRequest()
    {
        Action = "checkin";
    }

    [JsonPropertyName("action")]
    public string Action { get; set; }

    [JsonPropertyName("id_toko")]
    public int IdToko { get; set; }

    [JsonPropertyName("latitude_checkin")]
    public double LatitudeCheckin { get; set; }

    [JsonPropertyName("longitude_checkin")]
    public double LongitudeCheckin { get; set; }

    [JsonPropertyName("foto_bukti")]
    public string? FotoBukti { get; set; }

    [JsonPropertyName("jarak_meter")]
    public double? JarakMeter { get; set; }

    [JsonPropertyName("skor_wajah")]
    public double? SkorWajah { get; set; }

    [JsonPropertyName("tujuan_kunjungan")]
    public string? TujuanKunjungan { get; set; }

    [JsonPropertyName("catatan")]
    public string? Catatan { get; set; }
}

public sealed class CheckOutRequest
{
    public CheckOutRequest()
    {
        Action = "checkout";
    }

    [JsonPropertyName("action")]
    public string Action { get; set; }

    [JsonPropertyName("id_kunjungan")]
    public int IdKunjungan { get; set; }

    [JsonPropertyName("latitude_checkout")]
    public double LatitudeCheckout { get; set; }

    [JsonPropertyName("longitude_checkout")]
    public double LongitudeCheckout { get; set; }
}

public sealed class CheckInData
{
    [JsonPropertyName("id_kunjungan")]
    public int IdKunjungan { get; set; }

    [JsonPropertyName("id_sales")]
    public int IdSales { get; set; }

    [JsonPropertyName("id_toko")]
    public int IdToko { get; set; }

    [JsonPropertyName("nama_toko")]
    public string NamaToko { get; set; } = string.Empty;

    [JsonPropertyName("waktu_checkin")]
    public string WaktuCheckin { get; set; } = string.Empty;

    [JsonPropertyName("status_verifikasi")]
    public string StatusVerifikasi { get; set; } = string.Empty;

    [JsonPropertyName("jarak_meter")]
    public double? JarakMeter { get; set; }

    [JsonPropertyName("dalam_radius")]
    public bool? DalamRadius { get; set; }
}

public sealed class CheckOutData
{
    [JsonPropertyName("id_kunjungan")]
    public int IdKunjungan { get; set; }

    [JsonPropertyName("nama_toko")]
    public string NamaToko { get; set; } = string.Empty;

    [JsonPropertyName("waktu_checkout")]
    public string WaktuCheckout { get; set; } = string.Empty;

    [JsonPropertyName("durasi_menit")]
    public int? DurasiMenit { get; set; }

    [JsonPropertyName("status_verifikasi")]
    public string StatusVerifikasi { get; set; } = string.Empty;
}

/* ============================================================
 * kunjungan.php
 * ============================================================ */
public sealed class KunjunganDto
{
    [JsonPropertyName("id_kunjungan")]
    public int IdKunjungan { get; set; }

    [JsonPropertyName("id_sales")]
    public int IdSales { get; set; }

    [JsonPropertyName("id_toko")]
    public int IdToko { get; set; }

    [JsonPropertyName("nama_toko")]
    public string? NamaToko { get; set; }

    [JsonPropertyName("alamat_toko")]
    public string? AlamatToko { get; set; }

    [JsonPropertyName("kode_toko")]
    public string? KodeToko { get; set; }

    [JsonPropertyName("latitude_toko")]
    public double? LatitudeToko { get; set; }

    [JsonPropertyName("longitude_toko")]
    public double? LongitudeToko { get; set; }

    [JsonPropertyName("radius_toko")]
    public double? RadiusToko { get; set; }

    [JsonPropertyName("nomor_sales")]
    public string? NomorSales { get; set; }

    [JsonPropertyName("nama_sales")]
    public string? NamaSales { get; set; }

    [JsonPropertyName("wilayah")]
    public string? WilayahSales { get; set; }

    [JsonPropertyName("waktu_checkin")]
    public string WaktuCheckin { get; set; } = string.Empty;

    [JsonPropertyName("waktu_checkout")]
    public string? WaktuCheckout { get; set; }

    [JsonPropertyName("durasi_menit")]
    public int? DurasiMenit { get; set; }

    [JsonPropertyName("latitude_checkin")]
    public double? LatitudeCheckin { get; set; }

    [JsonPropertyName("longitude_checkin")]
    public double? LongitudeCheckin { get; set; }

    [JsonPropertyName("latitude_checkout")]
    public double? LatitudeCheckout { get; set; }

    [JsonPropertyName("longitude_checkout")]
    public double? LongitudeCheckout { get; set; }

    [JsonPropertyName("foto_bukti")]
    public string? FotoBukti { get; set; }

    [JsonPropertyName("status_verifikasi")]
    public string StatusVerifikasi { get; set; } = string.Empty;

    [JsonPropertyName("jarak_meter")]
    public double? JarakMeter { get; set; }

    [JsonPropertyName("skor_wajah")]
    public double? SkorWajah { get; set; }

    [JsonPropertyName("tujuan_kunjungan")]
    public string? TujuanKunjungan { get; set; }

    [JsonPropertyName("catatan")]
    public string? Catatan { get; set; }
}

/* ============================================================
 * permohonan.php
 * ============================================================ */
public sealed class PermohonanRequest
{
    [JsonPropertyName("jenis")]
    public string Jenis { get; set; } = "Cuti";

    [JsonPropertyName("tanggal_mulai")]
    public string TanggalMulai { get; set; } = string.Empty;

    [JsonPropertyName("tanggal_selesai")]
    public string TanggalSelesai { get; set; } = string.Empty;

    [JsonPropertyName("alasan")]
    public string Alasan { get; set; } = string.Empty;
}

public sealed class PersetujuanPermohonanRequest
{
    /// <summary>"approve" atau "reject".</summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = "approve";

    [JsonPropertyName("id_permohonan")]
    public int IdPermohonan { get; set; }

    [JsonPropertyName("catatan")]
    public string? Catatan { get; set; }
}

public sealed class PermohonanDto
{
    [JsonPropertyName("id_permohonan")]
    public int IdPermohonan { get; set; }

    [JsonPropertyName("id_sales")]
    public int IdSales { get; set; }

    [JsonPropertyName("nomor_sales")]
    public string? NomorSales { get; set; }

    [JsonPropertyName("nama_sales")]
    public string? NamaSales { get; set; }

    [JsonPropertyName("jenis")]
    public string Jenis { get; set; } = string.Empty;

    [JsonPropertyName("tanggal_mulai")]
    public string TanggalMulai { get; set; } = string.Empty;

    [JsonPropertyName("tanggal_selesai")]
    public string TanggalSelesai { get; set; } = string.Empty;

    [JsonPropertyName("alasan")]
    public string? Alasan { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("id_hr_approver")]
    public int? IdHrApprover { get; set; }

    [JsonPropertyName("catatan")]
    public string? Catatan { get; set; }

    [JsonPropertyName("catatan_hr")]
    public string? CatatanHr { get; set; }

    [JsonPropertyName("lampiran")]
    public string? Lampiran { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
}

/* ============================================================
 * laporan.php (HR)
 * ============================================================ */
public sealed class RingkasanKunjungan
{
    [JsonPropertyName("total_kunjungan")]
    public int TotalKunjungan { get; set; }

    [JsonPropertyName("valid")]
    public int Valid { get; set; }

    [JsonPropertyName("pending")]
    public int Pending { get; set; }

    [JsonPropertyName("invalid")]
    public int Invalid { get; set; }
}

public sealed class LaporanKunjunganData
{
    [JsonPropertyName("periode")]
    public string Periode { get; set; } = string.Empty;

    [JsonPropertyName("ringkasan")]
    public RingkasanKunjungan Ringkasan { get; set; } = new();

    [JsonPropertyName("items")]
    public List<KunjunganDto> Items { get; set; } = new();
}

public sealed class RingkasanPermohonan
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("pending")]
    public int Pending { get; set; }

    [JsonPropertyName("approved")]
    public int Approved { get; set; }

    [JsonPropertyName("rejected")]
    public int Rejected { get; set; }
}

public sealed class LaporanPermohonanData
{
    [JsonPropertyName("periode")]
    public string Periode { get; set; } = string.Empty;

    [JsonPropertyName("ringkasan")]
    public RingkasanPermohonan Ringkasan { get; set; } = new();

    [JsonPropertyName("items")]
    public List<PermohonanDto> Items { get; set; } = new();
}

public sealed class RekapSalesDto
{
    [JsonPropertyName("id_sales")]
    public int IdSales { get; set; }

    [JsonPropertyName("nomor_sales")]
    public string? NomorSales { get; set; }

    [JsonPropertyName("nama_sales")]
    public string? NamaSales { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("kunjungan_valid")]
    public int KunjunganValid { get; set; }

    [JsonPropertyName("total_kunjungan")]
    public int TotalKunjungan { get; set; }

    [JsonPropertyName("permohonan_pending")]
    public int PermohonanPending { get; set; }
}

public sealed class RekapSalesData
{
    [JsonPropertyName("periode")]
    public string Periode { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<RekapSalesDto> Items { get; set; } = new();

    [JsonPropertyName("ringkasan")]
    public JsonElement? Ringkasan { get; set; }
}

/* ============================================================
 * sales.php (khusus HR)
 * ============================================================ */
public sealed class SalesAdminDto
{
    [JsonPropertyName("id_sales")]
    public int IdSales { get; set; }

    [JsonPropertyName("id_pengguna")]
    public int IdPengguna { get; set; }

    [JsonPropertyName("nama")]
    public string Nama { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("nomor_sales")]
    public string NomorSales { get; set; } = string.Empty;

    [JsonPropertyName("nomor_telepon")]
    public string NomorTelepon { get; set; } = string.Empty;

    [JsonPropertyName("wilayah")]
    public string? Wilayah { get; set; }

    [JsonPropertyName("face_data")]
    public string? FaceData { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
}

public sealed class SalesCreateRequest
{
    [JsonPropertyName("nama")]
    public string Nama { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("nomor_sales")]
    public string NomorSales { get; set; } = string.Empty;

    [JsonPropertyName("nomor_telepon")]
    public string NomorTelepon { get; set; } = string.Empty;

    [JsonPropertyName("wilayah")]
    public string? Wilayah { get; set; }

    [JsonPropertyName("face_data")]
    public string? FaceData { get; set; }
}

/* ============================================================
 * toko.php (POST khusus HR)
 * ============================================================ */
public sealed class TokoRequest
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = "create";

    [JsonPropertyName("id_toko")]
    public int? IdToko { get; set; }

    [JsonPropertyName("kode_toko")]
    public string? KodeToko { get; set; }

    [JsonPropertyName("nama_toko")]
    public string NamaToko { get; set; } = string.Empty;

    [JsonPropertyName("alamat")]
    public string? Alamat { get; set; }

    [JsonPropertyName("pemilik")]
    public string? Pemilik { get; set; }

    [JsonPropertyName("telepon")]
    public string? Telepon { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("radius")]
    public double? Radius { get; set; }
}

/* ============================================================
 * penggajian.php (khusus HR)
 * ============================================================ */
public sealed class PengaturanGajiDto
{
    [JsonPropertyName("id_pengaturan")]
    public int IdPengaturan { get; set; }

    [JsonPropertyName("gaji_pokok")]
    public decimal GajiPokok { get; set; }

    [JsonPropertyName("insentif_per_kunjungan")]
    public decimal InsentifPerKunjungan { get; set; }

    [JsonPropertyName("periode_berlaku")]
    public string PeriodeBerlaku { get; set; } = string.Empty;
}

public sealed class PayrollItemDto
{
    [JsonPropertyName("id_sales")]
    public int IdSales { get; set; }

    [JsonPropertyName("id_gaji")]
    public int? IdGaji { get; set; }

    [JsonPropertyName("nama_sales")]
    public string NamaSales { get; set; } = string.Empty;

    [JsonPropertyName("nomor_sales")]
    public string NomorSales { get; set; } = string.Empty;

    [JsonPropertyName("hari_kerja")]
    public int HariKerja { get; set; }

    [JsonPropertyName("kunjungan_valid")]
    public int KunjunganValid { get; set; }

    [JsonPropertyName("jumlah_izin")]
    public int JumlahIzin { get; set; }

    [JsonPropertyName("gaji_pokok")]
    public decimal GajiPokok { get; set; }

    [JsonPropertyName("insentif_per_kunjungan")]
    public decimal InsentifPerKunjungan { get; set; }

    [JsonPropertyName("total_insentif")]
    public decimal TotalInsentif { get; set; }

    [JsonPropertyName("total_potongan")]
    public decimal TotalPotongan { get; set; }

    [JsonPropertyName("total_gaji")]
    public decimal TotalGaji { get; set; }

    [JsonPropertyName("status_target")]
    public string StatusTarget { get; set; } = string.Empty;

    [JsonPropertyName("sudah_diproses")]
    public bool SudahDiproses { get; set; }

    [JsonPropertyName("tanggal_proses")]
    public string? TanggalProses { get; set; }
}

public sealed class PenggajianData
{
    [JsonPropertyName("periode")]
    public string Periode { get; set; } = string.Empty;

    [JsonPropertyName("pengaturan")]
    public PengaturanGajiDto? Pengaturan { get; set; }

    [JsonPropertyName("hari_kerja_bulanan")]
    public int HariKerjaBulanan { get; set; } = 22;

    [JsonPropertyName("target_kunjungan")]
    public int TargetKunjungan { get; set; } = 176;

    [JsonPropertyName("items")]
    public List<PayrollItemDto> Items { get; set; } = new();
}

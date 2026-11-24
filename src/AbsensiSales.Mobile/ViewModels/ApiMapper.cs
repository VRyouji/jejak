using System.Globalization;
using AbsensiSales.Core.Api;
using AbsensiSales.Core.Models;

namespace AbsensiSales.Mobile.ViewModels;

/// <summary>
/// Pemetaan respons API ke model tampilan aplikasi Mobile.
/// Semua layar memakai helper ini supaya format tanggal/status konsisten.
/// </summary>
internal static class ApiMapper
{
    private static readonly CultureInfo Indonesia = CultureInfo.GetCultureInfo("id-ID");

    public static Sales KeSales(LoginData login, PenggunaDto pengguna, SalesDto sales) => new()
    {
        Id = sales.IdSales,
        Nip = sales.NomorSales,
        Nama = pengguna.Nama,
        Username = pengguna.Email,
        Wilayah = pengguna.Wilayah ?? string.Empty,
        FaceData = sales.FaceData ?? string.Empty
    };

    public static Toko KeToko(TokoDto toko) => new()
    {
        Id = toko.IdToko,
        KodeToko = string.IsNullOrWhiteSpace(toko.KodeToko)
            ? $"TK-{toko.IdToko}"
            : toko.KodeToko!,
        NamaToko = toko.NamaToko,
        Alamat = toko.Alamat ?? string.Empty,
        Pemilik = toko.Pemilik ?? string.Empty,
        Telepon = toko.Telepon ?? string.Empty,
        Latitude = toko.Latitude ?? 0,
        Longitude = toko.Longitude ?? 0,
        RadiusMeter = toko.RadiusMeter
    };

    public static DateTime? Waktu(string? nilai)
    {
        if (string.IsNullOrWhiteSpace(nilai))
        {
            return null;
        }

        return DateTime.TryParse(
            nilai,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var waktu)
            ? waktu
            : null;
    }

    /// <summary>Jam 24 jam, mis. "08.15".</summary>
    public static string Jam(string? nilai) =>
        Waktu(nilai)?.ToString("HH.mm", Indonesia) ?? "-";

    /// <summary>Tanggal + jam, mis. "19 Nov 2026 08.15".</summary>
    public static string TanggalJam(string? nilai) =>
        Waktu(nilai)?.ToString("dd MMM yyyy HH.mm", Indonesia) ?? "-";

    public static string Tanggal(DateTime nilai) =>
        nilai.ToString("dd MMMM yyyy", Indonesia);

    public static string TanggalApi(string? nilai)
    {
        if (string.IsNullOrWhiteSpace(nilai))
        {
            return "-";
        }

        return DateTime.TryParse(nilai, out var tanggal)
            ? tanggal.ToString("dd MMM yyyy", Indonesia)
            : nilai;
    }

    public static string StatusKunjungan(string? status) => status?.Trim() switch
    {
        "Valid" => "Tersimpan",
        "Invalid" => "Gagal validasi",
        "Pending" => "Menunggu",
        null or "" => "-",
        _ => status!
    };

    public static string StatusPermohonan(string? status) => status?.Trim() switch
    {
        "Approved" or "Disetujui" => "Disetujui",
        "Rejected" or "Ditolak" => "Ditolak",
        _ => "Pending"
    };

    public static string AtauDash(string? nilai) =>
        string.IsNullOrWhiteSpace(nilai) ? "-" : nilai.Trim();
}

using System.Globalization;

namespace AbsensiSales.HR.ViewModels;

/// <summary>
/// Pemetaan kecil dari bentuk data API (string tanggal, status Inggris)
/// ke teks yang dipakai di layar HR.
/// </summary>
internal static class ApiMapper
{
    private static readonly CultureInfo Indonesia = CultureInfo.GetCultureInfo("id-ID");

    public static string Inisial(string? nama)
    {
        if (string.IsNullOrWhiteSpace(nama))
        {
            return "?";
        }

        return string.Concat(nama
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0])));
    }

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

    /// <summary>Jam format 24 jam, mis. "14.35".</summary>
    public static string Jam(string? nilai) =>
        Waktu(nilai)?.ToString("HH.mm", Indonesia) ?? "-";

    /// <summary>Tanggal + jam pengajuan, mis. "12 Nov 2026 09.20".</summary>
    public static string TanggalJam(string? nilai) =>
        Waktu(nilai)?.ToString("dd MMM yyyy HH.mm", Indonesia) ?? "-";

    public static string TanggalPanjang(DateTime nilai) =>
        nilai.ToString("dd MMMM yyyy", Indonesia);

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
        "Approved" or "Disetujui" => "Approved",
        "Rejected" or "Ditolak" => "Rejected",
        _ => "Pending"
    };

    public static string AtauDash(string? nilai) =>
        string.IsNullOrWhiteSpace(nilai) ? "-" : nilai.Trim();
}

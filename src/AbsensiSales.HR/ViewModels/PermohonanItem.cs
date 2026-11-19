using System.Globalization;
using AbsensiSales.Core.Api;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AbsensiSales.HR.ViewModels;

/// <summary>
/// Item tampilan untuk daftar perizinan/cuti. Status dan catatan HR dibuat
/// observable agar kartu langsung berubah setelah di-approve/di-reject.
/// </summary>
public partial class PermohonanItem : ObservableObject
{
    public required int Id { get; init; }
    public int SalesId { get; init; }
    public required string NamaSales { get; init; }
    public string Jabatan { get; init; } = "Sales Lapangan";
    public required string Nip { get; init; }
    public required string Jenis { get; init; }
    public required string Periode { get; init; }
    public required string Alasan { get; init; }
    public required string Lampiran { get; init; }
    public required string Diajukan { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Pending))]
    [NotifyPropertyChangedFor(nameof(Approved))]
    [NotifyPropertyChangedFor(nameof(Rejected))]
    [NotifyPropertyChangedFor(nameof(Selesai))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private string _status = "Pending";

    [ObservableProperty]
    private string _catatanHr = string.Empty;

    public bool Pending => Status == "Pending";
    public bool Approved => Status is "Approved" or "Disetujui";
    public bool Rejected => Status is "Rejected" or "Ditolak";
    public bool Selesai => !Pending;

    public string StatusText => Status switch
    {
        "Approved" or "Disetujui" => "Approved",
        "Rejected" or "Ditolak" => "Rejected",
        _ => "Pending"
    };

    public string Inisial => string.IsNullOrWhiteSpace(NamaSales)
        ? "?"
        : string.Concat(NamaSales
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0])));

    /// <summary>Memetakan response permohonan.php ke model tampilan perizinan.</summary>
    public static PermohonanItem DariApi(PermohonanDto permohonan) => new()
    {
        Id = permohonan.IdPermohonan,
        SalesId = permohonan.IdSales,
        NamaSales = string.IsNullOrWhiteSpace(permohonan.NamaSales)
            ? "Karyawan"
            : permohonan.NamaSales!,
        Jabatan = "Sales Lapangan",
        Nip = string.IsNullOrWhiteSpace(permohonan.NomorSales)
            ? "-"
            : permohonan.NomorSales!,
        Jenis = permohonan.Jenis,
        Periode = FormatRentang(permohonan.TanggalMulai, permohonan.TanggalSelesai),
        Alasan = string.IsNullOrWhiteSpace(permohonan.Alasan)
            ? "-"
            : permohonan.Alasan!,
        Lampiran = string.IsNullOrWhiteSpace(permohonan.Lampiran)
            ? "Tanpa lampiran"
            : permohonan.Lampiran!,
        Diajukan = ApiMapper.TanggalJam(permohonan.CreatedAt),
        Status = ApiMapper.StatusPermohonan(permohonan.Status),
        CatatanHr = string.IsNullOrWhiteSpace(permohonan.CatatanHr)
            ? "-"
            : permohonan.CatatanHr!
    };

    public static string FormatRentang(string mulai, string selesai)
    {
        var teksMulai = FormatTanggal(mulai);
        var teksSelesai = FormatTanggal(selesai);

        return string.Equals(teksMulai, teksSelesai, StringComparison.OrdinalIgnoreCase)
            ? teksMulai
            : $"{teksMulai} - {teksSelesai}";
    }

    private static string FormatTanggal(string nilai) =>
        DateTime.TryParse(nilai, out var tanggal)
            ? tanggal.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("id-ID"))
            : nilai;
}

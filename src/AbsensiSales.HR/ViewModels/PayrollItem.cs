using System.Globalization;

namespace AbsensiSales.HR.ViewModels;

public partial class PayrollItem
{
    public required string NamaSales { get; init; }
    public required string KodeSales { get; init; }
    public required int HariKerja { get; init; }
    public required int KunjunganToko { get; init; }
    public required int JumlahIzin { get; init; }
    public required string StatusTarget { get; init; }
    public required decimal TotalGaji { get; init; }

    public required decimal GajiPokok { get; init; }
    public required decimal InsentifPerKunjungan { get; init; }
    public required decimal TotalInsentif { get; init; }
    public required decimal TotalPotongan { get; init; }

    public string TotalGajiText =>
        $"Rp {TotalGaji.ToString("N0", CultureInfo.InvariantCulture)}";
}

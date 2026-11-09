using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AbsensiSales.HR.Services;
using Microsoft.Maui.ApplicationModel;

namespace AbsensiSales.HR.ViewModels;

public partial class PayrollViewModel : ObservableObject
{
    private const decimal GajiPokok = 2_500_000m;
    private const decimal BonusPerKunjungan = 15_000m;
    private const int HariKerjaBulanan = 22;
    private const int TargetKunjunganBulanan = 176;

    private readonly PdfService _pdfService;

    public PayrollViewModel(PdfService pdfService)
    {
        _pdfService = pdfService;
    }

    public ObservableCollection<PayrollItem> DaftarGaji { get; } = new();

    [ObservableProperty]
    private string _periode = "November 2026";

    [ObservableProperty]
    private string _totalHariKerja = "22 hari";

    [ObservableProperty]
    private string _totalKunjunganTarget = "176 kunjungan";

    [ObservableProperty]
    private string _rekapSiap = "4 kunjungan tercatat - siap ditinjau";

    public Task MuatAsync()
    {
        DaftarGaji.Clear();

        foreach (var item in BuatDataMock())
            DaftarGaji.Add(item);

        Periode = "November 2026";
        TotalHariKerja = $"{HariKerjaBulanan} hari";
        TotalKunjunganTarget = $"{TargetKunjunganBulanan} kunjungan";

        var totalKunjungan = DaftarGaji.Sum(item => item.KunjunganToko);
        RekapSiap = $"{totalKunjungan} kunjungan tercatat - siap ditinjau";

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task HitungUlangAsync()
    {
        await MuatAsync();
        await Shell.Current.DisplayAlert(
            "Perhitungan selesai",
            "Rekapitulasi gaji berhasil dihitung ulang dari data absensi & izin.",
            "OK");
    }

    [RelayCommand]
    private async Task SetujuiRekapAsync() =>
        await Shell.Current.DisplayAlert(
            "Rekap disetujui",
            "Rekapitulasi gaji disetujui dan siap dicetak.",
            "OK");

    [RelayCommand]
    private async Task CetakSlipAsync(PayrollItem? item)
    {
        try
        {
            var filePath = item is null
                ? await _pdfService.BuatRekapGajiAsync(DaftarGaji, Periode)
                : await _pdfService.BuatSlipGajiAsync(item, Periode);

            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(filePath),
                Title = item is null ? "Rekap Gaji" : $"Slip Gaji {item.NamaSales}"
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(
                "Gagal mencetak slip",
                $"File PDF tidak dapat dibuat atau dibuka.\n\n{ex.Message}",
                "OK");
        }
    }

    private static IEnumerable<PayrollItem> BuatDataMock() => new List<PayrollItem>
    {
        BuatItem("Andi Pratama", "SLS-014", kunjungan: 2, izin: 2),
        BuatItem("Ayu Ningsih", "SLS-022", kunjungan: 1, izin: 1),
        BuatItem("Budi Raharjo", "SLS-011", kunjungan: 1, izin: 1),
        BuatItem("Citra Dewi", "SLS-015", kunjungan: 0, izin: 0)
    };

    private static PayrollItem BuatItem(
        string nama,
        string kode,
        int kunjungan,
        int izin)
    {
        decimal bonus = kunjungan * BonusPerKunjungan;
        decimal potonganIzin = izin * (GajiPokok / HariKerjaBulanan);
        decimal totalGaji = Math.Max(0, GajiPokok + bonus - potonganIzin);

        return new PayrollItem
        {
            NamaSales = nama,
            KodeSales = kode,
            HariKerja = HariKerjaBulanan - izin,
            KunjunganToko = kunjungan,
            JumlahIzin = izin,
            StatusTarget = kunjungan >= TargetKunjunganBulanan
                ? "Target tercapai"
                : "Belum tercapai",
            TotalGaji = totalGaji
        };
    }
}

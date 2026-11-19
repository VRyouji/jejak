using System.Collections.ObjectModel;
using System.Globalization;
using AbsensiSales.Core.Api;
using AbsensiSales.HR.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;

namespace AbsensiSales.HR.ViewModels;

public partial class PayrollViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly PdfService _pdfService;

    public PayrollViewModel(ApiService api, PdfService pdfService)
    {
        _api = api;
        _pdfService = pdfService;
    }

    public ObservableCollection<PayrollItem> DaftarGaji { get; } = new();

    [ObservableProperty]
    private string _periode = string.Empty;

    [ObservableProperty]
    private string _totalHariKerja = "-";

    [ObservableProperty]
    private string _totalKunjunganTarget = "-";

    [ObservableProperty]
    private string _rekapSiap = "Memuat data...";

    [ObservableProperty]
    private string _pesanError = string.Empty;

    [ObservableProperty]
    private bool _adaError;

    private string PeriodeApi { get; set; } = DateTime.Now.ToString("yyyy-MM");

    public async Task MuatAsync()
    {
        var sekarang = DateTime.Now;
        PeriodeApi = sekarang.ToString("yyyy-MM");
        Periode = sekarang.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("id-ID"));

        var hasil = await _api.GetPenggajianAsync(PeriodeApi);
        if (!hasil.Sukses || hasil.Data is null)
        {
            DaftarGaji.Clear();
            PesanError = hasil.Pesan;
            AdaError = true;
            RekapSiap = "Rekap gaji belum dapat dimuat.";
            TotalHariKerja = "-";
            TotalKunjunganTarget = "-";
            return;
        }

        var data = hasil.Data;
        PesanError = string.Empty;
        AdaError = false;

        TotalHariKerja = $"{data.HariKerjaBulanan} hari";
        TotalKunjunganTarget = $"{data.TargetKunjungan} kunjungan";

        DaftarGaji.Clear();
        foreach (var item in data.Items)
        {
            DaftarGaji.Add(new PayrollItem
            {
                NamaSales = item.NamaSales,
                KodeSales = item.NomorSales,
                HariKerja = item.HariKerja,
                KunjunganToko = item.KunjunganValid,
                JumlahIzin = item.JumlahIzin,
                StatusTarget = item.StatusTarget,
                TotalGaji = item.TotalGaji,
                GajiPokok = item.GajiPokok,
                InsentifPerKunjungan = item.InsentifPerKunjungan,
                TotalInsentif = item.TotalInsentif,
                TotalPotongan = item.TotalPotongan
            });
        }

        var totalKunjungan = DaftarGaji.Sum(item => item.KunjunganToko);
        RekapSiap = $"{totalKunjungan} kunjungan valid tercatat - siap ditinjau";
    }

    [RelayCommand]
    private async Task HitungUlangAsync()
    {
        await MuatAsync();

        await Shell.Current.DisplayAlert(
            AdaError ? "Gagal menghitung" : "Perhitungan selesai",
            AdaError
                ? PesanError
                : "Rekapitulasi gaji dihitung dari kunjungan valid dan izin yang disetujui.",
            "OK");
    }

    [RelayCommand]
    private async Task SetujuiRekapAsync()
    {
        if (DaftarGaji.Count == 0)
        {
            await Shell.Current.DisplayAlert(
                "Belum ada data",
                "Tidak ada sales yang dapat diproses pada periode ini.",
                "OK");
            return;
        }

        var hasil = await _api.ProsesPenggajianAsync(PeriodeApi);
        if (!hasil.Sukses)
        {
            await Shell.Current.DisplayAlert("Gagal menyimpan", hasil.Pesan, "OK");
            return;
        }

        await MuatAsync();

        await Shell.Current.DisplayAlert(
            "Rekap disimpan",
            $"Rekapitulasi gaji periode {Periode} berhasil disimpan ke database.",
            "OK");
    }

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
}

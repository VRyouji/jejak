using System.Collections.ObjectModel;
using System.Globalization;
using AbsensiSales.Core.Models;
using AbsensiSales.Core.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.HR.ViewModels;

/// <summary>
/// Ringkasan utama HR: jumlah sales aktif, kehadiran hari ini, dan
/// pengajuan perizinan yang masih menunggu persetujuan.
/// </summary>
public partial class HrDashboardViewModel : ObservableObject
{
    private readonly SalesRepository _salesRepo;
    private readonly KunjunganRepository _kunjunganRepo;
    private readonly PermohonanRepository _permohonanRepo;
    private readonly Services.HrNavigationService _nav;

    public HrDashboardViewModel(
        SalesRepository salesRepo,
        KunjunganRepository kunjunganRepo,
        PermohonanRepository permohonanRepo,
        Services.HrNavigationService nav)
    {
        _salesRepo = salesRepo;
        _kunjunganRepo = kunjunganRepo;
        _permohonanRepo = permohonanRepo;
        _nav = nav;
    }

    public ObservableCollection<PermohonanItem> PengajuanTerbaru { get; } = new();

    [ObservableProperty]
    private string _totalSalesActive = "0";

    [ObservableProperty]
    private string _karyawanHadir = "0";

    [ObservableProperty]
    private string _persenHadir = "0% dari total";

    [ObservableProperty]
    private string _pengajuanPending = "0";

    [ObservableProperty]
    private string _updatePada = string.Empty;

    [ObservableProperty]
    private string _ringkasanTindakLanjut = string.Empty;

    [ObservableProperty]
    private bool _tidakAdaPengajuan = true;

    public async Task MuatAsync()
    {
        var hariIni = DateTime.Now.ToString("yyyy-MM-dd");
        var semuaSales = await _salesRepo.GetAllAsync();
        var semuaKunjungan = await _kunjunganRepo.GetAllAsync(hariIni);
        var semuaPending = await _permohonanRepo.GetPendingAsync();

        var salesHadir = semuaSales
            .Where(sales => semuaKunjungan.Any(kunjungan =>
                kunjungan.SalesId == sales.Id
                && kunjungan.Status == nameof(StatusKunjungan.Tersimpan)))
            .ToList();

        TotalSalesActive = semuaSales.Count.ToString();
        KaryawanHadir = salesHadir.Count.ToString();
        PersenHadir = $"{(semuaSales.Count > 0 ? salesHadir.Count * 100 / semuaSales.Count : 0)}% dari total";
        PengajuanPending = semuaPending.Count.ToString();

        var pengajuanTerbaru = new List<PermohonanItem>();
        foreach (var permohonan in semuaPending.Take(4))
        {
            var sales = await _salesRepo.GetByIdAsync(permohonan.SalesId);
            pengajuanTerbaru.Add(PermohonanItem.DariData(permohonan, sales));
        }

        PengajuanTerbaru.Clear();
        foreach (var item in pengajuanTerbaru)
            PengajuanTerbaru.Add(item);

        TidakAdaPengajuan = pengajuanTerbaru.Count == 0;

        var belumHadir = semuaSales.Count - salesHadir.Count;
        RingkasanTindakLanjut =
            $"{belumHadir} sales belum mencatat kunjungan hari ini, "
            + $"{semuaPending.Count} pengajuan menunggu persetujuan.";

        UpdatePada = DateTime.Now.ToString(
            "dd MMM yyyy HH.mm", CultureInfo.GetCultureInfo("id-ID")) + " WIB";
    }

    [RelayCommand]
    private void BukaPerizinan() => _nav.NavigateTo(typeof(Views.HrPerizinanPage));

    [RelayCommand]
    private void BukaMonitoring() => _nav.NavigateTo(typeof(Views.MonitoringPage));

    [RelayCommand]
    private async Task SegarkanAsync() => await MuatAsync();
}

using System.Collections.ObjectModel;
using System.Globalization;
using AbsensiSales.Core.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.HR.ViewModels;

/// <summary>
/// Ringkasan utama HR: jumlah sales aktif, kehadiran hari ini, dan
/// pengajuan perizinan yang masih menunggu persetujuan.
/// Semua data diambil dari backend API.
/// </summary>
public partial class HrDashboardViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly Services.HrNavigationService _nav;

    public HrDashboardViewModel(ApiService api, Services.HrNavigationService nav)
    {
        _api = api;
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

    [ObservableProperty]
    private string _pesanError = string.Empty;

    [ObservableProperty]
    private bool _adaError;

    public async Task MuatAsync()
    {
        var hariIni = DateTime.Now.ToString("yyyy-MM-dd");

        var hasilSales = await _api.GetSalesAdminAsync();
        var hasilKunjungan = await _api.GetKunjunganAsync(tanggal: hariIni);
        var hasilPending = await _api.GetPermohonanAsync("Pending");

        var daftarSales = hasilSales.Data ?? new List<SalesAdminDto>();
        var daftarKunjungan = hasilKunjungan.Data ?? new List<KunjunganDto>();
        var daftarPending = hasilPending.Data ?? new List<PermohonanDto>();

        var pesan = new List<string>();
        if (!hasilSales.Sukses) pesan.Add(hasilSales.Pesan);
        if (!hasilKunjungan.Sukses) pesan.Add(hasilKunjungan.Pesan);
        if (!hasilPending.Sukses) pesan.Add(hasilPending.Pesan);
        PesanError = string.Join(" ", pesan.Distinct());
        AdaError = PesanError.Length > 0;

        int jumlahHadir = daftarKunjungan
            .Select(kunjungan => kunjungan.IdSales)
            .Distinct()
            .Count();

        TotalSalesActive = daftarSales.Count.ToString();
        KaryawanHadir = jumlahHadir.ToString();
        PersenHadir = $"{(daftarSales.Count > 0 ? jumlahHadir * 100 / daftarSales.Count : 0)}% dari total";
        PengajuanPending = daftarPending.Count.ToString();

        PengajuanTerbaru.Clear();
        foreach (var permohonan in daftarPending.Take(4))
        {
            PengajuanTerbaru.Add(PermohonanItem.DariApi(permohonan));
        }

        TidakAdaPengajuan = PengajuanTerbaru.Count == 0;

        int belumHadir = Math.Max(0, daftarSales.Count - jumlahHadir);
        RingkasanTindakLanjut =
            $"{belumHadir} sales belum mencatat kunjungan hari ini, "
            + $"{daftarPending.Count} pengajuan menunggu persetujuan.";

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

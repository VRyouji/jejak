using System.Collections.ObjectModel;
using AbsensiSales.Core.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.HR.ViewModels;

public partial class MonitoringViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly Services.HrNavigationService _nav;

    public MonitoringViewModel(ApiService api, Services.HrNavigationService nav)
    {
        _api = api;
        _nav = nav;
    }

    public ObservableCollection<SalesMonitorItem> DaftarSales { get; } = new();

    [ObservableProperty]
    private string _totalSales = "0";

    [ObservableProperty]
    private string _jumlahHadir = "0";

    [ObservableProperty]
    private string _persenHadir = "0% dari total";

    [ObservableProperty]
    private string _jumlahBelumAbsen = "0";

    [ObservableProperty]
    private string _kunjunganTercatat = "0";

    [ObservableProperty]
    private string _updatePada = string.Empty;

    [ObservableProperty]
    private string _ringkasanTindakLanjut = string.Empty;

    [ObservableProperty]
    private string _pesanError = string.Empty;

    [ObservableProperty]
    private bool _adaError;

    public async Task MuatAsync()
    {
        var tanggal = DateTime.Now.ToString("yyyy-MM-dd");
        var hasilSales = await _api.GetSalesAdminAsync();
        var hasilKunjungan = await _api.GetKunjunganAsync(tanggal: tanggal);

        var daftarSales = hasilSales.Data ?? new List<SalesAdminDto>();
        var daftarKunjungan = hasilKunjungan.Data ?? new List<KunjunganDto>();

        var pesan = new List<string>();
        if (!hasilSales.Sukses) pesan.Add(hasilSales.Pesan);
        if (!hasilKunjungan.Sukses) pesan.Add(hasilKunjungan.Pesan);
        PesanError = string.Join(" ", pesan.Distinct());
        AdaError = PesanError.Length > 0;

        int hadir = 0;
        DaftarSales.Clear();

        foreach (var sales in daftarSales)
        {
            var kunjunganSales = daftarKunjungan
                .Where(kunjungan => kunjungan.IdSales == sales.IdSales)
                .OrderByDescending(kunjungan => ApiMapper.Waktu(kunjungan.WaktuCheckin))
                .ToList();

            bool hadirHariIni = kunjunganSales.Count > 0;
            if (hadirHariIni)
            {
                hadir++;
            }

            var terakhir = kunjunganSales.FirstOrDefault();

            DaftarSales.Add(new SalesMonitorItem
            {
                Inisial = ApiMapper.Inisial(sales.Nama),
                Nama = sales.Nama,
                Nip = string.IsNullOrWhiteSpace(sales.NomorSales) ? "-" : sales.NomorSales,
                Wilayah = string.IsNullOrWhiteSpace(sales.Wilayah) ? "-" : sales.Wilayah!,
                Status = hadirHariIni ? "Hadir" : "Belum absen",
                Waktu = terakhir is null ? "-" : ApiMapper.Jam(terakhir.WaktuCheckin),
                SalesId = sales.IdSales
            });
        }

        int jumlahBelumAbsen = Math.Max(0, daftarSales.Count - hadir);

        TotalSales = daftarSales.Count.ToString();
        JumlahHadir = hadir.ToString();
        PersenHadir = $"{(daftarSales.Count > 0 ? hadir * 100 / daftarSales.Count : 0)}% dari total";
        JumlahBelumAbsen = jumlahBelumAbsen.ToString();
        KunjunganTercatat = daftarKunjungan.Count.ToString();
        UpdatePada = DateTime.Now.ToString("HH.mm") + " WIB";

        int valid = daftarKunjungan.Count(k => k.StatusVerifikasi == "Valid");
        int pending = daftarKunjungan.Count(k => k.StatusVerifikasi == "Pending");
        int invalid = daftarKunjungan.Count(k => k.StatusVerifikasi == "Invalid");

        RingkasanTindakLanjut =
            $"Pantau {jumlahBelumAbsen} sales belum absen, "
            + $"{valid} kunjungan valid, {pending} menunggu verifikasi"
            + (invalid > 0 ? $", {invalid} gagal validasi." : ".");
    }

    [RelayCommand]
    private void BukaLaporan() =>
        _nav.NavigateTo(typeof(Views.LaporanHrPage));

    [RelayCommand]
    private async Task KirimPengingatAsync()
    {
        var belumAbsen = DaftarSales
            .Where(sales => !sales.Hadir)
            .Select(sales => sales.Nama)
            .ToList();

        if (belumAbsen.Count == 0)
        {
            await Shell.Current.DisplayAlert(
                "Semua sudah absen",
                "Seluruh sales sudah mencatat kunjungan hari ini.",
                "OK");
            return;
        }

        await Shell.Current.DisplayAlert(
            "Sales belum absen",
            string.Join(Environment.NewLine, belumAbsen),
            "OK");
    }
}

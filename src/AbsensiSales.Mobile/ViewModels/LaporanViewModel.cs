using System.Collections.ObjectModel;
using AbsensiSales.Core.Api;
using AbsensiSales.Core.Services;
using AbsensiSales.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.Mobile.ViewModels;

public partial class LaporanViewModel : ObservableObject
{
    private const int TargetKunjunganHarian = 8;

    private readonly SessionService _session;
    private readonly ApiService _api;
    private readonly NotificationService _notif;

    public LaporanViewModel(
        SessionService session,
        ApiService api,
        NotificationService notif)
    {
        _session = session;
        _api = api;
        _notif = notif;
    }

    public ObservableCollection<LaporanItemViewModel> DaftarKunjungan { get; } = new();

    [ObservableProperty]
    private string _judulTanggal = string.Empty;

    [ObservableProperty]
    private string _infoSales = string.Empty;

    [ObservableProperty]
    private int _jumlahKunjungan;

    [ObservableProperty]
    private string _absensiPertama = "-";

    [ObservableProperty]
    private string _statusAbsensiPertama = "Belum tercatat";

    [ObservableProperty]
    private string _statusTargetHarian = string.Empty;

    public async void OnAppearing()
    {
        var sales = _session.SalesAktif;
        if (sales is null)
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        JudulTanggal = ApiMapper.Tanggal(DateTime.Now);
        InfoSales = string.IsNullOrWhiteSpace(sales.Wilayah)
            ? $"{sales.Nama} - {sales.Nip}"
            : $"{sales.Nama} - {sales.Nip} / {sales.Wilayah}";

        var hasil = await _api.GetKunjunganAsync(tanggal: _session.TanggalHariIni);
        if (!hasil.Sukses)
        {
            JumlahKunjungan = 0;
            AbsensiPertama = "-";
            StatusAbsensiPertama = "Belum tercatat";
            StatusTargetHarian = "Data kunjungan belum dapat dimuat.";
            DaftarKunjungan.Clear();
            await _notif.ShowErrorAsync(hasil.Pesan);
            return;
        }

        var kunjungan = (hasil.Data ?? new List<KunjunganDto>())
            .OrderByDescending(k => ApiMapper.Waktu(k.WaktuCheckin))
            .ToList();

        var tercatat = kunjungan
            .Where(k => !string.Equals(k.StatusVerifikasi, "Invalid", StringComparison.OrdinalIgnoreCase))
            .ToList();

        JumlahKunjungan = tercatat.Count;

        var pertama = tercatat
            .OrderBy(k => ApiMapper.Waktu(k.WaktuCheckin))
            .FirstOrDefault();
        AbsensiPertama = pertama is null ? "-" : ApiMapper.Jam(pertama.WaktuCheckin);
        StatusAbsensiPertama = pertama is null ? "Belum tercatat" : "Tercatat - WIB";

        StatusTargetHarian = JumlahKunjungan >= TargetKunjunganHarian
            ? $"Target harian tercapai ({JumlahKunjungan}/{TargetKunjunganHarian})"
            : $"{JumlahKunjungan} toko tercatat";

        DaftarKunjungan.Clear();
        foreach (var k in kunjungan)
        {
            DaftarKunjungan.Add(new LaporanItemViewModel
            {
                NamaToko = string.IsNullOrWhiteSpace(k.NamaToko) ? "Toko" : k.NamaToko!,
                KodeToko = string.IsNullOrWhiteSpace(k.KodeToko)
                    ? $"TK-{k.IdToko}"
                    : k.KodeToko!,
                Waktu = ApiMapper.Jam(k.WaktuCheckin),
                Alamat = k.AlamatToko ?? string.Empty,
                Status = ApiMapper.StatusKunjungan(k.StatusVerifikasi),
                CheckGps = FormatGps(k),
                CheckWajah = (k.SkorWajah ?? 0) > 0
                    ? $"+ Wajah {k.SkorWajah!.Value:F2}"
                    : "- Wajah belum ada",
                CheckFoto = string.IsNullOrWhiteSpace(k.FotoBukti)
                    ? "- Foto belum ada"
                    : "+ Foto lengkap"
            });
        }
    }

    private static string FormatGps(KunjunganDto kunjungan)
    {
        if (string.Equals(kunjungan.StatusVerifikasi, "Invalid", StringComparison.OrdinalIgnoreCase))
        {
            return "- Di luar radius";
        }

        return kunjungan.JarakMeter.HasValue
            ? $"+ GPS {kunjungan.JarakMeter.Value:F0} m"
            : "- GPS tidak tercatat";
    }

    [RelayCommand]
    private async Task AjukanIzinAsync() => await Shell.Current.GoToAsync("pengajuan-izin");

    [RelayCommand]
    private async Task LihatStatusPermohonanAsync() =>
        await Shell.Current.GoToAsync("status-permohonan");
}

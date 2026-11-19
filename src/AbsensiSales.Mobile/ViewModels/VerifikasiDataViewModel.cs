using AbsensiSales.Core.Models;
using AbsensiSales.Core.Repositories;
using AbsensiSales.Core.Services;
using AbsensiSales.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.Mobile.ViewModels;

public partial class VerifikasiDataViewModel : ObservableObject
{
    private readonly SessionService _session;
    private readonly KunjunganRepository _kunjunganRepo;
    private readonly TokoRepository _tokoRepo;
    private readonly AbsensiFlowState _flow;
    private readonly NotificationService _notif;
    private bool _sudahDisimpan;

    public VerifikasiDataViewModel(
        SessionService session,
        KunjunganRepository kunjunganRepo,
        TokoRepository tokoRepo,
        AbsensiFlowState flow,
        NotificationService notif)
    {
        _session = session;
        _kunjunganRepo = kunjunganRepo;
        _tokoRepo = tokoRepo;
        _flow = flow;
        _notif = notif;
    }

    [ObservableProperty]
    private bool _tersimpan;

    [ObservableProperty]
    private string _nomorKunjungan = string.Empty;

    [ObservableProperty]
    private string _waktuKunjungan = string.Empty;

    [ObservableProperty]
    private string _namaToko = string.Empty;

    [ObservableProperty]
    private string _infoToko = string.Empty;

    [ObservableProperty]
    private string _tujuan = string.Empty;

    [ObservableProperty]
    private string _alamat = string.Empty;

    [ObservableProperty]
    private string _infoGps = string.Empty;

    [ObservableProperty]
    private string _infoWaktu = string.Empty;

    [ObservableProperty]
    private string _infoWajah = string.Empty;

    [ObservableProperty]
    private bool _validasiGagal;

    [ObservableProperty]
    private string _pesanGagal = string.Empty;

    public async void OnAppearing()
    {
        if (_sudahDisimpan)
            return;

        var sales = _session.SalesAktif;
        if (sales is null || _flow.TokoTerpilih is null)
        {
            ValidasiGagal = true;
            PesanGagal = "Data kunjungan belum lengkap. Kembali ke Pengisian Data Toko.";
            await _notif.ShowErrorAsync(PesanGagal);
            return;
        }

        // Validasi akhir sebelum menyimpan (mengikuti activity diagram)
        bool gpsOk = _flow.DalamRadius;
        bool wajahOk = _flow.SkorWajah >= 0.363;
        bool dataOk = !string.IsNullOrWhiteSpace(_flow.TujuanKunjungan);
        bool fotoOk = _flow.FotoBukti is not null;

        if (!gpsOk || !wajahOk || !dataOk || !fotoOk)
        {
            ValidasiGagal = true;
            var masalah = new List<string>();
            if (!dataOk) masalah.Add("kode toko/wajib diisi");
            if (!gpsOk) masalah.Add("lokasi di luar radius");
            if (!wajahOk) masalah.Add("wajah belum terverifikasi");
            if (!fotoOk) masalah.Add("foto bukti belum ada");
            PesanGagal = $"Data belum dapat disimpan ({string.Join(", ", masalah)}). "
                + "Kembali ke Pengisian Data Toko, perbaiki data, lalu ulangi verifikasi.";
            await _notif.ShowErrorAsync($"Data belum lengkap: {string.Join(", ", masalah)}.");
            return;
        }

        var kunjungan = new AbsensiKunjungan
        {
            SalesId = sales.Id,
            TokoId = _flow.TokoTerpilih.Id,
            Tanggal = _flow.Tanggal,
            WaktuAbsensiAwal = _flow.WaktuAbsensiAwal,
            WaktuCatat = DateTime.Now.ToString("HH:mm:ss"),
            Latitude = _flow.Latitude,
            Longitude = _flow.Longitude,
            JarakMeter = _flow.JarakMeter,
            SkorWajah = _flow.SkorWajah,
            TujuanKunjungan = _flow.TujuanKunjungan,
            Catatan = _flow.Catatan,
            Status = nameof(StatusKunjungan.Tersimpan)
        };

        _flow.KunjunganId = await _kunjunganRepo.SaveAsync(kunjungan);
        _sudahDisimpan = true;

        ValidasiGagal = false;
        Tersimpan = true;
        NomorKunjungan = $"KJG-{DateTime.Now:ddMMyy}-{sales.Nip.Split('-').Last()}";
        WaktuKunjungan = $"{_flow.Tanggal} - {kunjungan.WaktuCatat} WIB";
        NamaToko = _flow.TokoTerpilih.NamaToko;
        InfoToko = $"{_flow.TokoTerpilih.KodeToko} - {sales.Nama} / {sales.Nip}";
        Tujuan = kunjungan.TujuanKunjungan;
        Alamat = _flow.TokoTerpilih.Alamat;
        InfoGps = $"GPS sesuai - Jarak {_flow.JarakMeter:F0} m\n{_flow.Latitude:F4}, {_flow.Longitude:F4} - +/-8 m";
        InfoWaktu = $"Absensi pertama {_flow.WaktuAbsensiAwal} WIB";
        InfoWajah = $"Wajah cocok - {_flow.WaktuVerifikasiWajah}";

        await _notif.ShowSuccessAsync("Kunjungan berhasil disimpan.");
    }

    [RelayCommand]
    private async Task KembaliKeTokoAsync()
    {
        _flow.Reset();
        await Shell.Current.GoToAsync("//utama/absensi");
    }

    [RelayCommand]
    private async Task LihatLaporanAsync()
    {
        _flow.Reset();
        await Shell.Current.GoToAsync("//utama/laporan");
    }
}

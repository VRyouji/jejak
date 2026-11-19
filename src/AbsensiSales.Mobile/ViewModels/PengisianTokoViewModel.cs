using System.Collections.ObjectModel;
using AbsensiSales.Core.Models;
using AbsensiSales.Core.Repositories;
using AbsensiSales.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.Mobile.ViewModels;

public partial class PengisianTokoViewModel : ObservableObject
{
    private readonly TokoRepository _tokoRepo;
    private readonly GpsService _gps;
    private readonly AbsensiFlowState _flow;
    private readonly NotificationService _notif;
    private bool _sudahNotifAbsensi;

    public PengisianTokoViewModel(
        TokoRepository tokoRepo,
        GpsService gps,
        AbsensiFlowState flow,
        NotificationService notif)
    {
        _tokoRepo = tokoRepo;
        _gps = gps;
        _flow = flow;
        _notif = notif;
    }

    public ObservableCollection<Toko> DaftarToko { get; } = new();

    [ObservableProperty]
    private Toko? _tokoTerpilih;

    [ObservableProperty]
    private string _tujuanKunjungan = "Penawaran & pemesanan produk";

    public string[] PilihanTujuan { get; } =
    {
        "Penawaran & pemesanan produk",
        "Kunjungan rutin",
        "Pengecekan stok",
        "Penagihan"
    };

    [ObservableProperty]
    private string _catatan = string.Empty;

    [ObservableProperty]
    private string _labelRadius = "Dalam radius";

    [ObservableProperty]
    private bool _dalamRadius;

    [ObservableProperty]
    private string _infoJarak = string.Empty;

    [ObservableProperty]
    private string _koordinatGps = string.Empty;

    [ObservableProperty]
    private string _waktuPengisian = string.Empty;

    [ObservableProperty]
    private string _waktuAbsensiAwal = string.Empty;

    [ObservableProperty]
    private bool _sedangMemuat;

    public async void OnAppearing()
    {
        WaktuAbsensiAwal = string.IsNullOrEmpty(_flow.WaktuAbsensiAwal)
            ? DateTime.Now.ToString("HH:mm:ss")
            : _flow.WaktuAbsensiAwal;

        WaktuPengisian = DateTime.Now.ToString("dd MMM yyyy - HH.mm.ss 'WIB'");

        if (!_sudahNotifAbsensi)
        {
            _sudahNotifAbsensi = true;
            await _notif.ShowSuccessAsync($"Absensi pertama tercatat pukul {WaktuAbsensiAwal} WIB.");
        }

        if (DaftarToko.Count == 0)
        {
            SedangMemuat = true;
            var toko = await _tokoRepo.GetAllAsync();
            foreach (var t in toko)
                DaftarToko.Add(t);
            SedangMemuat = false;
        }

        if (TokoTerpilih is null && DaftarToko.Count > 0)
            TokoTerpilih = DaftarToko[0];

        await PerbaruiBuktiAsync();
    }

    partial void OnTokoTerpilihChanged(Toko? value)
    {
        _ = PerbaruiBuktiAsync();
    }

    private async Task PerbaruiBuktiAsync()
    {
        if (TokoTerpilih is null)
        {
            DalamRadius = false;
            LabelRadius = "Belum dipilih";
            InfoJarak = "Pilih toko untuk memeriksa radius.";
            return;
        }

        // Pada mockup Windows, posisi disimulasikan dekat lokasi toko
        var lokasi = await _gps.GetLokasiAsync(TokoTerpilih.Latitude, TokoTerpilih.Longitude);
        if (lokasi is null)
        {
            DalamRadius = false;
            LabelRadius = "GPS tidak siap";
            InfoJarak = "Lokasi perangkat belum tersedia.";
            return;
        }

        double jarak = TokoRepository.HitungJarak(
            lokasi.Latitude, lokasi.Longitude,
            TokoTerpilih.Latitude, TokoTerpilih.Longitude);

        _flow.TokoTerpilih = TokoTerpilih;
        _flow.Latitude = lokasi.Latitude;
        _flow.Longitude = lokasi.Longitude;
        _flow.JarakMeter = Math.Round(jarak);
        _flow.DalamRadius = jarak <= TokoTerpilih.RadiusMeter;

        DalamRadius = _flow.DalamRadius;
        LabelRadius = DalamRadius ? "Dalam radius" : "Di luar radius";
        KoordinatGps = $"{lokasi.Latitude:F4}, {lokasi.Longitude:F4} - +/-{lokasi.AkurasiMeter:F0} m";
        InfoJarak = $"Jarak ke toko {Math.Round(jarak)} meter dari radius {TokoTerpilih.RadiusMeter:F0} meter.";
    }

    [RelayCommand]
    private async Task LanjutVerifikasiAsync()
    {
        if (TokoTerpilih is null)
        {
            await _notif.ShowErrorAsync("Pilih toko yang dikunjungi terlebih dahulu.");
            return;
        }

        if (!DalamRadius)
        {
            await _notif.ShowErrorAsync("Anda di luar radius toko. Absensi hanya diterima di area toko.");
            return;
        }

        _flow.TujuanKunjungan = TujuanKunjungan;
        _flow.Catatan = Catatan;

        await Shell.Current.GoToAsync("verifikasi-wajah");
    }
}

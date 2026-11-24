using System.Collections.ObjectModel;
using AbsensiSales.Core.Api;
using AbsensiSales.Core.Models;
using AbsensiSales.Core.Repositories;
using AbsensiSales.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.Mobile.ViewModels;

public partial class PengisianTokoViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly GpsService _gps;
    private readonly AbsensiFlowState _flow;
    private readonly NotificationService _notif;
    private bool _sudahNotifAbsensi;

    public PengisianTokoViewModel(
        ApiService api,
        GpsService gps,
        AbsensiFlowState flow,
        NotificationService notif)
    {
        _api = api;
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

        await MuatTokoAsync();

        if (TokoTerpilih is null && DaftarToko.Count > 0)
            TokoTerpilih = DaftarToko[0];

        await PerbaruiBuktiAsync();
    }

    private async Task MuatTokoAsync()
    {
        SedangMemuat = true;
        try
        {
            var hasil = await _api.GetTokoAsync();
            if (!hasil.Sukses)
            {
                await _notif.ShowErrorAsync(hasil.Pesan);
                return;
            }

            var idTerpilih = TokoTerpilih?.Id ?? 0;

            DaftarToko.Clear();
            foreach (var toko in hasil.Data ?? new List<TokoDto>())
            {
                DaftarToko.Add(ApiMapper.KeToko(toko));
            }

            if (idTerpilih > 0)
            {
                TokoTerpilih = DaftarToko.FirstOrDefault(t => t.Id == idTerpilih);
            }
        }
        finally
        {
            SedangMemuat = false;
        }
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

        var lokasi = await _gps.GetLokasiAsync();
        if (lokasi is null)
        {
            DalamRadius = false;
            LabelRadius = "GPS tidak siap";
            InfoJarak = "Lokasi perangkat belum tersedia. Aktifkan izin lokasi.";
            KoordinatGps = "-";
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
        KoordinatGps =
            $"{lokasi.Latitude:F4}, {lokasi.Longitude:F4} - +/-{lokasi.AkurasiMeter:F0} m";
        InfoJarak =
            $"Jarak ke toko {Math.Round(jarak)} meter dari radius {TokoTerpilih.RadiusMeter:F0} meter.";
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

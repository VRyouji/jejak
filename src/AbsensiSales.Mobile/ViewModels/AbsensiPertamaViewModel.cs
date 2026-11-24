using System.IO;
using AbsensiSales.Core.Api;
using AbsensiSales.Core.Services;
using AbsensiSales.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace AbsensiSales.Mobile.ViewModels;

public partial class AbsensiPertamaViewModel : ObservableObject
{
    private const int TargetKunjunganHarian = 8;

    private readonly SessionService _session;
    private readonly ApiService _api;
    private readonly GpsService _gps;
    private readonly NotificationService _notif;
    private readonly AbsensiFlowState _flow;

    private double _latitude;
    private double _longitude;
    private double _akurasi;

    public AbsensiPertamaViewModel(
        SessionService session,
        ApiService api,
        GpsService gps,
        NotificationService notif,
        AbsensiFlowState flowState)
    {
        _session = session;
        _api = api;
        _gps = gps;
        _notif = notif;
        _flow = flowState;
    }

    [ObservableProperty]
    private string _namaSales = string.Empty;

    [ObservableProperty]
    private string _inisial = "?";

    [ObservableProperty]
    private string _infoSales = string.Empty;

    [ObservableProperty]
    private string _waktuSekarang = string.Empty;

    [ObservableProperty]
    private string _tanggalManusia = string.Empty;

    [ObservableProperty]
    private string _statusAbsensi = "Belum absen";

    [ObservableProperty]
    private int _kunjunganTercatat;

    [ObservableProperty]
    private string _alamatLokasi = "Lokasi belum tersedia";

    [ObservableProperty]
    private string _koordinatLokasi = "-";

    [ObservableProperty]
    private string _labelLokasi = "Lokasi perangkat";

    public WebViewSource? PetaUrl
    {
        get => _petaUrl;
        private set => SetProperty(ref _petaUrl, value);
    }

    private WebViewSource? _petaUrl;

    [ObservableProperty]
    private bool _gpsSiap;

    [ObservableProperty]
    private bool _kameraSiap = true;

    public double Progress => Math.Min(1, (double)KunjunganTercatat / TargetKunjunganHarian);

    public string LabelProgress => $"{KunjunganTercatat} dari {TargetKunjunganHarian} toko";

    partial void OnKunjunganTercatatChanged(int value)
    {
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(LabelProgress));
    }

    public async void OnAppearing()
    {
        var sales = _session.SalesAktif;
        if (sales is null)
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        NamaSales = sales.Nama;
        Inisial = sales.Inisial;
        InfoSales = string.IsNullOrWhiteSpace(sales.Wilayah)
            ? sales.Nip
            : $"{sales.Nip} - {sales.Wilayah}";
        TanggalManusia = ApiMapper.Tanggal(DateTime.Now);
        WaktuSekarang = DateTime.Now.ToString("HH:mm:ss");

        await MuatKunjunganHariIniAsync();

        // Pastikan flow baru dimulai dengan bersih
        if (_flow.KunjunganId == 0 && _flow.TokoTerpilih is null)
        {
            _flow.WaktuAbsensiAwal = WaktuSekarang;
            _flow.Tanggal = _session.TanggalHariIni;
        }

        var lokasi = await _gps.GetLokasiAsync();
        if (lokasi is null)
        {
            GpsSiap = false;
            AlamatLokasi = "Lokasi perangkat belum tersedia";
            KoordinatLokasi = "Aktifkan izin lokasi untuk melanjutkan absensi.";
            LabelLokasi = "GPS tidak siap";
            MuatPetaDefault();
            return;
        }

        GpsSiap = true;
        AlamatLokasi = "Titik lokasi perangkat saat ini";
        KoordinatLokasi =
            $"{lokasi.Latitude:F4}, {lokasi.Longitude:F4} - Akurasi +/-{lokasi.AkurasiMeter:F0} meter";
        LabelLokasi = $"GPS +/-{lokasi.AkurasiMeter:F0} m";
        PerbaruiPeta(lokasi.Latitude, lokasi.Longitude, lokasi.AkurasiMeter);
    }

    private async void PerbaruiPeta(double lat, double lon, double akurasi)
    {
        _latitude = lat;
        _longitude = lon;
        _akurasi = akurasi;

        try
        {
            var html = await BuatHtmlPetaAsync(lat, lon, akurasi);
            PetaUrl = new HtmlWebViewSource { Html = html };
        }
        catch
        {
            PetaUrl = null;
        }
    }

    private async Task<string> BuatHtmlPetaAsync(double lat, double lon, double akurasi)
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("peta.html");
        using var reader = new StreamReader(stream);
        var html = await reader.ReadToEndAsync();

        var ci = System.Globalization.CultureInfo.InvariantCulture;
        return html
            .Replace("{LAT}", lat.ToString("F4", ci))
            .Replace("{LON}", lon.ToString("F4", ci))
            .Replace("{ZOOM}", "16")
            .Replace("{AKURASI}", akurasi.ToString("F0", ci));
    }

    /// <summary>Menampilkan peta default (Jakarta) saat GPS belum tersedia, agar WebView tidak kosong.</summary>
    private async void MuatPetaDefault()
    {
        try
        {
            // Koordinat pusat Jakarta
            var html = await BuatHtmlPetaAsync(-6.2088, 106.8456, 0);
            PetaUrl = new HtmlWebViewSource { Html = html };
        }
        catch
        {
            PetaUrl = null;
        }
    }

    private async Task MuatKunjunganHariIniAsync()
    {
        var hasil = await _api.GetKunjunganAsync(tanggal: _session.TanggalHariIni);
        if (!hasil.Sukses)
        {
            KunjunganTercatat = 0;
            StatusAbsensi = "Belum absen";
            await _notif.ShowErrorAsync(hasil.Pesan);
            return;
        }

        var daftar = hasil.Data ?? new List<KunjunganDto>();
        var tercatat = daftar
            .Where(k => !string.Equals(k.StatusVerifikasi, "Invalid", StringComparison.OrdinalIgnoreCase))
            .ToList();

        KunjunganTercatat = tercatat.Count;

        var pertama = tercatat
            .OrderBy(k => ApiMapper.Waktu(k.WaktuCheckin))
            .FirstOrDefault();

        StatusAbsensi = pertama is null
            ? "Belum absen"
            : $"Tercatat - pukul {ApiMapper.Jam(pertama.WaktuCheckin)}";
    }

    [RelayCommand]
    private async Task CatatAbsensiAsync()
    {
        var lokasi = await _gps.GetLokasiAsync();
        if (lokasi is null)
        {
            await _notif.ShowErrorAsync(
                "Lokasi perangkat tidak tersedia. Aktifkan GPS dan izin lokasi terlebih dahulu.");
            return;
        }

        GpsSiap = true;
        AlamatLokasi = "Titik lokasi perangkat saat ini";
        KoordinatLokasi =
            $"{lokasi.Latitude:F4}, {lokasi.Longitude:F4} - Akurasi +/-{lokasi.AkurasiMeter:F0} meter";
        LabelLokasi = $"GPS +/-{lokasi.AkurasiMeter:F0} m";
        PerbaruiPeta(lokasi.Latitude, lokasi.Longitude, lokasi.AkurasiMeter);

        _flow.WaktuAbsensiAwal = DateTime.Now.ToString("HH:mm:ss");
        _flow.Tanggal = _session.TanggalHariIni;
        _flow.Latitude = lokasi.Latitude;
        _flow.Longitude = lokasi.Longitude;

        await Shell.Current.GoToAsync("pengisian-toko");
    }
}

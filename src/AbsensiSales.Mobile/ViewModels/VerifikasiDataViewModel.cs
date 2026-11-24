using AbsensiSales.Core.Api;
using AbsensiSales.Core.Services;
using AbsensiSales.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.Mobile.ViewModels;

public partial class VerifikasiDataViewModel : ObservableObject
{
    private readonly SessionService _session;
    private readonly ApiService _api;
    private readonly AbsensiFlowState _flow;
    private readonly NotificationService _notif;
    private bool _sudahDisimpan;

    public VerifikasiDataViewModel(
        SessionService session,
        ApiService api,
        AbsensiFlowState flow,
        NotificationService notif)
    {
        _session = session;
        _api = api;
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

        // Validasi akhir sebelum dikirim ke backend.
        bool gpsOk = _flow.DalamRadius;
        bool wajahOk = _flow.SkorWajah >= FaceScore.Threshold;
        bool dataOk = !string.IsNullOrWhiteSpace(_flow.TujuanKunjungan);
        bool fotoOk = _flow.FotoBukti is { Length: > 0 };

        if (!gpsOk || !wajahOk || !dataOk || !fotoOk)
        {
            ValidasiGagal = true;
            var masalah = new List<string>();
            if (!dataOk) masalah.Add("tujuan kunjungan wajib diisi");
            if (!gpsOk) masalah.Add("lokasi di luar radius");
            if (!wajahOk) masalah.Add("wajah belum terverifikasi");
            if (!fotoOk) masalah.Add("foto bukti belum ada");
            PesanGagal = $"Data belum dapat disimpan ({string.Join(", ", masalah)}). "
                + "Kembali ke Pengisian Data Toko, perbaiki data, lalu ulangi verifikasi.";
            await _notif.ShowErrorAsync($"Data belum lengkap: {string.Join(", ", masalah)}.");
            return;
        }

        // 1. Unggah foto bukti asli ke backend.
        var unggah = await _api.UploadFotoAsync(_flow.FotoBukti!);
        if (!unggah.Sukses || unggah.Data is null)
        {
            ValidasiGagal = true;
            PesanGagal = "Foto bukti gagal diunggah. " + unggah.Pesan;
            await _notif.ShowErrorAsync(PesanGagal);
            return;
        }

        // 2. Simpan check-in kunjungan ke database melalui API.
        var hasil = await _api.CheckInAsync(new CheckInRequest
        {
            IdToko = _flow.TokoTerpilih.Id,
            LatitudeCheckin = _flow.Latitude,
            LongitudeCheckin = _flow.Longitude,
            FotoBukti = unggah.Data.File,
            JarakMeter = _flow.JarakMeter,
            SkorWajah = _flow.SkorWajah,
            TujuanKunjungan = _flow.TujuanKunjungan,
            Catatan = _flow.Catatan
        });

        if (!hasil.Sukses || hasil.Data is null)
        {
            ValidasiGagal = true;
            PesanGagal = hasil.Pesan;
            await _notif.ShowErrorAsync(hasil.Pesan);
            return;
        }

        var data = hasil.Data;
        _flow.KunjunganId = data.IdKunjungan;
        _sudahDisimpan = true;

        ValidasiGagal = false;
        Tersimpan = true;
        NomorKunjungan = $"KJG-{data.IdKunjungan:D5}";
        WaktuKunjungan = $"{ApiMapper.TanggalJam(data.WaktuCheckin)} WIB";
        NamaToko = string.IsNullOrWhiteSpace(data.NamaToko)
            ? _flow.TokoTerpilih.NamaToko
            : data.NamaToko;
        InfoToko = $"{_flow.TokoTerpilih.KodeToko} - {sales.Nama} / {sales.Nip}";
        Tujuan = _flow.TujuanKunjungan;
        Alamat = _flow.TokoTerpilih.Alamat;
        InfoGps = data.JarakMeter.HasValue
            ? $"Jarak {data.JarakMeter.Value:F0} m dari toko\n"
              + $"{_flow.Latitude:F4}, {_flow.Longitude:F4}"
            : $"{_flow.Latitude:F4}, {_flow.Longitude:F4}";
        InfoWaktu =
            $"Check-in {ApiMapper.Jam(data.WaktuCheckin)} WIB - "
            + $"status {ApiMapper.StatusKunjungan(data.StatusVerifikasi)}";
        InfoWajah = _flow.WajahSimulasi
            ? $"Mode simulasi (model wajah belum tersedia) - skor {_flow.SkorWajah:F3}"
            : $"Wajah cocok - skor {_flow.SkorWajah:F3}";

        await _notif.ShowSuccessAsync(
            data.DalamRadius == false
                ? "Kunjungan tersimpan, tetapi lokasi Anda di luar radius toko."
                : _flow.WajahSimulasi
                    ? "Kunjungan berhasil disimpan (verifikasi wajah masih mode simulasi)."
                    : "Kunjungan berhasil disimpan.");
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

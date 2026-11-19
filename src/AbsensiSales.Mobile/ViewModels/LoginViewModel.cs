using AbsensiSales.Core.Api;
using AbsensiSales.Core.Repositories;
using AbsensiSales.Core.Services;
using AbsensiSales.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.Mobile.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly SalesRepository _salesRepo;
    private readonly SessionService _session;
    private readonly ApiService _api;
    private readonly NotificationService _notif;

    public LoginViewModel(
        SalesRepository salesRepo,
        SessionService session,
        ApiService api,
        NotificationService notif)
    {
        _salesRepo = salesRepo;
        _session = session;
        _api = api;
        _notif = notif;
    }

    [ObservableProperty]
    private string _username = "andi.pratama";

    [ObservableProperty]
    private string _password = "demo123";

    [ObservableProperty]
    private bool _sedangMemproses;

    [ObservableProperty]
    private bool _sedangTesKoneksi;

    [ObservableProperty]
    private string _statusKoneksi = string.Empty;

    public string HariIni => DateTime.Now.ToString("dddd, dd MMMM yyyy");

    public string AlamatBackend => ApiConfig.BaseUrl;

    [RelayCommand]
    private async Task MasukAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            await _notif.ShowErrorAsync("Username dan password wajib diisi.");
            return;
        }

        SedangMemproses = true;

        var sales = await _salesRepo.LoginAsync(Username.Trim(), Password);

        SedangMemproses = false;

        if (sales is null)
        {
            await _notif.ShowErrorAsync("Username atau password salah.");
            return;
        }

        _session.SalesAktif = sales;
        await Shell.Current.GoToAsync("//utama/absensi");
        await _notif.ShowSuccessAsync($"Selamat bekerja, {sales.Nama.Split(' ')[0]}!");
    }

    /// <summary>
    /// Memanggil health.php untuk memastikan URL backend, hosting, dan
    /// database sudah benar. Hasilnya tampil di bawah tombol.
    /// </summary>
    [RelayCommand]
    private async Task TesKoneksiAsync()
    {
        if (SedangTesKoneksi)
            return;

        SedangTesKoneksi = true;
        StatusKoneksi = "Menghubungi backend...";

        try
        {
            if (ApiConfig.BelumDikonfigurasi)
            {
                StatusKoneksi =
                    "URL backend belum diisi. Buka src/AbsensiSales.Core/Api/ApiConfig.cs " +
                    "lalu ganti DefaultBaseUrl dengan domain InfinityFree kamu.";
                return;
            }

            var hasil = await _api.CekKoneksiAsync();

            if (!hasil.Terhubung)
            {
                StatusKoneksi = "Tidak terhubung. " + hasil.Pesan;
                return;
            }

            if (!hasil.Sukses)
            {
                StatusKoneksi = "Server menjawab, tetapi ada masalah. " + hasil.Pesan;
                return;
            }

            var data = hasil.Data;
            if (data is null)
            {
                StatusKoneksi = "Terhubung, tetapi data kesehatan server kosong.";
                return;
            }

            if (data.TabelLengkap)
            {
                StatusKoneksi =
                    $"Terhubung. Database: {data.Database}. " +
                    $"Server: {data.ServerTime} ({data.Timezone}). Semua tabel tersedia.";
            }
            else
            {
                var hilang = data.Tabel
                    .Where(t => !t.Value)
                    .Select(t => t.Key)
                    .ToList();

                StatusKoneksi =
                    "Terhubung, tetapi tabel berikut belum ada: " +
                    string.Join(", ", hilang) +
                    ". Import backend/database/01_schema.sql di phpMyAdmin.";
            }
        }
        finally
        {
            SedangTesKoneksi = false;
        }
    }

    [RelayCommand]
    private async Task BukaRegisterAsync() =>
        await Shell.Current.GoToAsync("register");
}

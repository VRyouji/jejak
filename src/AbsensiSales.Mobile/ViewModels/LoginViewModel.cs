using AbsensiSales.Core.Api;
using AbsensiSales.Core.Services;
using AbsensiSales.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.Mobile.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly SessionService _session;
    private readonly ApiService _api;
    private readonly NotificationService _notif;

    public LoginViewModel(
        SessionService session,
        ApiService api,
        NotificationService notif)
    {
        _session = session;
        _api = api;
        _notif = notif;
    }

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

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
        if (SedangMemproses)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            await _notif.ShowErrorAsync("Email/username dan password wajib diisi.");
            return;
        }

        SedangMemproses = true;
        try
        {
            var hasil = await _api.LoginAsync(Username.Trim(), Password);
            if (!hasil.Sukses || hasil.Data is null)
            {
                await _notif.ShowErrorAsync(hasil.Pesan);
                return;
            }

            var pengguna = hasil.Data.Pengguna;
            var sales = hasil.Data.Sales;

            if (pengguna is null || sales is null ||
                !string.Equals(pengguna.Role, "Sales", StringComparison.OrdinalIgnoreCase))
            {
                _api.LogoutLokal();
                await _notif.ShowErrorAsync(
                    "Akun ini bukan akun Sales. Gunakan aplikasi HR untuk akun HR.");
                return;
            }

            _session.SalesAktif = ApiMapper.KeSales(hasil.Data, pengguna, sales);
            _session.NamaPengguna = pengguna.Nama;
            _session.EmailPengguna = pengguna.Email;
            _session.RolePengguna = pengguna.Role;

            Password = string.Empty;
            await Shell.Current.GoToAsync("//utama/absensi");
            await _notif.ShowSuccessAsync($"Selamat bekerja, {pengguna.Nama.Split(' ')[0]}!");
        }
        finally
        {
            SedangMemproses = false;
        }
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
                    "lalu ganti DefaultBaseUrl dengan domain hosting kamu.";
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

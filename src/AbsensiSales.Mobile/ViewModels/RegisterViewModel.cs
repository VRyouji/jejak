using AbsensiSales.Core.Api;
using AbsensiSales.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.Mobile.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly NotificationService _notif;

    public RegisterViewModel(ApiService api, NotificationService notif)
    {
        _api = api;
        _notif = notif;
    }

    [ObservableProperty]
    private string _nama = string.Empty;

    [ObservableProperty]
    private string _nip = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _sedangMemproses;

    [RelayCommand]
    private async Task DaftarAsync()
    {
        if (SedangMemproses)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Nama) || string.IsNullOrWhiteSpace(Nip)
            || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            await _notif.ShowErrorAsync("Semua kolom wajib diisi.");
            return;
        }

        if (Password.Trim().Length < 6)
        {
            await _notif.ShowErrorAsync("Password minimal 6 karakter.");
            return;
        }

        SedangMemproses = true;
        try
        {
            var hasil = await _api.RegisterAsync(new RegisterRequest
            {
                Nama = Nama.Trim(),
                NomorSales = Nip.Trim(),
                Username = Username.Trim(),
                Password = Password
            });

            if (!hasil.Sukses)
            {
                await _notif.ShowErrorAsync(hasil.Pesan);
                return;
            }

            var email = hasil.Data?.Pengguna?.Email ?? Username.Trim();
            await Shell.Current.GoToAsync("..");
            await _notif.ShowSuccessAsync(
                $"Akun sales terdaftar. Login dengan {email} atau nama lengkap Anda.");
        }
        finally
        {
            SedangMemproses = false;
        }
    }

    [RelayCommand]
    private async Task KembaliAsync() => await Shell.Current.GoToAsync("..");
}

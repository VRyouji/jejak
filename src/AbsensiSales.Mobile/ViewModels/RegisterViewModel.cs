using AbsensiSales.Core.Models;
using AbsensiSales.Core.Repositories;
using AbsensiSales.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.Mobile.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly SalesRepository _salesRepo;
    private readonly NotificationService _notif;

    public RegisterViewModel(SalesRepository salesRepo, NotificationService notif)
    {
        _salesRepo = salesRepo;
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

    [RelayCommand]
    private async Task DaftarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nama) || string.IsNullOrWhiteSpace(Nip)
            || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            await _notif.ShowErrorAsync("Semua kolom wajib diisi.");
            return;
        }

        if (await _salesRepo.UsernameExistsAsync(Username.Trim()))
        {
            await _notif.ShowErrorAsync("Username sudah digunakan.");
            return;
        }

        await _salesRepo.RegisterAsync(new Sales
        {
            Nama = Nama.Trim(),
            Nip = Nip.Trim(),
            Username = Username.Trim(),
            Password = Password,
            Wilayah = "Surabaya Pusat"
        });

        await Shell.Current.GoToAsync("..");
        await _notif.ShowSuccessAsync("Akun sales terdaftar. Silakan masuk.");
    }

    [RelayCommand]
    private async Task KembaliAsync() => await Shell.Current.GoToAsync("..");
}

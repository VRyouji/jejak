using AbsensiSales.Core.Api;
using AbsensiSales.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.HR.ViewModels;

public partial class HrLoginViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly SessionService _session;

    public HrLoginViewModel(ApiService api, SessionService session)
    {
        _api = api;
        _session = session;
    }

    [ObservableProperty]
    private string _username = "hr@example.com";

    [ObservableProperty]
    private string _password = "hr123456";

    [ObservableProperty]
    private string _pesanError = string.Empty;

    [ObservableProperty]
    private bool _sedangMemproses;

    [RelayCommand]
    private async Task MasukAsync()
    {
        if (SedangMemproses)
        {
            return;
        }

        PesanError = string.Empty;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            PesanError = "Email dan password wajib diisi.";
            return;
        }

        SedangMemproses = true;
        try
        {
            var hasil = await _api.LoginAsync(Username.Trim(), Password);
            if (!hasil.Sukses || hasil.Data is null)
            {
                PesanError = hasil.Pesan;
                return;
            }

            var pengguna = hasil.Data.Pengguna;
            if (pengguna is null ||
                !string.Equals(pengguna.Role, "HR", StringComparison.OrdinalIgnoreCase))
            {
                _api.LogoutLokal();
                PesanError = "Akun ini bukan akun HR. Gunakan akun dengan role HR.";
                return;
            }

            _session.HrLogin = true;
            _session.NamaPengguna = pengguna.Nama;
            _session.EmailPengguna = pengguna.Email;
            _session.RolePengguna = pengguna.Role;

            Password = string.Empty;
            await Shell.Current.GoToAsync("workspace");
        }
        finally
        {
            SedangMemproses = false;
        }
    }
}

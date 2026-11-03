using System.Collections.ObjectModel;
using System.Globalization;
using AbsensiSales.Core.Models;
using AbsensiSales.Core.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.HR.ViewModels;

public partial class MasterDataViewModel : ObservableObject
{
    private readonly SalesRepository _salesRepo;
    private readonly TokoRepository _tokoRepo;

    public MasterDataViewModel(SalesRepository salesRepo, TokoRepository tokoRepo)
    {
        _salesRepo = salesRepo;
        _tokoRepo = tokoRepo;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTokoSelected))]
    [NotifyPropertyChangedFor(nameof(IsTabSales))]
    [NotifyPropertyChangedFor(nameof(IsTabToko))]
    private bool _isSalesSelected = true;

    public bool IsTokoSelected => !IsSalesSelected;
    public bool IsTabSales => IsSalesSelected;
    public bool IsTabToko => IsTokoSelected;

    public ObservableCollection<Sales> DaftarSales { get; } = new();
    public ObservableCollection<Toko> DaftarToko { get; } = new();

    [ObservableProperty]
    private string _jumlahSales = "0";

    [ObservableProperty]
    private string _jumlahToko = "0";

    // ---- Form tambah sales ----

    [ObservableProperty]
    private bool _isFormSalesTerbuka;

    [ObservableProperty]
    private string _formSalesNip = string.Empty;

    [ObservableProperty]
    private string _formSalesNama = string.Empty;

    [ObservableProperty]
    private string _formSalesUsername = string.Empty;

    [ObservableProperty]
    private string _formSalesPassword = string.Empty;

    [ObservableProperty]
    private string _formSalesWilayah = "Surabaya Pusat";

    [ObservableProperty]
    private string _formSalesFoto = string.Empty;

    [ObservableProperty]
    private string _pesanErrorSales = string.Empty;

    [ObservableProperty]
    private bool _adaErrorSales;

    // ---- Form tambah toko ----

    [ObservableProperty]
    private bool _isFormTokoTerbuka;

    [ObservableProperty]
    private string _formTokoKode = string.Empty;

    [ObservableProperty]
    private string _formTokoNama = string.Empty;

    [ObservableProperty]
    private string _formTokoAlamat = string.Empty;

    [ObservableProperty]
    private string _formTokoPemilik = string.Empty;

    [ObservableProperty]
    private string _formTokoTelepon = string.Empty;

    [ObservableProperty]
    private string _formTokoLatitude = "-7.2575";

    [ObservableProperty]
    private string _formTokoLongitude = "112.7521";

    [ObservableProperty]
    private string _formTokoRadius = "100";

    [ObservableProperty]
    private string _pesanErrorToko = string.Empty;

    [ObservableProperty]
    private bool _adaErrorToko;

    public async Task MuatAsync()
    {
        DaftarSales.Clear();
        foreach (var s in await _salesRepo.GetAllAsync())
            DaftarSales.Add(s);

        DaftarToko.Clear();
        foreach (var t in await _tokoRepo.GetAllAsync())
            DaftarToko.Add(t);

        JumlahSales = DaftarSales.Count.ToString();
        JumlahToko = DaftarToko.Count.ToString();
    }

    [RelayCommand]
    private void TampilkanSales() => IsSalesSelected = true;

    [RelayCommand]
    private void TampilkanToko() => IsSalesSelected = false;

    [RelayCommand]
    private void TambahSales()
    {
        IsFormTokoTerbuka = false;
        ResetFormSales();
        IsFormSalesTerbuka = true;
    }

    [RelayCommand]
    private void BatalSales() => IsFormSalesTerbuka = false;

    [RelayCommand]
    private async Task SimpanSalesAsync()
    {
        if (!ValidasiFormSales(out var pesan))
        {
            TampilkanErrorSales(pesan);
            return;
        }

        try
        {
            var username = FormSalesUsername.Trim();

            if (await _salesRepo.UsernameExistsAsync(username))
            {
                TampilkanErrorSales("Username tersebut sudah digunakan sales lain.");
                return;
            }

            var nama = FormSalesNama.Trim();

            await _salesRepo.RegisterAsync(new Sales
            {
                Nip = FormSalesNip.Trim(),
                Nama = nama,
                Username = username,
                Password = FormSalesPassword.Trim(),
                Wilayah = FormSalesWilayah.Trim(),
                FotoReferensi = FormSalesFoto.Trim()
            });

            IsFormSalesTerbuka = false;
            IsSalesSelected = true;
            await MuatAsync();

            await Shell.Current.DisplayAlert(
                "Sales ditambahkan",
                $"Akun sales {nama} berhasil ditambahkan.",
                "OK");
        }
        catch
        {
            TampilkanErrorSales("Data sales gagal disimpan. Silakan coba lagi.");
        }
    }

    [RelayCommand]
    private void TambahToko()
    {
        IsFormSalesTerbuka = false;
        ResetFormToko();
        IsFormTokoTerbuka = true;
    }

    [RelayCommand]
    private void BatalToko() => IsFormTokoTerbuka = false;

    [RelayCommand]
    private async Task SimpanTokoAsync()
    {
        if (!ValidasiFormToko(out var pesan, out var latitude, out var longitude, out var radius))
        {
            TampilkanErrorToko(pesan);
            return;
        }

        try
        {
            var kode = FormTokoKode.Trim();

            if (await _tokoRepo.GetByKodeAsync(kode) is not null)
            {
                TampilkanErrorToko("Kode toko tersebut sudah digunakan.");
                return;
            }

            var nama = FormTokoNama.Trim();

            await _tokoRepo.TambahAsync(new Toko
            {
                KodeToko = kode,
                NamaToko = nama,
                Alamat = FormTokoAlamat.Trim(),
                Pemilik = FormTokoPemilik.Trim(),
                Telepon = FormTokoTelepon.Trim(),
                Latitude = latitude,
                Longitude = longitude,
                RadiusMeter = radius
            });

            IsFormTokoTerbuka = false;
            IsSalesSelected = false;
            await MuatAsync();

            await Shell.Current.DisplayAlert(
                "Toko ditambahkan",
                $"Toko {nama} dengan radius {radius:F0} m berhasil ditambahkan.",
                "OK");
        }
        catch
        {
            TampilkanErrorToko("Data toko gagal disimpan. Silakan coba lagi.");
        }
    }

    private bool ValidasiFormSales(out string pesan)
    {
        if (string.IsNullOrWhiteSpace(FormSalesNip))
        {
            pesan = "NIP wajib diisi.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FormSalesNama))
        {
            pesan = "Nama sales wajib diisi.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FormSalesUsername))
        {
            pesan = "Username wajib diisi.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FormSalesPassword) || FormSalesPassword.Trim().Length < 6)
        {
            pesan = "Password wajib diisi minimal 6 karakter.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FormSalesWilayah))
        {
            pesan = "Wilayah penjualan wajib diisi.";
            return false;
        }

        if (DaftarSales.Any(s => string.Equals(
                s.Nip.Trim(), FormSalesNip.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            pesan = "NIP tersebut sudah terdaftar.";
            return false;
        }

        if (DaftarSales.Any(s => string.Equals(
                s.Username.Trim(), FormSalesUsername.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            pesan = "Username tersebut sudah digunakan sales lain.";
            return false;
        }

        pesan = string.Empty;
        return true;
    }

    private bool ValidasiFormToko(
        out string pesan,
        out double latitude,
        out double longitude,
        out double radius)
    {
        latitude = 0;
        longitude = 0;
        radius = 0;

        if (string.IsNullOrWhiteSpace(FormTokoKode))
        {
            pesan = "Kode toko wajib diisi.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FormTokoNama))
        {
            pesan = "Nama toko wajib diisi.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FormTokoAlamat))
        {
            pesan = "Alamat toko wajib diisi.";
            return false;
        }

        if (DaftarToko.Any(t => string.Equals(
                t.KodeToko.Trim(), FormTokoKode.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            pesan = "Kode toko tersebut sudah digunakan.";
            return false;
        }

        if (!TryParseAngka(FormTokoLatitude, out latitude) || latitude < -90 || latitude > 90)
        {
            pesan = "Latitude harus berupa angka antara -90 dan 90.";
            return false;
        }

        if (!TryParseAngka(FormTokoLongitude, out longitude) || longitude < -180 || longitude > 180)
        {
            pesan = "Longitude harus berupa angka antara -180 dan 180.";
            return false;
        }

        if (!TryParseAngka(FormTokoRadius, out radius) || radius <= 0)
        {
            pesan = "Radius harus berupa angka lebih dari 0 meter.";
            return false;
        }

        pesan = string.Empty;
        return true;
    }

    private static bool TryParseAngka(string teks, out double nilai)
    {
        const NumberStyles gaya = NumberStyles.Float;

        return double.TryParse(teks, gaya, CultureInfo.CurrentCulture, out nilai)
            || double.TryParse(teks, gaya, CultureInfo.InvariantCulture, out nilai);
    }

    private void ResetFormSales()
    {
        FormSalesNip = string.Empty;
        FormSalesNama = string.Empty;
        FormSalesUsername = string.Empty;
        FormSalesPassword = string.Empty;
        FormSalesWilayah = "Surabaya Pusat";
        FormSalesFoto = string.Empty;
        PesanErrorSales = string.Empty;
        AdaErrorSales = false;
    }

    private void ResetFormToko()
    {
        FormTokoKode = string.Empty;
        FormTokoNama = string.Empty;
        FormTokoAlamat = string.Empty;
        FormTokoPemilik = string.Empty;
        FormTokoTelepon = string.Empty;
        FormTokoLatitude = "-7.2575";
        FormTokoLongitude = "112.7521";
        FormTokoRadius = "100";
        PesanErrorToko = string.Empty;
        AdaErrorToko = false;
    }

    private void TampilkanErrorSales(string pesan)
    {
        PesanErrorSales = pesan;
        AdaErrorSales = true;
    }

    private void TampilkanErrorToko(string pesan)
    {
        PesanErrorToko = pesan;
        AdaErrorToko = true;
    }
}

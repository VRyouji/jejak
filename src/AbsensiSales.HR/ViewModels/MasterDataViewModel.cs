using System.Collections.ObjectModel;
using System.Globalization;
using AbsensiSales.Core.Api;
using AbsensiSales.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.HR.ViewModels;

public partial class MasterDataViewModel : ObservableObject
{
    private readonly ApiService _api;

    public MasterDataViewModel(ApiService api)
    {
        _api = api;
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

    [ObservableProperty]
    private string _pesanError = string.Empty;

    [ObservableProperty]
    private bool _adaError;

    // ---- Form tambah sales ----

    [ObservableProperty]
    private bool _isFormSalesTerbuka;

    [ObservableProperty]
    private string _formSalesNip = string.Empty;

    [ObservableProperty]
    private string _formSalesNama = string.Empty;

    [ObservableProperty]
    private string _formSalesEmail = string.Empty;

    [ObservableProperty]
    private string _formSalesPassword = string.Empty;

    [ObservableProperty]
    private string _formSalesTelepon = string.Empty;

    [ObservableProperty]
    private string _formSalesWilayah = string.Empty;

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
        var hasilSales = await _api.GetSalesAdminAsync();
        var hasilToko = await _api.GetTokoAsync();

        var pesan = new List<string>();
        if (!hasilSales.Sukses) pesan.Add(hasilSales.Pesan);
        if (!hasilToko.Sukses) pesan.Add(hasilToko.Pesan);
        PesanError = string.Join(" ", pesan.Distinct());
        AdaError = PesanError.Length > 0;

        DaftarSales.Clear();
        foreach (var sales in hasilSales.Data ?? new List<SalesAdminDto>())
        {
            DaftarSales.Add(new Sales
            {
                Id = sales.IdSales,
                Nip = sales.NomorSales,
                Nama = sales.Nama,
                Username = sales.Email,
                Wilayah = string.IsNullOrWhiteSpace(sales.Wilayah) ? "-" : sales.Wilayah!
            });
        }

        DaftarToko.Clear();
        foreach (var toko in hasilToko.Data ?? new List<TokoDto>())
        {
            DaftarToko.Add(new Toko
            {
                Id = toko.IdToko,
                KodeToko = string.IsNullOrWhiteSpace(toko.KodeToko)
                    ? $"TK-{toko.IdToko}"
                    : toko.KodeToko!,
                NamaToko = toko.NamaToko,
                Alamat = toko.Alamat ?? "-",
                Pemilik = toko.Pemilik ?? string.Empty,
                Telepon = toko.Telepon ?? string.Empty,
                Latitude = toko.Latitude ?? 0,
                Longitude = toko.Longitude ?? 0,
                RadiusMeter = toko.RadiusMeter
            });
        }

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

        var hasil = await _api.TambahSalesAsync(new SalesCreateRequest
        {
            Nama = FormSalesNama.Trim(),
            Email = FormSalesEmail.Trim(),
            Password = FormSalesPassword.Trim(),
            NomorSales = FormSalesNip.Trim(),
            NomorTelepon = FormSalesTelepon.Trim(),
            Wilayah = string.IsNullOrWhiteSpace(FormSalesWilayah)
                ? null
                : FormSalesWilayah.Trim()
        });

        if (!hasil.Sukses)
        {
            TampilkanErrorSales(hasil.Pesan);
            return;
        }

        var nama = hasil.Data?.Nama ?? FormSalesNama.Trim();

        IsFormSalesTerbuka = false;
        IsSalesSelected = true;
        await MuatAsync();

        await Shell.Current.DisplayAlert(
            "Sales ditambahkan",
            $"Akun sales {nama} berhasil ditambahkan.",
            "OK");
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

        var hasil = await _api.SimpanTokoAsync(new TokoRequest
        {
            Action = "create",
            KodeToko = string.IsNullOrWhiteSpace(FormTokoKode) ? null : FormTokoKode.Trim(),
            NamaToko = FormTokoNama.Trim(),
            Alamat = FormTokoAlamat.Trim(),
            Pemilik = FormTokoPemilik.Trim(),
            Telepon = FormTokoTelepon.Trim(),
            Latitude = latitude,
            Longitude = longitude,
            Radius = radius
        });

        if (!hasil.Sukses)
        {
            TampilkanErrorToko(hasil.Pesan);
            return;
        }

        var nama = hasil.Data?.NamaToko ?? FormTokoNama.Trim();

        IsFormTokoTerbuka = false;
        IsSalesSelected = false;
        await MuatAsync();

        await Shell.Current.DisplayAlert(
            "Toko ditambahkan",
            $"Toko {nama} dengan radius {radius:F0} m berhasil ditambahkan.",
            "OK");
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

        if (string.IsNullOrWhiteSpace(FormSalesEmail) || !FormSalesEmail.Contains('@'))
        {
            pesan = "Email wajib diisi dengan format yang benar.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FormSalesPassword) || FormSalesPassword.Trim().Length < 6)
        {
            pesan = "Password wajib diisi minimal 6 karakter.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FormSalesTelepon))
        {
            pesan = "Nomor telepon wajib diisi.";
            return false;
        }

        if (DaftarSales.Any(s => string.Equals(
                s.Nip.Trim(), FormSalesNip.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            pesan = "NIP tersebut sudah terdaftar.";
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

        if (!string.IsNullOrWhiteSpace(FormTokoKode)
            && DaftarToko.Any(t => string.Equals(
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
        FormSalesEmail = string.Empty;
        FormSalesPassword = string.Empty;
        FormSalesTelepon = string.Empty;
        FormSalesWilayah = string.Empty;
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

using System.Collections.ObjectModel;
using System.Globalization;
using AbsensiSales.Core.Api;
using AbsensiSales.Core.Maps;
using AbsensiSales.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.HR.ViewModels;

public partial class MasterDataViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly MapsLinkResolver _maps = new();
    private CancellationTokenSource? _mapsCts;

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

    public ObservableCollection<SalesMasterItem> DaftarSales { get; } = new();
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
    private string _formSalesJudul = "Tambah sales";

    [ObservableProperty]
    private string _formSalesPasswordHint = "Minimal 6 karakter";

    private int? _editingSalesId;

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
    private string _formTokoJudul = "Tambah toko";

    private int? _editingTokoId;

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
    private string _formTokoLatitude = string.Empty;

    [ObservableProperty]
    private string _formTokoLongitude = string.Empty;

    [ObservableProperty]
    private string _formTokoRadius = "100";

    /// <summary>Link Google Maps (panjang/pendek) atau koordinat polos yang ditempel HR.</summary>
    [ObservableProperty]
    private string _formTokoUrlMaps = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdaInfoKoordinat))]
    private string _infoKoordinat = string.Empty;

    [ObservableProperty]
    private bool _infoKoordinatGagal;

    public bool AdaInfoKoordinat => InfoKoordinat.Length > 0;

    [ObservableProperty]
    private string _pesanErrorToko = string.Empty;

    [ObservableProperty]
    private bool _adaErrorToko;

    /// <summary>
    /// Dipanggil otomatis setiap isi kolom link Google Maps berubah.
    /// Menunggu sebentar (debounce) supaya tidak memproses link yang
    /// masih diketik, lalu mengisi Latitude/Longitude. Hasilnya tetap
    /// bisa diedit manual oleh HR.
    /// </summary>
    partial void OnFormTokoUrlMapsChanged(string value)
    {
        _mapsCts?.Cancel();
        _mapsCts = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            InfoKoordinat = string.Empty;
            InfoKoordinatGagal = false;
            return;
        }

        _mapsCts = new CancellationTokenSource();
        _ = IsiKoordinatDariUrlAsync(value, _mapsCts.Token);
    }

    private async Task IsiKoordinatDariUrlAsync(string masukan, CancellationToken ct)
    {
        try
        {
            await Task.Delay(600, ct);

            InfoKoordinatGagal = false;
            InfoKoordinat = "Membaca lokasi dari link...";

            var hasil = await _maps.AmbilAsync(masukan, ct);
            if (ct.IsCancellationRequested)
            {
                return;
            }

            if (!hasil.Sukses)
            {
                InfoKoordinatGagal = true;
                InfoKoordinat = hasil.Pesan;
                return;
            }

            FormTokoLatitude = hasil.Koordinat.Latitude.ToString("0.#######", CultureInfo.InvariantCulture);
            FormTokoLongitude = hasil.Koordinat.Longitude.ToString("0.#######", CultureInfo.InvariantCulture);
            InfoKoordinat = "Koordinat terisi dari link. Periksa kembali dan sesuaikan jika perlu.";
        }
        catch (OperationCanceledException)
        {
            // Link berubah lagi sebelum selesai: abaikan, proses baru akan menggantikan.
        }
    }

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
            DaftarSales.Add(SalesMasterItem.DariApi(sales));
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
        _editingSalesId = null;
        FormSalesJudul = "Tambah sales";
        FormSalesPasswordHint = "Minimal 6 karakter";
        ResetFormSales();
        IsFormSalesTerbuka = true;
    }

    [RelayCommand]
    private void UbahSales(SalesMasterItem? item)
    {
        if (item is null)
        {
            return;
        }

        IsFormTokoTerbuka = false;
        _editingSalesId = item.Id;
        FormSalesJudul = $"Ubah sales - {item.Nama}";
        FormSalesPasswordHint = "Kosongkan jika password tidak diubah.";
        FormSalesNip = item.Nip == "-" ? string.Empty : item.Nip;
        FormSalesNama = item.Nama;
        FormSalesEmail = item.Email == "-" ? string.Empty : item.Email;
        FormSalesPassword = string.Empty;
        FormSalesTelepon = item.Telepon == "-" ? string.Empty : item.Telepon;
        FormSalesWilayah = item.Wilayah == "-" ? string.Empty : item.Wilayah;
        PesanErrorSales = string.Empty;
        AdaErrorSales = false;
        IsFormSalesTerbuka = true;
    }

    [RelayCommand]
    private async Task HapusSalesAsync(SalesMasterItem? item)
    {
        if (item is null)
        {
            return;
        }

        bool yakin = await Shell.Current.DisplayAlert(
            "Hapus sales",
            $"Hapus akun {item.Nama} ({item.Nip})? Riwayat kunjungan, permohonan, "
            + "dan penggajian milik sales ini juga akan terhapus.",
            "Hapus",
            "Batal");

        if (!yakin)
        {
            return;
        }

        var hasil = await _api.HapusSalesAsync(item.Id);
        if (!hasil.Sukses)
        {
            await Shell.Current.DisplayAlert("Gagal menghapus", hasil.Pesan, "OK");
            return;
        }

        await MuatAsync();
        await Shell.Current.DisplayAlert(
            "Sales dihapus",
            $"Akun sales {item.Nama} berhasil dihapus.",
            "OK");
    }

    [RelayCommand]
    private void BatalSales()
    {
        _editingSalesId = null;
        IsFormSalesTerbuka = false;
    }

    [RelayCommand]
    private async Task SimpanSalesAsync()
    {
        if (!ValidasiFormSales(out var pesan))
        {
            TampilkanErrorSales(pesan);
            return;
        }

        if (_editingSalesId is int idSales)
        {
            var hasilUbah = await _api.UpdateSalesAsync(new SalesUpdateRequest
            {
                Action = "update",
                IdSales = idSales,
                Nama = FormSalesNama.Trim(),
                Email = FormSalesEmail.Trim(),
                Password = string.IsNullOrWhiteSpace(FormSalesPassword)
                    ? null
                    : FormSalesPassword.Trim(),
                NomorSales = FormSalesNip.Trim(),
                NomorTelepon = FormSalesTelepon.Trim(),
                Wilayah = string.IsNullOrWhiteSpace(FormSalesWilayah)
                    ? null
                    : FormSalesWilayah.Trim()
            });

            if (!hasilUbah.Sukses)
            {
                TampilkanErrorSales(hasilUbah.Pesan);
                return;
            }

            var namaDiubah = hasilUbah.Data?.Nama ?? FormSalesNama.Trim();

            IsFormSalesTerbuka = false;
            _editingSalesId = null;
            IsSalesSelected = true;
            await MuatAsync();

            await Shell.Current.DisplayAlert(
                "Sales diperbarui",
                $"Data sales {namaDiubah} berhasil diperbarui.",
                "OK");
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
        _editingTokoId = null;
        FormTokoJudul = "Tambah toko";
        ResetFormToko();
        IsFormTokoTerbuka = true;
    }

    [RelayCommand]
    private void UbahToko(Toko? item)
    {
        if (item is null)
        {
            return;
        }

        IsFormSalesTerbuka = false;
        _editingTokoId = item.Id;
        FormTokoJudul = $"Ubah toko - {item.NamaToko}";
        FormTokoKode = item.KodeToko;
        FormTokoNama = item.NamaToko;
        FormTokoAlamat = item.Alamat == "-" ? string.Empty : item.Alamat;
        FormTokoPemilik = item.Pemilik;
        FormTokoTelepon = item.Telepon;
        FormTokoUrlMaps = string.Empty;
        FormTokoLatitude = item.Latitude.ToString(CultureInfo.InvariantCulture);
        FormTokoLongitude = item.Longitude.ToString(CultureInfo.InvariantCulture);
        FormTokoRadius = item.RadiusMeter.ToString(CultureInfo.InvariantCulture);
        PesanErrorToko = string.Empty;
        AdaErrorToko = false;
        IsFormTokoTerbuka = true;
    }

    [RelayCommand]
    private async Task HapusTokoAsync(Toko? item)
    {
        if (item is null)
        {
            return;
        }

        bool yakin = await Shell.Current.DisplayAlert(
            "Hapus toko",
            $"Hapus toko {item.NamaToko} ({item.KodeToko})? Kunjungan yang "
            + "tercatat di toko ini juga akan terhapus.",
            "Hapus",
            "Batal");

        if (!yakin)
        {
            return;
        }

        var hasil = await _api.HapusTokoAsync(item.Id);
        if (!hasil.Sukses)
        {
            await Shell.Current.DisplayAlert("Gagal menghapus", hasil.Pesan, "OK");
            return;
        }

        await MuatAsync();
        await Shell.Current.DisplayAlert(
            "Toko dihapus",
            $"Toko {item.NamaToko} berhasil dihapus.",
            "OK");
    }

    [RelayCommand]
    private void BatalToko()
    {
        _editingTokoId = null;
        IsFormTokoTerbuka = false;
    }

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
            Action = _editingTokoId is null ? "create" : "update",
            IdToko = _editingTokoId,
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
        bool baru = _editingTokoId is null;

        IsFormTokoTerbuka = false;
        _editingTokoId = null;
        IsSalesSelected = false;
        await MuatAsync();

        await Shell.Current.DisplayAlert(
            baru ? "Toko ditambahkan" : "Toko diperbarui",
            baru
                ? $"Toko {nama} dengan radius {radius:F0} m berhasil ditambahkan."
                : $"Data toko {nama} berhasil diperbarui.",
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

        bool passwordKosong = string.IsNullOrWhiteSpace(FormSalesPassword);

        if (_editingSalesId is null && passwordKosong)
        {
            pesan = "Password wajib diisi minimal 6 karakter.";
            return false;
        }

        if (!passwordKosong && FormSalesPassword.Trim().Length < 6)
        {
            pesan = "Password minimal 6 karakter.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FormSalesTelepon))
        {
            pesan = "Nomor telepon wajib diisi.";
            return false;
        }

        if (DaftarSales.Any(s => s.Id != _editingSalesId
                && string.Equals(
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
            && DaftarToko.Any(t => t.Id != _editingTokoId
                && string.Equals(
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
        FormTokoUrlMaps = string.Empty;
        FormTokoLatitude = string.Empty;
        FormTokoLongitude = string.Empty;
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

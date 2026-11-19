using System.Collections.ObjectModel;
using System.Text;
using AbsensiSales.Core.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;

namespace AbsensiSales.HR.ViewModels;

public partial class LaporanHrViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly Services.HrNavigationService _nav;
    private readonly List<KunjunganDto> _semua = new();

    public LaporanHrViewModel(ApiService api, Services.HrNavigationService nav)
    {
        _api = api;
        _nav = nav;
    }

    public ObservableCollection<KunjunganTabelItem> DaftarKunjungan { get; } = new();

    [ObservableProperty]
    private string _judulTanggal = string.Empty;

    [ObservableProperty]
    private string _salesHadir = "0 / 0";

    [ObservableProperty]
    private string _totalKunjungan = "0";

    [ObservableProperty]
    private string _berhasilTersimpan = "0";

    [ObservableProperty]
    private string _perluTindakLanjut = "0";

    [ObservableProperty]
    private string _jumlahHasil = string.Empty;

    [ObservableProperty]
    private string _filterStatus = "Semua status";

    public string[] PilihanStatus { get; } =
        { "Semua status", "Valid", "Pending", "Invalid" };

    [ObservableProperty]
    private KunjunganTabelItem? _kunjunganTerpilih;

    [ObservableProperty]
    private string _pesanError = string.Empty;

    [ObservableProperty]
    private bool _adaError;

    public async Task MuatAsync()
    {
        var sekarang = DateTime.Now;
        var periode = sekarang.ToString("yyyy-MM");
        var tanggal = sekarang.ToString("yyyy-MM-dd");
        JudulTanggal = ApiMapper.TanggalPanjang(sekarang);

        var hasilLaporan = await _api.GetLaporanKunjunganAsync(periode, tanggal);
        var hasilSales = await _api.GetSalesAdminAsync();

        var pesan = new List<string>();
        if (!hasilLaporan.Sukses) pesan.Add(hasilLaporan.Pesan);
        if (!hasilSales.Sukses) pesan.Add(hasilSales.Pesan);
        PesanError = string.Join(" ", pesan.Distinct());
        AdaError = PesanError.Length > 0;

        _semua.Clear();
        if (hasilLaporan.Data is not null)
        {
            _semua.AddRange(hasilLaporan.Data.Items);
        }

        var daftarSales = hasilSales.Data ?? new List<SalesAdminDto>();
        int hadir = _semua.Select(kunjungan => kunjungan.IdSales).Distinct().Count();

        SalesHadir = $"{hadir} / {daftarSales.Count}";
        TotalKunjungan = _semua.Count.ToString();
        BerhasilTersimpan = _semua.Count(k => k.StatusVerifikasi == "Valid").ToString();
        PerluTindakLanjut = _semua.Count(k => k.StatusVerifikasi != "Valid").ToString();

        TerapkanFilter();
    }

    partial void OnFilterStatusChanged(string value) => TerapkanFilter();

    private void TerapkanFilter()
    {
        var hasil = FilterStatus switch
        {
            "Valid" => _semua.Where(k => k.StatusVerifikasi == "Valid"),
            "Pending" => _semua.Where(k => k.StatusVerifikasi == "Pending"),
            "Invalid" => _semua.Where(k => k.StatusVerifikasi == "Invalid"),
            _ => _semua
        };

        DaftarKunjungan.Clear();
        foreach (var kunjungan in hasil
                     .OrderByDescending(k => ApiMapper.Waktu(k.WaktuCheckin)))
        {
            DaftarKunjungan.Add(new KunjunganTabelItem
            {
                Id = kunjungan.IdKunjungan,
                NamaSales = string.IsNullOrWhiteSpace(kunjungan.NamaSales)
                    ? "-"
                    : kunjungan.NamaSales!,
                Nip = string.IsNullOrWhiteSpace(kunjungan.NomorSales)
                    ? "-"
                    : kunjungan.NomorSales!,
                NamaToko = string.IsNullOrWhiteSpace(kunjungan.NamaToko)
                    ? "-"
                    : kunjungan.NamaToko!,
                KodeToko = string.IsNullOrWhiteSpace(kunjungan.KodeToko)
                    ? $"TK-{kunjungan.IdToko}"
                    : kunjungan.KodeToko!,
                WaktuAwal = ApiMapper.Jam(kunjungan.WaktuCheckin),
                WaktuCatat = ApiMapper.Jam(kunjungan.WaktuCheckout),
                Lokasi = FormatLokasi(kunjungan),
                Status = ApiMapper.StatusKunjungan(kunjungan.StatusVerifikasi),
                AdaWajah = (kunjungan.SkorWajah ?? 0) > 0,
                AdaFoto = !string.IsNullOrWhiteSpace(kunjungan.FotoBukti)
            });
        }

        JumlahHasil = $"{DaftarKunjungan.Count} hasil - seluruh waktu dalam WIB";
    }

    private static string FormatLokasi(KunjunganDto kunjungan)
    {
        if (kunjungan.StatusVerifikasi == "Invalid")
        {
            return "Di luar radius";
        }

        if (kunjungan.JarakMeter.HasValue)
        {
            return $"Sesuai - +/-{kunjungan.JarakMeter.Value:F0} m";
        }

        if (kunjungan.LatitudeCheckin.HasValue && kunjungan.LongitudeCheckin.HasValue)
        {
            return $"{kunjungan.LatitudeCheckin.Value:F4}, {kunjungan.LongitudeCheckin.Value:F4}";
        }

        return "-";
    }

    [RelayCommand]
    private void BukaDetail(KunjunganTabelItem? item)
    {
        if (item is null)
        {
            return;
        }

        _nav.NavigateTo(typeof(Views.DetailKunjunganPage), item.Id);
    }

    [RelayCommand]
    private async Task EksporAsync()
    {
        try
        {
            var filePath = Path.Combine(
                FileSystem.Current.CacheDirectory,
                $"laporan-kunjungan-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

            var baris = new List<string>
            {
                "Sales,NIP,Toko,Kode toko,Waktu check-in,Waktu check-out,Lokasi,Status"
            };

            foreach (var item in DaftarKunjungan)
            {
                baris.Add(string.Join(',', new[]
                {
                    item.NamaSales,
                    item.Nip,
                    item.NamaToko,
                    item.KodeToko,
                    item.WaktuAwal,
                    item.WaktuCatat,
                    item.Lokasi,
                    item.Status
                }.Select(KutipCsv)));
            }

            await File.WriteAllTextAsync(
                filePath,
                string.Join(Environment.NewLine, baris),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(filePath),
                Title = "Laporan kunjungan"
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(
                "Ekspor gagal",
                "File laporan tidak dapat dibuat atau dibuka.\n\n" + ex.Message,
                "OK");
        }
    }

    private static string KutipCsv(string nilai) =>
        '"' + (nilai ?? string.Empty).Replace("\"", "\"\"") + '"';
}

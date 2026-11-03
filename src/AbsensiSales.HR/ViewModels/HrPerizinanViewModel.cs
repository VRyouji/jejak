using System.Collections.ObjectModel;
using System.Globalization;
using AbsensiSales.Core.Models;
using AbsensiSales.Core.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.HR.ViewModels;

/// <summary>
/// ViewModel halaman perizinan HR &amp; sales. Data diambil dari SQLite bila
/// tersedia; jika database masih kosong dipakai data mock agar halaman bisa
/// langsung diuji tanpa persiapan tambahan.
/// </summary>
public partial class HrPerizinanViewModel : ObservableObject
{
    private readonly PermohonanRepository _permohonanRepo;
    private readonly SalesRepository _salesRepo;
    private readonly List<PermohonanItem> _semua = new();

    public HrPerizinanViewModel(PermohonanRepository permohonanRepo, SalesRepository salesRepo)
    {
        _permohonanRepo = permohonanRepo;
        _salesRepo = salesRepo;
    }

    public ObservableCollection<PermohonanItem> DaftarPerizinan { get; } = new();

    [ObservableProperty]
    private string _totalPerizinan = "0";

    [ObservableProperty]
    private string _jumlahPending = "0";

    [ObservableProperty]
    private string _jumlahDisetujui = "0";

    [ObservableProperty]
    private string _jumlahDitolak = "0";

    [ObservableProperty]
    private string _updatePada = string.Empty;

    public async Task MuatAsync()
    {
        List<PermohonanItem> hasil;

        try
        {
            var data = await _permohonanRepo.GetAllAsync();

            if (data.Count == 0)
            {
                hasil = BuatDataMock().ToList();
            }
            else
            {
                hasil = new List<PermohonanItem>();

                foreach (var permohonan in data)
                {
                    var sales = await _salesRepo.GetByIdAsync(permohonan.SalesId);
                    hasil.Add(PermohonanItem.DariData(permohonan, sales));
                }
            }
        }
        catch
        {
            // Database belum siap / gagal dibuka: tetap tampilkan data mock.
            hasil = BuatDataMock().ToList();
        }

        _semua.Clear();
        _semua.AddRange(hasil);

        DaftarPerizinan.Clear();
        foreach (var item in hasil)
            DaftarPerizinan.Add(item);

        PerbaruiRingkasan();
        UpdatePada = $"Diperbarui {DateTime.Now.ToString("dd MMM yyyy HH.mm", CultureInfo.GetCultureInfo("id-ID"))} WIB";
    }

    [RelayCommand]
    private async Task ApproveAsync(PermohonanItem? item)
    {
        if (item is null || !item.Pending)
            return;

        item.Status = "Approved";
        item.CatatanHr = "Disetujui oleh HR.";
        await SimpanStatusAsync(item, nameof(StatusPermohonan.Disetujui));
        PerbaruiRingkasan();

        await Shell.Current.DisplayAlert(
            "Permohonan disetujui",
            $"Permohonan {item.Jenis} dari {item.NamaSales} telah disetujui.",
            "OK");
    }

    [RelayCommand]
    private async Task RejectAsync(PermohonanItem? item)
    {
        if (item is null || !item.Pending)
            return;

        var alasan = await Shell.Current.DisplayPromptAsync(
            "Tolak permohonan",
            $"Masukkan alasan penolakan untuk {item.NamaSales}:",
            accept: "Tolak",
            cancel: "Batal",
            placeholder: "Contoh: jadwal kunjungan padat dan belum ada pengganti.",
            maxLength: 250,
            keyboard: Keyboard.Text);

        if (alasan is null)
            return;

        if (string.IsNullOrWhiteSpace(alasan))
        {
            await Shell.Current.DisplayAlert(
                "Alasan wajib diisi",
                "Alasan penolakan tidak boleh kosong.",
                "OK");
            return;
        }

        item.Status = "Rejected";
        item.CatatanHr = alasan.Trim();
        await SimpanStatusAsync(item, nameof(StatusPermohonan.Ditolak));
        PerbaruiRingkasan();

        await Shell.Current.DisplayAlert(
            "Permohonan ditolak",
            $"Permohonan {item.Jenis} dari {item.NamaSales} ditolak.",
            "OK");
    }

    [RelayCommand]
    private async Task SegarkanAsync() => await MuatAsync();

    private async Task SimpanStatusAsync(PermohonanItem item, string status)
    {
        // Data mock tidak memiliki Id database, jadi cukup diperbarui di memori.
        if (item.Id <= 0)
            return;

        var data = (await _permohonanRepo.GetAllAsync())
            .FirstOrDefault(permohonan => permohonan.Id == item.Id);

        if (data is null)
            return;

        data.Status = status;
        data.CatatanHr = item.CatatanHr;
        await _permohonanRepo.SaveAsync(data);
    }

    private void PerbaruiRingkasan()
    {
        TotalPerizinan = _semua.Count.ToString();
        JumlahPending = _semua.Count(item => item.Pending).ToString();
        JumlahDisetujui = _semua.Count(item => item.Approved).ToString();
        JumlahDitolak = _semua.Count(item => item.Rejected).ToString();
    }

    private static IEnumerable<PermohonanItem> BuatDataMock()
    {
        var hariIni = DateTime.Today;
        var budaya = CultureInfo.GetCultureInfo("id-ID");

        string Periode(DateTime mulai, DateTime selesai) =>
            PermohonanItem.FormatRentang(
                mulai.ToString("yyyy-MM-dd"),
                selesai.ToString("yyyy-MM-dd"));

        string Diajukan(DateTime waktu) =>
            waktu.ToString("dd MMM yyyy HH.mm", budaya);

        return new List<PermohonanItem>
        {
            new()
            {
                Id = 0,
                SalesId = 0,
                NamaSales = "Budi Raharjo",
                Jabatan = "Sales Executive",
                Nip = "SLS-011",
                Jenis = "Cuti Tahunan",
                Periode = Periode(hariIni.AddDays(2), hariIni.AddDays(4)),
                Alasan = "Acara keluarga di luar kota.",
                Lampiran = "surat_cuti.pdf",
                Diajukan = Diajukan(hariIni.AddDays(-1).AddHours(9).AddMinutes(20)),
                Status = "Pending"
            },
            new()
            {
                Id = 0,
                SalesId = 0,
                NamaSales = "Ayu Ningsih",
                Jabatan = "Sales Supervisor",
                Nip = "SLS-022",
                Jenis = "Izin Sakit",
                Periode = Periode(hariIni, hariIni),
                Alasan = "Demam dan disarankan istirahat oleh dokter.",
                Lampiran = "surat_dokter.jpg",
                Diajukan = Diajukan(hariIni.AddHours(7).AddMinutes(30)),
                Status = "Pending"
            },
            new()
            {
                Id = 0,
                SalesId = 0,
                NamaSales = "Citra Dewi",
                Jabatan = "Sales Representative",
                Nip = "SLS-015",
                Jenis = "Izin Keperluan Pribadi",
                Periode = Periode(hariIni.AddDays(1), hariIni.AddDays(1)),
                Alasan = "Mengurus administrasi kependudukan.",
                Lampiran = "Tanpa lampiran",
                Diajukan = Diajukan(hariIni.AddHours(8).AddMinutes(15)),
                Status = "Pending"
            },
            new()
            {
                Id = 0,
                SalesId = 0,
                NamaSales = "Doni Kurniawan",
                Jabatan = "Sales Executive",
                Nip = "SLS-017",
                Jenis = "Cuti",
                Periode = Periode(hariIni.AddDays(-5), hariIni.AddDays(-4)),
                Alasan = "Keperluan keluarga.",
                Lampiran = "Tanpa lampiran",
                Diajukan = Diajukan(hariIni.AddDays(-7)),
                Status = "Approved",
                CatatanHr = "Disetujui, pastikan target kunjungan tetap tercapai."
            },
            new()
            {
                Id = 0,
                SalesId = 0,
                NamaSales = "Eka Wijaya",
                Jabatan = "Admin HR",
                Nip = "HR-003",
                Jenis = "Izin Sakit",
                Periode = Periode(hariIni.AddDays(-2), hariIni.AddDays(-2)),
                Alasan = "Kontrol rutin ke dokter.",
                Lampiran = "surat_kontrol.jpg",
                Diajukan = Diajukan(hariIni.AddDays(-3)),
                Status = "Approved",
                CatatanHr = "Disetujui."
            },
            new()
            {
                Id = 0,
                SalesId = 0,
                NamaSales = "Fitri Handayani",
                Jabatan = "Sales Representative",
                Nip = "SLS-024",
                Jenis = "Cuti",
                Periode = Periode(hariIni, hariIni.AddDays(1)),
                Alasan = "Keperluan pribadi mendadak.",
                Lampiran = "Tanpa lampiran",
                Diajukan = Diajukan(hariIni.AddDays(-1).AddHours(16).AddMinutes(5)),
                Status = "Rejected",
                CatatanHr = "Ditolak, jadwal kunjungan padat dan belum ada pengganti."
            }
        };
    }
}

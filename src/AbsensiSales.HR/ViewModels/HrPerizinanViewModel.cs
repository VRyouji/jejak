using System.Collections.ObjectModel;
using System.Globalization;
using AbsensiSales.Core.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.HR.ViewModels;

/// <summary>
/// Halaman perizinan HR &amp; sales. Data dan proses approve/reject
/// seluruhnya lewat API.
/// </summary>
public partial class HrPerizinanViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly List<PermohonanItem> _semua = new();

    public HrPerizinanViewModel(ApiService api)
    {
        _api = api;
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

    [ObservableProperty]
    private string _pesanError = string.Empty;

    [ObservableProperty]
    private bool _adaError;

    public async Task MuatAsync()
    {
        var hasil = await _api.GetPermohonanAsync();

        PesanError = hasil.Sukses ? string.Empty : hasil.Pesan;
        AdaError = PesanError.Length > 0;

        _semua.Clear();
        if (hasil.Data is not null)
        {
            _semua.AddRange(hasil.Data.Select(PermohonanItem.DariApi));
        }

        DaftarPerizinan.Clear();
        foreach (var item in _semua)
        {
            DaftarPerizinan.Add(item);
        }

        PerbaruiRingkasan();
        UpdatePada = $"Diperbarui {DateTime.Now.ToString("dd MMM yyyy HH.mm", CultureInfo.GetCultureInfo("id-ID"))} WIB";
    }

    [RelayCommand]
    private async Task ApproveAsync(PermohonanItem? item)
    {
        if (item is null || !item.Pending)
        {
            return;
        }

        var hasil = await _api.SetujuiPermohonanAsync(
            item.Id,
            setujui: true,
            catatan: "Disetujui oleh HR.");

        if (!hasil.Sukses)
        {
            await Shell.Current.DisplayAlert("Gagal menyetujui", hasil.Pesan, "OK");
            return;
        }

        item.Status = ApiMapper.StatusPermohonan(hasil.Data?.Status ?? "Approved");
        item.CatatanHr = string.IsNullOrWhiteSpace(hasil.Data?.CatatanHr)
            ? "Disetujui oleh HR."
            : hasil.Data!.CatatanHr!;
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
        {
            return;
        }

        var alasan = await Shell.Current.DisplayPromptAsync(
            "Tolak permohonan",
            $"Masukkan alasan penolakan untuk {item.NamaSales}:",
            accept: "Tolak",
            cancel: "Batal",
            placeholder: "Contoh: jadwal kunjungan padat dan belum ada pengganti.",
            maxLength: 250,
            keyboard: Keyboard.Text);

        if (alasan is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(alasan))
        {
            await Shell.Current.DisplayAlert(
                "Alasan wajib diisi",
                "Alasan penolakan tidak boleh kosong.",
                "OK");
            return;
        }

        var hasil = await _api.SetujuiPermohonanAsync(
            item.Id,
            setujui: false,
            catatan: alasan.Trim());

        if (!hasil.Sukses)
        {
            await Shell.Current.DisplayAlert("Gagal menolak", hasil.Pesan, "OK");
            return;
        }

        item.Status = ApiMapper.StatusPermohonan(hasil.Data?.Status ?? "Rejected");
        item.CatatanHr = alasan.Trim();
        PerbaruiRingkasan();

        await Shell.Current.DisplayAlert(
            "Permohonan ditolak",
            $"Permohonan {item.Jenis} dari {item.NamaSales} ditolak.",
            "OK");
    }

    [RelayCommand]
    private async Task SegarkanAsync() => await MuatAsync();

    private void PerbaruiRingkasan()
    {
        TotalPerizinan = _semua.Count.ToString();
        JumlahPending = _semua.Count(item => item.Pending).ToString();
        JumlahDisetujui = _semua.Count(item => item.Approved).ToString();
        JumlahDitolak = _semua.Count(item => item.Rejected).ToString();
    }
}

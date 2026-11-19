using System.Collections.ObjectModel;
using AbsensiSales.Core.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.HR.ViewModels;

public partial class ApprovalViewModel : ObservableObject
{
    private readonly ApiService _api;

    public ApprovalViewModel(ApiService api)
    {
        _api = api;
    }

    public ObservableCollection<PermohonanItem> DaftarPermohonan { get; } = new();

    [ObservableProperty]
    private string _jumlahPending = "0";

    [ObservableProperty]
    private string _pesanError = string.Empty;

    [ObservableProperty]
    private bool _adaError;

    public async Task MuatAsync()
    {
        var hasil = await _api.GetPermohonanAsync();

        PesanError = hasil.Sukses ? string.Empty : hasil.Pesan;
        AdaError = PesanError.Length > 0;

        DaftarPermohonan.Clear();
        foreach (var permohonan in hasil.Data ?? new List<PermohonanDto>())
        {
            DaftarPermohonan.Add(PermohonanItem.DariApi(permohonan));
        }

        JumlahPending = DaftarPermohonan.Count(item => item.Pending).ToString();
    }

    [RelayCommand]
    private async Task SetujuiAsync(PermohonanItem? item)
    {
        if (item is null)
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
        JumlahPending = DaftarPermohonan.Count(permohonan => permohonan.Pending).ToString();

        await Shell.Current.DisplayAlert(
            "Disetujui",
            $"Permohonan {item.Jenis} dari {item.NamaSales} disetujui.",
            "OK");
    }

    [RelayCommand]
    private async Task TolakAsync(PermohonanItem? item)
    {
        if (item is null)
        {
            return;
        }

        var hasil = await _api.SetujuiPermohonanAsync(
            item.Id,
            setujui: false,
            catatan: "Ditolak oleh HR.");

        if (!hasil.Sukses)
        {
            await Shell.Current.DisplayAlert("Gagal menolak", hasil.Pesan, "OK");
            return;
        }

        item.Status = ApiMapper.StatusPermohonan(hasil.Data?.Status ?? "Rejected");
        item.CatatanHr = string.IsNullOrWhiteSpace(hasil.Data?.CatatanHr)
            ? "Ditolak oleh HR."
            : hasil.Data!.CatatanHr!;
        JumlahPending = DaftarPermohonan.Count(permohonan => permohonan.Pending).ToString();

        await Shell.Current.DisplayAlert(
            "Ditolak",
            $"Permohonan {item.Jenis} dari {item.NamaSales} ditolak.",
            "OK");
    }
}

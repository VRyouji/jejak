using System.Collections.ObjectModel;
using AbsensiSales.Core.Api;
using AbsensiSales.Core.Services;
using AbsensiSales.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.Mobile.ViewModels;

public partial class StatusPermohonanViewModel : ObservableObject
{
    private readonly SessionService _session;
    private readonly ApiService _api;
    private readonly NotificationService _notif;

    public StatusPermohonanViewModel(
        SessionService session,
        ApiService api,
        NotificationService notif)
    {
        _session = session;
        _api = api;
        _notif = notif;
    }

    public ObservableCollection<PermohonanItemViewModel> DaftarPermohonan { get; } = new();

    [ObservableProperty]
    private string _infoSales = string.Empty;

    [ObservableProperty]
    private bool _kosong;

    public async void OnAppearing()
    {
        var sales = _session.SalesAktif;
        if (sales is null)
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        InfoSales = $"{sales.Nama} - {sales.Nip}";

        var hasil = await _api.GetPermohonanAsync();
        if (!hasil.Sukses)
        {
            DaftarPermohonan.Clear();
            Kosong = true;
            await _notif.ShowErrorAsync(hasil.Pesan);
            return;
        }

        DaftarPermohonan.Clear();
        foreach (var p in hasil.Data ?? new List<PermohonanDto>())
        {
            var periode = string.Equals(p.TanggalMulai, p.TanggalSelesai)
                ? ApiMapper.TanggalApi(p.TanggalMulai)
                : $"{ApiMapper.TanggalApi(p.TanggalMulai)} - {ApiMapper.TanggalApi(p.TanggalSelesai)}";

            DaftarPermohonan.Add(new PermohonanItemViewModel
            {
                Jenis = p.Jenis,
                Periode = periode,
                Alasan = string.IsNullOrWhiteSpace(p.Alasan) ? "-" : p.Alasan!,
                Status = ApiMapper.StatusPermohonan(p.Status),
                Diajukan = ApiMapper.TanggalJam(p.CreatedAt)
            });
        }

        Kosong = DaftarPermohonan.Count == 0;
    }

    [RelayCommand]
    private async Task AjukanBaruAsync() => await Shell.Current.GoToAsync("pengajuan-izin");
}

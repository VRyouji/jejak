using AbsensiSales.Core.Api;
using AbsensiSales.Core.Services;
using AbsensiSales.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.Mobile.ViewModels;

public partial class PengajuanIzinViewModel : ObservableObject
{
    private readonly SessionService _session;
    private readonly ApiService _api;
    private readonly NotificationService _notif;

    public PengajuanIzinViewModel(
        SessionService session,
        ApiService api,
        NotificationService notif)
    {
        _session = session;
        _api = api;
        _notif = notif;
    }

    public string[] JenisPermohonan { get; } = { "Cuti", "Izin Sakit" };

    [ObservableProperty]
    private string _jenis = "Cuti";

    [ObservableProperty]
    private DateTime _tanggalMulai = DateTime.Today;

    [ObservableProperty]
    private DateTime _tanggalSelesai = DateTime.Today;

    [ObservableProperty]
    private string _alasan = string.Empty;

    [ObservableProperty]
    private string _lampiran = string.Empty;

    [ObservableProperty]
    private bool _sedangMemproses;

    [RelayCommand]
    private void PilihCuti() => Jenis = "Cuti";

    [RelayCommand]
    private void PilihSakit() => Jenis = "Izin Sakit";

    [RelayCommand]
    private async Task KirimAsync()
    {
        if (SedangMemproses)
        {
            return;
        }

        if (_session.SalesAktif is null)
        {
            await _notif.ShowErrorAsync("Sesi login tidak ditemukan. Silakan masuk kembali.");
            return;
        }

        if (TanggalSelesai < TanggalMulai)
        {
            await _notif.ShowErrorAsync("Tanggal selesai tidak boleh sebelum tanggal mulai.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Alasan))
        {
            await _notif.ShowErrorAsync("Alasan wajib diisi.");
            return;
        }

        SedangMemproses = true;
        try
        {
            var hasil = await _api.AjukanPermohonanAsync(new PermohonanRequest
            {
                Jenis = Jenis,
                TanggalMulai = TanggalMulai.ToString("yyyy-MM-dd"),
                TanggalSelesai = TanggalSelesai.ToString("yyyy-MM-dd"),
                Alasan = Alasan.Trim(),
                Lampiran = string.IsNullOrWhiteSpace(Lampiran) ? null : Lampiran.Trim()
            });

            if (!hasil.Sukses)
            {
                await _notif.ShowErrorAsync(hasil.Pesan);
                return;
            }

            await Shell.Current.GoToAsync("..");
            await _notif.ShowSuccessAsync("Permohonan terkirim dan menunggu persetujuan HR.");
        }
        finally
        {
            SedangMemproses = false;
        }
    }

    [RelayCommand]
    private async Task BatalAsync() => await Shell.Current.GoToAsync("..");
}

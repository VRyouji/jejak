using AbsensiSales.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsensiSales.Mobile.ViewModels;

public partial class FotoTokoViewModel : ObservableObject
{
    private readonly AbsensiFlowState _flow;
    private readonly NotificationService _notif;

    public FotoTokoViewModel(AbsensiFlowState flow, NotificationService notif)
    {
        _flow = flow;
        _notif = notif;
    }

    [ObservableProperty]
    private string _labelToko = string.Empty;

    [ObservableProperty]
    private ImageSource? _pratinjauFoto;

    [ObservableProperty]
    private bool _adaFoto;

    [ObservableProperty]
    private bool _fotoSiap;

    public void OnAppearing()
    {
        LabelToko = _flow.TokoTerpilih is null
            ? "Toko"
            : $"{_flow.TokoTerpilih.KodeToko} - {_flow.TokoTerpilih.NamaToko}";

        FotoSiap = _flow.FotoBukti is { Length: > 0 };
        PerbaruiPratinjau();
    }

    [RelayCommand]
    private async Task AmbilFotoAsync()
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            await _notif.ShowErrorAsync(
                "Kamera tidak tersedia di perangkat ini. Foto bukti wajib diambil dari kamera.");
            return;
        }

        if (!await PastikanIzinKameraAsync())
        {
            await _notif.ShowErrorAsync("Izin kamera belum aktif. Buka pengaturan aplikasi untuk mengizinkan kamera.");
            return;
        }

        FileResult? file;
        try
        {
            file = await MediaPicker.Default.CapturePhotoAsync();
        }
        catch (Exception)
        {
            await _notif.ShowErrorAsync("Kamera gagal dibuka. Silakan coba lagi.");
            return;
        }

        if (file is null)
        {
            await _notif.ShowInfoAsync("Pengambilan foto dibatalkan.");
            return;
        }

        try
        {
            using var stream = await file.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            _flow.FotoBukti = ms.ToArray();
        }
        catch (Exception)
        {
            await _notif.ShowErrorAsync("Foto gagal dibaca. Silakan ambil ulang.");
            return;
        }

        _flow.WaktuFoto = DateTime.Now.ToString("HH:mm:ss");
        FotoSiap = true;
        PerbaruiPratinjau();

        await _notif.ShowSuccessAsync("Foto bukti kunjungan siap.");
    }

    /// <summary>
    /// Pastikan izin kamera sudah diberikan sebelum membuka aplikasi kamera.
    /// </summary>
    private static async Task<bool> PastikanIzinKameraAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            return status == PermissionStatus.Granted;
        }
        catch (FeatureNotSupportedException)
        {
            // Platform tanpa API izin kamera: biarkan MediaPicker mencoba.
            return true;
        }
        catch
        {
            return true;
        }
    }

    private void PerbaruiPratinjau()
    {
        if (_flow.FotoBukti is { Length: > 0 } data)
        {
            PratinjauFoto = ImageSource.FromStream(() => new MemoryStream(data));
            AdaFoto = true;
        }
        else
        {
            PratinjauFoto = null;
            AdaFoto = false;
        }
    }

    [RelayCommand]
    private async Task GunakanFotoAsync()
    {
        if (!FotoSiap)
        {
            await _notif.ShowErrorAsync("Ambil foto bukti kunjungan terlebih dahulu.");
            return;
        }

        await Shell.Current.GoToAsync("verifikasi-data");
    }
}

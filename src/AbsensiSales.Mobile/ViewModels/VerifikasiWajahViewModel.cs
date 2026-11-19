using AbsensiSales.Core.Services;
using AbsensiSales.Face;
using AbsensiSales.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AbsensiSales.Mobile.ViewModels;

/// <summary>
/// Halaman verifikasi wajah berjalan otomatis: saat halaman dibuka, izin kamera
/// diminta sekali lalu pemindaian langsung dimulai tanpa tombol tambahan.
/// Satu pemindaian yang cocok langsung melanjutkan ke tahap foto toko.
/// </summary>
public partial class VerifikasiWajahViewModel : ObservableObject
{
    private readonly SessionService _session;
    private readonly FaceService _face;
    private readonly AbsensiFlowState _flow;
    private readonly NotificationService _notif;

    private CancellationTokenSource? _batal;
    private bool _sedangBerjalan;

    public VerifikasiWajahViewModel(
        SessionService session,
        FaceService face,
        AbsensiFlowState flow,
        NotificationService notif)
    {
        _session = session;
        _face = face;
        _flow = flow;
        _notif = notif;
    }

    [ObservableProperty]
    private string _namaToko = string.Empty;

    [ObservableProperty]
    private string _judulStatus = "Menyiapkan kamera...";

    [ObservableProperty]
    private string _pesanStatus = "Izinkan akses kamera saat perangkat meminta.";

    [ObservableProperty]
    private bool _sedangMemindai;

    [ObservableProperty]
    private bool _terverifikasi;

    [ObservableProperty]
    private bool _izinDitolak;

    /// <summary>
    /// Dipanggil setiap halaman tampil (termasuk saat aplikasi kembali dari
    /// pengaturan izin). Bila pemindaian belum berjalan, langsung mulai.
    /// </summary>
    public void OnAppearing()
    {
        NamaToko = _flow.TokoTerpilih?.NamaToko ?? "Toko";

        if (_sedangBerjalan)
        {
            return;
        }

        _sedangBerjalan = true;
        _batal = new CancellationTokenSource();
        _ = MulaiVerifikasiAsync(_batal.Token);
    }

    public void OnDisappearing()
    {
        _batal?.Cancel();
        _batal?.Dispose();
        _batal = null;
        _sedangBerjalan = false;
        SedangMemindai = false;
    }

    private async Task MulaiVerifikasiAsync(CancellationToken token)
    {
        try
        {
            var sales = _session.SalesAktif;
            if (sales is null)
            {
                JudulStatus = "Sesi tidak ditemukan";
                PesanStatus = "Silakan masuk kembali untuk memindai wajah.";
                return;
            }

            // Minta izin kamera satu kali; pengguna bisa mengubahnya kapan saja
            // lewat pengaturan aplikasi di perangkat masing-masing.
            JudulStatus = "Meminta izin kamera...";
            PesanStatus = "Pilih Izinkan agar kamera bisa memindai wajah.";

            var diizinkan = await MintaIzinKameraAsync();
            token.ThrowIfCancellationRequested();

            if (!diizinkan)
            {
                IzinDitolak = true;
                JudulStatus = "Kamera belum diizinkan";
                PesanStatus = "Buka Pengaturan > Aplikasi > Jejak Sales > Izin, lalu aktifkan Kamera. Pemindaian akan mulai otomatis saat Anda kembali.";
                await _notif.ShowErrorAsync("Aktifkan izin kamera di pengaturan aplikasi.");
                return;
            }

            IzinDitolak = false;
            await Task.Delay(350, token);

            while (!token.IsCancellationRequested && !Terverifikasi)
            {
                SedangMemindai = true;
                JudulStatus = "Memindai wajah...";
                PesanStatus = "Tetap di dalam bingkai dan lihat ke arah kamera.";

                FaceVerifyResult hasil;
                try
                {
                    hasil = await Task.Run(() => _face.Verifikasi(sales.Nama), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    hasil = new FaceVerifyResult(false, 0, "Kamera belum bisa dibaca. Pemindaian diulang otomatis.", true);
                }

                if (token.IsCancellationRequested)
                {
                    break;
                }

                SedangMemindai = false;

                if (hasil.Cocok)
                {
                    Terverifikasi = true;
                    JudulStatus = "Wajah terverifikasi";
                    PesanStatus = $"Kecocokan {(hasil.Skor * 100):F0}% - melanjutkan ke foto toko...";

                    _flow.SkorWajah = hasil.Skor;
                    _flow.WaktuVerifikasiWajah = DateTime.Now.ToString("dd MMM yyyy - HH.mm.ss 'WIB'");

                    await _notif.ShowSuccessAsync("Wajah terverifikasi.");
                    await Task.Delay(1200, token);

                    await Shell.Current.GoToAsync("foto-toko");
                    return;
                }

                JudulStatus = "Wajah belum terbaca";
                PesanStatus = $"{hasil.Pesan} Pemindaian diulang otomatis.";
                await Task.Delay(2200, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Halaman ditinggalkan sebelum pemindaian selesai.
        }
        finally
        {
            SedangMemindai = false;
            _sedangBerjalan = false;
        }
    }

    private static async Task<bool> MintaIzinKameraAsync()
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
            // Sebagian platform (mis. desktop Windows) tidak punya dialog izin
            // kamera. Biarkan pemindaian berjalan memakai webcam yang tersedia.
            return true;
        }
        catch
        {
            return true;
        }
    }
}

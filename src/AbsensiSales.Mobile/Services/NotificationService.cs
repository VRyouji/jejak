using AbsensiSales.Mobile.Controls;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace AbsensiSales.Mobile.Services;

/// <summary>
/// Notifikasi singkat untuk pesan berhasil/gagal/info.
/// Tampil sesaat lalu hilang otomatis, tanpa dialog modal dan tanpa blok permanen.
/// Memakai <see cref="NotificationHost"/> pada halaman aktif agar pasti terlihat
/// di semua platform (termasuk Windows unpackaged). Native toast hanya dipakai
/// sebagai cadangan bila halaman belum memiliki host.
/// </summary>
public class NotificationService
{
    public Task ShowSuccessAsync(string message) => ShowAsync(message, NotificationKind.Success);

    public Task ShowErrorAsync(string message) => ShowAsync(message, NotificationKind.Error);

    public Task ShowInfoAsync(string message) => ShowAsync(message, NotificationKind.Info);

    private static async Task ShowAsync(string message, NotificationKind kind)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var host = CariHost();
        if (host is not null)
        {
            try
            {
                if (MainThread.IsMainThread)
                {
                    await host.ShowAsync(message, kind);
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() => host.ShowAsync(message, kind));
                }

                return;
            }
            catch
            {
                // Jatuh ke native toast bila overlay gagal.
            }
        }

        try
        {
            var toast = Toast.Make(
                message,
                ToastDuration.Short,
                textSize: 14);

            await toast.Show();
        }
        catch
        {
            // Notifikasi tidak boleh menghentikan alur utama aplikasi.
        }
    }

    private static NotificationHost? CariHost()
    {
        var halaman = Shell.Current?.CurrentPage
            ?? Application.Current?.Windows.FirstOrDefault()?.Page;

        return halaman is null ? null : CariDi(halaman);
    }

    private static NotificationHost? CariDi(IVisualTreeElement elemen)
    {
        if (elemen is NotificationHost host)
        {
            return host;
        }

        foreach (var anak in elemen.GetVisualChildren())
        {
            if (anak is not null && CariDi(anak) is { } ditemukan)
            {
                return ditemukan;
            }
        }

        return null;
    }
}

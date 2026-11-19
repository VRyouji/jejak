namespace AbsensiSales.Mobile.Controls;

/// <summary>Jenis notifikasi sesaat yang ditampilkan <see cref="NotificationHost"/>.</summary>
public enum NotificationKind
{
    Success,
    Error,
    Info
}

/// <summary>
/// Overlay notifikasi sesaat milik satu halaman. Letakkan sebagai anak terakhir
/// sebuah Grid agar tampil di atas konten, lalu panggil lewat
/// <c>NotificationService</c>. Notifikasi memudar masuk, bertahan sebentar,
/// lalu memudar keluar sendiri tanpa dialog modal.
/// </summary>
public partial class NotificationHost : ContentView
{
    private static readonly Color SuccessSolid = Color.FromArgb("#2F7D53");
    private static readonly Color ErrorSolid = Color.FromArgb("#C6402F");
    private static readonly Color InfoSolid = Color.FromArgb("#10243D");
    private static readonly Color OnSolid = Color.FromArgb("#FFFFFF");
    private static readonly Color OnSolidMuted = Color.FromArgb("#F4F8FF");
    private static readonly Color IconBackdrop = Color.FromArgb("#33FFFFFF");

    private CancellationTokenSource? _pembatal;

    public NotificationHost()
    {
        InitializeComponent();
    }

    /// <summary>Tampilkan pesan sesaat. Pesan baru menggantikan notifikasi yang sedang tampil.</summary>
    public async Task ShowAsync(string message, NotificationKind kind)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _pembatal?.Cancel();
        var pembatal = new CancellationTokenSource();
        _pembatal = pembatal;

        LabelPesan.Text = message;
        ApplyTone(kind);

        this.AbortAnimation("notifikasi");
        IsVisible = true;
        Opacity = 0;

        await this.FadeToAsync(1, 180, Easing.CubicOut);

        if (pembatal.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await Task.Delay(2400, pembatal.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        await this.FadeToAsync(0, 220, Easing.CubicIn);

        if (!pembatal.IsCancellationRequested)
        {
            IsVisible = false;
        }
    }

    private void ApplyTone(NotificationKind kind)
    {
        var (surface, ikon) = kind switch
        {
            NotificationKind.Error => (ErrorSolid, "!"),
            NotificationKind.Info => (InfoSolid, "i"),
            _ => (SuccessSolid, "OK")
        };

        Kartu.BackgroundColor = surface;
        KotakIkon.BackgroundColor = IconBackdrop;
        LabelIkon.Text = ikon;
        LabelIkon.TextColor = OnSolid;
        LabelPesan.TextColor = kind == NotificationKind.Info ? OnSolidMuted : OnSolid;
    }
}

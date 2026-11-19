namespace AbsensiSales.Mobile.Controls;

/// <summary>
/// Tombol kembali kecil di kiri atas halaman. Memakai riwayat navigasi Shell;
/// bila tidak ada riwayat (mis. halaman tab utama) memakai <see cref="FallbackRoute"/>.
/// </summary>
public partial class BackButton : ContentView
{
    public static readonly BindableProperty FallbackRouteProperty = BindableProperty.Create(
        nameof(FallbackRoute),
        typeof(string),
        typeof(BackButton),
        string.Empty);

    public string FallbackRoute
    {
        get => (string)GetValue(FallbackRouteProperty);
        set => SetValue(FallbackRouteProperty, value);
    }

    public BackButton()
    {
        InitializeComponent();
    }

    private async void OnClicked(object? sender, EventArgs e)
    {
        try
        {
            var shell = Shell.Current;
            if (shell is null)
            {
                return;
            }

            if (shell.Navigation.NavigationStack.Count > 1)
            {
                await shell.GoToAsync("..");
                return;
            }

            if (!string.IsNullOrWhiteSpace(FallbackRoute))
            {
                await shell.GoToAsync(FallbackRoute);
            }
        }
        catch
        {
            // Navigasi kembali tidak boleh menghentikan aplikasi.
        }
    }
}

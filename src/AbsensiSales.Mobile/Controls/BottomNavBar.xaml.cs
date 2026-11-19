using Microsoft.Maui.Graphics;
using MauiPath = Microsoft.Maui.Controls.Shapes.Path;

namespace AbsensiSales.Mobile.Controls;

/// <summary>
/// Navbar bawah aplikasi. Setiap halaman utama cukup memanggil elemen ini
/// dan menentukan tab aktifnya, misal <c>ActiveTab="absensi"</c>.
/// </summary>
public partial class BottomNavBar : ContentView
{
    public const string TabAbsensi = "absensi";
    public const string TabLaporan = "laporan";

    private static readonly Color ActiveSurface = Color.FromArgb("#FED766");
    private static readonly Color ActiveContent = Color.FromArgb("#10243D");
    private static readonly Color InactiveContent = Color.FromArgb("#FFFFFF");

    public static readonly BindableProperty ActiveTabProperty = BindableProperty.Create(
        nameof(ActiveTab),
        typeof(string),
        typeof(BottomNavBar),
        TabAbsensi,
        propertyChanged: OnActiveTabChanged);

    public string ActiveTab
    {
        get => (string)GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }

    public BottomNavBar()
    {
        InitializeComponent();
        ApplyActiveState();
    }

    private static void OnActiveTabChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is BottomNavBar navBar)
        {
            navBar.ApplyActiveState();
        }
    }

    private void ApplyActiveState()
    {
        bool absensiActive = string.Equals(ActiveTab, TabAbsensi, StringComparison.OrdinalIgnoreCase);

        SetItemState(
            AbsensiItem, AbsensiIcon, AbsensiLabel, absensiActive);

        SetItemState(
            LaporanItem, LaporanIcon, LaporanLabel, !absensiActive);
    }

    private static void SetItemState(Border item, MauiPath icon, Label label, bool isActive)
    {
        item.BackgroundColor = isActive ? ActiveSurface : Colors.Transparent;
        icon.Fill = new SolidColorBrush(isActive ? ActiveContent : InactiveContent);
        label.TextColor = isActive ? ActiveContent : InactiveContent;
    }

    private async void OnAbsensiTapped(object? sender, TappedEventArgs e)
    {
        if (string.Equals(ActiveTab, TabAbsensi, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await Shell.Current.GoToAsync("//utama/absensi");
    }

    private async void OnLaporanTapped(object? sender, TappedEventArgs e)
    {
        if (string.Equals(ActiveTab, TabLaporan, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await Shell.Current.GoToAsync("//utama/laporan");
    }
}

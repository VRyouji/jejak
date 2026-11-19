using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace AbsensiSales.Mobile.Controls;

/// <summary>
/// Varian warna solid yang tersedia untuk <see cref="SolidCard"/>.
/// Cukup atur dari XAML: <c>Tone="Yellow"</c>, <c>Tone="White"</c>, atau <c>Tone="Navy"</c>.
/// </summary>
public enum SolidTone
{
    White,
    Yellow,
    Navy
}

/// <summary>
/// Satu elemen kartu/container solid tanpa outline.
/// Warna diatur lewat properti <see cref="Tone"/> sehingga halaman front-end
/// tinggal memanggil elemen ini tanpa mendefinisikan Border berulang-ulang.
/// </summary>
public class SolidCard : Border
{
    private static readonly Color WhiteSolid = Color.FromArgb("#FFFFFF");
    private static readonly Color YellowSolid = Color.FromArgb("#FFF3C4");
    private static readonly Color NavySolid = Color.FromArgb("#10243D");
    private static readonly Color ShadowSolid = Color.FromArgb("#10243D");

    public static readonly BindableProperty ToneProperty = BindableProperty.Create(
        nameof(Tone),
        typeof(SolidTone),
        typeof(SolidCard),
        SolidTone.White,
        propertyChanged: OnToneChanged);

    public SolidTone Tone
    {
        get => (SolidTone)GetValue(ToneProperty);
        set => SetValue(ToneProperty, value);
    }

    public SolidCard()
    {
        StrokeThickness = 0;
        Padding = new Thickness(16);
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) };
        ApplyTone();
    }

    private static void OnToneChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SolidCard card)
        {
            card.ApplyTone();
        }
    }

    private void ApplyTone()
    {
        BackgroundColor = Tone switch
        {
            SolidTone.Yellow => YellowSolid,
            SolidTone.Navy => NavySolid,
            _ => WhiteSolid
        };

        // Kartu putih tetap tanpa outline; diberi shadow sangat lembut agar
        // masih terlihat sebagai container di atas background putih.
        Shadow = Tone == SolidTone.White
            ? new Shadow
            {
                Brush = new SolidColorBrush(ShadowSolid),
                Opacity = 0.06f,
                Radius = 18,
                Offset = new Point(0, 6)
            }
            : new Shadow { Opacity = 0 };
    }
}

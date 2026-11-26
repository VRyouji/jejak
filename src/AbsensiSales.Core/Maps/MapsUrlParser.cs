using System.Globalization;
using System.Text.RegularExpressions;

namespace AbsensiSales.Core.Maps;

/// <summary>Pasangan koordinat hasil pembacaan link Google Maps.</summary>
public readonly record struct Koordinat(double Latitude, double Longitude);

/// <summary>
/// Membaca koordinat dari teks (tanpa akses internet).
///
/// Format yang dikenali, berurutan dari yang paling akurat:
///   1. ...!3d-6.2087634!4d106.8455990...   -> posisi PIN sebenarnya
///   2. /maps/place/-6.2087634,106.8455990  -> pin yang dijatuhkan (dropped pin)
///   3. ?q=-6.2087634,106.8455990           -> juga query / ll / destination
///   4. /@-6.2087634,106.8455990,17z        -> titik tengah tampilan peta (cadangan)
///   5. -6.2087634, 106.8455990             -> koordinat polos (hasil klik kanan di Google Maps)
///
/// Link pendek (maps.app.goo.gl) TIDAK bisa dibaca di sini karena isinya
/// hanya kode; gunakan <see cref="MapsLinkResolver"/> untuk mengikuti redirect-nya.
/// </summary>
public static class MapsUrlParser
{
    private const string Angka = @"-?\d{1,3}(?:\.\d+)?";
    private const RegexOptions Opsi = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;

    // Urutan array = urutan prioritas.
    private static readonly Regex[] PolaLink =
    {
        new($@"!3d({Angka})!4d({Angka})", Opsi),
        new($@"/maps/place/\s*({Angka})\s*,\s*({Angka})(?=[/?@&]|$)", Opsi),
        new($@"[?&](?:q|query|ll|destination|daddr|center)=\s*({Angka})\s*,\s*({Angka})(?=[&#]|$)", Opsi),
        new($@"@({Angka}),({Angka})", Opsi),
    };

    private static readonly Regex PolaPolos =
        new($@"^\s*\(?\s*({Angka})\s*[,;]\s*({Angka})\s*\)?\s*$", Opsi);

    /// <summary>Coba baca koordinat dari link atau teks koordinat.</summary>
    public static bool TryParse(string? teks, out Koordinat koordinat)
    {
        koordinat = default;

        if (string.IsNullOrWhiteSpace(teks))
        {
            return false;
        }

        var bersih = teks.Trim();

        // %2C -> ',' , %21 -> '!' dst., supaya pola di atas cocok.
        try
        {
            bersih = Uri.UnescapeDataString(bersih);
        }
        catch (UriFormatException)
        {
            // Biarkan apa adanya kalau ada escape yang rusak.
        }

        bersih = bersih.Replace('+', ' ');

        var polos = PolaPolos.Match(bersih);
        if (polos.Success && TryBuat(polos.Groups[1].Value, polos.Groups[2].Value, out koordinat))
        {
            return true;
        }

        foreach (var pola in PolaLink)
        {
            var cocok = pola.Match(bersih);
            if (cocok.Success && TryBuat(cocok.Groups[1].Value, cocok.Groups[2].Value, out koordinat))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryBuat(string lat, string lng, out Koordinat koordinat)
    {
        koordinat = default;

        if (!double.TryParse(lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var y)
            || !double.TryParse(lng, NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
        {
            return false;
        }

        if (y is < -90 or > 90 || x is < -180 or > 180)
        {
            return false;
        }

        // (0,0) hampir pasti bukan toko sungguhan, melainkan hasil parse yang gagal.
        if (y == 0 && x == 0)
        {
            return false;
        }

        koordinat = new Koordinat(y, x);
        return true;
    }
}

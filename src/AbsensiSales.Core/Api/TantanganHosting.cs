using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace AbsensiSales.Core.Api;

/// <summary>
/// Sebagian hosting gratis (InfinityFree/iFastNet) memasang "JavaScript challenge":
/// request dari aplikasi dibalas halaman HTML berisi slowAES + cookie <c>__test</c>,
/// bukan JSON. Kelas ini menghitung cookie tersebut supaya <see cref="ApiService"/>
/// bisa mengulang request dan mendapatkan respons JSON asli.
///
/// Halaman tantangan berisi potongan seperti:
///   var a=toNumbers("f655..."),b=toNumbers("9834..."),c=toNumbers("085c...");
///   document.cookie="__test="+toHex(slowAES.decrypt(c,2,a,b))+...
///
/// Mode slowAES: 0 = OFB, 1 = CFB, 2 = CBC. Deskripsi resmi memakai CBC.
/// </summary>
public static partial class TantanganHosting
{
    [GeneratedRegex(
        "(?<nama>[A-Za-z_$][\\w$]*)\\s*=\\s*toNumbers\\(\\s*\"(?<hex>[0-9a-fA-F]+)\"\\s*\\)",
        RegexOptions.CultureInvariant)]
    private static partial Regex PolaNilai();

    [GeneratedRegex(
        "slowAES\\.decrypt\\(\\s*(?<c>[A-Za-z_$][\\w$]*)\\s*,\\s*(?<mode>\\d+)\\s*,\\s*(?<kunci>[A-Za-z_$][\\w$]*)\\s*,\\s*(?<iv>[A-Za-z_$][\\w$]*)\\s*\\)",
        RegexOptions.CultureInvariant)]
    private static partial Regex PolaDekripsi();

    /// <summary>True kalau teks respons adalah halaman tantangan JavaScript hosting.</summary>
    public static bool Terdeteksi(string? html) =>
        !string.IsNullOrEmpty(html) &&
        html.Contains("slowAES", StringComparison.Ordinal) &&
        html.Contains("toNumbers", StringComparison.Ordinal);

    /// <summary>
    /// Menghitung nilai cookie <c>__test</c> dari halaman tantangan.
    /// Mengembalikan false kalau format halaman tidak dikenali.
    /// </summary>
    public static bool CobaSelesaikan(string html, out string cookie)
    {
        cookie = string.Empty;

        if (!Terdeteksi(html))
        {
            return false;
        }

        var nilai = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match m in PolaNilai().Matches(html))
        {
            nilai[m.Groups["nama"].Value] = m.Groups["hex"].Value;
        }

        var dekripsi = PolaDekripsi().Match(html);
        if (!dekripsi.Success)
        {
            return false;
        }

        if (!nilai.TryGetValue(dekripsi.Groups["c"].Value, out var hexCipher) ||
            !nilai.TryGetValue(dekripsi.Groups["kunci"].Value, out var hexKunci) ||
            !nilai.TryGetValue(dekripsi.Groups["iv"].Value, out var hexIv) ||
            !int.TryParse(dekripsi.Groups["mode"].Value, out var mode))
        {
            return false;
        }

        var cipher = HexKeByte(hexCipher);
        var kunci = HexKeByte(hexKunci);
        var iv = HexKeByte(hexIv);

        if (kunci.Length is not (16 or 24 or 32) ||
            iv.Length != 16 ||
            cipher.Length == 0 ||
            cipher.Length % 16 != 0)
        {
            return false;
        }

        byte[] polos;
        try
        {
            using var aes = Aes.Create();
            aes.Key = kunci;
            aes.IV = iv;

            switch (mode)
            {
                case 2: // CBC
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.None;
                    break;

                case 1: // CFB, slowAES memakai feedback 128 bit
                    aes.Mode = CipherMode.CFB;
                    aes.FeedbackSize = 128;
                    aes.Padding = PaddingMode.None;
                    break;

                default: // OFB tidak didukung System.Security.Cryptography
                    return false;
            }

            using var dekriptor = aes.CreateDecryptor();
            polos = dekriptor.TransformFinalBlock(cipher, 0, cipher.Length);
        }
        catch (CryptographicException)
        {
            return false;
        }

        // slowAES hanya membuang padding kalau hasilnya lebih dari 16 byte.
        if (polos.Length > 16)
        {
            var pad = polos[^1];
            if (pad is > 0 and <= 16 && pad <= polos.Length)
            {
                var cocok = true;
                for (var i = polos.Length - pad; i < polos.Length; i++)
                {
                    if (polos[i] != pad)
                    {
                        cocok = false;
                        break;
                    }
                }

                if (cocok)
                {
                    Array.Resize(ref polos, polos.Length - pad);
                }
            }
        }

        cookie = Convert.ToHexString(polos).ToLowerInvariant();
        return cookie.Length > 0;
    }

    private static byte[] HexKeByte(string hex)
    {
        var hasil = new byte[hex.Length / 2];
        for (var i = 0; i < hasil.Length; i++)
        {
            hasil[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }

        return hasil;
    }
}

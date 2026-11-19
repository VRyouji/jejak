namespace AbsensiSales.Core.Api;
public static class ApiConfig
{
    /// <summary>
    /// URL dasar API, tanpa garis miring di akhir.
    /// Di hosting ini file API di-upload ke root htdocs, jadi tanpa "/api".
    /// Kalau nanti dipindah ke folder htdocs/api, ubah menjadi ".../api".
    /// </summary>
    public const string DefaultBaseUrl = "https://jejaksales.site.je";

    /// <summary>Bisa diubah saat runtime (mis. untuk pengujian).</summary>
    public static string BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>True kalau URL masih placeholder bawaan.</summary>
    public static bool BelumDikonfigurasi =>
        (BaseUrl ?? string.Empty).Contains("GANTI-DOMAIN", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gabungkan BaseUrl dengan path endpoint, mis. "login.php".</summary>
    public static string Url(string path)
    {
        var dasar = (BaseUrl ?? string.Empty).TrimEnd('/');
        var bersih = (path ?? string.Empty).TrimStart('/');
        return $"{dasar}/{bersih}";
    }
}

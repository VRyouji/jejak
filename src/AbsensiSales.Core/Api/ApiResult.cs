namespace AbsensiSales.Core.Api;

/// <summary>
/// Hasil pemanggilan API. Selalu dikembalikan (tidak melempar exception),
/// supaya UI bisa menampilkan pesan yang ramah.
/// </summary>
public sealed class ApiResult<T>
{
    public bool Sukses { get; init; }

    /// <summary>Pesan dari server atau pesan kesalahan lokal.</summary>
    public string Pesan { get; init; } = string.Empty;

    /// <summary>Kode error dari PHP, mis. TOKEN_INVALID, ALREADY_CHECKED_IN.</summary>
    public string? Kode { get; init; }

    /// <summary>HTTP status (0 kalau tidak ada response / jaringan gagal).</summary>
    public int StatusCode { get; init; }

    public T? Data { get; init; }

    /// <summary>
    /// False kalau perangkat tidak bisa menghubungi server sama sekali
    /// (salah URL, tidak ada internet, server mati).
    /// </summary>
    public bool Terhubung { get; init; } = true;

    public static ApiResult<T> Berhasil(T? data, string pesan = "OK", int statusCode = 200) => new()
    {
        Sukses = true,
        Pesan = pesan,
        StatusCode = statusCode,
        Data = data
    };

    public static ApiResult<T> Gagal(string pesan, string? kode = null, int statusCode = 0) => new()
    {
        Sukses = false,
        Pesan = pesan,
        Kode = kode,
        StatusCode = statusCode
    };

    public static ApiResult<T> TidakTerhubung(string pesan) => new()
    {
        Sukses = false,
        Pesan = pesan,
        Kode = "TIDAK_TERHUBUNG",
        StatusCode = 0,
        Terhubung = false
    };
}

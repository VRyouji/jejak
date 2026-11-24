using SQLite;

namespace AbsensiSales.Core.Models;

public class Sales
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Nip { get; set; } = string.Empty;
    public string Nama { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Wilayah { get; set; } = string.Empty;
    public string FotoReferensi { get; set; } = string.Empty;

    /// <summary>Template wajah Base64 dari backend (tbl_sales.face_data).</summary>
    public string FaceData { get; set; } = string.Empty;

    [Ignore]
    public string Inisial =>
        string.IsNullOrWhiteSpace(Nama)
            ? "?"
            : string.Concat(Nama.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Select(p => char.ToUpperInvariant(p[0])));
}

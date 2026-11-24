namespace AbsensiSales.Face;

/// <summary>
/// Hasil pembuatan template wajah dari satu foto. Template null berarti
/// proses gagal; pesan berisi alasan yang bisa ditampilkan ke pengguna.
/// </summary>
public sealed record FaceTemplateResult(
    string? Template,
    int JumlahWajah,
    string Pesan,
    bool Simulasi = false)
{
    public bool Berhasil => !string.IsNullOrWhiteSpace(Template) && !Simulasi;
}

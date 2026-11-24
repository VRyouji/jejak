namespace AbsensiSales.Face;

public record FaceVerifyResult(bool Cocok, double Skor, string Pesan, bool Simulasi)
{
    /// <summary>Jumlah wajah yang terdeteksi pada foto verifikasi.</summary>
    public int JumlahWajah { get; init; }
}

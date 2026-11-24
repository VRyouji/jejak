namespace AbsensiSales.Face;

/// <summary>
/// Konfigurasi engine face recognition. Semua angka yang sebelumnya
/// ditulis terpisah di beberapa file dikumpulkan di sini agar konsisten.
/// </summary>
public sealed class FaceOptions
{
    /// <summary>Batas cosine similarity agar wajah dianggap cocok.</summary>
    public double MatchThreshold { get; init; } = Core.Services.FaceScore.Threshold;

    /// <summary>Jumlah wajah yang boleh ada dalam satu foto.</summary>
    public int MaxFacesAllowed { get; init; } = 1;

    /// <summary>Ukuran minimum sisi wajah dalam piksel gambar asli.</summary>
    public int MinFaceSize { get; init; } = 80;

    /// <summary>Skor minimum detektor UltraFace sebelum kotak wajah diterima.</summary>
    public float MinDetectionScore { get; init; } = 0.7f;

    /// <summary>Batas bawah rata-rata kecerahan wajah (0-255).</summary>
    public double MinBrightness { get; init; } = 35;

    /// <summary>Batas atas rata-rata kecerahan wajah (0-255).</summary>
    public double MaxBrightness { get; init; } = 235;
}

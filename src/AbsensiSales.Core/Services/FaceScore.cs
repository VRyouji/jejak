namespace AbsensiSales.Core.Services;

/// <summary>
/// Batas skor kecocokan wajah yang dipakai bersama oleh aplikasi Mobile,
/// model kunjungan, dan library face recognition.
///
/// Nilai ini adalah titik awal untuk model SFace. Kalibrasi ulang dengan
/// data nyata sebelum dipakai di produksi.
/// </summary>
public static class FaceScore
{
    public const double Threshold = 0.363;
}

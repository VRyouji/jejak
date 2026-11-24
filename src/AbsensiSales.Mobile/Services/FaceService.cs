using AbsensiSales.Face;

namespace AbsensiSales.Mobile.Services;

/// <summary>
/// Singleton layanan verifikasi wajah. Model ONNX ditaruh di folder
/// "models" di samping executable (lihat README). Jika belum tersedia,
/// otomatis memakai mode simulasi.
/// </summary>
public class FaceService : IDisposable
{
    private readonly FaceVerificationService _service;

    public FaceService()
    {
        var baseFolder = AppContext.BaseDirectory;
        _service = new FaceVerificationService(baseFolder);
    }

    public bool ModeSimulasi => _service.ModeSimulasi;

    /// <summary>True jika engine UltraFace + SFace berhasil dimuat.</summary>
    public bool EngineSiap => _service.EngineSiap;

    /// <summary>
    /// Alur lama: verifikasi langsung dari webcam dan folder reference_faces.
    /// Dipertahankan selama halaman verifikasi belum dimigrasikan.
    /// </summary>
    public FaceVerifyResult Verifikasi(string namaSales) =>
        _service.VerifikasiDariKamera(namaSales);

    /// <summary>Buat template wajah dari foto (JPEG/PNG).</summary>
    public FaceTemplateResult BuatTemplate(byte[] foto) => _service.BuatTemplate(foto);

    /// <summary>Verifikasi foto terhadap template Base64 milik sales.</summary>
    public FaceVerifyResult VerifikasiDenganTemplate(byte[] foto, string template) =>
        _service.Verifikasi(foto, template);

    public void Dispose() => _service.Dispose();
}

using OpenCvSharp;

namespace AbsensiSales.Face;

/// <summary>
/// Layanan verifikasi wajah untuk aplikasi absensi.
///
/// Ada dua jalur yang hidup berdampingan selama masa transisi:
/// 1. Jalur baru (disarankan): UltraFace + SFace, menerima foto/stream dan
///    template Base64. Jalan di Windows maupun Android.
/// 2. Jalur lama: Haar Cascade + webcam + folder reference_faces, dipertahankan
///    agar halaman verifikasi yang sekarang tetap bisa dipakai saat demo.
///
/// Jika model belum tersedia, jalur lama otomatis memakai mode simulasi.
/// Jalur baru tidak pernah membuat template palsu.
/// </summary>
public class FaceVerificationService : IDisposable
{
    private readonly FaceOptions _options;
    private readonly string _sfacePath;
    private readonly string _detectorPath;
    private readonly string _cascadePath;
    private readonly string _referenceDir;

    private CascadeClassifier? _cascade;
    private FaceDetector? _detector;
    private FaceEmbedder? _embedder;
    private readonly List<(string Nama, float[] Embedding)> _referensi = new();

    private bool _sudahInisialisasi;
    private bool _engineSiap;
    private bool _legacySiap;

    public FaceVerificationService(string baseFolder, FaceOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseFolder);

        _options = options ?? new FaceOptions();
        _sfacePath = Path.Combine(baseFolder, "models", "face_recognition_sface_2021dec.onnx");
        _detectorPath = Path.Combine(baseFolder, "models", "version-RFB-320.onnx");
        _cascadePath = Path.Combine(baseFolder, "models", "haarcascade_frontalface_default.xml");
        _referenceDir = Path.Combine(baseFolder, "reference_faces");
    }

    /// <summary>
    /// Membuat layanan dari byte model. Dipakai platform seperti Android yang
    /// membaca model dari asset paket, bukan dari folder executable.
    /// </summary>
    public FaceVerificationService(byte[] detectorModel, byte[] sfaceModel, FaceOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(detectorModel);
        ArgumentNullException.ThrowIfNull(sfaceModel);

        _options = options ?? new FaceOptions();
        _sfacePath = string.Empty;
        _detectorPath = string.Empty;
        _cascadePath = string.Empty;
        _referenceDir = string.Empty;

        _detector = new FaceDetector(detectorModel, _options);
        _embedder = new FaceEmbedder(sfaceModel);
        _engineSiap = true;
        _sudahInisialisasi = true;
    }

    /// <summary>True jika tidak ada engine yang bisa dipakai sama sekali.</summary>
    public bool ModeSimulasi => !_engineSiap && !_legacySiap;

    /// <summary>True jika engine UltraFace + SFace berhasil dimuat.</summary>
    public bool EngineSiap => _engineSiap;

    /// <summary>Batas skor yang dipakai layanan ini.</summary>
    public double MatchThreshold => _options.MatchThreshold;

    /// <summary>
    /// Inisialisasi sekali di awal aplikasi. Aman dipanggil berulang.
    /// Memuat engine baru dan jalur lama secara terpisah.
    /// </summary>
    public void Inisialisasi()
    {
        if (_sudahInisialisasi)
            return;

        _sudahInisialisasi = true;
        CobaMuatEngineBaru();
        CobaMuatJalurLama();
    }

    /* ============================================================
     * JALUR BARU: foto/stream -> template -> verifikasi
     * ============================================================ */

    /// <summary>
    /// Buat template wajah dari byte gambar (JPEG/PNG). Foto tidak disimpan.
    /// </summary>
    public FaceTemplateResult BuatTemplate(byte[] gambar, int maxSide = 1000)
    {
        ArgumentNullException.ThrowIfNull(gambar);
        Inisialisasi();

        if (!_engineSiap || _detector is null || _embedder is null)
        {
            return new FaceTemplateResult(
                null,
                0,
                "Model deteksi wajah belum tersedia. Letakkan version-RFB-320.onnx dan " +
                "face_recognition_sface_2021dec.onnx di folder models.");
        }

        try
        {
            var image = FaceImageLoader.Load(gambar, maxSide);
            return BuatTemplateDariGambar(image);
        }
        catch (Exception ex)
        {
            return new FaceTemplateResult(null, 0, "Gagal memproses gambar: " + ex.Message);
        }
    }

    /// <summary>Buat template wajah dari stream gambar.</summary>
    public FaceTemplateResult BuatTemplate(Stream gambar, int maxSide = 1000)
    {
        ArgumentNullException.ThrowIfNull(gambar);

        using var buffer = new MemoryStream();
        gambar.CopyTo(buffer);
        return BuatTemplate(buffer.ToArray(), maxSide);
    }

    /// <summary>
    /// Verifikasi wajah dari byte gambar terhadap template Base64 milik sales.
    /// </summary>
    public FaceVerifyResult Verifikasi(byte[] gambar, string templateBase64, int maxSide = 1000)
    {
        ArgumentNullException.ThrowIfNull(gambar);
        Inisialisasi();

        if (!_engineSiap || _detector is null || _embedder is null)
        {
            return new FaceVerifyResult(
                false,
                0,
                "Model deteksi wajah belum tersedia. Verifikasi tidak dapat dilakukan.",
                true);
        }

        if (string.IsNullOrWhiteSpace(templateBase64))
        {
            return new FaceVerifyResult(
                false,
                0,
                "Template wajah sales belum terdaftar. Lakukan pendaftaran wajah terlebih dahulu.",
                false);
        }

        float[] template;
        try
        {
            template = FaceTemplateCodec.FromBase64(templateBase64);
        }
        catch (Exception ex)
        {
            return new FaceVerifyResult(false, 0, ex.Message, false);
        }

        try
        {
            var image = FaceImageLoader.Load(gambar, maxSide);
            var faces = _detector.Detect(image);

            if (faces.Count == 0)
            {
                return new FaceVerifyResult(
                    false,
                    0,
                    "Wajah tidak terdeteksi. Posisikan wajah di dalam bingkai dan pastikan cahaya cukup.",
                    false) { JumlahWajah = 0 };
            }

            if (faces.Count > _options.MaxFacesAllowed)
            {
                return new FaceVerifyResult(
                    false,
                    0,
                    $"Terdeteksi {faces.Count} wajah. Pastikan hanya wajah Anda yang masuk ke kamera.",
                    false) { JumlahWajah = faces.Count };
            }

            var face = faces.OrderByDescending(item => item.W * item.H).First();
            var validationError = ValidasiKualitasWajah(image, face);
            if (validationError is not null)
            {
                return new FaceVerifyResult(false, 0, validationError, false)
                {
                    JumlahWajah = faces.Count
                };
            }

            var embedding = _embedder.GetEmbedding(image, face);
            double score = FaceEmbedder.CosineSimilarity(embedding, template);
            bool cocok = score >= _options.MatchThreshold;

            return new FaceVerifyResult(
                cocok,
                score,
                cocok
                    ? $"Wajah cocok (skor {score:F3}, batas {_options.MatchThreshold:F3})."
                    : $"Wajah tidak cocok (skor {score:F3}, batas {_options.MatchThreshold:F3}).",
                false)
            {
                JumlahWajah = faces.Count
            };
        }
        catch (Exception ex)
        {
            return new FaceVerifyResult(false, 0, "Gagal memproses gambar: " + ex.Message, false);
        }
    }

    /// <summary>Verifikasi wajah dari stream gambar.</summary>
    public FaceVerifyResult Verifikasi(Stream gambar, string templateBase64, int maxSide = 1000)
    {
        ArgumentNullException.ThrowIfNull(gambar);

        using var buffer = new MemoryStream();
        gambar.CopyTo(buffer);
        return Verifikasi(buffer.ToArray(), templateBase64, maxSide);
    }

    private FaceTemplateResult BuatTemplateDariGambar(RgbImage image)
    {
        var faces = _detector!.Detect(image);

        if (faces.Count == 0)
        {
            return new FaceTemplateResult(
                null,
                0,
                "Wajah tidak terdeteksi. Coba foto lebih jelas dan menghadap ke kamera.");
        }

        if (faces.Count > _options.MaxFacesAllowed)
        {
            return new FaceTemplateResult(
                null,
                faces.Count,
                $"Terdeteksi {faces.Count} wajah. Pastikan hanya satu wajah dalam foto.");
        }

        var face = faces.OrderByDescending(item => item.W * item.H).First();
        var validationError = ValidasiKualitasWajah(image, face);
        if (validationError is not null)
            return new FaceTemplateResult(null, faces.Count, validationError);

        var embedding = _embedder!.GetEmbedding(image, face);
        if (embedding.Length == 0)
            return new FaceTemplateResult(null, faces.Count, "Embedding wajah kosong. Coba ulangi foto.");

        return new FaceTemplateResult(
            FaceTemplateCodec.ToBase64(embedding),
            faces.Count,
            $"Template wajah berhasil dibuat (skor deteksi {face.Score:F2}).");
    }

    private string? ValidasiKualitasWajah(RgbImage image, FaceBox face)
    {
        if (face.W <= 0 || face.H <= 0)
            return "Kotak wajah tidak valid. Coba ulangi foto.";

        if (MathF.Min(face.W, face.H) < _options.MinFaceSize)
        {
            return $"Wajah terlalu kecil (minimal {_options.MinFaceSize} piksel). " +
                   "Dekatkan wajah ke kamera.";
        }

        double brightness = FaceImageLoader.AverageBrightness(image, face);
        if (brightness < _options.MinBrightness)
            return "Wajah terlalu gelap. Cari tempat dengan cahaya yang lebih baik.";

        if (brightness > _options.MaxBrightness)
            return "Wajah terlalu terang. Hindari cahaya langsung yang menyilaukan.";

        return null;
    }

    /* ============================================================
     * JALUR LAMA: webcam + folder reference_faces (kompatibilitas demo)
     * ============================================================ */

    /// <summary>
    /// Verifikasi wajah dari frame kamera webcam terhadap foto referensi sales.
    /// Dipertahankan agar alur demo Windows yang sekarang tidak langsung rusak.
    /// </summary>
    public FaceVerifyResult VerifikasiDariKamera(string namaSales)
    {
        Inisialisasi();

        if (!_legacySiap || _embedder is null || _cascade is null)
            return Simulasi(namaSales);

        try
        {
            using var capture = new VideoCapture(0);
            if (!capture.IsOpened())
                return Simulasi(namaSales);

            using var frame = new Mat();
            capture.Read(frame);
            if (frame.Empty())
                return Simulasi(namaSales);

            var faces = DeteksiWajah(frame);
            if (faces.Length == 0)
            {
                return new FaceVerifyResult(
                    false,
                    0,
                    "Wajah tidak terdeteksi. Coba posisikan wajah di dalam bingkai.",
                    false);
            }

            var utama = faces.OrderByDescending(rect => rect.Width * rect.Height).First();
            var embedding = _embedder.GetEmbedding(frame, utama);

            string namaTerbaik = string.Empty;
            double skorTerbaik = -1;
            foreach (var (nama, referenceEmbedding) in _referensi)
            {
                double score = FaceEmbedder.CosineSimilarity(embedding, referenceEmbedding);
                if (score > skorTerbaik)
                {
                    skorTerbaik = score;
                    namaTerbaik = nama;
                }
            }

            bool cocok = skorTerbaik >= _options.MatchThreshold
                && namaTerbaik.Contains(namaSales.Split(' ')[0], StringComparison.OrdinalIgnoreCase);

            return cocok
                ? new FaceVerifyResult(true, skorTerbaik, $"Wajah cocok dengan {namaTerbaik}.", false)
                : new FaceVerifyResult(false, skorTerbaik, "Wajah tidak cocok dengan data referensi.", false);
        }
        catch
        {
            return Simulasi(namaSales);
        }
    }

    private static FaceVerifyResult Simulasi(string namaSales)
    {
        // Skor simulasi stabil di atas threshold agar alur mockup lancar.
        var skor = 0.94 + (Math.Abs(namaSales.GetHashCode(StringComparison.Ordinal)) % 5) / 100.0;
        return new FaceVerifyResult(
            true,
            skor,
            $"Simulasi: wajah {namaSales} dianggap cocok (model demo).",
            true);
    }

    private Rect[] DeteksiWajah(Mat image)
    {
        using var gray = new Mat();
        Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
        Cv2.EqualizeHist(gray, gray);
        return _cascade!.DetectMultiScale(gray, 1.1, 5, minSize: new Size(80, 80));
    }

    private void CobaMuatEngineBaru()
    {
        try
        {
            if (!File.Exists(_detectorPath) || !File.Exists(_sfacePath))
                return;

            _detector = new FaceDetector(_detectorPath, _options);
            _embedder = new FaceEmbedder(_sfacePath);
            _engineSiap = true;
        }
        catch
        {
            _detector?.Dispose();
            _detector = null;
            _embedder?.Dispose();
            _embedder = null;
            _engineSiap = false;
        }
    }

    private void CobaMuatJalurLama()
    {
        try
        {
            if (!File.Exists(_cascadePath) || !File.Exists(_sfacePath))
                return;

            _cascade ??= new CascadeClassifier(_cascadePath);
            _embedder ??= new FaceEmbedder(_sfacePath);

            if (Directory.Exists(_referenceDir))
            {
                foreach (var file in Directory.GetFiles(_referenceDir)
                    .Where(path => new[] { ".jpg", ".jpeg", ".png" }
                        .Contains(Path.GetExtension(path).ToLowerInvariant())))
                {
                    using var image = Cv2.ImRead(file);
                    if (image.Empty())
                        continue;

                    var faces = DeteksiWajah(image);
                    if (faces.Length == 0)
                        continue;

                    var utama = faces.OrderByDescending(rect => rect.Width * rect.Height).First();
                    var name = Path.GetFileNameWithoutExtension(file);
                    _referensi.Add((name, _embedder.GetEmbedding(image, utama)));
                }
            }

            _legacySiap = _referensi.Count > 0;
        }
        catch
        {
            _cascade?.Dispose();
            _cascade = null;
            _referensi.Clear();
            _legacySiap = false;
        }
    }

    public void Dispose()
    {
        _detector?.Dispose();
        _embedder?.Dispose();
        _cascade?.Dispose();
    }
}

namespace AbsensiFaceAppToken.Services;

public sealed record FaceResult(float[]? Embedding, string? Error, int FaceCount);

/// <summary>Menggabungkan deteksi, embedding, dan penyimpanan token dalam satu pintu.</summary>
public sealed class FaceService : IDisposable
{
    public const double MatchThreshold = 0.363; // titik awal; sesuaikan setelah melihat skor nyata

    private FaceDetector? _detector;
    private FaceEmbedder? _embedder;

    public TokenStore Store { get; } = new();
    public bool IsReady => _detector != null && _embedder != null;

    // Model ditaruh di Resources/Raw dan dibaca dari paket aplikasi
    public async Task InitAsync()
    {
        if (IsReady) return;
        _detector = new FaceDetector(await ReadAssetAsync("version-RFB-320.onnx"));
        _embedder = new FaceEmbedder(await ReadAssetAsync("face_recognition_sface_2021dec.onnx"));
    }

    private static async Task<byte[]> ReadAssetAsync(string fileName)
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }

    /// <summary>Foto -> deteksi wajah -> embedding. Dijalankan di thread terpisah agar UI tidak macet.</summary>
    public Task<FaceResult> ExtractAsync(Stream imageStream)
    {
        return Task.Run(() =>
        {
            var img = ImageUtil.Load(imageStream);
            var faces = _detector!.Detect(img);
            if (faces.Count == 0)
                return new FaceResult(null, "Tidak ada wajah terdeteksi. Coba foto lebih jelas, menghadap depan.", 0);

            var main = faces.OrderByDescending(f => f.W * f.H).First();
            var embedding = _embedder!.GetEmbedding(img, main);
            return new FaceResult(embedding, null, faces.Count);
        });
    }

    public (string Name, double Score) FindBestMatch(float[] embedding)
    {
        string bestName = "";
        double bestScore = -1;
        foreach (var entry in Store.LoadAll())
        {
            double score = FaceEmbedder.CosineSimilarity(embedding, TokenStore.FromToken(entry.Token));
            if (score > bestScore) { bestScore = score; bestName = entry.Name; }
        }
        return (bestName, bestScore);
    }

    public void Dispose()
    {
        _detector?.Dispose();
        _embedder?.Dispose();
    }
}

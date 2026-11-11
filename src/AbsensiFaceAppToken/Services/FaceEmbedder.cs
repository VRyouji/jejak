using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace AbsensiFaceAppToken.Services;

/// <summary>Wajah -> embedding (vektor ciri) memakai model SFace (ONNX).</summary>
public sealed class FaceEmbedder : IDisposable
{
    private const int Size = 112;
    private readonly InferenceSession _session;
    private readonly string _inputName;

    public FaceEmbedder(byte[] modelBytes)
    {
        _session = new InferenceSession(modelBytes);
        _inputName = _session.InputMetadata.Keys.First();
    }

    public float[] GetEmbedding(RgbImage img, FaceBox box)
    {
        // Crop persegi di sekitar wajah (dengan sedikit margin) agar tidak gepeng saat di-resize
        float cx = box.X + box.W / 2f;
        float cy = box.Y + box.H / 2f;
        float side = MathF.Max(box.W, box.H) * 1.3f;

        var rgb = ImageUtil.CropResizeRgb(img, cx - side / 2f, cy - side / 2f, side, side, Size, Size);

        // Input: [1, 3, 112, 112], RGB, nilai 0-255 tanpa normalisasi
        var tensor = new DenseTensor<float>(new[] { 1, 3, Size, Size });
        var span = tensor.Buffer.Span;
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                for (int c = 0; c < 3; c++)
                    span[c * Size * Size + y * Size + x] = rgb[(y * Size + x) * 3 + c];

        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(_inputName, tensor) };
        using var results = _session.Run(inputs);
        return results.First().AsEnumerable<float>().ToArray();
    }

    public static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb) + 1e-9);
    }

    public void Dispose() => _session.Dispose();
}

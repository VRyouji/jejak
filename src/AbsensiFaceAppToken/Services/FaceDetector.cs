using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace AbsensiFaceAppToken.Services;

public readonly record struct FaceBox(float X, float Y, float W, float H, float Score);

/// <summary>
/// Deteksi wajah memakai model UltraFace (version-RFB-320.onnx) lewat OnnxRuntime.
/// Jalan di Android, iOS, dan Windows tanpa library tambahan.
/// </summary>
public sealed class FaceDetector : IDisposable
{
    private const int InW = 320, InH = 240;
    private const float ScoreThreshold = 0.7f;
    private const float IouThreshold = 0.3f;

    private readonly InferenceSession _session;
    private readonly string _inputName;

    public FaceDetector(byte[] modelBytes)
    {
        _session = new InferenceSession(modelBytes);
        _inputName = _session.InputMetadata.Keys.First();
    }

    public List<FaceBox> Detect(RgbImage img)
    {
        var rgb = ImageUtil.CropResizeRgb(img, 0, 0, img.Width, img.Height, InW, InH);

        // Input: [1, 3, 240, 320], RGB, (piksel - 127) / 128
        var tensor = new DenseTensor<float>(new[] { 1, 3, InH, InW });
        var span = tensor.Buffer.Span;
        for (int y = 0; y < InH; y++)
            for (int x = 0; x < InW; x++)
                for (int c = 0; c < 3; c++)
                    span[c * InH * InW + y * InW + x] = (rgb[(y * InW + x) * 3 + c] - 127f) / 128f;

        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(_inputName, tensor) };
        using var results = _session.Run(inputs);

        Tensor<float>? scores = null, boxes = null;
        foreach (var r in results)
        {
            var t = r.AsTensor<float>();
            int last = t.Dimensions[t.Dimensions.Length - 1];
            if (last == 2) scores = t;
            else if (last == 4) boxes = t;
        }
        if (scores == null || boxes == null)
            throw new InvalidOperationException("Output model detektor wajah tidak sesuai harapan.");

        int n = scores.Dimensions[1];
        var candidates = new List<FaceBox>();
        for (int i = 0; i < n; i++)
        {
            float s = scores[0, i, 1];
            if (s < ScoreThreshold) continue;

            // Box sudah ternormalisasi (x1, y1, x2, y2) -> ubah ke piksel gambar asli
            float x1 = boxes[0, i, 0] * img.Width;
            float y1 = boxes[0, i, 1] * img.Height;
            float x2 = boxes[0, i, 2] * img.Width;
            float y2 = boxes[0, i, 3] * img.Height;
            candidates.Add(new FaceBox(x1, y1, x2 - x1, y2 - y1, s));
        }
        return Nms(candidates);
    }

    private static List<FaceBox> Nms(List<FaceBox> boxes)
    {
        var kept = new List<FaceBox>();
        foreach (var b in boxes.OrderByDescending(b => b.Score))
            if (kept.All(k => Iou(k, b) < IouThreshold)) kept.Add(b);
        return kept;
    }

    private static float Iou(FaceBox a, FaceBox b)
    {
        float x1 = MathF.Max(a.X, b.X), y1 = MathF.Max(a.Y, b.Y);
        float x2 = MathF.Min(a.X + a.W, b.X + b.W), y2 = MathF.Min(a.Y + a.H, b.Y + b.H);
        float inter = MathF.Max(0, x2 - x1) * MathF.Max(0, y2 - y1);
        float union = a.W * a.H + b.W * b.H - inter;
        return union <= 0 ? 0 : inter / union;
    }

    public void Dispose() => _session.Dispose();
}

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace AbsensiSales.Face;

/// <summary>
/// Deteksi wajah memakai UltraFace (version-RFB-320.onnx) melalui ONNX Runtime.
/// Model ini berjalan tanpa OpenCV sehingga lebih aman untuk Android.
/// </summary>
public sealed class FaceDetector : IDisposable
{
    private const int InputWidth = 320;
    private const int InputHeight = 240;
    private const float NmsIouThreshold = 0.3f;

    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly float _scoreThreshold;

    public FaceDetector(string modelPath, FaceOptions? options = null)
        : this(File.ReadAllBytes(modelPath), options)
    {
    }

    public FaceDetector(byte[] modelBytes, FaceOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(modelBytes);
        _scoreThreshold = (options ?? new FaceOptions()).MinDetectionScore;
        _session = new InferenceSession(modelBytes);
        _inputName = _session.InputMetadata.Keys.First();
    }

    public List<FaceBox> Detect(RgbImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        var rgb = FaceImageLoader.CropResizeRgb(
            image,
            0,
            0,
            image.Width,
            image.Height,
            InputWidth,
            InputHeight);

        // Input model: [1, 3, 240, 320], RGB, (piksel - 127) / 128.
        var tensor = new DenseTensor<float>(new[] { 1, 3, InputHeight, InputWidth });
        var span = tensor.Buffer.Span;
        for (int y = 0; y < InputHeight; y++)
        {
            for (int x = 0; x < InputWidth; x++)
            {
                for (int channel = 0; channel < 3; channel++)
                {
                    span[channel * InputHeight * InputWidth + y * InputWidth + x] =
                        (rgb[(y * InputWidth + x) * 3 + channel] - 127f) / 128f;
                }
            }
        }

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(_inputName, tensor)
        };

        using var results = _session.Run(inputs);

        Tensor<float>? scores = null;
        Tensor<float>? boxes = null;
        foreach (var result in results)
        {
            var value = result.AsTensor<float>();
            int lastDimension = value.Dimensions[value.Dimensions.Length - 1];
            if (lastDimension == 2)
                scores = value;
            else if (lastDimension == 4)
                boxes = value;
        }

        if (scores is null || boxes is null)
            throw new InvalidOperationException("Output model detektor wajah tidak sesuai harapan.");

        int count = scores.Dimensions[1];
        var candidates = new List<FaceBox>();
        for (int i = 0; i < count; i++)
        {
            float score = scores[0, i, 1];
            if (score < _scoreThreshold)
                continue;

            // Kotak dari model sudah ternormalisasi (x1, y1, x2, y2).
            float x1 = boxes[0, i, 0] * image.Width;
            float y1 = boxes[0, i, 1] * image.Height;
            float x2 = boxes[0, i, 2] * image.Width;
            float y2 = boxes[0, i, 3] * image.Height;
            candidates.Add(new FaceBox(x1, y1, x2 - x1, y2 - y1, score));
        }

        return NonMaximumSuppression(candidates);
    }

    private static List<FaceBox> NonMaximumSuppression(List<FaceBox> boxes)
    {
        var kept = new List<FaceBox>();
        foreach (var box in boxes.OrderByDescending(item => item.Score))
        {
            if (kept.All(item => IntersectionOverUnion(item, box) < NmsIouThreshold))
                kept.Add(box);
        }

        return kept;
    }

    private static float IntersectionOverUnion(FaceBox a, FaceBox b)
    {
        float x1 = MathF.Max(a.X, b.X);
        float y1 = MathF.Max(a.Y, b.Y);
        float x2 = MathF.Min(a.X + a.W, b.X + b.W);
        float y2 = MathF.Min(a.Y + a.H, b.Y + b.H);

        float intersection = MathF.Max(0, x2 - x1) * MathF.Max(0, y2 - y1);
        float union = a.W * a.H + b.W * b.H - intersection;
        return union <= 0 ? 0 : intersection / union;
    }

    public void Dispose() => _session.Dispose();
}

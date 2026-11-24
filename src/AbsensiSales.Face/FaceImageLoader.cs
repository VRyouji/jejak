using SkiaSharp;

namespace AbsensiSales.Face;

/// <summary>
/// Gambar RGBA sederhana yang tidak bergantung pada OpenCV, sehingga bisa
/// dipakai di Windows maupun Android.
/// </summary>
public sealed class RgbImage
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Rgba { get; }

    public RgbImage(int width, int height, byte[] rgba)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Ukuran gambar harus lebih dari nol.");
        if (rgba.Length < width * height * 4)
            throw new ArgumentException("Buffer RGBA lebih kecil dari ukuran gambar.", nameof(rgba));

        Width = width;
        Height = height;
        Rgba = rgba;
    }
}

/// <summary>
/// Decode gambar (JPEG/PNG), perbaiki rotasi EXIF, dan sediakan crop/resize
/// berbasis piksel. Dipakai oleh FaceDetector dan FaceEmbedder.
/// </summary>
public static class FaceImageLoader
{
    /// <summary>Decode gambar, perbaiki rotasi, lalu perkecil jika terlalu besar.</summary>
    public static RgbImage Load(Stream input, int maxSide = 1000)
    {
        ArgumentNullException.ThrowIfNull(input);

        using var ms = new MemoryStream();
        input.CopyTo(ms);
        ms.Position = 0;

        using var codec = SKCodec.Create(ms)
            ?? throw new InvalidOperationException("Format gambar tidak didukung.");

        var info = new SKImageInfo(
            codec.Info.Width,
            codec.Info.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);

        using var decoded = SKBitmap.Decode(codec, info)
            ?? throw new InvalidOperationException("Gambar gagal dibaca.");

        int degrees = codec.EncodedOrigin switch
        {
            SKEncodedOrigin.RightTop => 90,
            SKEncodedOrigin.BottomRight => 180,
            SKEncodedOrigin.LeftBottom => 270,
            _ => 0
        };

        var oriented = degrees == 0 ? decoded : Rotate(decoded, degrees);
        try
        {
            var image = new RgbImage(oriented.Width, oriented.Height, oriented.Bytes);
            return Shrink(image, maxSide);
        }
        finally
        {
            if (!ReferenceEquals(oriented, decoded))
                oriented.Dispose();
        }
    }

    public static RgbImage Load(byte[] bytes, int maxSide = 1000)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var ms = new MemoryStream(bytes, writable: false);
        return Load(ms, maxSide);
    }

    private static SKBitmap Rotate(SKBitmap src, int degrees)
    {
        bool swap = degrees is 90 or 270;
        int width = swap ? src.Height : src.Width;
        int height = swap ? src.Width : src.Height;

        var rotated = new SKBitmap(new SKImageInfo(
            width,
            height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul));

        using var canvas = new SKCanvas(rotated);
        switch (degrees)
        {
            case 90:
                canvas.Translate(src.Height, 0);
                canvas.RotateDegrees(90);
                break;
            case 180:
                canvas.Translate(src.Width, src.Height);
                canvas.RotateDegrees(180);
                break;
            case 270:
                canvas.Translate(0, src.Width);
                canvas.RotateDegrees(270);
                break;
        }

        canvas.DrawBitmap(src, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
        return rotated;
    }

    // Perkecil dengan rata-rata blok (box filter) agar tidak patah-patah.
    private static RgbImage Shrink(RgbImage image, int maxSide)
    {
        int factor = Math.Max(1, Math.Max(image.Width, image.Height) / Math.Max(1, maxSide));
        if (factor == 1)
            return image;

        int newWidth = image.Width / factor;
        int newHeight = image.Height / factor;
        var destination = new byte[newWidth * newHeight * 4];
        int divisor = factor * factor;

        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                int r = 0, g = 0, b = 0;
                for (int dy = 0; dy < factor; dy++)
                {
                    int row = ((y * factor + dy) * image.Width + x * factor) * 4;
                    for (int dx = 0; dx < factor; dx++)
                    {
                        int index = row + dx * 4;
                        r += image.Rgba[index];
                        g += image.Rgba[index + 1];
                        b += image.Rgba[index + 2];
                    }
                }

                int output = (y * newWidth + x) * 4;
                destination[output] = (byte)(r / divisor);
                destination[output + 1] = (byte)(g / divisor);
                destination[output + 2] = (byte)(b / divisor);
                destination[output + 3] = 255;
            }
        }

        return new RgbImage(newWidth, newHeight, destination);
    }

    /// <summary>
    /// Ambil area dari gambar lalu resize bilinear. Hasil berupa byte RGB
    /// berurutan. Koordinat di luar gambar dianggap sebagai piksel tepi.
    /// </summary>
    public static byte[] CropResizeRgb(
        RgbImage image,
        float sourceX,
        float sourceY,
        float sourceWidth,
        float sourceHeight,
        int outputWidth,
        int outputHeight)
    {
        var output = new byte[outputWidth * outputHeight * 3];

        for (int y = 0; y < outputHeight; y++)
        {
            float fy = sourceY + (y + 0.5f) * sourceHeight / outputHeight - 0.5f;
            int y0 = (int)MathF.Floor(fy);
            float ay = fy - y0;

            for (int x = 0; x < outputWidth; x++)
            {
                float fx = sourceX + (x + 0.5f) * sourceWidth / outputWidth - 0.5f;
                int x0 = (int)MathF.Floor(fx);
                float ax = fx - x0;

                for (int channel = 0; channel < 3; channel++)
                {
                    float v00 = Pixel(image, x0, y0, channel);
                    float v10 = Pixel(image, x0 + 1, y0, channel);
                    float v01 = Pixel(image, x0, y0 + 1, channel);
                    float v11 = Pixel(image, x0 + 1, y0 + 1, channel);
                    float top = v00 + (v10 - v00) * ax;
                    float bottom = v01 + (v11 - v01) * ax;
                    output[(y * outputWidth + x) * 3 + channel] =
                        (byte)Math.Clamp(top + (bottom - top) * ay, 0f, 255f);
                }
            }
        }

        return output;
    }

    /// <summary>Hitung rata-rata kecerahan (luma) pada area wajah.</summary>
    public static double AverageBrightness(RgbImage image, FaceBox box)
    {
        int left = Math.Clamp((int)MathF.Floor(box.X), 0, image.Width - 1);
        int top = Math.Clamp((int)MathF.Floor(box.Y), 0, image.Height - 1);
        int right = Math.Clamp((int)MathF.Ceiling(box.X + box.W), left + 1, image.Width);
        int bottom = Math.Clamp((int)MathF.Ceiling(box.Y + box.H), top + 1, image.Height);

        // Sampling grid supaya tidak membaca seluruh area wajah.
        const int samplesPerSide = 16;
        double total = 0;
        int count = 0;

        for (int sy = 0; sy < samplesPerSide; sy++)
        {
            int y = top + (bottom - top) * sy / samplesPerSide;
            for (int sx = 0; sx < samplesPerSide; sx++)
            {
                int x = left + (right - left) * sx / samplesPerSide;
                int index = (y * image.Width + x) * 4;
                double luma = 0.299 * image.Rgba[index]
                    + 0.587 * image.Rgba[index + 1]
                    + 0.114 * image.Rgba[index + 2];
                total += luma;
                count++;
            }
        }

        return count == 0 ? 0 : total / count;
    }

    private static float Pixel(RgbImage image, int x, int y, int channel)
    {
        x = Math.Clamp(x, 0, image.Width - 1);
        y = Math.Clamp(y, 0, image.Height - 1);
        return image.Rgba[(y * image.Width + x) * 4 + channel];
    }
}

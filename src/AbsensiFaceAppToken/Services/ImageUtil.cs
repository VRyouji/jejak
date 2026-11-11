using SkiaSharp;

namespace AbsensiFaceAppToken.Services;

/// <summary>Gambar sederhana: piksel RGBA (4 byte per piksel).</summary>
public sealed class RgbImage
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Rgba { get; }

    public RgbImage(int width, int height, byte[] rgba)
    {
        Width = width;
        Height = height;
        Rgba = rgba;
    }
}

public static class ImageUtil
{
    /// <summary>Decode gambar (JPEG/PNG), perbaiki rotasi EXIF, perkecil jika terlalu besar.</summary>
    public static RgbImage Load(Stream input, int maxSide = 1000)
    {
        using var ms = new MemoryStream();
        input.CopyTo(ms);
        ms.Position = 0;

        using var codec = SKCodec.Create(ms);
        if (codec == null) throw new InvalidOperationException("Format gambar tidak didukung.");

        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var decoded = SKBitmap.Decode(codec, info);
        if (decoded == null) throw new InvalidOperationException("Gambar gagal dibaca.");

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
            var img = new RgbImage(oriented.Width, oriented.Height, oriented.Bytes);
            return Shrink(img, maxSide);
        }
        finally
        {
            if (!ReferenceEquals(oriented, decoded)) oriented.Dispose();
        }
    }

    private static SKBitmap Rotate(SKBitmap src, int degrees)
    {
        bool swap = degrees == 90 || degrees == 270;
        int w = swap ? src.Height : src.Width;
        int h = swap ? src.Width : src.Height;
        var rotated = new SKBitmap(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));

        using var canvas = new SKCanvas(rotated);
        switch (degrees)
        {
            case 90: canvas.Translate(src.Height, 0); canvas.RotateDegrees(90); break;
            case 180: canvas.Translate(src.Width, src.Height); canvas.RotateDegrees(180); break;
            case 270: canvas.Translate(0, src.Width); canvas.RotateDegrees(270); break;
        }
        canvas.DrawBitmap(src, 0, 0);
        return rotated;
    }

    // Perkecil dengan rata-rata blok (box filter) agar tidak patah-patah
    private static RgbImage Shrink(RgbImage img, int maxSide)
    {
        int f = Math.Max(1, Math.Max(img.Width, img.Height) / maxSide);
        if (f == 1) return img;

        int nw = img.Width / f, nh = img.Height / f;
        var dst = new byte[nw * nh * 4];
        int div = f * f;

        for (int y = 0; y < nh; y++)
        {
            for (int x = 0; x < nw; x++)
            {
                int r = 0, g = 0, b = 0;
                for (int dy = 0; dy < f; dy++)
                {
                    int row = ((y * f + dy) * img.Width + x * f) * 4;
                    for (int dx = 0; dx < f; dx++)
                    {
                        int i = row + dx * 4;
                        r += img.Rgba[i];
                        g += img.Rgba[i + 1];
                        b += img.Rgba[i + 2];
                    }
                }
                int o = (y * nw + x) * 4;
                dst[o] = (byte)(r / div);
                dst[o + 1] = (byte)(g / div);
                dst[o + 2] = (byte)(b / div);
                dst[o + 3] = 255;
            }
        }
        return new RgbImage(nw, nh, dst);
    }

    /// <summary>
    /// Ambil area (sx, sy, sw, sh) dari gambar lalu resize bilinear ke ow x oh.
    /// Hasil: byte RGB berurutan (ow * oh * 3). Koordinat di luar gambar diperlakukan sebagai tepi.
    /// </summary>
    public static byte[] CropResizeRgb(RgbImage img, float sx, float sy, float sw, float sh, int ow, int oh)
    {
        var output = new byte[ow * oh * 3];
        for (int y = 0; y < oh; y++)
        {
            float fy = sy + (y + 0.5f) * sh / oh - 0.5f;
            int y0 = (int)MathF.Floor(fy);
            float ay = fy - y0;

            for (int x = 0; x < ow; x++)
            {
                float fx = sx + (x + 0.5f) * sw / ow - 0.5f;
                int x0 = (int)MathF.Floor(fx);
                float ax = fx - x0;

                for (int c = 0; c < 3; c++)
                {
                    float v00 = Px(img, x0, y0, c), v10 = Px(img, x0 + 1, y0, c);
                    float v01 = Px(img, x0, y0 + 1, c), v11 = Px(img, x0 + 1, y0 + 1, c);
                    float top = v00 + (v10 - v00) * ax;
                    float bottom = v01 + (v11 - v01) * ax;
                    output[(y * ow + x) * 3 + c] = (byte)Math.Clamp(top + (bottom - top) * ay, 0f, 255f);
                }
            }
        }
        return output;
    }

    private static float Px(RgbImage img, int x, int y, int c)
    {
        x = Math.Clamp(x, 0, img.Width - 1);
        y = Math.Clamp(y, 0, img.Height - 1);
        return img.Rgba[(y * img.Width + x) * 4 + c];
    }
}

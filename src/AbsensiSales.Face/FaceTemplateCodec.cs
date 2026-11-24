using System.Buffers.Binary;

namespace AbsensiSales.Face;

/// <summary>
/// Konversi embedding wajah (float[]) menjadi template teks Base64 dan
/// sebaliknya. Template inilah yang disimpan di <c>tbl_sales.face_data</c>
/// atau SQLite lokal, bukan foto mentah.
/// </summary>
public static class FaceTemplateCodec
{
    public const int ExpectedEmbeddingLength = 128;

    public static string ToBase64(float[] embedding)
    {
        ArgumentNullException.ThrowIfNull(embedding);
        if (embedding.Length == 0)
            throw new ArgumentException("Embedding tidak boleh kosong.", nameof(embedding));

        var bytes = new byte[embedding.Length * sizeof(float)];
        for (int i = 0; i < embedding.Length; i++)
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * sizeof(float)), embedding[i]);

        return Convert.ToBase64String(bytes);
    }

    public static float[] FromBase64(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
            throw new ArgumentException("Template wajah kosong.", nameof(template));

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(template.Trim());
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Template wajah bukan Base64 yang valid.", nameof(template), ex);
        }

        if (bytes.Length == 0 || bytes.Length % sizeof(float) != 0)
            throw new ArgumentException("Panjang template wajah tidak valid.", nameof(template));

        var embedding = new float[bytes.Length / sizeof(float)];
        for (int i = 0; i < embedding.Length; i++)
            embedding[i] = BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(i * sizeof(float)));

        return embedding;
    }

    public static bool TryFromBase64(string? template, out float[] embedding)
    {
        try
        {
            embedding = FromBase64(template ?? string.Empty);
            return true;
        }
        catch
        {
            embedding = Array.Empty<float>();
            return false;
        }
    }
}

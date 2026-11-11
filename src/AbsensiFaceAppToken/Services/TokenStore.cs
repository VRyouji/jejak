using System.Text.Json;

namespace AbsensiFaceAppToken.Services;

public sealed class TokenEntry
{
    public string Name { get; set; } = "";
    public string Token { get; set; } = "";
}

/// <summary>
/// Penyimpanan token wajah (file JSON di folder data aplikasi) sebagai pengganti database untuk prototype.
/// Nanti diganti tabel database: kolom Name dan Token (teks).
/// </summary>
public sealed class TokenStore
{
    private readonly string _path = Path.Combine(FileSystem.AppDataDirectory, "face_tokens.json");

    public List<TokenEntry> LoadAll()
    {
        if (!File.Exists(_path)) return new List<TokenEntry>();
        return JsonSerializer.Deserialize<List<TokenEntry>>(File.ReadAllText(_path)) ?? new List<TokenEntry>();
    }

    public void Save(string name, string token)
    {
        var all = LoadAll();
        all.RemoveAll(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        all.Add(new TokenEntry { Name = name, Token = token });
        File.WriteAllText(_path, JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true }));
    }

    public void Clear()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    // Embedding (array angka) -> token teks (Base64)
    public static string ToToken(float[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
        return Convert.ToBase64String(bytes);
    }

    // Token teks (Base64) -> embedding (array angka)
    public static float[] FromToken(string token)
    {
        var bytes = Convert.FromBase64String(token);
        var embedding = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, embedding, 0, bytes.Length);
        return embedding;
    }
}

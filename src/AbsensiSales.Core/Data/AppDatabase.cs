using AbsensiSales.Core.Models;
using SQLite;

namespace AbsensiSales.Core.Data;

/// <summary>
/// Akses database SQLite lokal. Aplikasi Mobile sekarang mengambil seluruh data
/// dari backend API, jadi file database dibuat kosong tanpa data contoh.
/// Struktur tabel tetap disediakan untuk kebutuhan cache/offline di masa depan.
/// </summary>
public class AppDatabase
{
    private const string FileName = "absensi_sales.db3";
    private SQLiteAsyncConnection? _connection;
    private readonly string _folderPath;

    public AppDatabase(string? folderPath = null)
    {
        _folderPath = folderPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AbsensiSales");
    }

    public string DatabasePath => Path.Combine(_folderPath, FileName);

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
            return _connection;

        Directory.CreateDirectory(_folderPath);
        _connection = new SQLiteAsyncConnection(DatabasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        await _connection.CreateTableAsync<Sales>();
        await _connection.CreateTableAsync<Toko>();
        await _connection.CreateTableAsync<AbsensiKunjungan>();
        await _connection.CreateTableAsync<Permohonan>();

        return _connection;
    }
}

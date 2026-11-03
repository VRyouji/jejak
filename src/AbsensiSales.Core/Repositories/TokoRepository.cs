using AbsensiSales.Core.Data;
using AbsensiSales.Core.Models;
using SQLite;

namespace AbsensiSales.Core.Repositories;

public class TokoRepository
{
    private readonly AppDatabase _db;

    public TokoRepository(AppDatabase db) => _db = db;

    public async Task<List<Toko>> GetAllAsync()
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Toko>().OrderBy(t => t.NamaToko).ToListAsync();
    }

    public async Task<int> TambahAsync(Toko toko)
    {
        var conn = await _db.GetConnectionAsync();
        await conn.InsertAsync(toko);
        return toko.Id;
    }

    public async Task<Toko?> GetByKodeAsync(string kodeToko)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Toko>()
            .Where(t => t.KodeToko == kodeToko)
            .FirstOrDefaultAsync();
    }

    public async Task<Toko?> GetByIdAsync(int id)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Toko>().Where(t => t.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Menghitung jarak titik sales ke lokasi toko (rumus haversine, hasil dalam meter).
    /// </summary>
    public static double HitungJarak(double lat1, double lon1, double lat2, double lon2)
    {
        const double radiusBumi = 6371000;
        var dLat = ToRadian(lat2 - lat1);
        var dLon = ToRadian(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRadian(lat1)) * Math.Cos(ToRadian(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return radiusBumi * c;
    }

    private static double ToRadian(double derajat) => derajat * Math.PI / 180;
}

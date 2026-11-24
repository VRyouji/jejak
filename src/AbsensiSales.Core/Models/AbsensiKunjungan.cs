using AbsensiSales.Core.Services;
using SQLite;

namespace AbsensiSales.Core.Models;

public enum StatusKunjungan
{
    Draf,
    Tersimpan,
    GagalValidasi
}

public class AbsensiKunjungan
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int SalesId { get; set; }
    public int TokoId { get; set; }
    public string Tanggal { get; set; } = string.Empty;
    public string WaktuAbsensiAwal { get; set; } = string.Empty;
    public string WaktuCatat { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double JarakMeter { get; set; }
    public double SkorWajah { get; set; }
    public string TujuanKunjungan { get; set; } = string.Empty;
    public string Catatan { get; set; } = string.Empty;
    public string Status { get; set; } = nameof(StatusKunjungan.Tersimpan);

    [Ignore]
    public StatusKunjungan StatusEnum =>
        Enum.TryParse<StatusKunjungan>(Status, out var value) ? value : StatusKunjungan.Draf;

    [Ignore]
    public bool WajahTerverifikasi => SkorWajah >= FaceScore.Threshold;

    [Ignore]
    public string StatusLabel => StatusEnum switch
    {
        StatusKunjungan.Tersimpan => "Tersimpan",
        StatusKunjungan.GagalValidasi => "Gagal validasi",
        _ => "Draf"
    };
}

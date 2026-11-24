using AbsensiSales.Core.Models;

namespace AbsensiSales.Mobile.Services;

/// <summary>
/// Menyimpan state alur absensi yang berpindah antar halaman
/// (Absensi pertama -> Data toko -> Verifikasi wajah -> Foto toko -> Verifikasi data).
/// </summary>
public class AbsensiFlowState
{
    public string WaktuAbsensiAwal { get; set; } = string.Empty;
    public string Tanggal { get; set; } = string.Empty;

    public Toko? TokoTerpilih { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double JarakMeter { get; set; }
    public bool DalamRadius { get; set; }

    public string TujuanKunjungan { get; set; } = string.Empty;
    public string Catatan { get; set; } = string.Empty;

    public double SkorWajah { get; set; }
    public bool WajahSimulasi { get; set; }
    public string WaktuVerifikasiWajah { get; set; } = string.Empty;

    public byte[]? FotoBukti { get; set; }
    public string WaktuFoto { get; set; } = string.Empty;

    public int KunjunganId { get; set; }

    public void Reset()
    {
        TokoTerpilih = null;
        TujuanKunjungan = string.Empty;
        Catatan = string.Empty;
        SkorWajah = 0;
        WajahSimulasi = false;
        WaktuVerifikasiWajah = string.Empty;
        FotoBukti = null;
        WaktuFoto = string.Empty;
        JarakMeter = 0;
        DalamRadius = false;
        KunjunganId = 0;
    }
}

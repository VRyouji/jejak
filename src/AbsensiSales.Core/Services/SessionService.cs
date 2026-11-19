using AbsensiSales.Core.Models;

namespace AbsensiSales.Core.Services;

/// <summary>
/// Menyimpan sesi pengguna yang sedang login (sales maupun HR).
/// Pada mockup, status login hanya berlaku selama aplikasi berjalan.
/// </summary>
public class SessionService
{
    public Sales? SalesAktif { get; set; }
    public bool HrLogin { get; set; }

    /// <summary>Nama pengguna yang sedang login (dari response API).</summary>
    public string NamaPengguna { get; set; } = string.Empty;

    public string EmailPengguna { get; set; } = string.Empty;

    public string RolePengguna { get; set; } = string.Empty;

    public string TanggalHariIni => DateTime.Now.ToString("yyyy-MM-dd");

    public void Logout()
    {
        SalesAktif = null;
        HrLogin = false;
        NamaPengguna = string.Empty;
        EmailPengguna = string.Empty;
        RolePengguna = string.Empty;
    }
}

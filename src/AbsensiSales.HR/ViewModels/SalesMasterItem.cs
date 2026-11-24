using AbsensiSales.Core.Api;

namespace AbsensiSales.HR.ViewModels;

/// <summary>Item daftar Sales di halaman Data Master.</summary>
public sealed class SalesMasterItem
{
    public required int Id { get; init; }
    public required string Nama { get; init; }
    public required string Nip { get; init; }
    public required string Email { get; init; }
    public required string Telepon { get; init; }
    public required string Wilayah { get; init; }

    public string Inisial =>
        string.IsNullOrWhiteSpace(Nama)
            ? "?"
            : string.Concat(Nama
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Select(part => char.ToUpperInvariant(part[0])));

    public static SalesMasterItem DariApi(SalesAdminDto sales) => new()
    {
        Id = sales.IdSales,
        Nama = string.IsNullOrWhiteSpace(sales.Nama) ? "-" : sales.Nama.Trim(),
        Nip = string.IsNullOrWhiteSpace(sales.NomorSales) ? "-" : sales.NomorSales.Trim(),
        Email = string.IsNullOrWhiteSpace(sales.Email) ? "-" : sales.Email.Trim(),
        Telepon = string.IsNullOrWhiteSpace(sales.NomorTelepon) ? "-" : sales.NomorTelepon.Trim(),
        Wilayah = string.IsNullOrWhiteSpace(sales.Wilayah) ? "-" : sales.Wilayah!.Trim()
    };
}

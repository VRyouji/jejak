using System.Globalization;
using AbsensiSales.HR.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using IContainer = QuestPDF.Infrastructure.IContainer;
using Colors = QuestPDF.Helpers.Colors;

namespace AbsensiSales.HR.Services;

public sealed class PdfService
{
    private const decimal GajiPokok = 2_500_000m;
    private const decimal BonusPerKunjungan = 15_000m;
    private const int HariKerjaBulanan = 22;

    private static readonly CultureInfo Indonesia = CultureInfo.GetCultureInfo("id-ID");

    static PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<string> BuatSlipGajiAsync(PayrollItem item, string periode)
    {
        var filePath = BuatFilePath("Slip-Gaji", item.KodeSales);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(style => style.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Element(header => TulisHeader(header, "SLIP GAJI KARYAWAN", periode));

                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(16);

                    column.Item().Text("Data Sales")
                        .FontSize(12)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken4);

                    column.Item().Table(table => TulisDataSales(table, item));

                    column.Item().Text("Rincian Perhitungan Gaji")
                        .FontSize(12)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken4);

                    column.Item().Table(table => TulisRincianGaji(table, item));

                    column.Item().Element(container => TulisTotalGaji(container, item));
                });

                page.Footer().Element(TulisFooter);
            });
        });

        return Task.Run(() =>
        {
            document.GeneratePdf(filePath);
            return filePath;
        });
    }

    public Task<string> BuatRekapGajiAsync(IReadOnlyCollection<PayrollItem> items, string periode)
    {
        var filePath = BuatFilePath("Rekap-Gaji", periode);
        var rows = items.ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(style => style.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Element(header => TulisHeader(header, "REKAPITULASI GAJI SALES", periode));

                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(14);

                    column.Item().Table(table => TulisTabelRekap(table, rows));

                    column.Item().AlignRight().Column(summary =>
                    {
                        summary.Spacing(4);
                        summary.Item().Text($"{rows.Count} sales")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Medium);
                        summary.Item().Text($"Total gaji: {Rupiah(rows.Sum(item => item.TotalGaji))}")
                            .FontSize(13)
                            .Bold()
                            .FontColor(Colors.Green.Darken3);
                    });
                });

                page.Footer().Element(TulisFooter);
            });
        });

        return Task.Run(() =>
        {
            document.GeneratePdf(filePath);
            return filePath;
        });
    }

    private static void TulisHeader(IContainer container, string judul, string periode)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("Jejak Sales HR")
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Black);
                    left.Item().Text("Sistem Absensi & Monitoring Sales")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Medium);
                });

                row.ConstantItem(190).AlignRight().Column(right =>
                {
                    right.Item().Text(judul)
                        .FontSize(11)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken4);
                    right.Item().Text($"Periode: {periode}")
                        .FontSize(10);
                    right.Item().Text($"Dicetak: {DateTime.Now.ToString("dd MMMM yyyy HH:mm", Indonesia)}")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Medium);
                });
            });

            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private static void TulisDataSales(TableDescriptor table, PayrollItem item)
    {
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(1.1f);
            columns.RelativeColumn(1.9f);
            columns.RelativeColumn(1.1f);
            columns.RelativeColumn(1.9f);
        });

        table.Cell().Element(LabelCell).Text("Nama Sales");
        table.Cell().Element(ValueCell).Text(item.NamaSales);
        table.Cell().Element(LabelCell).Text("Kode SLS");
        table.Cell().Element(ValueCell).Text(item.KodeSales);

        table.Cell().Element(LabelCell).Text("Hari Kerja");
        table.Cell().Element(ValueCell).Text($"{item.HariKerja} hari");
        table.Cell().Element(LabelCell).Text("Kunjungan");
        table.Cell().Element(ValueCell).Text($"{item.KunjunganToko} toko");

        table.Cell().Element(LabelCell).Text("Izin / Cuti");
        table.Cell().Element(ValueCell).Text($"{item.JumlahIzin}x");
        table.Cell().Element(LabelCell).Text("Status Target");
        table.Cell().Element(ValueCell).Text(item.StatusTarget);
    }

    private static void TulisRincianGaji(TableDescriptor table, PayrollItem item)
    {
        decimal tunjangan = item.KunjunganToko * BonusPerKunjungan;
        decimal potongan = item.JumlahIzin * (GajiPokok / HariKerjaBulanan);

        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(2.5f);
            columns.RelativeColumn(2);
            columns.RelativeColumn(1.3f);
        });

        table.Cell().Element(HeaderCell).Text("Komponen");
        table.Cell().Element(HeaderCell).Text("Keterangan");
        table.Cell().Element(HeaderCell).AlignRight().Text("Nominal");

        TulisBarisGaji(table, "Gaji Pokok", "Gaji bulanan", Rupiah(GajiPokok));
        TulisBarisGaji(table, "Tunjangan Kunjungan", $"{item.KunjunganToko} x {Rupiah(BonusPerKunjungan)}", Rupiah(tunjangan));
        TulisBarisGaji(table, "Potongan Izin", $"{item.JumlahIzin} x {Rupiah(GajiPokok / HariKerjaBulanan)}", $"-{Rupiah(potongan)}");
    }

    private static void TulisBarisGaji(TableDescriptor table, string komponen, string keterangan, string nominal)
    {
        table.Cell().Element(BodyCell).Text(komponen);
        table.Cell().Element(BodyCell).Text(keterangan);
        table.Cell().Element(BodyCell).AlignRight().Text(nominal);
    }

    private static void TulisTotalGaji(IContainer container, PayrollItem item)
    {
        container
            .Background(Colors.Green.Lighten4)
            .Padding(14)
            .Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("Total Gaji Akhir")
                        .FontSize(13)
                        .Bold()
                        .FontColor(Colors.Green.Darken3);
                    left.Item().Text("Diterima setelah tunjangan dan potongan izin/cuti.")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(210).AlignRight().AlignMiddle()
                    .Text(Rupiah(item.TotalGaji))
                    .FontSize(18)
                    .Bold()
                    .FontColor(Colors.Green.Darken3);
            });
    }

    private static void TulisTabelRekap(TableDescriptor table, IReadOnlyList<PayrollItem> rows)
    {
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(2.1f);
            columns.RelativeColumn(1.1f);
            columns.RelativeColumn(1);
            columns.RelativeColumn(1.1f);
            columns.RelativeColumn(0.9f);
            columns.RelativeColumn(1.4f);
            columns.RelativeColumn(1.4f);
        });

        table.Header(header =>
        {
            header.Cell().Element(HeaderCell).Text("Sales");
            header.Cell().Element(HeaderCell).Text("Kode");
            header.Cell().Element(HeaderCell).AlignRight().Text("Hari");
            header.Cell().Element(HeaderCell).AlignRight().Text("Kunjungan");
            header.Cell().Element(HeaderCell).AlignRight().Text("Izin");
            header.Cell().Element(HeaderCell).Text("Status");
            header.Cell().Element(HeaderCell).AlignRight().Text("Gaji Akhir");
        });

        foreach (var item in rows)
        {
            table.Cell().Element(BodyCell).Text(item.NamaSales);
            table.Cell().Element(BodyCell).Text(item.KodeSales);
            table.Cell().Element(BodyCell).AlignRight().Text(item.HariKerja.ToString());
            table.Cell().Element(BodyCell).AlignRight().Text(item.KunjunganToko.ToString());
            table.Cell().Element(BodyCell).AlignRight().Text($"{item.JumlahIzin}x");
            table.Cell().Element(BodyCell).Text(item.StatusTarget);
            table.Cell().Element(BodyCell).AlignRight().Text(Rupiah(item.TotalGaji));
        }
    }

    private static void TulisFooter(IContainer container)
    {
        container
            .PaddingTop(8)
            .BorderTop(1)
            .BorderColor(Colors.Grey.Lighten1)
            .PaddingTop(8)
            .AlignCenter()
            .Text("Dokumen ini dihasilkan otomatis oleh Jejak Sales HR.")
            .FontSize(8)
            .FontColor(Colors.Grey.Medium);
    }

    private static IContainer LabelCell(IContainer container) =>
        container
            .Background(Colors.Grey.Lighten4)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(8);

    private static IContainer ValueCell(IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(8);

    private static IContainer HeaderCell(IContainer container) =>
        container
            .Background(Colors.Grey.Lighten3)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten1)
            .Padding(8)
            .DefaultTextStyle(style => style.SemiBold());

    private static IContainer BodyCell(IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(8);

    private static string Rupiah(decimal value) =>
        $"Rp {value.ToString("N0", Indonesia)}";

    private static string BuatFilePath(string prefix, string identifier)
    {
        var safeIdentifier = string.Concat(identifier.Select(character =>
            char.IsLetterOrDigit(character) ? character : '-'));

        var fileName = $"{prefix}-{safeIdentifier}-{DateTime.Now:yyyyMMdd-HHmmss-fff}.pdf";

        return Path.Combine(FileSystem.Current.CacheDirectory, fileName);
    }
}

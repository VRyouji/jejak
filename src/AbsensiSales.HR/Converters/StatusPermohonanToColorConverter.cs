using System.Globalization;

namespace AbsensiSales.HR.Converters;

/// <summary>
/// Mengubah string status permohonan menjadi warna badge.
/// </summary>
public class StatusPermohonanToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string) switch
        {
            "Approved" or "Disetujui" or "Hadir" or "Tersimpan" => Color.FromArgb("#3F9160"),
            "Rejected" or "Ditolak" or "Tidak hadir" => Color.FromArgb("#C6402F"),
            _ => Color.FromArgb("#B07C00")
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

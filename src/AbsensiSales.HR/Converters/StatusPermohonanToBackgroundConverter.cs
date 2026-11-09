using System.Globalization;

namespace AbsensiSales.HR.Converters;

/// <summary>
/// Mengubah string status perizinan menjadi warna latar badge.
/// Mendukung status versi tampilan (Approved/Rejected) maupun database
/// (Disetujui/Ditolak).
/// </summary>
public class StatusPermohonanToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string) switch
        {
            "Approved" or "Disetujui" or "Hadir" or "Tersimpan" => Color.FromArgb("#E9F6E6"),
            "Rejected" or "Ditolak" or "Tidak hadir" => Color.FromArgb("#FDECEA"),
            _ => Color.FromArgb("#FFF3C9")
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

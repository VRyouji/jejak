using Microsoft.Maui.Devices.Sensors;

namespace AbsensiSales.Mobile.Services;

/// <summary>
/// Lokasi perangkat asli (GPS/network). Tidak ada posisi simulasi: kalau izin
/// ditolak atau GPS tidak tersedia, aplikasi menampilkan pesan dan pengguna
/// tidak bisa melanjutkan absensi.
/// </summary>
public class GpsService
{
    public record Lokasi(double Latitude, double Longitude, double AkurasiMeter);

    public async Task<Lokasi?> GetLokasiAsync(
        double? dekatLat = null,
        double? dekatLon = null,
        CancellationToken ct = default)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                return null;
            }

            var lokasi = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(15)),
                ct);

            lokasi ??= await Geolocation.Default.GetLastKnownLocationAsync();

            if (lokasi is null)
            {
                return null;
            }

            return new Lokasi(
                lokasi.Latitude,
                lokasi.Longitude,
                lokasi.Accuracy ?? 0);
        }
        catch (PermissionException)
        {
            return null;
        }
        catch (FeatureNotSupportedException)
        {
            return null;
        }
        catch (FeatureNotEnabledException)
        {
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }
}

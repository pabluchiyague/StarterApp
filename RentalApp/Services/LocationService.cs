using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace RentalApp.Services;

/// <summary>
/// This reads the device's last known or current GPS position for nearby item
/// discovery while keeping GPS access outside ViewModels.
/// </summary>
public class LocationService : ILocationService
{
    public async Task<(double Latitude, double Longitude)?> GetCurrentLocationAsync()
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

        var location = await Geolocation.Default.GetLastKnownLocationAsync()
            ?? await Geolocation.Default.GetLocationAsync(new GeolocationRequest(
                GeolocationAccuracy.Medium,
                TimeSpan.FromSeconds(10)));

        return location == null
            ? null
            : (location.Latitude, location.Longitude);
    }
}

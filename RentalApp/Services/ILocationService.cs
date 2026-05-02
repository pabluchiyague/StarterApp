namespace RentalApp.Services;

/// <summary>
/// This abstracts device location lookup so nearby-search ViewModels can be
/// tested without using the phone/emulator GPS directly.
/// </summary>
public interface ILocationService
{
    Task<(double Latitude, double Longitude)?> GetCurrentLocationAsync();
}

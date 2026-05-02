namespace RentalApp.Database.Models;

/// <summary>
/// This carries the result of a nearby item search, including the origin and
/// radius that produced the returned items.
/// </summary>
public record NearbySearchResult(
    IReadOnlyList<Item> Items,
    double Latitude,
    double Longitude,
    double RadiusKm,
    int TotalResults);

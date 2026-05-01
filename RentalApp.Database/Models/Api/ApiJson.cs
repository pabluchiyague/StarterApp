using System.Text.Json;
using System.Text.Json.Serialization;

namespace RentalApp.Models.Api;

/// <summary>
/// Centralised JSON options used by every <c>HttpClient</c> call to the
/// SET09102 rental API. Pre-registers the
/// <see cref="RentalStatusJsonConverter"/> so <c>RentalStatus.OutForRent</c>
/// round-trips with the API's <c>"Out for Rent"</c> string form.
/// </summary>
public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new RentalStatusJsonConverter() }
    };
}

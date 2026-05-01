using System.Text.Json;
using System.Text.Json.Serialization;
using RentalApp.Database.Models;

namespace RentalApp.Models.Api;

/// <summary>
/// Translates between the API's wire form for rental statuses (string with
/// spaces, e.g., <c>"Out for Rent"</c>) and our local <see cref="RentalStatus"/>
/// enum. Without this, deserialising any rental DTO breaks the moment the
/// status field includes a space.
/// </summary>
public class RentalStatusJsonConverter : JsonConverter<RentalStatus>
{
    private static readonly Dictionary<string, RentalStatus> FromString = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Requested"]    = RentalStatus.Requested,
        ["Approved"]     = RentalStatus.Approved,
        ["Rejected"]     = RentalStatus.Rejected,
        ["Out for Rent"] = RentalStatus.OutForRent,
        ["Overdue"]      = RentalStatus.Overdue,
        ["Returned"]     = RentalStatus.Returned,
        ["Completed"]    = RentalStatus.Completed,
    };

    private static readonly Dictionary<RentalStatus, string> ToWireString =
        FromString.ToDictionary(kv => kv.Value, kv => kv.Key);

    public override RentalStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString()
            ?? throw new JsonException("Expected a non-null rental status string.");

        if (!FromString.TryGetValue(raw, out var status))
            throw new JsonException($"Unknown rental status: '{raw}'.");

        return status;
    }

    public override void Write(Utf8JsonWriter writer, RentalStatus value, JsonSerializerOptions options)
    {
        if (!ToWireString.TryGetValue(value, out var raw))
            throw new JsonException($"Unsupported RentalStatus value: {value}");

        writer.WriteStringValue(raw);
    }
}

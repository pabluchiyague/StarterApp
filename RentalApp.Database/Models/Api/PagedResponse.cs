namespace RentalApp.Models.Api;

/// <summary>
/// Generic pagination envelope used by every list-shaped endpoint
/// (<c>GET /items</c>, <c>GET /items/{id}/reviews</c>, etc.).
/// Field names match the JSON shape the server returns.
/// </summary>
public class PagedResponse<T>
{
    public List<T> Items      { get; set; } = new();
    public int     TotalItems { get; set; }
    public int     Page       { get; set; }
    public int     PageSize   { get; set; }
    public int     TotalPages { get; set; }
}

/// <summary>
/// Domain-side pagination result used by repositories. Keeps the domain
/// repository interfaces independent of API DTO types.
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int              TotalItems,
    int              Page,
    int              PageSize,
    int              TotalPages);

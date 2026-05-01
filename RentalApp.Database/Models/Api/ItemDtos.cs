namespace RentalApp.Models.Api;

/// <summary>
/// Trimmed item shape returned by the list endpoints
/// (<c>GET /items</c> and the <c>items[]</c> array on <c>GET /items/nearby</c>).
/// Includes joined fields like <c>OwnerName</c> and <c>Category</c> (slug).
/// </summary>
public class ItemSummaryDto
{
    public int      Id            { get; set; }
    public string   Title         { get; set; } = string.Empty;
    public string?  Description   { get; set; }
    public decimal  DailyRate     { get; set; }
    public int      CategoryId    { get; set; }

    /// <summary>The category slug, e.g., "tools".</summary>
    public string   Category      { get; set; } = string.Empty;

    public int      OwnerId       { get; set; }
    public string   OwnerName     { get; set; } = string.Empty;
    public double?  OwnerRating   { get; set; }

    public bool     IsAvailable   { get; set; }
    public double?  AverageRating { get; set; }
    public string?  ImageUrl      { get; set; }
    public DateTime CreatedAt     { get; set; }

    /// <summary>Only populated by the /items/nearby endpoint.</summary>
    public double?  Latitude      { get; set; }
    public double?  Longitude     { get; set; }
    public double?  Distance      { get; set; }
}

/// <summary>Full item shape returned by <c>GET /items/{id}</c>.</summary>
public class ItemDetailDto : ItemSummaryDto
{
    public int            TotalReviews { get; set; }
    public List<ReviewDto> Reviews     { get; set; } = new();
}

/// <summary>POST /items request body. dailyRate &gt; 0 and &lt;= 1000.</summary>
public record CreateItemRequest(
    string  Title,
    string? Description,
    decimal DailyRate,
    int     CategoryId,
    double  Latitude,
    double  Longitude);

/// <summary>
/// PUT /items/{id} request body. Every field is optional — server applies
/// only the ones present in the JSON.
/// </summary>
public class UpdateItemRequest
{
    public string?  Title       { get; set; }
    public string?  Description { get; set; }
    public decimal? DailyRate   { get; set; }
    public bool?    IsAvailable { get; set; }
}

/// <summary>GET /items/nearby response wrapper.</summary>
public class NearbyResponse
{
    public List<ItemSummaryDto> Items          { get; set; } = new();
    public NearbyOrigin         SearchLocation { get; set; } = new();
    public double               Radius         { get; set; }
    public int                  TotalResults   { get; set; }
}

public class NearbyOrigin
{
    public double Latitude  { get; set; }
    public double Longitude { get; set; }
}

/// <summary>GET /categories response item.</summary>
public class CategoryDto
{
    public int    Id        { get; set; }
    public string Name      { get; set; } = string.Empty;
    public string Slug      { get; set; } = string.Empty;
    public int    ItemCount { get; set; }
}

/// <summary>Wrapper for the <c>{ "categories": [...] }</c> top-level response.</summary>
public class CategoryListResponse
{
    public List<CategoryDto> Categories { get; set; } = new();
}

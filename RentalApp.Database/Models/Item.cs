using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace RentalApp.Database.Models;

/// <summary>
/// A rental item — something a user (the Owner) lists for others to borrow.
/// Maps to <c>POST /items</c>, <c>GET /items</c>, <c>GET /items/{id}</c>,
/// <c>PUT /items/{id}</c> on the API. The geographic <c>Location</c> field
/// is added by a later migration in Phase 6 once PostGIS is enabled.
/// </summary>
[Table("items")]
[PrimaryKey(nameof(Id))]
public class Item
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Short title shown in lists. Required, 5–100 chars per API.</summary>
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Long description. Optional on API, max 1000 chars.</summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>Daily rate in the marketplace's currency. Must be &gt; 0, &lt;= 1000.</summary>
    [Column(TypeName = "numeric(10,2)")]
    public decimal DailyRate { get; set; }

    /// <summary>FK to <see cref="Category"/>. Required.</summary>
    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    /// <summary>FK to <see cref="User"/> (the lister/owner).</summary>
    public int OwnerId { get; set; }

    [ForeignKey(nameof(OwnerId))]
    public User? Owner { get; set; }

    /// <summary>
    /// Whether the item is currently listed for rent. Owner toggles via
    /// <c>PUT /items/{id}</c>. Items get hidden from search when false.
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>Optional cover image URL (the API returns a URL string).</summary>
    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property: all rentals against this item.</summary>
    public List<Rental> Rentals { get; set; } = new List<Rental>();

    // -----------------------------------------------------------------
    // Phase 6 / runtime-only fields
    // -----------------------------------------------------------------

    /// <summary>
    /// Distance from the search origin in kilometres, populated by
    /// <c>GetNearbyAsync</c>. Not persisted (NotMapped). For local results
    /// it's computed from PostGIS; for API results it comes from the
    /// server-provided <c>distance</c> field on /items/nearby.
    /// </summary>
    [NotMapped]
    public double? DistanceKm { get; set; }

    /// <summary>
    /// This stores the persisted PostGIS geography point used by local nearby
    /// searches. X is longitude and Y is latitude.
    /// </summary>
    public Point? Location { get; set; }

    /// <summary>
    /// This keeps the API latitude value with an item while the app is running
    /// and mirrors <see cref="Location"/> for view-model form binding.
    /// </summary>
    [NotMapped]
    public double? Latitude { get; set; }

    /// <summary>
    /// This keeps the API longitude value with an item while the app is running
    /// and mirrors <see cref="Location"/> for view-model form binding.
    /// </summary>
    [NotMapped]
    public double? Longitude { get; set; }

    /// <summary>
    /// Cached average rating populated when the API returns it. Not
    /// persisted locally (the rating is computed from <see cref="Review"/>
    /// rows when needed). The API includes this on /items list responses.
    /// </summary>
    [NotMapped]
    public double? AverageRating { get; set; }
}

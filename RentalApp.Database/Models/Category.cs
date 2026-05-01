using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RentalApp.Database.Models;

/// <summary>
/// A rental category. Maps to the API's category contract:
/// the integer <see cref="Id"/> is used by EF / local writes, while the
/// lowercase <see cref="Slug"/> matches the value passed in API filters
/// such as <c>GET /items?category=tools</c>.
/// </summary>
[Table("categories")]
[PrimaryKey(nameof(Id))]
public class Category
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Display name (e.g., "Tools", "Camping").</summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-safe lowercase identifier the API uses for filtering
    /// (e.g., "tools" for the "Tools" category).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Hex color used by UI badges (e.g., "#F44336").</summary>
    [Required]
    [MaxLength(7)]
    public string ColorHex { get; set; } = "#808080";

    /// <summary>Optional human-readable description.</summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>Navigation property: all items in this category.</summary>
    public List<Item> Items { get; set; } = new List<Item>();
}

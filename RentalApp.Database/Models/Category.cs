using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RentalApp.Database.Models;

/// <summary>
/// Represents a category for organizing notes
/// </summary>
[Table("categories")]
[PrimaryKey(nameof(Id))]
public class Category
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Category name (e.g., "Work", "Personal", "Study")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Hex color code for visual identification (e.g., "#FF5733")
    /// </summary>
    [Required]
    [MaxLength(7)]
    public string ColorHex { get; set; } = "#808080";  // Default gray

    /// <summary>
    /// Optional description of category purpose
    /// </summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// Navigation property: All notes in this category
    /// </summary>
    public List<Note> Notes { get; set; } = new List<Note>();
}

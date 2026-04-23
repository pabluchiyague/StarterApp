using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StarterApp.Database.Models;

/// <summary>
/// Represents a single note with title, content, and categorization
/// </summary>
[Table("notes")]
[PrimaryKey(nameof(Id))]
public class Note
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Note title/subject
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Full note content (can be large)
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to Category
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Navigation property: The category this note belongs to
    /// </summary>
    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    /// <summary>
    /// When the note was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the note was last modified
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Computed property: Preview of content for list views
    /// </summary>
    [NotMapped]
    public string ContentPreview => Content.Length > 100
        ? Content.Substring(0, 100) + "..."
        : Content;
    
    // NEW: importance level, defaults to Normal
    public NoteImportance Importance { get; set; } = NoteImportance.Normal;

    // NEW: computed icon for display, not stored in DB
    [NotMapped]
    public string ImportanceIcon => Importance switch
    {
        NoteImportance.High => "⬆",
        NoteImportance.Low  => "⬇",
        _                    => ""
    };
}

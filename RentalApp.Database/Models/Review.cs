using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RentalApp.Database.Models;

/// <summary>
/// A review left by a Borrower after a rental is Completed. The API
/// (<c>POST /reviews</c>) enforces: rental must be Completed, reviewer
/// must be the borrower, no duplicate reviews per rental.
/// </summary>
[Table("reviews")]
[PrimaryKey(nameof(Id))]
public class Review
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>FK to the rental being reviewed. One review per rental (unique).</summary>
    public int RentalId { get; set; }

    [ForeignKey(nameof(RentalId))]
    public Rental? Rental { get; set; }

    /// <summary>FK to the user who left the review (must be the rental's borrower).</summary>
    public int ReviewerId { get; set; }

    [ForeignKey(nameof(ReviewerId))]
    public User? Reviewer { get; set; }

    /// <summary>1–5 stars. Validated by API and locally by the service layer.</summary>
    public int Rating { get; set; }

    /// <summary>Optional written feedback. API limit: 500 chars.</summary>
    [MaxLength(500)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

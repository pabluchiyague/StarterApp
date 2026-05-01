using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using RentalApp.Database.States;

namespace RentalApp.Database.Models;

/// <summary>
/// A rental — the relationship between an Item, its Owner, and a Borrower
/// for a specified date range. State is tracked through
/// <see cref="RentalStatus"/> and follows the workflow enforced by the API
/// at <c>PATCH /rentals/{id}/status</c>.
/// </summary>
[Table("rentals")]
[PrimaryKey(nameof(Id))]
public class Rental
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>FK to the rented <see cref="Item"/>.</summary>
    public int ItemId { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    /// <summary>FK to the user requesting the rental.</summary>
    public int BorrowerId { get; set; }

    [ForeignKey(nameof(BorrowerId))]
    public User? Borrower { get; set; }

    /// <summary>Inclusive start date (date-only on the API side).</summary>
    [Column(TypeName = "date")]
    public DateTime StartDate { get; set; }

    /// <summary>Inclusive end date (must be &gt;= StartDate).</summary>
    [Column(TypeName = "date")]
    public DateTime EndDate { get; set; }

    /// <summary>Current state in the lifecycle. See <see cref="RentalStatus"/>.</summary>
    public RentalStatus Status { get; set; } = RentalStatus.Requested;

    /// <summary>Computed: <c>DailyRate * (EndDate - StartDate + 1)</c>.</summary>
    [Column(TypeName = "numeric(10,2)")]
    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set when the owner approves the request. Null until then.</summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>Navigation property: review left by the borrower (if any).</summary>
    public Review? Review { get; set; }

    [NotMapped]
    public IRentalState State => Status switch
    {
        RentalStatus.Requested => new RequestedState(),
        RentalStatus.Approved => new ApprovedState(),
        RentalStatus.Rejected => new RejectedState(),
        RentalStatus.OutForRent => new OutForRentState(),
        RentalStatus.Overdue => new OverdueState(),
        RentalStatus.Returned => new ReturnedState(),
        RentalStatus.Completed => new CompletedState(),
        _ => throw new InvalidOperationException($"Unknown rental status: {Status}")
    };
}

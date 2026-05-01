using RentalApp.Database.Models;

namespace RentalApp.Models.Api;

/// <summary>POST /rentals request body. Date-only ISO 8601 strings server-side.</summary>
public record CreateRentalRequest(int ItemId, string StartDate, string EndDate);

/// <summary>PATCH /rentals/{id}/status request body.</summary>
public record UpdateRentalStatusRequest(RentalStatus Status);

/// <summary>
/// Rental list-row shape returned by GET /rentals/incoming|outgoing.
/// Joined fields differ slightly per endpoint (incoming includes borrower,
/// outgoing includes owner) — this DTO carries the union; absent fields
/// stay null.
/// </summary>
public class RentalSummaryDto
{
    public int      Id             { get; set; }
    public int      ItemId         { get; set; }
    public string   ItemTitle      { get; set; } = string.Empty;
    public int?     BorrowerId     { get; set; }
    public string?  BorrowerName   { get; set; }
    public double?  BorrowerRating { get; set; }
    public int?     OwnerId        { get; set; }
    public string?  OwnerName      { get; set; }
    public double?  OwnerRating    { get; set; }
    public DateTime StartDate      { get; set; }
    public DateTime EndDate        { get; set; }
    public RentalStatus Status     { get; set; }
    public decimal  TotalPrice     { get; set; }
    public DateTime RequestedAt    { get; set; }
    public DateTime? ApprovedAt    { get; set; }
}

/// <summary>Full rental shape returned by GET /rentals/{id}.</summary>
public class RentalDetailDto : RentalSummaryDto
{
    public string?              ItemDescription { get; set; }
    public List<StatusHistoryEntry> StatusHistory { get; set; } = new();
}

public class StatusHistoryEntry
{
    public RentalStatus Status    { get; set; }
    public DateTime     Timestamp { get; set; }
}

/// <summary>Wrapper for <c>{ "rentals": [...], "totalRentals": n }</c>.</summary>
public class RentalListResponse
{
    public List<RentalSummaryDto> Rentals      { get; set; } = new();
    public int                    TotalRentals { get; set; }
}

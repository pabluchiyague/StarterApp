namespace RentalApp.Database.Models;

/// <summary>
/// The state of a rental in its lifecycle. Persisted as an integer in the
/// database for EF; serialised over the wire as the API's string form
/// (e.g., <c>"Out for Rent"</c>) via
/// <see cref="RentalApp.Models.Api.RentalStatusJsonConverter"/>.
///
/// Valid transitions (mirrored on the server, enforced locally by
/// IRentalState in Phase 7):
/// <code>
/// Requested  ── Owner ──→  Approved
/// Requested  ── Owner ──→  Rejected
/// Approved   ── System/Owner ──→  OutForRent
/// OutForRent ── Borrower    ──→  Returned
/// OutForRent ── System(auto) ─→  Overdue
/// Overdue    ── Borrower    ──→  Returned
/// Returned   ── Owner       ──→  Completed
/// </code>
/// </summary>
public enum RentalStatus
{
    Requested  = 0,
    Approved   = 1,
    Rejected   = 2,
    OutForRent = 3,
    Overdue    = 4,
    Returned   = 5,
    Completed  = 6,
}

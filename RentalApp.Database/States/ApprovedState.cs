using RentalApp.Database.Models;

namespace RentalApp.Database.States;

public class ApprovedState : RentalStateBase
{
    public override RentalStatus Status => RentalStatus.Approved;

    /// <summary>This moves an approved rental to out for rent.</summary>
    public override IRentalState MarkOutForRent() => new OutForRentState();
}

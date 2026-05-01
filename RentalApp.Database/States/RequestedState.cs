using RentalApp.Database.Models;

namespace RentalApp.Database.States;

public class RequestedState : RentalStateBase
{
    public override RentalStatus Status => RentalStatus.Requested;

    /// <summary>This moves a requested rental to approved.</summary>
    public override IRentalState Approve() => new ApprovedState();

    /// <summary>This moves a requested rental to rejected.</summary>
    public override IRentalState Reject() => new RejectedState();
}

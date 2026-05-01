using RentalApp.Database.Models;

namespace RentalApp.Database.States;

public class OutForRentState : RentalStateBase
{
    public override RentalStatus Status => RentalStatus.OutForRent;

    /// <summary>This moves an out-for-rent rental to returned.</summary>
    public override IRentalState MarkReturned() => new ReturnedState();

    /// <summary>This moves an out-for-rent rental to overdue.</summary>
    public override IRentalState MarkOverdue() => new OverdueState();
}

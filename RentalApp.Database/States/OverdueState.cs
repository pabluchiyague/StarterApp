using RentalApp.Database.Models;

namespace RentalApp.Database.States;

public class OverdueState : RentalStateBase
{
    public override RentalStatus Status => RentalStatus.Overdue;

    /// <summary>This moves an overdue rental to returned.</summary>
    public override IRentalState MarkReturned() => new ReturnedState();
}

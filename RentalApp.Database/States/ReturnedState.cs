using RentalApp.Database.Models;

namespace RentalApp.Database.States;

public class ReturnedState : RentalStateBase
{
    public override RentalStatus Status => RentalStatus.Returned;

    /// <summary>This moves a returned rental to completed.</summary>
    public override IRentalState Complete() => new CompletedState();
}

using RentalApp.Database.Models;

namespace RentalApp.Database.States;

public class CompletedState : RentalStateBase
{
    public override RentalStatus Status => RentalStatus.Completed;
}

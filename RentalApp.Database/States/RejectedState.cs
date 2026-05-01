using RentalApp.Database.Models;

namespace RentalApp.Database.States;

public class RejectedState : RentalStateBase
{
    public override RentalStatus Status => RentalStatus.Rejected;
}

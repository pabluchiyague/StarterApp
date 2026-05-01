using RentalApp.Database.Models;

namespace RentalApp.Database.States;

public interface IRentalState
{
    RentalStatus Status { get; }
    IRentalState Approve();
    IRentalState Reject();
    IRentalState MarkOutForRent();
    IRentalState MarkReturned();
    IRentalState MarkOverdue();
    IRentalState Complete();
}

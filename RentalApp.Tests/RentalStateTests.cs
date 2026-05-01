using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Tests;

public class RentalStateTests
{
    [Theory]
    [InlineData(RentalStatus.Requested, nameof(IRentalState.Approve), RentalStatus.Approved)]
    [InlineData(RentalStatus.Requested, nameof(IRentalState.Reject), RentalStatus.Rejected)]
    [InlineData(RentalStatus.Approved, nameof(IRentalState.MarkOutForRent), RentalStatus.OutForRent)]
    [InlineData(RentalStatus.OutForRent, nameof(IRentalState.MarkReturned), RentalStatus.Returned)]
    [InlineData(RentalStatus.OutForRent, nameof(IRentalState.MarkOverdue), RentalStatus.Overdue)]
    [InlineData(RentalStatus.Overdue, nameof(IRentalState.MarkReturned), RentalStatus.Returned)]
    [InlineData(RentalStatus.Returned, nameof(IRentalState.Complete), RentalStatus.Completed)]
    public void ValidTransition_ReturnsExpectedState(RentalStatus from, string action, RentalStatus expected)
    {
        var state = new Rental { Status = from }.State;

        var next = Invoke(state, action);

        Assert.Equal(expected, next.Status);
    }

    [Theory]
    [InlineData(RentalStatus.Requested, nameof(IRentalState.Complete))]
    [InlineData(RentalStatus.Approved, nameof(IRentalState.Reject))]
    [InlineData(RentalStatus.Rejected, nameof(IRentalState.Approve))]
    [InlineData(RentalStatus.Completed, nameof(IRentalState.MarkReturned))]
    public void InvalidTransition_Throws(RentalStatus from, string action)
    {
        var state = new Rental { Status = from }.State;

        Assert.Throws<InvalidStateTransitionException>(() => Invoke(state, action));
    }

    private static IRentalState Invoke(IRentalState state, string action)
    {
        return action switch
        {
            nameof(IRentalState.Approve) => state.Approve(),
            nameof(IRentalState.Reject) => state.Reject(),
            nameof(IRentalState.MarkOutForRent) => state.MarkOutForRent(),
            nameof(IRentalState.MarkReturned) => state.MarkReturned(),
            nameof(IRentalState.MarkOverdue) => state.MarkOverdue(),
            nameof(IRentalState.Complete) => state.Complete(),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}

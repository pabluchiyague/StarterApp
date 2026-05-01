using RentalApp.Database.Models;

namespace RentalApp.Database.States;

public abstract class RentalStateBase : IRentalState
{
    public abstract RentalStatus Status { get; }

    /// <summary>This rejects approval unless the concrete state overrides it.</summary>
    public virtual IRentalState Approve() => Throw(nameof(Approve));
    /// <summary>This rejects rejection unless the concrete state overrides it.</summary>
    public virtual IRentalState Reject() => Throw(nameof(Reject));
    /// <summary>This rejects out-for-rent movement unless the concrete state overrides it.</summary>
    public virtual IRentalState MarkOutForRent() => Throw(nameof(MarkOutForRent));
    /// <summary>This rejects return movement unless the concrete state overrides it.</summary>
    public virtual IRentalState MarkReturned() => Throw(nameof(MarkReturned));
    /// <summary>This rejects overdue movement unless the concrete state overrides it.</summary>
    public virtual IRentalState MarkOverdue() => Throw(nameof(MarkOverdue));
    /// <summary>This rejects completion unless the concrete state overrides it.</summary>
    public virtual IRentalState Complete() => Throw(nameof(Complete));

    /// <summary>
    /// This throws a consistent invalid-transition exception for blocked
    /// workflow actions.
    /// </summary>
    protected IRentalState Throw(string action)
    {
        throw new InvalidStateTransitionException($"{Status}.{action}");
    }
}

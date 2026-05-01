namespace RentalApp.Database.States;

public class InvalidStateTransitionException : InvalidOperationException
{
    /// <summary>
    /// This creates a workflow exception that names the blocked transition.
    /// </summary>
    public InvalidStateTransitionException(string transition)
        : base($"Invalid state transition: {transition}")
    {
    }
}

namespace DicingBlade.Classes.Processes
{
    /// <summary>
    /// Invoked in the new state before invoking OnEntry methods
    /// </summary>
    /// <param name="SourceState">An old State</param>
    /// <param name="DestinationState">A new State</param>
    /// <param name="Trigger">A trigger caused the new State</param>
    public record ProcessStateChanging(State SourceState, State DestinationState, Trigger Trigger):IProcessNotify;
}

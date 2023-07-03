using System;

namespace DicingBlade.Classes.Processes
{
    [Flags]
    public enum CompletionStatus
    {
        Success = 1,
        Cancelled = 2
    }
}
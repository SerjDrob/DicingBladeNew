using MachineClassLibrary.Classes;
using MachineClassLibrary.GeometryUtility;
using System;

namespace DicingBlade.Classes.Processes
{
    public class ProcessCompletedEventArgs : EventArgs
    {
        public ProcessCompletedEventArgs(CompletionStatus status, ICoorSystem<LMPlace> coorSystem)
        {
            Status = status;
            CoorSystem = coorSystem;
        }

        public CompletionStatus Status { get; init; }
        public ICoorSystem<LMPlace> CoorSystem { get; init; }
    }
}
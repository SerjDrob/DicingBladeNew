using MachineClassLibrary.Classes;
using MachineClassLibrary.GeometryUtility;
using MachineClassLibrary.Laser.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DicingBlade.Classes.Processes
{
    public interface IProcess : IObservable<IProcessNotify>
    {
        void ExcludeObject(IProcObject procObject);
        void IncludeObject(IProcObject procObject);
        Task CreateProcess();
        Task Deny();
        Task Next();
        Task StartAsync();
        Task StartAsync(CancellationToken cancellationToken);
    }

    [Flags]
    public enum CompletionStatus
    {
        Success = 1,
        Cancelled = 2
    }

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
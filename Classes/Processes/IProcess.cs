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
        bool ProcessEndOrDenied
        {
            get;
        }
    }
}
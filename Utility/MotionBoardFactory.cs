using MachineClassLibrary.Machine.MotionDevices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DicingBlade.Utility
{
    internal class MotionBoardFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly MachineConfiguration _machineConfiguration;

        public MotionBoardFactory(IServiceProvider serviceProvider, MachineConfiguration machineConfiguration)
        {
            _serviceProvider = serviceProvider;
            _machineConfiguration = machineConfiguration;
        }
        public IMotionDevicePCI1240U? GetMotionBoard()
        {
            if (_machineConfiguration.IsPCI1240U) return _serviceProvider.GetService<MotionDevicePCI1240U>();
            if (_machineConfiguration.IsPCI1245E) return _serviceProvider.GetService<MotionDevicePCI1245E>();
            if (_machineConfiguration.IsMOCKBOARD) return _serviceProvider.GetService<MotDevMock>();
            return null;
        }
    }
}

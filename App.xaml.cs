#nullable enable

using System;
using System.IO;
using System.Windows;
using DicingBlade.Utility;
using DicingBlade.ViewModels;
using DicingBlade.Views;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Machine.MotionDevices;
using MachineClassLibrary.SFC;
using MachineClassLibrary.VideoCapture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
//using MachineClassLibrary.Machine.MotionDevices;

namespace DicingBlade
{
    /// <summary>
    ///     Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public ServiceCollection MainIoC
        {
            get; private set;
        }



        public App()
        {
            var machineconfigs = ExtensionMethods
            .DeserilizeObject<DicingMachineConfiguration>(AppPaths.MachineConfigs)
            ?? throw new NullReferenceException("Machine configs isn't defined");

            MainIoC = new ServiceCollection();

            MainIoC.AddSingleton<MotDevMock>()
                   .AddSingleton<MotionDevicePCI1240U>()
                   .AddSingleton<MotionDevicePCI1245E>()
                   .AddSingleton(sp =>
                   {
                       return new MotionBoardFactory(sp, machineconfigs).GetMotionBoard();
                   })
                   .AddSingleton(machineconfigs)
                   .AddSingleton<ExceptionsAgregator>()
                   .AddScoped<IVideoCapture, USBCamera>()
                   //.AddSingleton<ISpindle, Spindle3>()
                   .AddSingleton<ISpindle, CommanderSK>(sp =>
                   new CommanderSK("COM1", 19200, new SpindleParams
                   {
                       Acc = 5,
                       Dec = 5,
                       MinFreq = 100,
                       MaxFreq = 600,
                       RatedCurrent = 9,
                       RatedVoltage = 60
                   }))
                   //.AddSingleton<ISpindle, MockSpindle>()
                   .AddSingleton(machineconfigs)
                   .AddSingleton<DicingBladeMachine>()
                   .AddSingleton<MainViewModel>()
                   .AddLogging(builder =>
                   {
                       builder.AddFile(AppPaths.Applog);
                   })
                   //.AddDbContext<DbContext, LaserDbContext>()
                   ;
        }
        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            _principleLogger.LogError(e.Exception, "An unhandled Exception was thrown");
        }
        private ILogger _principleLogger;
        protected override void OnStartup(StartupEventArgs e)
        {
            var provider = MainIoC.BuildServiceProvider();
            var loggerProvider = provider.GetRequiredService<ILoggerProvider>();
            _principleLogger = loggerProvider.CreateLogger("AppLogger");
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;

#if PCIInserted
            var viewModel = provider.GetService<MainViewModel>();
#else
            //var viewModel = new MainViewModel();
#endif   
            base.OnStartup(e);

            new MainWindowView(viewModel).Show();
        }
        protected override void OnExit(ExitEventArgs e)
        {
            Environment.Exit(0);
            base.OnExit(e);
        }
    }
}
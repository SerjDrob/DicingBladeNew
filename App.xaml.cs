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
using Microsoft.Extensions.Hosting;

namespace DicingBlade
{
    /// <summary>
    ///     Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public ServiceCollection MainIoC { get; private set; }



        public App()
        {
            var machineconfigs = ExtensionMethods
            .DeserilizeObject<MachineConfiguration>(Path.Combine(ProjectPath.GetFolderPath("AppSettings"), "MachineConfigs.json"));


            MainIoC = new ServiceCollection();

            MainIoC//.AddMediatR(Assembly.GetExecutingAssembly())
                   //.AddSingleton<ISubject,Subject>()

                       .AddSingleton<MotDevMock>()
                       .AddSingleton<MotionDevicePCI1240U>()
                       .AddSingleton<MotionDevicePCI1245E>()
                       .AddSingleton(sp =>
                       {
                           return new MotionBoardFactory(sp, machineconfigs).GetMotionBoard();
                       })
                       .AddSingleton<ExceptionsAgregator>()
                       .AddScoped<IVideoCapture, USBCamera>()
                       //.AddSingleton<ISpindle, Spindle3>()
                       .AddSingleton<ISpindle, MockSpindle>()
                       .AddSingleton<DicingBladeMachine>()
                       .AddSingleton<MainViewModel>()
                       //.AddDbContext<DbContext, LaserDbContext>()
                       ;
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            var provider = MainIoC.BuildServiceProvider();

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
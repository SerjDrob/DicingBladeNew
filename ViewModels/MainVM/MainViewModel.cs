using Advantech.Motion;
using DicingBlade.Classes;
using DicingBlade.Classes.Miscellaneous;
using DicingBlade.Classes.Processes;
using DicingBlade.Classes.Technology;
using DicingBlade.Classes.WaferGeometry;
using DicingBlade.Properties;
using DicingBlade.Utility;
using DicingBlade.Views;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Machine.MotionDevices;
using MachineClassLibrary.Machine.Parts;
using MachineClassLibrary.SFC;
using MachineClassLibrary.VideoCapture;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.Input;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Growl = HandyControl.Controls.Growl;
using MsgBox = HandyControl.Controls.MessageBox;
using Point = System.Windows.Point;

namespace DicingBlade.ViewModels
{

    [AddINotifyPropertyChangedInterface]
    internal partial class MainViewModel : IMainViewModel
    {
        const string APP_SETTINGS_FOLDER = "AppSettings";

        private readonly DicingBladeMachine _machine;
        private ITechnology _technology;
        private IComSensor _flowMeter;
        private DicingProcess _dicingProcess;
        private bool _isProcessInCorrection;
        private bool _isSingleCutAvaliable;
        private IWafer _currentWafer;

        //public double CameraScale { get; private set; }
        public bool CutWidthMarkerVisibility { get; set; }
        //public ScaleGrid ScaleGridView { get; private set; }
        public double Flow { get; set; }
        public Velocity VelocityRegime { get; set; } = Velocity.Fast;
        public ObservableCollection<TraceLine> TracesCollectionView { get; set; } = new();
        //public double CutOffsetView { get; set; }

        //public double XTrace { get; set; }
        //public double YTrace { get; set; }
        //public double XTraceEnd { get; set; }


        public object CentralView { get; set; }
        public object RightSideView {  get; set; }


        public CameraVM CamVM { get; set; } = new();

        //public BitmapImage Bi { get; set; }

        public AxisStateView XAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        public AxisStateView YAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        public AxisStateView ZAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        public AxisStateView UAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        
        public bool ChuckVacuumValveView { get; set; }
        public bool CoolantValveView { get; set; }
        public bool BlowingValveView { get; set; }
        public bool ChuckVacuumSensorView { get; set; }
        public bool CoolantSensorView { get; set; }
        public bool AirSensorView { get; set; }
        public bool SpindleCoolantSensorView { get; set; }
        //public double BCCenterXView { get; set; }
        //public double BCCenterYView { get; set; }
        //public double CCCenterXView { get; set; }
        //public double CCCenterYView { get; set; }
        public double ZBladeTouchView { get; set; }
        public int SpindleFreqView { get; set; }
        public double SpindleCurrentView { get; set; }
        //public double WaferCurrentShiftView { get; set; }
        //public bool ResetView { get; private set; }
        public bool SpindleOnFreq { get; private set; }
        public bool SpindleAccelarating { get; private set; }
        public bool SpindleDeccelarating { get; private set; }
        public bool SpindleStops { get; private set; }
        //public double CutWidthView { get; set; } = 0.05;
        //public double RealCutWidthView { get; set; } = 0.13;
        public Wafer Wafer { get; set; }
        public Wafer2D Substrate { get; private set; }
        //public WaferView WaferView { get; set; }
        //public double WvAngle { get; set; }
        //public bool WvRotate { get; set; }
        //public double RotatingTime { get; set; } = 1;
        //public double Thickness { get; set; } = 1;

        //public int[] Rows { get; set; }
        //public int[] Cols { get; set; }

        public bool TeachVScaleMarkersVisibility { get; private set; }
        public string ProcessStatus { get; private set; }
        public bool UserConfirmation { get; private set; }
        public double TeachMarkersRatio { get; } = 2;



        public SubstrateVM SubstrVM { get; set; } = new();

        public MainViewModel(DicingBladeMachine machine)
        {
            //Cols = new[] { 0, 1 };
            //Rows = new[] { 2, 1 };

            SubstrVM.SubstrateClicked += SubstrVM_SubstrateClicked;
            CamVM.ImageClicked += CamVM_ImageClicked;
            CentralView = SubstrVM;
            RightSideView = CamVM;
            //Bi = new BitmapImage();
            //CameraScale = Settings.Default.CameraScale;

            CamVM.CameraScale = Settings.Default.CameraScale;

            try
            {
                _technology = StatMethods.DeSerializeObjectJson<Technology>(Settings.Default.TechnologyLastFile);

                _machine = machine;

                ImplementMachineSettings();

                _machine.SwitchOffValve(Valves.Blowing);
                _machine.SwitchOffValve(Valves.ChuckVacuum);
                _machine.SwitchOffValve(Valves.Coolant);
                _machine.SwitchOffValve(Valves.SpindleContact);

                _machine.StartCamera(0, 1);
                _machine.OnBitmapChanged += _machine_OnBitmapChanged;

                _machine.OnAxisMotionStateChanged += _machine_OnAxisMotionStateChanged;
                _machine.OnSensorStateChanged += _machine_OnSensorStateChanged;
                _machine.OnValveStateChanged += _machine_OnValveStateChanged;

                _machine.OnSpindleStateChanging += _machine_OnSpindleStateChanging;
            }
            catch (MotionException ex)
            {
                MessageBox.Show(ex.Message, ex.StackTrace);
            }

            if (File.Exists(Settings.Default.WaferLastFile))
            {
                _currentWafer = ExtensionMethods.DeserilizeObject<TempWafer>(Settings.Default.WaferLastFile);

            }
            else
            {
                _currentWafer = new TempWafer
                {
                    Width = 60,
                    Height = 48,
                    IndexH = 2,
                    IndexW = 3,
                    Thickness = 0.5,
                    IsRound = false
                };
            }
            AjustWaferTechnology(_currentWafer);
            _flowMeter = new FlowMeter("COM9");
            _flowMeter.GetData += _flowMeter_GetData;
            InitCommands();


            var viewfinders = ExtensionMethods.DeserilizeObject<ViewFindersVM>(ProjectPath.GetFilePathInFolder(APP_SETTINGS_FOLDER, "Viewfinders.json")).DivideDoubles(1000);

            CamVM.ScaleGridView = viewfinders;
            CamVM.RealCutWidthView = viewfinders.RealCutWidth;
            CamVM.CutWidthView = viewfinders.CorrectingCutWidth;

        }

       

        private void _flowMeter_GetData(decimal obj)
        {
            Flow = (double)obj;
        }

        


        

        private void _machine_OnSpindleStateChanging(object? obj, SpindleEventArgs e)
        {
            SpindleFreqView = e.Rpm;
            SpindleCurrentView = e.Current;
            SpindleOnFreq = e.OnFreq;
            SpindleAccelarating = e.Accelerating;
            SpindleDeccelarating = e.Deccelarating;
            SpindleStops = e.Stop;
        }

        private void _machine_OnBitmapChanged(object? sender, VideoCaptureEventArgs e)
        {
            CamVM.Bi = e.Image;
        }

        private void _machine_OnValveStateChanged(object? sender, ValveEventArgs eventArgs)
        {
            switch (eventArgs.Valve)
            {
                case Valves.Blowing:
                    BlowingValveView = eventArgs.State;
                    break;
                case Valves.Coolant:
                    CoolantValveView = eventArgs.State;
                    break;
                case Valves.ChuckVacuum:
                    ChuckVacuumValveView = eventArgs.State;
                    break;
            }
        }

        private void _machine_OnSensorStateChanged(object? sender, SensorsEventArgs eventArgs)
        {
            switch (eventArgs.Sensor)
            {
                case Sensors.ChuckVacuum:
                    ChuckVacuumSensorView = eventArgs.State;
                    break;
                case Sensors.Air:
                    AirSensorView = eventArgs.State;
                    break;
                case Sensors.Coolant:
                    CoolantSensorView = eventArgs.State;
                    break;
                case Sensors.SpindleCoolant:
                    SpindleCoolantSensorView = eventArgs.State;
                    break;
            }
        }

        private void _machine_OnAxisMotionStateChanged(object? sender, AxisStateEventArgs e)
        {
            try
            {
                var state = new AxisStateView(Math.Round(e.Position, 3), Math.Round(e.CmdPosition, 3), e.NLmt, e.PLmt, e.MotionDone, e.MotionStart);

                switch (e.Axis)
                {
                    case Ax.X:
                        XAxis = state;
                        SubstrVM.XView = state.Position;
                        break;
                    case Ax.Y:
                        YAxis = state;
                        SubstrVM.YView = state.Position;
                        break;
                    case Ax.Z:
                        ZAxis = state;
                        break;
                    case Ax.U:
                        UAxis = state;
                        SubstrVM.YView = state.Position;
                        break;
                }
            }
            catch (Exception)
            {
                //throw;
            }
        }

        //[ICommand]
        //private async Task ClickOnImage(object o)
        //{
        //    var point = (Point)o;

        //    var x = XAxis.Position - point.X * CamVM.CameraScale;
        //    var y = YAxis.Position + point.Y * CamVM.CameraScale;


        //    _machine.SetVelocity(Velocity.Service);
        //    await Task.WhenAll(
        //    _machine.MoveAxInPosAsync(Ax.X, x),
        //    _machine.MoveAxInPosAsync(Ax.Y, y, true));
        //}
        private async void CamVM_ImageClicked(object sender, ImageClickedArgs e)
        {
            var x = XAxis.Position - e.X;
            var y = YAxis.Position + e.Y;


            _machine.SetVelocity(Velocity.Service);
            await Task.WhenAll(
            _machine.MoveAxInPosAsync(Ax.X, x),
            _machine.MoveAxInPosAsync(Ax.Y, y, true));
        }
        //[ICommand]
        //private async Task LeftClickOnWafer(object o)
        //{
        //    var points = (Point[])o;
        //    var x = 0d;
        //    var y = 0d;
        //    var index = WaferView.ShapeSize[0] > WaferView.ShapeSize[1] ? 0 : 1;
        //    x = points[index].X * 1.4 * WaferView.ShapeSize[index];
        //    y = points[index].Y * 1.4 * WaferView.ShapeSize[index];

        //    x = _machine.TranslateSpecCoor(Place.CameraChuckCenter, -x, Ax.X);
        //    y = _machine.TranslateSpecCoor(Place.CameraChuckCenter, -y, Ax.Y);

        //    _machine.SetVelocity(Velocity.Service);
        //    await Task.WhenAll(
        //    _machine.MoveAxInPosAsync(Ax.X, x),
        //    _machine.MoveAxInPosAsync(Ax.Y, y));
        //}

        //[ICommand]
        //private async Task RightClickOnWafer(object o)
        //{
        //    var points = (Point[])o;
        //    var x = 0d;
        //    var y = 0d;
        //    var index = WaferView.ShapeSize[0] > WaferView.ShapeSize[1] ? 0 : 1;
        //    x = points[index].X * 1.4 * WaferView.ShapeSize[index];
        //    y = points[index].Y * 1.4 * WaferView.ShapeSize[index];

        //    x = _machine.TranslateSpecCoor(Place.CameraChuckCenter, -x, Ax.X);
        //    y = _machine.TranslateSpecCoor(Place.CameraChuckCenter, -Substrate.GetNearestY(y), Ax.Y);

        //    _machine.SetVelocity(Velocity.Service);
        //    await Task.WhenAll(
        //    _machine.MoveAxInPosAsync(Ax.X, x),
        //    _machine.MoveAxInPosAsync(Ax.Y, y));
        //}

        private async void SubstrVM_SubstrateClicked(object sender, SubstrateClickedArgs e)
        {
            var x = 0d;
            var y = 0d;

            if (e.IsByLeftButtonClicked)
            {
                x = _machine.TranslateSpecCoor(Place.CameraChuckCenter, -e.X, Ax.X);
                y = _machine.TranslateSpecCoor(Place.CameraChuckCenter, -e.Y, Ax.Y);
            }
            else
            {
                x = _machine.TranslateSpecCoor(Place.CameraChuckCenter, -x, Ax.X);
                y = _machine.TranslateSpecCoor(Place.CameraChuckCenter, -Substrate.GetNearestY(y), Ax.Y);
            }
            
            _machine.SetVelocity(Velocity.Service);
                await Task.WhenAll(
                _machine.MoveAxInPosAsync(Ax.X, x),
                _machine.MoveAxInPosAsync(Ax.Y, y));
        }
        private void GetSubscriptions(IObservable<IProcessNotify> dicingProcess)
        {
#pragma warning disable VSTHRD101 // Avoid unsupported async delegates

            dicingProcess.OfType<ProcessStateChanged>()
                .Subscribe(state =>
                {
                    switch (state.DestinationState)
                    {
                        case State.Correction:
                            {
                                ChangeScreensRegime(true);
                                _isProcessInCorrection = true;
                                _isSingleCutAvaliable = true;
                                _isReadyForAligning = true;
                                CutWidthMarkerVisibility = true;
                            }
                            break;
                        case State.TeachSides:
                            {
                                _isSingleCutAvaliable = true;
                                _isReadyForAligning = true;
                            }
                            break;
                        default:
                            break;
                    }
                });
            dicingProcess.OfType<ProcessStateChanging>()
                .Subscribe(async state =>
                {
                    ProcessPercentage = _dicingProcess.ProcessPercentage;
                    var tracingTask = Task.CompletedTask;
                    var tracingTaskCancellationTokenSource = new CancellationTokenSource();
                    switch (state.SourceState)
                    {
                        case State.Correction:
                            {
                                ChangeScreensRegime(false);
                                _isProcessInCorrection = false;
                                _isSingleCutAvaliable = false;
                                _isReadyForAligning = false;
                                CutWidthMarkerVisibility = false;
                                CamVM.CutOffsetView = 0;
                            }
                            break;

                        case State.TeachSides:
                            {
                                _isSingleCutAvaliable = false;
                                _isReadyForAligning = false;
                            }
                            break;

                        case State.Correction or State.Inspection:
                            {
                                SubstrVM.AddControlPoint(XAxis.Position, YAxis.Position, -Substrate.CurrentSideAngle);
                            }
                            break;

                        case State.Cutting or State.SingleCut:
                            //Cancellation the tracing after cutting is end.
                            {

                                tracingTaskCancellationTokenSource.Cancel();

                                var rotateTransform = new RotateTransform(
                                    -WaferCurrentSideAngle,
                                    //BCCenterXView,
                                    //BCCenterYView
                                    SubstrVM.BCCenterXView,
                                    SubstrVM.BCCenterYView
                                );

                                //var point1 = rotateTransform.Transform(new Point(XTrace, YTrace + WaferCurrentShiftView));
                                //var point2 = rotateTransform.Transform(new Point(XTraceEnd, YTrace + WaferCurrentShiftView));
                                //point1 = new TranslateTransform(-BCCenterXView, -BCCenterYView).Transform(point1);
                                //point2 = new TranslateTransform(-BCCenterXView, -BCCenterYView).Transform(point2);

                                var point1 = rotateTransform.Transform(new Point(SubstrVM.XTrace, SubstrVM.YTrace + SubstrVM.WaferCurrentShiftView));
                                var point2 = rotateTransform.Transform(new Point(SubstrVM.XTraceEnd, SubstrVM.YTrace + SubstrVM.WaferCurrentShiftView));
                                point1 = new TranslateTransform(-SubstrVM.BCCenterXView, -SubstrVM.BCCenterYView).Transform(point1);
                                point2 = new TranslateTransform(-SubstrVM.BCCenterXView, -SubstrVM.BCCenterYView).Transform(point2);

                                TracesCollectionView.Add(new TraceLine
                                {
                                    XStart = point1.X,
                                    XEnd = point2.X,
                                    YStart = point1.Y,
                                    YEnd = point2.Y
                                });

                                TracesCollectionView = new ObservableCollection<TraceLine>(TracesCollectionView);
                                //XTrace = new double();
                                //YTrace = new double();
                                //XTraceEnd = new double();

                                SubstrVM.ResetTrace();

                                try
                                {
                                    await tracingTask;
                                }
                                catch (OperationCanceledException)
                                {
                                }
                                finally
                                {
                                    tracingTask?.Dispose();
                                }

                            }
                            break;
                    }

                    switch (state.DestinationState)
                    {
                        case State.GoingNextDepthZ:
                            {
                                ProcessStatus = "Работа";
                            }
                            break;
                        case State.Inspection:
                            {
                                ProcessStatus = "Инспекция";
                            }
                            break;
                        case State.Correction:
                            {
                                ProcessStatus = "Коррекция";
                            }
                            break;
                        case State.TeachSides:
                            {
                                ProcessStatus = "Обучение";
                            }
                            break;
                        case State.Cutting or State.SingleCut:
                            //Starting a tracing after cutting is started;
                            {
                                tracingTaskCancellationTokenSource = new CancellationTokenSource();
                                BladeTracingTaskAsync(tracingTaskCancellationTokenSource.Token);
                            }
                            break;
                        case State.ProcessEnd:
                            {
                                SubstrVM.ResetWaferView();
                                AjustWaferTechnology(_currentWafer);
                                MsgBox.Success("Процесс завершён.", "Процесс");
                            }
                            break;
                        case State.ProcessInterrupted:
                            {
                                SubstrVM.ResetWaferView();
                                AjustWaferTechnology(_currentWafer);
                                MsgBox.Fatal("Процесс прерван оператором.", "Процесс");
                            }
                            break;
                    }
                });
#pragma warning restore VSTHRD101 // Avoid unsupported async delegates

            dicingProcess.OfType<RotationStarted>()
                    .Subscribe(rotation =>
                    {
                        SubstrVM.WvAngle = rotation.Angle;
                        SubstrVM.RotatingTime = rotation.Duration.Seconds;
                        SubstrVM.WvRotate ^= true;
                    });
            dicingProcess.OfType<WaferAligningChanged>()
                .Subscribe(arg =>
                {
                    //WaferCurrentShiftView = Substrate.CurrentShift;
                    SubstrVM.WaferCurrentShiftView = Substrate.CurrentShift;
                    WaferCurrentSideAngle = Substrate.CurrentSideAngle;
                });

            dicingProcess.OfType<ProcessMessage>()
                .Subscribe(arg =>
                {

                    switch (arg.MessageType)
                    {
                        case MessageType.Info:
                            Growl.Info(arg.Message);
                            break;
                        case MessageType.Warning:
                            Growl.Warning(arg.Message);
                            break;
                        case MessageType.Danger:
                            Growl.Error(arg.Message);
                            break;
                        case MessageType.ToChangeCurrentStateTo:
                            ProcessStatus += $" -> {arg.Message}";
                            break;
                        default:
                            break;
                    }
                });
            dicingProcess.OfType<CheckPointOccured>()
                 .Subscribe(arg =>
                 {
                     SubstrVM.AddControlPoint(XAxis.Position, YAxis.Position, -Substrate.CurrentSideAngle);
                 });
            dicingProcess.Subscribe(
                arg => { },
                ex =>
                {
                    try
                    {
                        if (ex is ProcessInterruptedException interruptedException)
                        {
                            //interruption actions
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                },
                () =>
                {
                    _machine.StartCamera(0);
                });
        }
        //private void ResetWaferView()
        //{
        //    WvAngle = default;
        //    ResetView ^= true;
        //}

        public double WaferCurrentSideAngle { get; set; }

        private async Task BladeTracingTaskAsync(CancellationToken cancellationToken)
        {
            //XTrace = XAxis.Position;
            //YTrace = YAxis.Position;

            SubstrVM.XTrace = XAxis.Position;
            SubstrVM.YTrace = YAxis.Position;

            await Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    //XTraceEnd = XAxis.Position;
                    SubstrVM.XTraceEnd = XAxis.Position;
                    await Task.Delay(100);
                }
            });

            cancellationToken.ThrowIfCancellationRequested();
        }


        private void ImplementMachineSettings()
        {
            var axesConfigs = ExtensionMethods
                .DeserilizeObject<MachineAxesConfiguration>(ProjectPath.GetFilePathInFolder(APP_SETTINGS_FOLDER, "AxesConfigs.json"));

            Guard.IsNotNull(axesConfigs, nameof(axesConfigs));

            var xpar = new MotionDeviceConfigs
            {
                maxAcc = 180,
                maxDec = 180,
                maxVel = 50,
                axDirLogic = (int)DirLogic.DIR_ACT_HIGH,
                plsOutMde = (int)PulseOutMode.OUT_DIR,
                reset = (int)HomeReset.HOME_RESET_EN,
                acc = Settings.Default.XAcc,
                dec = Settings.Default.XDec,
                ppu = Settings.Default.XPPU,
                homeVelLow = Settings.Default.XVelLow,
                homeVelHigh = Settings.Default.XVelService
            };
            var ypar = new MotionDeviceConfigs
            {
                maxAcc = 180,
                maxDec = 180,
                maxVel = 50,
                plsOutMde = (int)PulseOutMode.OUT_DIR_ALL_NEG,
                axDirLogic = (int)DirLogic.DIR_ACT_HIGH,
                reset = (int)HomeReset.HOME_RESET_EN,
                acc = Settings.Default.YAcc,
                dec = Settings.Default.YDec,
                ppu = Settings.Default.YPPU,
                plsInMde = (int)PulseInMode.AB_4X,
                homeVelLow = Settings.Default.YVelLow,
                homeVelHigh = Settings.Default.YVelService
            };
            var zpar = new MotionDeviceConfigs
            {
                maxAcc = 180,
                maxDec = 180,
                maxVel = 50,
                axDirLogic = (int)DirLogic.DIR_ACT_HIGH,
                plsOutMde = (int)PulseOutMode.OUT_DIR,
                reset = (int)HomeReset.HOME_RESET_EN,
                acc = Settings.Default.ZAcc,
                dec = Settings.Default.ZDec,
                ppu = Settings.Default.ZPPU,
                homeVelLow = Settings.Default.ZVelLow,
                homeVelHigh = Settings.Default.ZVelService
            };
            Settings.Default.UPPU = 1000;
            var upar = new MotionDeviceConfigs
            {
                maxAcc = 180,
                maxDec = 180,
                maxVel = 50,
                axDirLogic = (int)DirLogic.DIR_ACT_HIGH,
                plsOutMde = (int)PulseOutMode.OUT_DIR,
                reset = (int)HomeReset.HOME_RESET_EN,
                acc = Settings.Default.UAcc,
                dec = Settings.Default.UDec,
                ppu = Settings.Default.UPPU,
                homeVelLow = Settings.Default.UVelLow,
                homeVelHigh = Settings.Default.UVelService
            };

            _machine.AddAxis(Ax.X, axesConfigs.XLine)
                .WithConfigs(xpar)
                .WithVelRegime(Velocity.Fast, Settings.Default.XVelHigh)
                .WithVelRegime(Velocity.Slow, Settings.Default.XVelLow)
                .WithVelRegime(Velocity.Service, Settings.Default.XVelService)
                .Build();

            _machine.AddAxis(Ax.U, axesConfigs.ULine)
                .WithConfigs(upar)
                .WithVelRegime(Velocity.Fast, Settings.Default.UVelHigh)
                .WithVelRegime(Velocity.Slow, Settings.Default.UVelLow)
                .WithVelRegime(Velocity.Service, Settings.Default.UVelService)
                .Build();

            _machine.AddAxis(Ax.Z, axesConfigs.ZLine)
                .WithConfigs(zpar)
                .WithVelRegime(Velocity.Fast, Settings.Default.ZVelHigh)
                .WithVelRegime(Velocity.Slow, Settings.Default.ZVelLow)
                .WithVelRegime(Velocity.Service, Settings.Default.ZVelService)
                .Build();

            _machine.AddAxis(Ax.Y, axesConfigs.YLine)
                .WithConfigs(ypar)
                .WithVelRegime(Velocity.Fast, Settings.Default.YVelHigh)
                .WithVelRegime(Velocity.Slow, Settings.Default.YVelLow)
                .WithVelRegime(Velocity.Service, Settings.Default.YVelService)
                .Build();

            _machine.ConfigureHomingForAxis(Ax.X)
                .SetHomingDirection(AxDir.Neg)
                .SetHomingMode(HmMode.MODE2_Lmt)
                .SetHomingReset(HomeRst.HOME_RESET_EN)
                .SetHomingVelocity(Settings.Default.XVelService)
                .SetPositionAfterHoming(Settings.Default.XLoad)
                .Configure();

            _machine.ConfigureHomingForAxis(Ax.Y)
                .SetHomingDirection(AxDir.Neg)
                .SetHomingMode(HmMode.MODE2_Lmt)
                .SetHomingReset(HomeRst.HOME_RESET_EN)
                .SetHomingVelocity(Settings.Default.YVelService)
                .SetPositionAfterHoming(Settings.Default.YLoad)
                .Configure();

            _machine.ConfigureHomingForAxis(Ax.Z)
                .SetHomingDirection(AxDir.Neg)
                .SetHomingMode(HmMode.MODE2_Lmt)
                .SetHomingReset(HomeRst.HOME_RESET_EN)
                .SetHomingVelocity(Settings.Default.ZVelService)
                .SetPositionAfterHoming(1)
                .Configure();

            _machine.AddGroup(Groups.XY, Ax.X, Ax.Y);

            _machine.ConfigureValves(new Dictionary<Valves, (Ax, Do)>
                {
                    {Valves.Blowing, (Ax.Z, Do.Out6)},
                    {Valves.ChuckVacuum, (Ax.Z, Do.Out4)},
                    {Valves.Coolant, (Ax.U, Do.Out4)},
                    {Valves.SpindleContact, (Ax.U, Do.Out5)}
                });

            _machine.ConfigureSensors(new Dictionary<Sensors, (Ax, Di, Boolean, string)>
                {
                    {Sensors.Air, (Ax.Z, Di.In1, false, "Воздух")},
                    {Sensors.ChuckVacuum, (Ax.X, Di.In2, false, "Вакуум")},
                    {Sensors.Coolant, (Ax.U, Di.In2, false, "СОЖ")},
                    {Sensors.SpindleCoolant, (Ax.Y, Di.In2, false, "Охлаждение шпинделя")}
                });

            _machine.SetVelocity(VelocityRegime);

            //BCCenterXView = Settings.Default.XDisk;
            //BCCenterYView = Settings.Default.YObjective - Settings.Default.DiskShift;
            //CCCenterXView = Settings.Default.XObjective;
            //CCCenterYView = Settings.Default.YObjective;


            SubstrVM.BCCenterXView = Settings.Default.XDisk;
            SubstrVM.BCCenterYView = Settings.Default.YObjective - Settings.Default.DiskShift;
            SubstrVM.CCCenterXView = Settings.Default.XObjective;
            SubstrVM.CCCenterYView = Settings.Default.YObjective;


            ZBladeTouchView = Settings.Default.ZTouch;

            _machine.ConfigureGeometryFor(Place.BladeChuckCenter)
                .SetCoordinateForPlace(Ax.X, Settings.Default.XDisk)
                .SetCoordinateForPlace(Ax.Y, Settings.Default.YObjective + Settings.Default.DiskShift)
                .Build();

            _machine.ConfigureGeometryFor(Place.CameraChuckCenter)
                .SetCoordinateForPlace(Ax.X, Settings.Default.XObjective)
                .SetCoordinateForPlace(Ax.Y, Settings.Default.YObjective)
                .Build();

            _machine.ConfigureGeometryFor(Place.Loading)
                .SetCoordinateForPlace(Ax.X, Settings.Default.XLoad)
                .SetCoordinateForPlace(Ax.Y, Settings.Default.YLoad)
                .Build();

            _machine.ConfigureGeometryFor(Place.ZBladeTouch)
                .SetCoordinateForPlace(Ax.Z, Settings.Default.ZTouch)
                .Build();

            _machine.ConfigureGeometryFor(Place.ZFocus)
                .SetCoordinateForPlace(Ax.Z, Settings.Default.ZObjective)
                .Build();


            _machine.ConfigureDoubleFeatures(new Dictionary<MFeatures, double>
            {
                {MFeatures.CameraBladeOffset, Settings.Default.DiskShift},
                {MFeatures.ZBladeTouch, Settings.Default.ZTouch},
                {MFeatures.CameraFocus, 3}
            });

            _machine.SetBridgeOnSensors(Sensors.ChuckVacuum, Settings.Default.VacuumSensorDsbl);
            _machine.SetBridgeOnSensors(Sensors.Coolant, Settings.Default.CoolantSensorDsbl);
            _machine.SetBridgeOnSensors(Sensors.Air, Settings.Default.AirSensorDsbl);
            _machine.SetBridgeOnSensors(Sensors.SpindleCoolant, Settings.Default.SpindleCoolantSensorDsbl);
        }

        private void AjustWaferTechnology(IWafer wafer)
        {
            _currentWafer = wafer;
            //var fileName = Settings.Default.WaferLastFile;
            //var waf = new TempWafer();
            var tech = new Technology();

            // TODO ASYNC
            //StatMethods.DeSerializeObjectJson<TempWafer>(fileName).CopyPropertiesTo(waf);
            IShape shape = wafer.IsRound ? new Circle2D(wafer.Diameter) : new Rectangle2D(wafer.Height, wafer.Width);
            Substrate = new Substrate2D(wafer.IndexH, wafer.IndexW, wafer.Thickness, shape);

            TracesCollectionView = new ObservableCollection<TraceLine>();
            SubstrVM.ClearControlPoints();
            //ResetView ^= true;
            var wfViewFactory = new WaferViewFactory(Substrate);
            //ResetView ^= true;



            //WaferView = new();
            //WaferView.SetView(wfViewFactory);
            //WaferView.IsRound = wafer.IsRound;

            SubstrVM.WaferView.SetView(wfViewFactory);
            SubstrVM.WaferView.IsRound = wafer.IsRound;


            var fileName = Settings.Default.TechnologyLastFile;
            if (File.Exists(fileName))
            {
                // TODO ASYNC
                StatMethods.DeSerializeObjectJson<Technology>(fileName).CopyPropertiesTo(tech);
                PropContainer.Technology = tech;
                
                //Thickness = wafer.Thickness;
                SubstrVM.Thickness = wafer.Thickness;
            }

        }

        private async Task WaitForConfirmationAsync()
        {
            UserConfirmation = false;
            await Task.Run(() =>
            {
                while (!UserConfirmation) Task.Delay(1).Wait();
            });
        }

        private void ChangeScreensRegime(bool regime) => (CentralView, RightSideView) = (RightSideView, CentralView);
    }
}
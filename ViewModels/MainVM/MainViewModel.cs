using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Advantech.Motion;
using DicingBlade.Classes.WaferGrid;
using DicingBlade.Classes.Miscellaneous;
using DicingBlade.Classes.Processes;
using DicingBlade.Classes.Technology;
using DicingBlade.Classes.WaferGeometry;
using DicingBlade.Properties;
using DicingBlade.Utility;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Machine.MotionDevices;
using MachineClassLibrary.Machine.Parts;
using MachineClassLibrary.SFC;
using MathNet.Numerics.Integration;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Diagnostics;
using PropertyChanged;
using Growl = HandyControl.Controls.Growl;
using MsgBox = HandyControl.Controls.MessageBox;
using System.Collections.ObjectModel;

namespace DicingBlade.ViewModels
{

    [AddINotifyPropertyChangedInterface]
    internal partial class MainViewModel : IMainViewModel
    {
        const string APP_SETTINGS_FOLDER = "AppSettings";

        private readonly DicingBladeMachine _machine;
        private ITechnology _technology;
        private IComSensor _flowMeter;
        private IProcess _dicingProcess;
        private bool _isProcessInCorrection;
        private bool _isSingleCutAvaliable;
        private IWafer _currentWafer;
        public bool CutWidthMarkerVisibility
        {
            get; set;
        }
        public double Flow
        {
            get; set;
        }
        public Velocity VelocityRegime { get; set; } = Velocity.Fast;
        public object CentralView
        {
            get; set;
        }
        public object RightSideView
        {
            get; set;
        }
        public CameraVM CamVM { get; set; } = new();
        public AxisStateView XAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        public AxisStateView YAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        public AxisStateView ZAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        public AxisStateView UAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        public bool ChuckVacuumValveView
        {
            get; set;
        }
        public bool CoolantValveView
        {
            get; set;
        }
        public bool BlowingValveView
        {
            get; set;
        }
        public bool ChuckVacuumSensorView
        {
            get; set;
        }
        public bool CoolantSensorView
        {
            get; set;
        }
        public bool AirSensorView
        {
            get; set;
        }
        public bool SpindleCoolantSensorView
        {
            get; set;
        }
        public double ZBladeTouchView
        {
            get; set;
        }
        public int SpindleFreqView
        {
            get; set;
        }
        public double SpindleCurrentView
        {
            get; set;
        }
        public bool SpindleOnFreq
        {
            get; private set;
        }
        public bool SpindleAccelerating
        {
            get; private set;
        }
        public bool SpindleDecelerating
        {
            get; private set;
        }
        public bool SpindleStops
        {
            get; private set;
        }
        public Wafer Wafer
        {
            get; set;
        }
        public Wafer2D Substrate
        {
            get; private set;
        }
        public bool TeachVScaleMarkersVisibility
        {
            get; private set;
        }
        public string ProcessStatus
        {
            get; private set;
        }
        public bool UserConfirmation
        {
            get; private set;
        }
        public bool IsNot04PP100 { get; set; } = true;
        private bool _isSingleCutAvailable;
        public double TeachMarkersRatio { get; } = 2;
        public SubstrateVM SubstrateVM { get; set; } = new();
        private readonly List<IDisposable> _subscriptions = new();
        private readonly ILogger _logger;
        public MainViewModel(DicingBladeMachine machine, DicingMachineConfiguration machineConfiguration, ILoggerProvider loggerProvider)
        {
            _logger = loggerProvider.CreateLogger("MainVM");
            IsNot04PP100 = !machineConfiguration.IsO4PP100;
            SubstrateVM.SubstrateClicked += SubstrVM_SubstrateClicked;
            CamVM.ImageClicked += CamVM_ImageClicked;
            CutLinesVM = new(null, Settings.Default.XObjective,
               Settings.Default.YObjective, Settings.Default.XDisk,
               (Settings.Default.YObjective + Settings.Default.DiskShift));
            CentralView = CutLinesVM; //SubstrateVM;
            RightSideView = CamVM;
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
                _machine.AdjustWidthToHeight = true;
                _machine.StartCamera(0, 0);
                _machine.OnBitmapChanged += CamVM.OnBitmapChanged;

                _machine.OnAxisMotionStateChanged += _machine_OnAxisMotionStateChanged;
                _machine.OnAxisMotionStateChanged += SubstrateVM.OnAxisMotionStateChanged;
                _machine.OnSensorStateChanged += _machine_OnSensorStateChanged;
                _machine.OnValveStateChanged += _machine_OnValveStateChanged;
                var result = _machine.TryConnectSpindle();
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
            AdjustWaferTechnology(_currentWafer);
            _flowMeter = new FlowMeter("COM9");
            _flowMeter.GetData += _flowMeter_GetData;
            InitCommands();

            var viewfinders = ExtensionMethods.DeserilizeObject<ViewFindersVM>(ProjectPath.GetFilePathInFolder(APP_SETTINGS_FOLDER, "Viewfinders.json")).DivideDoubles(1000);

            CamVM.ScaleGridView = viewfinders;
            CamVM.RealCutWidthView = viewfinders.RealCutWidth;
            CamVM.CutWidthView = viewfinders.CorrectingCutWidth;

            //----

            FirstPasses = new ObservableCollection<Pass>()
                        {
                            new()
                            {
                                DepthShare = 30,
                                FeedSpeed = 10,
                                RPM = 28000
                            },
                            new()
                            {
                                PassNumber = 1,
                                DepthShare = 30,
                                FeedSpeed = 5,
                                RPM = 25000
                            },
                            new()
                            {
                                PassNumber = 2,
                                DepthShare = 40,
                                FeedSpeed = 3,
                                RPM = 18000
                            },
                        };

            FirstCuttingStep = new()
            {
                Count = 5,
                Index = 5.2,
                Length = 48,
                Passes = FirstPasses
            };

            CutSet ??= new CutSet
            {
                CuttingSteps = new List<CuttingStep>
                {
                    new CuttingStep
                    {
                        Count = 1,
                        Index = _currentWafer.IndexH,
                        Length = 60,
                        StepNumber = 1,
                        Passes = new ObservableCollection<Pass>
                        {
                            new Pass
                            {
                                DepthShare = 100,
                                FeedSpeed = _technology.FeedSpeed,
                                PassNumber = 0,
                                RPM = _technology.SpindleFreq
                            }
                        }
                    }
                }
            };
            //var cutLines = CutLinesFactory.GetCutLines(CutSet, 0, 0, 0, new Blade() { Diameter = 56, Thickness = 0.1 });

            

            _machine.OnAxisMotionStateChanged += CutLinesVM.eventHandler;

            //----



            _logger.Log(LogLevel.Information, "App started");
        }

        public CutSet CutSet
        {
            get;
            set;
        }

        public CuttingStep FirstCuttingStep
        {
            get;
            set;
        }

        public ObservableCollection<Pass> FirstPasses
        {
            get;
            set;
        }

        public CutLinesVM CutLinesVM
        {
            get;
            set;
        }

        public CutLines CutLinesView
        {
            get;
            set;
        }
        public void LogMessage(LogLevel loggerLevel, string message) => _logger.Log(loggerLevel, message);
        private void _flowMeter_GetData(decimal obj)
        {
            Flow = (double)obj;
        }
        private void _machine_OnSpindleStateChanging(object? obj, SpindleEventArgs e)
        {
            SpindleFreqView = e.Rpm;
            SpindleCurrentView = e.Current;
            SpindleOnFreq = e.OnFreq;
            SpindleAccelerating = e.Accelerating;
            SpindleDecelerating = e.Deccelarating;
            SpindleStops = e.Stop;
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
                        break;
                    case Ax.Y:
                        YAxis = state;
                        break;
                    case Ax.Z:
                        ZAxis = state;
                        break;
                    case Ax.U:
                        UAxis = state;
                        break;
                }
            }
            catch (Exception)
            {
                //throw;
            }
        }
        private async void CamVM_ImageClicked(object sender, ImageClickedArgs e)
        {
            var x = XAxis.Position + e.X;
            var y = YAxis.Position + e.Y;
            _machine.SetVelocity(Velocity.Service);
            await Task.WhenAll(
            _machine.MoveAxInPosAsync(Ax.X, x),
            _machine.MoveAxInPosAsync(Ax.Y, y, true));
        }
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

            _subscriptions.ForEach(s => s.Dispose());

            dicingProcess.OfType<ProcessStateChanged>()
                .Subscribe(state =>
                {
                    switch (state.DestinationState)
                    {
                        case State.Correction:
                            {
                                ChangeScreensRegime();
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
                })
                .AddToSubscriptions(_subscriptions);
            dicingProcess.OfType<ProcessStateChanging>()
                .Select(state => Observable.FromAsync(async () =>
                {
                    ProcessPercentage = ((DicingProcess3)_dicingProcess).ProcessPercentage;
                    var tracingTask = Task.CompletedTask;
                    var tracingTaskCancellationTokenSource = new CancellationTokenSource();
                    switch (state.SourceState)
                    {
                        case State.Correction:
                            {
                                ChangeScreensRegime();
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
                                SubstrateVM.AddControlPoint(XAxis.Position, YAxis.Position, -Substrate.CurrentSideAngle);
                            }
                            break;

                        case State.Cutting or State.SingleCut:
                            //Cancellation the tracing after cutting is end.
                            {
                                tracingTaskCancellationTokenSource.Cancel();
                                SubstrateVM.AddTrace(-WaferCurrentSideAngle);

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
                                await SubstrateVM.BladeTracingTaskAsync(tracingTaskCancellationTokenSource.Token);
                            }
                            break;
                        case State.ProcessEnd:
                            {
                                SubstrateVM.ResetWaferView();
                                AdjustWaferTechnology(_currentWafer);
                                MsgBox.Success("Процесс завершён.", "Процесс");
                            }
                            break;
                        case State.ProcessInterrupted:
                            {
                                SubstrateVM.ResetWaferView();
                                AdjustWaferTechnology(_currentWafer);
                                MsgBox.Fatal("Процесс прерван оператором.", "Процесс");
                            }
                            break;
                    }
                }))
                .Concat()
                .Subscribe()
                .AddToSubscriptions(_subscriptions);

            dicingProcess.OfType<NewCutLinesOccurred>()
                .Subscribe(arg =>
                {
                    CutLinesVM.SetCutLines(arg.CutLines);
                })
                .AddToSubscriptions(_subscriptions);
#pragma warning restore VSTHRD101 // Avoid unsupported async delegates

            dicingProcess.OfType<RotationStarted>()
                    .Subscribe(rotation =>
                    {
                        SubstrateVM.WvAngle = rotation.Angle;
                        SubstrateVM.RotatingTime = rotation.Duration.Seconds;
                        SubstrateVM.WvRotate ^= true;
                    })
                    .AddToSubscriptions(_subscriptions);
            dicingProcess.OfType<WaferAligningChanged>()
                .Subscribe(arg =>
                {
                    SubstrateVM.WaferCurrentShiftView = Substrate.CurrentShift;
                    WaferCurrentSideAngle = Substrate.CurrentSideAngle;
                })
                .AddToSubscriptions(_subscriptions);

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
                })
                .AddToSubscriptions(_subscriptions);
            dicingProcess.OfType<CheckPointOccured>()
                 .Subscribe(arg =>
                 {
                     SubstrateVM.AddControlPoint(XAxis.Position, YAxis.Position, -Substrate.CurrentSideAngle);
                 })
                 .AddToSubscriptions(_subscriptions);
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
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, $"Throwed in {nameof(GetSubscriptions)} method.  ");
                    }
                },() =>_machine.StartCamera(0))
                .AddToSubscriptions(_subscriptions);
        }
        public double WaferCurrentSideAngle
        {
            get; set;
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
                plsOutMde = (int)PulseOutMode.OUT_DIR_DIR_NEG,
                reset = (int)HomeReset.HOME_RESET_EN,
                acc = Settings.Default.XAcc,
                dec = Settings.Default.XDec,
                ppu = Settings.Default.XPPU,
                homeVelLow = Settings.Default.XVelLow,
                homeVelHigh = Settings.Default.XVelService,
                hLmtLogic = (uint)HLmtLogic.HLMT_ACT_HIGH
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
                ppu = 21333,//Settings.Default.YPPU,
                //denominator = 12,
                plsInMde = (int)PulseInMode.AB_4X,
                homeVelLow = Settings.Default.YVelLow,
                homeVelHigh = Settings.Default.YVelService,
                hLmtLogic = (uint)HLmtLogic.HLMT_ACT_HIGH
            };
            var zpar = new MotionDeviceConfigs
            {
                maxAcc = 180,
                maxDec = 180,
                maxVel = 50,
                axDirLogic = (int)DirLogic.DIR_ACT_HIGH,
                plsOutMde = (int)PulseOutMode.OUT_DIR_DIR_NEG,
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

            _machine.AddAxis(Ax.Y, axesConfigs.YLine)
               .WithConfigs(ypar)
               .WithVelRegime(Velocity.Fast, Settings.Default.YVelHigh)
               .WithVelRegime(Velocity.Slow, Settings.Default.YVelLow)
               .WithVelRegime(Velocity.Service, Settings.Default.YVelService)
               .Build();

            _machine.AddAxis(Ax.Z, axesConfigs.ZLine)
                .WithConfigs(zpar)
                .WithVelRegime(Velocity.Fast, Settings.Default.ZVelHigh)
                .WithVelRegime(Velocity.Slow, Settings.Default.ZVelLow)
                .WithVelRegime(Velocity.Service, Settings.Default.ZVelService)
                .Build();

            _machine.AddAxis(Ax.U, axesConfigs.ULine)
                 .WithConfigs(upar)
                 .WithVelRegime(Velocity.Fast, Settings.Default.UVelHigh)
                 .WithVelRegime(Velocity.Slow, Settings.Default.UVelLow)
                 .WithVelRegime(Velocity.Service, Settings.Default.UVelService)
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
                    {Valves.ChuckVacuum, (Ax.Z, Do.Out5)},
                    {Valves.Coolant, (Ax.U, Do.Out4)},
                    {Valves.SpindleContact, (Ax.U, Do.Out5)}
                });

            _machine.ConfigureSensors(new Dictionary<Sensors, (Ax, Di, bool, string)>
                {
                    {Sensors.Air, (Ax.U, Di.In3, true, "Воздух")},
                    {Sensors.ChuckVacuum, (Ax.X, Di.In3, true, "Вакуум")},
                    {Sensors.Coolant, (Ax.Z, Di.In3, false, "СОЖ")},
                    {Sensors.SpindleCoolant, (Ax.Y, Di.In3, false, "Охлаждение шпинделя")}
                });

            _machine.SetVelocity(VelocityRegime);

            SubstrateVM.BCCenterXView = Settings.Default.XDisk;
            SubstrateVM.BCCenterYView = Settings.Default.YObjective - Settings.Default.DiskShift;
            SubstrateVM.CCCenterXView = Settings.Default.XObjective;
            SubstrateVM.CCCenterYView = Settings.Default.YObjective;


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
        private void AdjustWaferTechnology(IWafer wafer)
        {
            _currentWafer = wafer;
            var tech = new Technology();

            // TODO ASYNC
            IShape shape = wafer.IsRound ? new Circle2D(wafer.Diameter) : new Rectangle2D(wafer.Height, wafer.Width);
            Substrate = new Substrate2D(wafer.IndexH, wafer.IndexW, wafer.Thickness, shape);

            SubstrateVM.ClearTrace();
            SubstrateVM.ClearControlPoints();
            var wfViewFactory = new WaferViewFactory(Substrate);
            SubstrateVM.WaferView.SetView(wfViewFactory);
            SubstrateVM.WaferView.IsRound = wafer.IsRound;


            var fileName = Settings.Default.TechnologyLastFile;
            if (File.Exists(fileName))
            {
                // TODO ASYNC
                StatMethods.DeSerializeObjectJson<Technology>(fileName).CopyPropertiesTo(tech);
                PropContainer.Technology = tech;
                SubstrateVM.Thickness = wafer.Thickness;
            }
        }
        private async Task WaitForConfirmationAsync()
        {
            UserConfirmation = false;
            while (!UserConfirmation) await Task.Delay(100);
        }
        private void ChangeScreensRegime() => (CentralView, RightSideView) = (RightSideView, CentralView);
    }
}
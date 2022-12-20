using Advantech.Motion;
using DicingBlade.Classes;
using DicingBlade.Classes.BehaviourTrees;
using DicingBlade.Classes.Processes;
using DicingBlade.Classes.Test;
using DicingBlade.Properties;
using DicingBlade.Utility;
using DicingBlade.Views;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Machine.MotionDevices;
using MachineClassLibrary.SFC;
using MachineClassLibrary.VideoCapture;
using Microsoft.Toolkit.Diagnostics;
using netDxf;
using netDxf.Entities;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using Point = System.Windows.Point;
using Growl = HandyControl.Controls.Growl;
using MsgBox = HandyControl.Controls.MessageBox;

namespace DicingBlade.ViewModels
{

    [AddINotifyPropertyChangedInterface]
    internal partial class MainViewModel : IMainViewModel
    {
        const string APP_SETTINGS_FOLDER = "AppSettings";

        private readonly ExceptionsAgregator _exceptionsAgregator;
        private readonly DicingBladeMachine _machine;

        public double CameraScale { get; private set; }
        private bool _homeDone;
        private ITechnology _technology;

        private IComSensor _flowMeter;

        private Task _tracingTask;
        private CancellationToken _tracingTaskCancellationToken;

        private CancellationTokenSource _tracingTaskCancellationTokenSource;
        private WatchSettingsService _settingsService;
        public bool CutWidthMarkerVisibility { get; set; }
        //[Obsolete("Only for design data", true)]
        //public MainViewModel()
        //    : this(null, null)
        //{
        //    if ((bool)DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue)
        //    {
        //        throw new Exception("Use only for design mode");
        //    }
        //}

        public MainViewModel(ExceptionsAgregator exceptionsAgregator, DicingBladeMachine machine)
        {
            _exceptionsAgregator = exceptionsAgregator;

            Test = false;
            Cols = new[] { 0, 1 };
            Rows = new[] { 2, 1 };

            ClickOnImageCmd = new Command(args => ClickOnImage(args));
            LeftClickOnWaferCmd = new Command(args => LeftClickOnWafer(args));
            RightClickOnWaferCmd = new Command(args => RightClickOnWafer(args));
            

            Bi = new BitmapImage();

            _exceptionsAgregator.SetShowMethod(s => { MessageBox.Show(s); });
            _exceptionsAgregator.SetShowMethod(s => { ProcessMessage = s; });
            CameraScale = Settings.Default.CameraScale;

            try
            {
                _technology = StatMethods.DeSerializeObjectJson<Technology>(Settings.Default.TechnologyLastFile);

                _machine = machine;

                ImplementMachineSettings();

                _machine.SwitchOffValve(Valves.Blowing);
                _machine.SwitchOffValve(Valves.ChuckVacuum);
                _machine.SwitchOffValve(Valves.Coolant);
                _machine.SwitchOffValve(Valves.SpindleContact);

                _machine.StartCamera(0,1);
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
            
            _settingsService = new();
            _settingsService.OnSettingsChangedEvent += _settingsService_OnSettingsChangedEvent;
            AjustWaferTechnology();
            _flowMeter = new FlowMeter("COM9");
            _flowMeter.GetData += _flowMeter_GetData;
            InitCommands();


            var viewfinders = ExtensionMethods.DeserilizeObject<ViewFindersVM>(ProjectPath.GetFilePathInFolder(APP_SETTINGS_FOLDER, "Viewfinders.json")).DivideDoubles(1000);

            ScaleGridView = viewfinders;
            RealCutWidthView = viewfinders.RealCutWidth;
            CutWidthView = viewfinders.CorrectingCutWidth;

        }

        public ScaleGrid ScaleGridView { get; private set; }
        public double Flow { get; set; }
        private void _flowMeter_GetData(decimal obj)
        {
            Flow = (double)obj;
        }

        private void _settingsService_OnSettingsChangedEvent(object sender, SettingsChangedEventArgs eventArgs)
        {

            //if (eventArgs.Settings is IWafer)
            //{
            //    var wf = (IWafer)eventArgs.Settings;
            //    var substrate = new Substrate2D(wf.IndexH, wf.IndexW, wf.Thickness, new Rectangle2D(wf.Height, wf.Width));
            //    if (Process is null)
            //    {
            //        Substrate = substrate;
            //    }
            //    else
            //    {
            //        substrate.SetSide(Process.CurrentDirection);
            //    }

            //    var wfViewFactory = new WaferViewFactory(substrate);
            //    ResetView ^= true;
            //    WaferView = new();
            //    WaferView.SetView(wfViewFactory);
            //}
        }

        public Velocity VelocityRegime { get; set; } = Velocity.Fast;
        public ObservableCollection<TraceLine> TracesCollectionView { get; set; } = new();
        public double CutOffsetView { get; set; }
        public double XTrace { get; set; }
        public double YTrace { get; set; }
        public double XTraceEnd { get; set; }
        public BitmapImage Bi { get; set; }
        public double XView { get; set; }
        public double YView { get; set; }
        public double ZView { get; set; }
        public double UView { get; set; }
        public bool XpLmtView { get; set; }
        public bool YpLmtView { get; set; }
        public bool ZpLmtView { get; set; }
        public bool UpLmtView { get; set; }
        public bool XnLmtView { get; set; }
        public bool YnLmtView { get; set; }
        public bool ZnLmtView { get; set; }
        public bool UnLmtView { get; set; }
        public bool XMotDoneView { get; set; }
        public bool YMotDoneView { get; set; }
        public bool ZMotDoneView { get; set; }
        public bool UMotDoneView { get; set; }
        public bool ChuckVacuumValveView { get; set; }
        public bool CoolantValveView { get; set; }
        public bool BlowingValveView { get; set; }
        public bool ChuckVacuumSensorView { get; set; }
        public bool CoolantSensorView { get; set; }
        public bool TestBool { get; set; } = true;
        public bool AirSensorView { get; set; }
        public double CoolantRateView { get; set; }
        public bool SpindleCoolantSensorView { get; set; }
        public double BCCenterXView { get; set; }
        public double BCCenterYView { get; set; }
        public double CCCenterXView { get; set; }
        public double CCCenterYView { get; set; }
        public double ZBladeTouchView { get; set; }
        public int SpindleFreqView { get; set; }
        public double SpindleCurrentView { get; set; }
        public double WaferCurrentShiftView { get; set; }
        public ObservableCollection<TraceLine> ControlPointsView { get; set; } = new();
        public bool ResetView { get; private set; }
        public bool SpindleOnFreq { get; private set; }
        public bool SpindleAccelarating { get; private set; }
        public bool SpindleDeccelarating { get; private set; }
        public bool SpindleStops { get; private set; }
        public double PointX { get; set; }
        public double PointY { get; set; }
        public double CutWidthView { get; set; } = 0.05;
        public double RealCutWidthView { get; set; } = 0.13;
        public Wafer Wafer { get; set; }
        public Wafer2D Substrate { get; private set; }
        public WaferView WaferView { get; set; }
        public ObservableCollection<TracePath> Traces { get; set; }
        public double WvAngle { get; set; }
        public bool WvRotate { get; set; }
        public double RotatingTime { get; set; } = 1;
        public Map Condition { get; set; }
        public double Thickness { get; set; } = 1;
        public bool Test { get; set; }
        public int[] Rows { get; set; }

        public int[] Cols { get; set; }
        public ICommand RotateCmd { get; }
        public ICommand TestCmd { get; }
        public ICommand ClickOnImageCmd { get; }
        public ICommand LeftClickOnWaferCmd { get; }
        public ICommand RightClickOnWaferCmd { get; }

        public Visibility TeachVScaleMarkersVisibility { get; private set; } = Visibility.Hidden;
        public string ProcessMessage { get; private set; }
        public string ProcessStatus { get; private set; }
        public bool UserConfirmation { get; private set; }
        public double TeachMarkersRatio { get; } = 2;
        public bool MachineIsStillView { get; private set; }


        private DicingProcess _dicingProcess;
        private bool _isProcessInCorrection;
        private bool _isSingleCutAvaliable;

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
            Bi = e.Image;
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
                switch (e.Axis)
                {
                    case Ax.X:
                        XView = e.Position;
                        XpLmtView = e.PLmt;
                        XnLmtView = e.NLmt;
                        XMotDoneView = e.MotionStart ? true : e.MotionDone ? false : XMotDoneView;
                        //XAxis = new AxisStateView(Math.Round(e.Position, 3), Math.Round(e.CmdPosition, 3), e.NLmt, e.PLmt, e.MotionDone, e.MotionStart);
                        //LaserViewfinderX = _coorSystem?.FromSub(LMPlace.FileOnWaferUnderLaser, XAxis.Position, YAxis.Position)[0] * FileScale ?? 0;
                        //CameraViewfinderX = _coorSystem?.FromSub(LMPlace.FileOnWaferUnderCamera, XAxis.Position, YAxis.Position)[0] * FileScale ?? 0;
                        break;
                    case Ax.Y:
                        YView = e.Position;
                        YpLmtView = e.PLmt;
                        YnLmtView = e.NLmt;
                        YMotDoneView = e.MotionStart ? true : e.MotionDone ? false : YMotDoneView;
                        //YAxis = new AxisStateView(Math.Round(e.Position, 3), Math.Round(e.CmdPosition, 3), e.NLmt, e.PLmt, e.MotionDone, e.MotionStart);
                        //LaserViewfinderY = _coorSystem?.FromSub(LMPlace.FileOnWaferUnderLaser, XAxis.Position, YAxis.Position)[1] * FileScale ?? 0;
                        //CameraViewfinderY = _coorSystem?.FromSub(LMPlace.FileOnWaferUnderCamera, XAxis.Position, YAxis.Position)[1] * FileScale ?? 0;
                        break;
                    case Ax.Z:
                        ZView = e.Position;
                        ZpLmtView = e.PLmt;
                        ZnLmtView = e.NLmt;
                        ZMotDoneView = e.MotionStart ? true : e.MotionDone ? false : ZMotDoneView;
                        //ZAxis = new AxisStateView(Math.Round(e.Position, 3), Math.Round(e.CmdPosition, 3), e.NLmt, e.PLmt, e.MotionDone, e.MotionStart);
                        break;
                    case Ax.U:
                        UView = e.Position;
                        UpLmtView = e.PLmt;
                        UnLmtView = e.NLmt;
                        UMotDoneView = e.MotionStart ? true : e.MotionDone ? false : UMotDoneView;
                        break;
                }
                MachineIsStillView = XMotDoneView & YMotDoneView & ZMotDoneView & UMotDoneView;
            }
            catch (Exception)
            {
                //throw;
            }
        }

        private async Task ClickOnImage(object o)
        {
            var point = (Point)o;
            PointX = XView - point.X * CameraScale;
            PointY = YView + point.Y * CameraScale;
            _machine.SetVelocity(Velocity.Service);
            _machine.MoveAxInPosAsync(Ax.X, PointX);
            _machine.MoveAxInPosAsync(Ax.Y, PointY, true);
        }

        private async Task LeftClickOnWafer(object o)
        {
            var points = (Point[])o;

            if (WaferView.ShapeSize[0] > WaferView.ShapeSize[1])
            {
                PointX = points[0].X * 1.4 * WaferView.ShapeSize[0];
                PointY = points[0].Y * 1.4 * WaferView.ShapeSize[0];
            }
            else
            {
                PointX = points[1].X * 1.4 * WaferView.ShapeSize[1];
                PointY = points[1].Y * 1.4 * WaferView.ShapeSize[1];
            }

            PointX = _machine.TranslateSpecCoor(Place.CameraChuckCenter, -PointX, Ax.X);
            PointY = _machine.TranslateSpecCoor(Place.CameraChuckCenter, -PointY, Ax.Y);

            _machine.SetVelocity(Velocity.Service);
            _machine.MoveAxInPosAsync(Ax.X, PointX);
            _machine.MoveAxInPosAsync(Ax.Y, PointY);
        }

        private async Task RightClickOnWafer(object o)
        {
            var points = (Point[])o;

            if (WaferView.ShapeSize[0] > WaferView.ShapeSize[1])
            {
                PointX = points[0].X * 1.4 * WaferView.ShapeSize[0];
                PointY = points[0].Y * 1.4 * WaferView.ShapeSize[0];
            }
            else
            {
                PointX = points[1].X * 1.4 * WaferView.ShapeSize[1];
                PointY = points[1].Y * 1.4 * WaferView.ShapeSize[1];
            }

            PointX = _machine.TranslateSpecCoor(Place.CameraChuckCenter, -PointX, Ax.X);
            PointY = _machine.TranslateSpecCoor(Place.CameraChuckCenter, -Substrate.GetNearestY(PointY), Ax.Y);

            _machine.SetVelocity(Velocity.Service);
            _machine.MoveAxInPosAsync(Ax.X, PointX);
            _machine.MoveAxInPosAsync(Ax.Y, PointY);
        }

        //private void SetRotation(double angle, double time)
        //{
        //    WvAngle = angle;
        //    RotatingTime = time;
        //    WvRotate ^= true;
        //}

        //private void Machine_OnAirWanished(DiEventArgs eventArgs)
        //{
        //    throw new NotImplementedException();
        //}

        //private void Func(object args)
        //{
        //}

        //private void GetWvAngle(bool changing)
        //{
        //}

        

        private async Task KeyDownAsync(object args)
        {
            var key = (KeyEventArgs)args;

            //if (key.Key == Key.Multiply)
            //{
            //    UserConfirmation = true;
            //}
            
                       
            //if (key.Key == Key.Divide)
            //{
            //    if (_homeDone)
            //    {
            //        if (_dicingProcess is null)
            //        {
            //            var blade = new Blade();
            //            blade.Diameter = 55.6;
            //            blade.Thickness = 0.11;
            //            Substrate.ResetWafer();
            //            //Process5 = new Process5(_machine, Substrate, blade, _technology);
            //            //Process5.GetRotationEvent += SetRotation;
            //            //Process5.ChangeScreensEvent += ChangeScreensRegime;
            //            //Process5.BladeTracingEvent += Process_BladeTracingEvent;
            //            //Process5.OnProcessStatusChanged += Process_OnProcessStatusChanged;
            //            //Process5.OnProcParamsChanged += Process_OnProcParamsChanged;
            //            //Process5.OnControlPointAppeared += Process_OnControlPointAppeared;
            //            //Process5.OnProcStatusChanged += Process5_OnProcStatusChanged;

            //            _dicingProcess = new DicingProcess(_machine, Substrate, blade, _technology);
            //            GetSubscriptions(_dicingProcess);
            //        }
            //        else
            //        {
            //            //Process5.StartPauseProc();
            //        } 
            //    }
            //    else
            //    {
            //        MessageBox.Show("Необходимо обнулить координаты. Нажмите клавишу Home");
            //    }

            //}

            //if (key.Key == Key.A)
            //{
            //    if (VelocityRegime == Velocity.Step)
            //    {
            //        var velocity = VelocityRegime;
            //        _machine.SetVelocity(Velocity.Service);
            //        var y = YView - 0.03;
            //        await _machine.MoveAxInPosAsync(Ax.Y, y + Substrate.CurrentIndex, true);
            //        await Task.Delay(300);
            //        await _machine.MoveAxInPosAsync(Ax.Y, y + 0.03 + Substrate.CurrentIndex, true);
            //        _machine.SetVelocity(velocity);
            //    }
            //    else
            //    {
            //        _machine.GoWhile(Ax.Y, AxDir.Pos);
            //    }
            //}

            //if (key.Key == Key.Z)
            //{
            //    if (VelocityRegime == Velocity.Step)
            //    {
            //        var velocity = VelocityRegime;
            //        _machine.SetVelocity(Velocity.Service);
            //        var y = YView - 0.03;
            //        await _machine.MoveAxInPosAsync(Ax.Y, y - Substrate.CurrentIndex, true);
            //        await Task.Delay(300);
            //        await _machine.MoveAxInPosAsync(Ax.Y, y + 0.03 - Substrate.CurrentIndex, true);
            //        _machine.SetVelocity(velocity);
            //    }
            //    else
            //    {
            //        _machine.GoWhile(Ax.Y, AxDir.Neg);
            //    }
            //}

                    

                       
           
            

            
            if (Keyboard.IsKeyDown(Key.RightCtrl) && Keyboard.IsKeyDown(Key.Oem6))
            {
                await _machine?.GoThereAsync(Place.CameraChuckCenter);
            }

            if (Keyboard.IsKeyDown(Key.RightCtrl) && Keyboard.IsKeyDown(Key.Oem4))
            {
                await _machine?.GoThereAsync(Place.BladeChuckCenter);
            }
                       

            //if (key.Key == Key.F12)
            //{
            //    if (_dicingProcess is not null)
            //    {
            //        //_dicingProcess.EmergencyScript();
            //        //_dicingProcess.WaitProcDoneAsync().Wait();
            //        //_dicingProcess = null;
            //        Substrate = null;
            //        ResetWaferView();
            //        AjustWaferTechnology();
            //        //MessageBox.Show("Процесс экстренно прерван оператором.", "Процесс", MessageBoxButton.OK, MessageBoxImage.Exclamation);

            //    }
            //}
            //key.Handled = true;
        }

        private void GetSubscriptions(IObservable<IProcessNotify> dicingProcess)
        {
#pragma warning disable VSTHRD101 // Avoid unsupported async delegates

            dicingProcess.OfType<ProcessStateChanged>()
                .Subscribe(async state =>
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
                    switch (state.SourceState)
                    {
                        case State.Correction:
                            {
                                ChangeScreensRegime(false);
                                _isProcessInCorrection = false;
                                _isSingleCutAvaliable = false;
                                _isReadyForAligning = false;
                                CutWidthMarkerVisibility = false;
                                CutOffsetView = 0;
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
                                var rotateTransform = new RotateTransform(-Substrate.CurrentSideAngle);
                                var point = new TranslateTransform(-CCCenterXView, -CCCenterYView).Transform(new Point(XView, YView));
                                var point1 = rotateTransform.Transform(new Point(point.X - 1, point.Y + WaferCurrentShiftView));
                                var point2 = rotateTransform.Transform(new Point(point.X + 1, point.Y + WaferCurrentShiftView));
                                List<TraceLine> temp = new(ControlPointsView);
                                temp.ForEach(br => br.Brush = Brushes.Blue);
                                temp.Add(new TraceLine()
                                { XStart = point1.X, XEnd = point2.X, YStart = point1.Y, YEnd = point2.Y, Brush = Brushes.OrangeRed });
                                ControlPointsView = new ObservableCollection<TraceLine>(temp);
                            }
                            break;

                        case State.Cutting or State.SingleCut:
                            //Cancellation the tracing after cutting is end.
                            {

                                _tracingTaskCancellationTokenSource.Cancel();

                                var rotateTransform = new RotateTransform(
                                    -WaferCurrentSideAngle,
                                    BCCenterXView,
                                    BCCenterYView
                                );

                                var point1 = rotateTransform.Transform(new Point(XTrace, YTrace + WaferCurrentShiftView));
                                var point2 = rotateTransform.Transform(new Point(XTraceEnd, YTrace + WaferCurrentShiftView));
                                point1 = new TranslateTransform(-BCCenterXView, -BCCenterYView).Transform(point1);
                                point2 = new TranslateTransform(-BCCenterXView, -BCCenterYView).Transform(point2);

                                TracesCollectionView.Add(new TraceLine
                                {
                                    XStart = point1.X,
                                    XEnd = point2.X,
                                    YStart = point1.Y,
                                    YEnd = point2.Y
                                });

                                TracesCollectionView = new ObservableCollection<TraceLine>(TracesCollectionView);
                                XTrace = new double();
                                YTrace = new double();
                                XTraceEnd = new double();

                                try
                                {
                                    await _tracingTask;
                                }
                                catch (OperationCanceledException)
                                {
                                }
                                finally
                                {
                                    _tracingTaskCancellationTokenSource.Dispose();
                                    _tracingTask?.Dispose();
                                }

                            }
                            break;
                    }

                    switch(state.DestinationState) 
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
                                _tracingTaskCancellationTokenSource = new CancellationTokenSource();
                                _tracingTaskCancellationToken = _tracingTaskCancellationTokenSource.Token;
                                _tracingTask = new Task(() => BladeTracingTaskAsync(), _tracingTaskCancellationToken);
                                _tracingTask.Start();
                            }
                            break;
                        case State.ProcessEnd:
                            {
                                ResetWaferView();
                                AjustWaferTechnology();
                                MsgBox.Success("Процесс завершён.", "Процесс");
                            }
                            break;
                        case State.ProcessInterrupted:
                            {
                                ResetWaferView();
                                AjustWaferTechnology();
                                MsgBox.Fatal("Процесс прерван оператором.", "Процесс");
                            }
                            break;
                    }                                       
                });
#pragma warning restore VSTHRD101 // Avoid unsupported async delegates

            dicingProcess.OfType<RotationStarted>()
                    .Subscribe(rotation =>
                    {
                        WvAngle = rotation.Angle;
                        RotatingTime = rotation.Duration.Seconds;
                        WvRotate ^= true;
                    });
            dicingProcess.OfType<WaferAligningChanged>()
                .Subscribe(arg =>
                {
                    WaferCurrentShiftView = Substrate.CurrentShift;
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

        //private void Process5_OnProcStatusChanged(object sender, Process5.Stat e)
        //{
        //    switch (e)
        //    {
        //        case Process5.Stat.Cancelled:
        //            break;
        //        case Process5.Stat.End:
        //            Process5.WaitProcDoneAsync().ContinueWith
        //                (async t =>
        //                {
        //                    Process5 = null;
        //                    Substrate = null;
        //                    ResetWaferView();
        //                    AjustWaferTechnology();
        //                    await _machine.GoThereAsync(Place.Loading);
        //                    MessageBox.Show("Процесс завершён.", "Процесс", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        //                });
                    
        //            break;
        //        default:
        //            break;
        //    }
        //}

        private void ResetWaferView()
        {
            WvAngle = default;
            ResetView ^= true;
        }
        //private void Process_OnControlPointAppeared()
        //{
        //    var rotateTransform = new RotateTransform(-Substrate.CurrentSideAngle);

        //    var point = new TranslateTransform(-CCCenterXView, -CCCenterYView).Transform(new Point(XView, YView));
        //    var point1 = rotateTransform.Transform(new Point(point.X - 1, point.Y + WaferCurrentShiftView));
        //    var point2 = rotateTransform.Transform(new Point(point.X + 1, point.Y + WaferCurrentShiftView));
        //    List<TraceLine> temp = new(ControlPointsView);
        //    temp.ForEach(br => br.Brush = Brushes.Blue);
        //    temp.Add(new TraceLine()
        //    { XStart = point1.X, XEnd = point2.X, YStart = point1.Y, YEnd = point2.Y, Brush = Brushes.OrangeRed });
        //    ControlPointsView = new ObservableCollection<TraceLine>(temp);
        //}

        //private void Process_OnProcParamsChanged(object arg1, ProcParams procParamsEventArgs)
        //{
        //    WaferCurrentShiftView = procParamsEventArgs.currentShift;
        //    WaferCurrentSideAngle = procParamsEventArgs.currentSideAngle;
        //}

        public double WaferCurrentSideAngle { get; set; }

        //private void Process_OnProcessStatusChanged(string status)
        //{
        //    ProcessStatus = status;
        //}

        //private async void Process_BladeTracingEvent(bool trace)
        //{
        //    if (trace)
        //    {
        //        _tracingTaskCancellationTokenSource = new CancellationTokenSource();
        //        _tracingTaskCancellationToken = _tracingTaskCancellationTokenSource.Token;
        //        _tracingTask = new Task(() => BladeTracingTaskAsync(), _tracingTaskCancellationToken);

        //        _tracingTask.Start();
        //    }
        //    else
        //    {
        //        _tracingTaskCancellationTokenSource.Cancel();

        //        var rotateTransform = new RotateTransform(

        //            //-Substrate.CurrentSideAngle,
        //            -WaferCurrentSideAngle,
        //            BCCenterXView,
        //            BCCenterYView
        //        );

        //        var point1 = rotateTransform.Transform(new Point(XTrace, YTrace + WaferCurrentShiftView));
        //        var point2 = rotateTransform.Transform(new Point(XTraceEnd, YTrace + WaferCurrentShiftView));
        //        point1 = new TranslateTransform(-BCCenterXView, -BCCenterYView).Transform(point1);
        //        point2 = new TranslateTransform(-BCCenterXView, -BCCenterYView).Transform(point2);

        //        TracesCollectionView.Add(new TraceLine
        //        {
        //            XStart = point1.X,
        //            XEnd = point2.X,
        //            YStart = point1.Y,
        //            YEnd = point2.Y
        //        });

        //        TracesCollectionView = new ObservableCollection<TraceLine>(TracesCollectionView);
        //        XTrace = new double();
        //        YTrace = new double();
        //        XTraceEnd = new double();

        //        try
        //        {
        //            await _tracingTask;
        //        }
        //        catch (OperationCanceledException)
        //        {
        //        }
        //        finally
        //        {
        //            _tracingTaskCancellationTokenSource.Dispose();
        //            _tracingTask?.Dispose();
        //        }
        //    }
        //}

        private async Task BladeTracingTaskAsync()
        {
            XTrace = XView;
            YTrace = YView;

            while (!_tracingTaskCancellationToken.IsCancellationRequested)
            {
                XTraceEnd = XView;
                Task.Delay(100).Wait();
            }

            _tracingTaskCancellationToken.ThrowIfCancellationRequested();
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

            //_machine.SwitchOffValve(Valves.Blowing);
            //_machine.SwitchOffValve(Valves.ChuckVacuum);
            //_machine.SwitchOffValve(Valves.Coolant);
            //_machine.SwitchOffValve(Valves.SpindleContact);

            _machine.ConfigureSensors(new Dictionary<Sensors, (Ax, Di, Boolean, string)>
                {
                    {Sensors.Air, (Ax.Z, Di.In1, false, "Воздух")},
                    {Sensors.ChuckVacuum, (Ax.X, Di.In2, false, "Вакуум")},
                    {Sensors.Coolant, (Ax.U, Di.In2, false, "СОЖ")},
                    {Sensors.SpindleCoolant, (Ax.Y, Di.In2, false, "Охлаждение шпинделя")}
                });

            _machine.SetVelocity(VelocityRegime);

            BCCenterXView = Settings.Default.XDisk;
            BCCenterYView = Settings.Default.YObjective - Settings.Default.DiskShift;
            CCCenterXView = Settings.Default.XObjective;
            CCCenterYView = Settings.Default.YObjective;
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

        private void AjustWaferTechnology(int side = -1)
        {
            var fileName = Settings.Default.WaferLastFile;
            var waf = new TempWafer();
            var tech = new Technology();

            if (File.Exists(fileName))
            {
                // TODO ASYNC
                StatMethods.DeSerializeObjectJson<TempWafer>(fileName).CopyPropertiesTo(waf);
                IShape shape = waf.IsRound ? new Circle2D(waf.Diameter) : new Rectangle2D(waf.Height, waf.Width);
                Substrate = new Substrate2D(waf.IndexH, waf.IndexW, waf.Thickness, shape);
            }
            TracesCollectionView = new ObservableCollection<TraceLine>();
            ControlPointsView = new ObservableCollection<TraceLine>();
            //ResetView ^= true;
            var wfViewFactory = new WaferViewFactory(Substrate);
            //ResetView ^= true;
            WaferView = new();
            WaferView.SetView(wfViewFactory);
            WaferView.IsRound = waf.IsRound;

            fileName = Settings.Default.TechnologyLastFile;
            if (File.Exists(fileName))
            {
                // TODO ASYNC
                StatMethods.DeSerializeObjectJson<Technology>(fileName).CopyPropertiesTo(tech);
                PropContainer.Technology = tech;
                Thickness = waf.Thickness;
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

        private void ChangeScreensRegime(bool regime)
        {
            if (regime && Cols[0] == 0) Change();
            else if (Cols[0] == 1) Change();
        }
    }
}
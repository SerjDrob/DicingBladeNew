using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Laser.Parameters;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using Stateless;
using Stateless.Graph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DicingBlade.Classes;
using System.Windows;
using Microsoft.Toolkit.Diagnostics;

namespace DicingBlade.Classes.Processes
{

    public interface IProcessNotify { }
    [Flags]
    public enum State
    {
        ProcessStarted = 1,
        TeachSides = 1<<1,
        Processing = 1<<2,
        Inspection = 1<<3,
        ProcessInterrupted = 1<<4,
        ProcessEnd = 1<<5,
        Cutting = 1<<6,
        GoingTransferingZ = 1<<7,
        GoingNextCutXY = 1<<8,
        GoingNextDepthZ = 1<<9,
        MovingNextSide = 1<<10,
        Correction = 1<<11,
        MovingTeachNextDirection = 1<<12,
        SingleCut = 1<<13
    }
    public enum Trigger
    {
        Next,
        Pause,
        Deny,
        End,
        NextCut,
        NextSide,
        Inspection,
        Correction,
        Teach,
        DoSingleCut
    }
    /// <summary>
    /// Invoked in the new state before invoking OnEntry methods
    /// </summary>
    /// <param name="SourceState">An old State</param>
    /// <param name="DestinationState">A new State</param>
    /// <param name="Trigger">A trigger caused the new State</param>
    public record ProcessStateChanging(State SourceState, State DestinationState, Trigger Trigger):IProcessNotify;
    /// <summary>
    /// Invoked in the new state after invoking OnEntry methods
    /// </summary>
    /// <param name="SourceState">An old State</param>
    /// <param name="DestinationState">A new State</param>
    /// <param name="Trigger">A trigger caused the new State</param>
    public record ProcessStateChanged(State SourceState, State DestinationState, Trigger Trigger) : IProcessNotify;
    public record RotationStarted(double Angle, TimeSpan Duration) : IProcessNotify;
    public record WaferAligningChanged():IProcessNotify;

    [Serializable]
    public class ProcessInterruptedException : Exception
    {
        public ProcessInterruptedException() { }
        public ProcessInterruptedException(string message) : base(message) { }
        public ProcessInterruptedException(string message, Exception inner) : base(message, inner) { }
        protected ProcessInterruptedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    public class DicingProcess : IProcess
    {
        private readonly DicingBladeMachine _machine;
        private readonly Wafer2D _wafer;
        private readonly Blade _blade;
        private readonly ITechnology _technology;
        private StateMachine<State, Trigger> _stateMachine;
        private bool _inProcess = false;
        private readonly double _waferThickness;
        private readonly ISubject<IProcessNotify> _subject;
        private double _xActual;
        private double _yActual;
        private double _zActual;
        private double _uActual;
        private double _bladeTransferGapZ = 4;
        private bool _spindleWorking;
        private double _zRatio = 0;
        private double _feedSpeed;
        private double _undercut;
        private double _lastCutY;
        private double _inspectX;
        private bool _isWaitingToTeach = false;
        //private Sensors _offSensors = 0;
        private CheckCutControl _checkCut;
        private RepeatUntillCancell _repeatUntillCancell;

        private bool IsCutting { get; set; } = false;
        public Visibility CutWidthMarkerVisibility { get; set; } = Visibility.Hidden;
        public double CutOffset { get; set; } = 0;

        public bool ProcessEndOrDenied { get => _stateMachine?.State.HasFlag(State.ProcessEnd | State.ProcessInterrupted) ?? false; }

        public DicingProcess(DicingBladeMachine machine, Wafer2D wafer, Blade blade, ITechnology technology)
        {
            _machine = machine ?? throw new ProcessException("Не выбрана установка для процесса");
            _wafer = wafer ?? throw new ProcessException("Не выбрана подложка для процесса");
            _blade = blade ?? throw new ProcessException("Не выбран диск для процесса");
            _technology = technology;

            _subject = new Subject<IProcessNotify>();
        }


        public async Task CreateProcess()
        {
            _stateMachine = new StateMachine<State, Trigger>(State.ProcessStarted, FiringMode.Queued);

            _stateMachine.Configure(State.ProcessStarted)
                .OnActivateAsync(async () => 
                {
                    await GoTransferingHeightZAsync();
                    await _machine.GoThereAsync(Place.Loading); 
                    _isWaitingToTeach = true;
                })//TODO New initial state on loading point
                .InternalTransitionAsync(Trigger.DoSingleCut, async tr =>
                {
                    if (tr.Source == State.TeachSides || tr.Source == State.Correction)
                    {
                        var x = _xActual;
                        var y = _yActual;

                        var line = _wafer.GetCurrentCut();
                        var xCoor = line.Start.X;
                        xCoor = xCoor + Math.Sign(xCoor) * _blade.XGap(_wafer.Thickness);
                        var yCoor = y - _machine.GetFeature(MFeatures.CameraBladeOffset);
                        xCoor = _machine.TranslateSpecCoor(Place.BladeChuckCenter, -xCoor, Ax.X);
                        var xy = new double[] { xCoor, yCoor };
                        var z = _machine.TranslateSpecCoor(Place.ZBladeTouch, _wafer[_zRatio] + _undercut, Ax.Z);

                        await GoTransferingHeightZAsync();                       
                        await _machine.MoveGpInPosAsync(Groups.XY, xy, true);
                        await _machine.MoveAxInPosAsync(Ax.Z, z);
                        await CuttingXAsync();
                        await GoCameraPointXyzAsync();
                    }
                })
                .Permit(Trigger.Deny, State.ProcessInterrupted)
                .Permit(Trigger.Teach, State.TeachSides);
//------------------------Teaching-------------------------------------
            _stateMachine.Configure(State.TeachSides)
                .SubstateOf(State.ProcessStarted)
                .OnEntryAsync(async () =>
                {
                    await StartLearningAsync();
                })
                .Ignore(Trigger.Teach)
                .Permit(Trigger.Next, State.MovingTeachNextDirection);

            _stateMachine.Configure(State.MovingTeachNextDirection)
                .SubstateOf(State.ProcessStarted)
                .OnEntryAsync(async () =>
                {
                    Learning();
                    if (!_wafer.IsLastSide) await MovingNextDirAsync();
                })
                .Ignore(Trigger.Teach)
                .PermitDynamic(Trigger.Next, () => 
                {
                    return _wafer.IncrementSide() ? State.TeachSides : State.Processing;
                });
//-----------------------Processing------------------------------------
            _stateMachine.Configure(State.Processing)
                .SubstateOf(State.ProcessStarted)
                .InitialTransition(State.GoingTransferingZ)
                .Permit(Trigger.Inspection, State.Inspection)
                .Permit(Trigger.Correction, State.Correction);

            _stateMachine.Configure(State.GoingTransferingZ)
                .SubstateOf(State.Processing)
                .OnEntryAsync(async () =>
                {
                    await GoTransferingHeightZAsync();
                })
                .Ignore(Trigger.Teach)
                .PermitDynamic(Trigger.Next, () =>
                {
                    if (_checkCut.Check)
                    {
                        return State.Inspection;
                    }
                    else
                    {
                        return State.GoingNextCutXY;
                    }
                });

            _stateMachine.Configure(State.GoingNextCutXY)
                .SubstateOf(State.Processing)
                .OnEntryAsync(async () =>
                {
                    await GoNextCutXYAsync();
                })
                .Ignore(Trigger.Teach)
                .Permit(Trigger.Next, State.GoingNextDepthZ);

            _stateMachine.Configure(State.GoingNextDepthZ)
                .SubstateOf(State.Processing)
                .OnEntryAsync(async () =>
                {
                    await GoNextDepthZAsync();
                })
                .Ignore(Trigger.Teach)
                .Permit(Trigger.Next, State.Cutting);

            _stateMachine.Configure(State.Cutting)
                .SubstateOf(State.Processing)
                .OnEntryAsync(async () =>
                {                    
                    await CuttingXAsync();
                })
                .PermitDynamic(Trigger.Next, () =>
                {
                    if (_wafer.IncrementCut())
                    {
                        return State.GoingTransferingZ;
                    }
                    else if (_wafer.DecrementSide())
                    {
                        return State.MovingNextSide;
                    }
                    return State.ProcessEnd;
                });

            _stateMachine.Configure(State.MovingNextSide)
                .SubstateOf(State.Processing)
                .OnEntryAsync(async () =>
                {
                    await GoTransferingHeightZAsync();
                    await GoNextDirectionAsync();
                })
                .Ignore(Trigger.Teach)
                .Permit(Trigger.Next, State.GoingTransferingZ);
//-------------------------Inspection and Correction-----------------------
            _stateMachine.Configure(State.Inspection)
               .SubstateOf(State.ProcessStarted)
               .OnEntryAsync(async()=>await TakeThePhotoAsync())
               .Ignore(Trigger.Teach)
               .PermitDynamic(Trigger.Next, () =>
               {
                   if (!(_wafer.IsLastSide && _wafer.LastCutOfTheSide))
                   {
                       return State.GoingTransferingZ;
                   }
                   return State.ProcessEnd;
               });
            
            _stateMachine.Configure(State.Correction)
              .SubstateOf(State.ProcessStarted)
              .OnEntryAsync(async () =>
              {
                  await CorrectionAsync();
              })
              .PermitDynamic(Trigger.Next, () =>
              {
                  if (!(_wafer.IsLastSide && _wafer.LastCutOfTheSide))
                  {
                      return State.GoingTransferingZ;
                  }
                  return State.ProcessEnd;
              })
              .Ignore(Trigger.Teach)
              .OnExitAsync(() => EndCorrection());
//-------------------------Ending and Interruption-------------------------
            _stateMachine.Configure(State.ProcessInterrupted)
                .OnEntryAsync(async () =>
                {
                    await GoTransferingHeightZAsync();
                    await _machine.GoThereAsync(Place.Loading);
                    _subject.OnError(new ProcessInterruptedException($"The process was interrupted"));
                });

            _stateMachine.Configure(State.ProcessEnd)
                .OnEntryAsync(async () =>
                {
                    await GoTransferingHeightZAsync();
                    await _machine.GoThereAsync(Place.Loading);
                    _subject.OnCompleted();
                });

            _stateMachine.OnUnhandledTrigger((s, t) => { });
            _stateMachine.OnTransitioned(tr => _subject.OnNext(new ProcessStateChanging(tr.Source, tr.Destination, tr.Trigger)));
            _stateMachine.OnTransitionCompleted(tr =>
            {
                if (tr.Source==State.Processing && tr.Destination == State.GoingTransferingZ)
                {
                    _inProcess = true;
                }
                _subject.OnNext(new ProcessStateChanged(tr.Source, tr.Destination, tr.Trigger)); 
            });

            await _stateMachine.ActivateAsync();
        }

        private async Task CorrectionAsync()
        {
            await GoTransferingHeightZAsync();
            await GoCameraPointXyzAsync();
            _machine.SwitchOnValve(Valves.Blowing);
            await Task.Delay(500);
            _machine.SwitchOffValve(Valves.Blowing);
            CutWidthMarkerVisibility = Visibility.Visible;
            _machine.StartCamera(0);
        }
        private Task EndCorrection()
        {
            var result = MessageBox.Show($"Сместить следующие резы на {CutOffset} мм?", "", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                _wafer.AddToSideShift(CutOffset);
            }
            var nearestNum = _wafer.GetNearestNum(_yActual - _machine.GetGeometry(Place.CameraChuckCenter, Ax.Y));
            if (_wafer.CurrentCutNum != nearestNum)
            {
                result = MessageBox.Show($"Изменить номер реза на {nearestNum}?", "", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    _wafer.SetCurrentCutNum(nearestNum - 1);
                }
            }
            CutWidthMarkerVisibility = Visibility.Hidden;
            CutOffset = 0;
            _machine.FreezeCameraImage();
            _inspectX = _xActual;
            return Task.CompletedTask;
        }
        private async Task GoCameraPointXyzAsync()
        {
            _machine.SetVelocity(Velocity.Service);
            var z = _machine.TranslateSpecCoor(Place.ZBladeTouch, _wafer.Thickness + _bladeTransferGapZ, Ax.Z);
            await _machine.MoveAxInPosAsync(Ax.Z, z);

            var y = -_machine.TranslateSpecCoor(Place.BladeChuckCenter, _yActual, Ax.Y);
            y = _machine.TranslateSpecCoor(Place.CameraChuckCenter, -y, Ax.Y);
            await _machine.MoveGpInPosAsync(Groups.XY, new double[] { _inspectX, y }, true);
            await _machine.MoveAxesInPlaceAsync(Place.ZFocus);
        }
        private async Task TakeThePhotoAsync()
        {
            _machine.StartCamera(0);
            await GoTransferingHeightZAsync();
            await GoCameraPointXyzAsync();
            _machine.SwitchOnValve(Valves.Blowing);
            await Task.Delay(100);
            _machine.FreezeCameraImage();
            _machine.SwitchOffValve(Valves.Blowing);
        }
        private async Task GoNextDirectionAsync()
        {
            _machine.SetVelocity(Velocity.Service);
            await MoveNextDirAsync();
            _subject.OnNext(new WaferAligningChanged());
        }
        private async Task GoTransferingHeightZAsync()
        {
            _machine.SetVelocity(Velocity.Service);
            await _machine.MoveAxInPosAsync(Ax.Z, _machine.GetFeature(MFeatures.ZBladeTouch) - _wafer.Thickness - _bladeTransferGapZ);
        }
        private async Task CuttingXAsync()
        {
            _machine.SwitchOnValve(Valves.Coolant);
            await Task.Delay(300);
            _machine.SetAxFeedSpeed(Ax.X, _feedSpeed);
            IsCutting = true;
            var xCurLineEnd = _wafer.GetCurrentCut().End.X;
            var x = _machine.TranslateSpecCoor(Place.BladeChuckCenter, -xCurLineEnd, 0);
            await _machine.MoveAxInPosAsync(Ax.X, x);
            IsCutting = false;
            _lastCutY = _yActual;
            _checkCut.addToCurrentCut();
            _machine.SwitchOffValve(Valves.Coolant);
        }
        private async Task GoNextDepthZAsync()
        {
            _machine.SetVelocity(Velocity.Service);
            var z = _machine.TranslateSpecCoor(Place.ZBladeTouch, _wafer[_zRatio] + _undercut, Ax.Z);
            await _machine.MoveAxInPosAsync(Ax.Z, z);
        }
        private async Task GoNextCutXYAsync()
        {
            var line = _wafer.GetCurrentCut();
            _machine.SetVelocity(Velocity.Service);
            var x = line.Start.X;
            x = x + Math.Sign(x) * _blade.XGap(_wafer.Thickness);
            var y = line.Start.Y - _wafer.CurrentShift;
            var arr = _machine.TranslateActualCoors(Place.BladeChuckCenter, new (Ax, double)[] { (Ax.X, -x), (Ax.Y, -y) });
            var xy = new double[] { arr.GetVal(Ax.X), arr.GetVal(Ax.Y) };
            await _machine.MoveGpInPosAsync(Groups.XY, xy, true);
        }
        private async Task StartLearningAsync()
        {
            _machine.SetVelocity(Velocity.Service);
            await _machine.MoveAxInPosAsync(Ax.Z, _machine.GetFeature(MFeatures.ZBladeTouch) - _wafer.Thickness - _bladeTransferGapZ);
            var y = _wafer.GetNearestY(0);
            var arr = _machine.TranslateActualCoors(Place.CameraChuckCenter, new (Ax, double)[] { (Ax.X, 0), (Ax.Y, -y) });
            var point = new double[] { arr.GetVal(Ax.X), arr.GetVal(Ax.Y) };
            await _machine.MoveGpInPosAsync(Groups.XY, point);
            await _machine.MoveAxesInPlaceAsync(Place.ZFocus);
        }
        private Task Learning()
        {
            var y = _machine.TranslateActualCoors(Place.CameraChuckCenter, Ax.Y);
            _wafer.TeachSideShift(y);
            _subject.OnNext(new WaferAligningChanged());
            _wafer.TeachSideAngle(_uActual);
            return Task.CompletedTask;
        }
        private async Task MovingNextDirAsync()
        {
            _machine.SetVelocity(Velocity.Service);
            await MoveNextDirAsync(false);
            _subject.OnNext(new WaferAligningChanged());
        }
        private async Task MoveNextDirAsync(bool next = true)
        {
            double angle = _wafer.CurrentSideAngle;
            double time = 0;
            var deltaAngle = _wafer.CurrentSideAngle - _wafer.PrevSideAngle;
            if (_wafer.CurrentSideActualAngle == _wafer.CurrentSideAngle)
            {
                angle = _wafer.PrevSideActualAngle - _wafer.PrevSideAngle + _wafer.CurrentSideAngle;
                time = Math.Abs(angle - _uActual) / _machine.GetAxisSetVelocity(Ax.U);
            }
            else
            {
                angle = _wafer.CurrentSideActualAngle;
                time = Math.Abs(_wafer.CurrentSideActualAngle - _uActual) / _machine.GetAxisSetVelocity(Ax.U);
            }
            _subject.OnNext(new RotationStarted(deltaAngle, TimeSpan.FromSeconds(time)));
            await _machine.MoveAxInPosAsync(Ax.U, angle);
        }


        public async Task TriggerNextState() => await _stateMachine.FireAsync(Trigger.Next);
        public async Task TriggerSingleCutState() => await _stateMachine.FireAsync(Trigger.DoSingleCut);
        public async Task TriggerNextCutState() => await _stateMachine.FireAsync(Trigger.Correction);
        public async Task TriggerTeachState() => await _stateMachine?.FireAsync(Trigger.Teach); 

        public void EmergencyScript()
        {
            //_rootSequence?.CancellAction(true);
            _machine.Stop(Ax.X);
            _machine.MoveAxInPosAsync(Ax.Z, 0);
            _machine.StopSpindle();
        }
        public async Task StartAsync()
        {
            while (!(_wafer.IsLastSide && _wafer.LastCutOfTheSide))
            {
                await _stateMachine.FireAsync(Trigger.Next);
            }
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            
        }
        public override string ToString()
        {
            return UmlDotGraph.Format(_stateMachine.GetInfo());
        }
        public async Task Deny()
        {
            await _repeatUntillCancell.Cancel();
            await _stateMachine.FireAsync(Trigger.Deny);
        }
        public async Task Next()
        {
            Guard.IsNotNull(_stateMachine, nameof(_stateMachine));
            if (_stateMachine.State.HasFlag(State.ProcessStarted) && _isWaitingToTeach)
            {
                await _stateMachine.FireAsync(Trigger.Teach);
            }
            else if (!_stateMachine.State.HasFlag(State.Processing | State.ProcessInterrupted | State.ProcessEnd | State.Correction))
            {
                await _stateMachine.FireAsync(Trigger.Next);  
            }
            else if (_stateMachine.State == State.Processing)
            {
                await _repeatUntillCancell.Cancel();
                await _stateMachine.FireAsync(Trigger.Correction);
            }
            else if(_inProcess || _stateMachine.State == State.Correction)
            {
                _repeatUntillCancell = new RepeatUntillCancell(() => _stateMachine.FireAsync(Trigger.Next));
                await _repeatUntillCancell.Start();
            }
        }
        
        private class RepeatUntillCancell
        {
            private readonly Func<Task> _next;
            private CancellationTokenSource _cacellationTokenSource;
            private CancellationTokenSource _cacellationTokenSourceForCancelling;

            public RepeatUntillCancell(Func<Task> next)
            {
                _next = next;
            }
            public async Task Start()
            {
                Guard.IsNotNull(_next, nameof(_next));
                if (!_cacellationTokenSource?.IsCancellationRequested ?? false) return;

                _cacellationTokenSource = new CancellationTokenSource();
                _cacellationTokenSourceForCancelling = new CancellationTokenSource();

                var cancellationToken = _cacellationTokenSource.Token;
                await Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await _next.Invoke();
                    }
                });
                _cacellationTokenSourceForCancelling.Cancel();
            }

            public async Task Cancel()
            {
                Guard.IsNotNull(_cacellationTokenSourceForCancelling, nameof(_cacellationTokenSourceForCancelling));
                var token = _cacellationTokenSourceForCancelling.Token;
                _cacellationTokenSource.Cancel();
                await Task.Run(() =>
                {
                    while (!token.IsCancellationRequested) ;
                });
            }
        }

        public void ExcludeObject(IProcObject procObject)
        {
            throw new NotImplementedException();
        }
        public void IncludeObject(IProcObject procObject)
        {
            throw new NotImplementedException();
        }
        public IDisposable Subscribe(IObserver<IProcessNotify> observer) => _subject.Subscribe(observer);
               
    }
}

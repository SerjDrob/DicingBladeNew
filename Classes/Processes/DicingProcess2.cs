using DicingBlade.Classes;
using DicingBlade.Classes.Miscellaneous;
using DicingBlade.Classes.Technology;
using DicingBlade.Classes.WaferGeometry;
using DicingBlade.Utility;
using Humanizer;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Miscellanius;
using Microsoft.Toolkit.Diagnostics;
using Stateless;
using Stateless.Graph;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MsgBox = HandyControl.Controls.MessageBox;

namespace DicingBlade.Classes.Processes
{
    public partial class DicingProcess2 : IProcess
    {
        private readonly DicingBladeMachine _machine;
        private readonly Wafer2D _wafer;
        private readonly Blade _blade;
        private ITechnology _technology;
        private StateMachine<State, Trigger> _stateMachine;
        private bool _inProcess = false;
        private readonly ISubject<IProcessNotify> _subject;
        private double _xActual;
        private double _yActual;
        private double _zActual;
        private double _uActual;
        private double _bladeTransferGapZ = 1;
        private double _zRatio = 0;
        private double _lastCutY;
        private double _inspectX;
        private bool _isWaitingToTeach = false;
        private CheckCutControl _checkCut;
        private RepeatUntilCancel _repeatUntillCancell;
        private bool _afterCorrection;

        private bool IsCutting { get; set; } = false;
        public double CutOffset { get; set; } = 0;
        public int ProcessPercentage
        {
            get; set;
        }
        private int _currentScore;
        private bool _isCancelled;
        public bool ProcessEndOrDenied => (_stateMachine?.IsInState(State.ProcessEnd) ?? false) || (_stateMachine?.IsInState(State.ProcessInterrupted) ?? false);

        public DicingProcess2(DicingBladeMachine machine, Wafer2D wafer, Blade blade, ITechnology technology)
        {
            _machine = machine ?? throw new ProcessException("Не выбрана установка для процесса");
            _wafer = wafer ?? throw new ProcessException("Не выбрана подложка для процесса");
            _blade = blade ?? throw new ProcessException("Не выбран диск для процесса");
            _technology = technology;
            _subject = new Subject<IProcessNotify>();
        }


        public void RefreshTechnology(ITechnology technology) => _technology = technology;

        private void _machine_OnAxisMotionStateChanged(object sender, AxisStateEventArgs e)
        {
            switch (e.Axis)
            {
                case Ax.X:
                    _xActual = e.Position;
                    break;
                case Ax.Y:
                    _yActual = e.Position;
                    break;
                case Ax.Z:
                    _zActual = e.Position;
                    break;
                case Ax.U:
                    _uActual = e.Position;
                    break;
                case Ax.All:
                    break;
                default:
                    break;
            }
        }

        public async Task CreateProcess()
        {
            _checkCut.Set(_technology.StartControlNum, _technology.ControlPeriod);
            _inspectX = _machine.GetGeometry(Place.CameraChuckCenter, Ax.X);
            _machine.OnAxisMotionStateChanged += _machine_OnAxisMotionStateChanged;

            _stateMachine = new StateMachine<State, Trigger>(State.ProcessStarted, FiringMode.Queued);

            _stateMachine.Configure(State.ProcessStarted)
                .OnActivateAsync(async () =>
                {
                    await GoTransferringHeightZAsync();
                    await _machine.GoThereAsync(Place.Loading);
                    _isWaitingToTeach = true;
                    _subject.OnNext(new ProcessMessage(MessageType.Info, @"Установите подложку и нажмите продолжить"));
                }, "Going to loading point")//TODO New initial state on loading point
                .InternalTransitionAsync(Trigger.DoSingleCut, async tr =>
                {
                    if (tr.Source == State.TeachSides || tr.Source == State.Correction)
                    {
                        var oldVel = _machine.SetVelocity(Velocity.Service);

                        var x = _xActual;
                        var y = _yActual;

                        var line = _wafer.GetCurrentCut();
                        var xCoor = line.Start.X;
                        xCoor = xCoor + Math.Sign(xCoor) * _blade.XGap(_wafer.Thickness);
                        var yCoor = y - _machine.GetFeature(MFeatures.CameraBladeOffset);
                        xCoor = _machine.TranslateSpecCoor(Place.BladeChuckCenter, -xCoor, Ax.X);
                        var xy = new double[] { xCoor, yCoor };
                        var z = _machine.TranslateSpecCoor(Place.ZBladeTouch, _wafer[_zRatio] + _technology.UnterCut, Ax.Z);

                        await GoTransferringHeightZAsync();
                        await _machine.MoveGpInPosAsync(Groups.XY, xy, true);
                        await Task.Delay(300);
                        await _machine.MoveAxInPosAsync(Ax.Y, yCoor, true);


                        await _machine.MoveAxInPosAsync(Ax.Z, z);

                        await CuttingXAsync();
                        _machine.SetVelocity(Velocity.Service);
                        await GoTransferringHeightZAsync();
                        await _machine.MoveGpInPosAsync(Groups.XY, new[] { x, y }, true);
                        await Task.Delay(300);
                        await _machine.MoveAxInPosAsync(Ax.Y, y, true);
                        await _machine.MoveAxesInPlaceAsync(Place.ZFocus);
                        //await GoCameraPointXyzAsync();
                        _machine.SetVelocity(oldVel);
                    }
                })
                .Permit(Trigger.Deny, State.ProcessInterrupted)
                .Permit(Trigger.Teach, State.TeachSides);

            //------------------------Teaching-------------------------------------
            _stateMachine.Configure(State.TeachSides)
                .SubstateOf(State.ProcessStarted)
                .OnEntryAsync(async () =>
                {
                    _isWaitingToTeach = false;
                    _machine.SwitchOnValve(Valves.ChuckVacuum);
                    await GoTransferringHeightZAsync();
                    await StartLearningAsync();
                    if (_wafer.CurrentSide == 0)
                    {
                        _subject.OnNext(new ProcessMessage(MessageType.Info,$"Обучите {(_wafer.CurrentSide + 1).ToOrdinalWords(GrammaticalGender.Feminine).ApplyCase(GrammaticalCase.Accusative)} сторону и нажмите продолжить"));
                    }
                    else if (_wafer.IsLastSide)
                    {
                        await GoNextDirectionAsync();
                        _subject.OnNext(new ProcessMessage(MessageType.Info,$"Поверните столик на 90 и обучите {(_wafer.CurrentSide + 1).ToOrdinalWords(GrammaticalGender.Feminine).ApplyCase(GrammaticalCase.Accusative)} сторону и нажмите продолжить"));
                    }
                })
                .Ignore(Trigger.Teach)
                .Permit(Trigger.Next, State.Processing);

            //-----------------------Processing------------------------------------

            _stateMachine.Configure(State.Processing)
                .SubstateOf(State.ProcessStarted)
                .InitialTransition(State.GoingTransferingZ)
                .Permit(Trigger.Inspection, State.Inspection)
                .Permit(Trigger.Correction, State.Correction);

            _stateMachine.Configure(State.GoingTransferingZ)
                .SubstateOf(State.Processing)
                .OnEntryAsync(GoTransferringHeightZAsync, "Transfer by Z into the safe position")
                .Ignore(Trigger.Teach)
                .PermitDynamic(Trigger.Next, () =>
                {
                    if (_checkCut.Check && !_afterCorrection)
                    {
                        return State.Inspection;
                    }
                    else
                    {
                        _afterCorrection = false;
                        return State.GoingNextCutXY;
                    }
                });

            _stateMachine.Configure(State.GoingNextCutXY)
                .SubstateOf(State.Processing)
                .OnEntryAsync(GoNextCutXYAsync, "Going to a next cut by XY")
                .Ignore(Trigger.Teach)
                .Permit(Trigger.Next, State.GoingNextDepthZ);

            _stateMachine.Configure(State.GoingNextDepthZ)
                .SubstateOf(State.Processing)
                .OnEntryAsync(GoNextDepthZAsync, "Going to a next depth by Z")
                .Ignore(Trigger.Teach)
                .Permit(Trigger.Next, State.Cutting);


            _stateMachine.Configure(State.Cutting)
                .SubstateOf(State.Processing)
                .OnEntryAsync(CuttingXAsync, "Cutting")
                .PermitDynamicIf(Trigger.Next, () =>
                {
                    if (_wafer.IncrementCut())
                    {
                        return State.GoingTransferingZ;
                    }
                    else if (_wafer.IncrementSide())
                    {
                        _repeatUntillCancell.CancelAsync();
                         return State.TeachSides;
                    }
                    return State.ProcessEnd;
                }, () => !_checkCut.Check)
                .PermitIf(Trigger.Next, State.Inspection, () => _checkCut.Check && !_repeatUntillCancell.IsCancellationRequested);

            _stateMachine.Configure(State.MovingNextSide)
                .SubstateOf(State.Processing)
                .OnEntryAsync(async () =>
                {
                    await GoTransferringHeightZAsync();
                    await GoNextDirectionAsync();
                }, "Moving to a next side")
                .Ignore(Trigger.Teach)
                .Permit(Trigger.Next, State.GoingTransferingZ);
            //-------------------------Inspection and Correction-----------------------
            _stateMachine.Configure(State.Inspection)
               .SubstateOf(State.ProcessStarted)
               .OnEntryAsync(async () =>
               {
                   await TakeThePhotoAsync();
                   _checkCut.Checked();
                   _subject.OnNext(new CheckPointOccured());
               }, "Inspecting a cut")
               .Ignore(Trigger.Teach)
               .PermitDynamic(Trigger.Next, () =>
               {
                   if (_wafer.IncrementCut())
                   {
                       return State.GoingTransferingZ;
                   }
                   else if (_wafer.IncrementSide())
                   {
                       return State.TeachSides;
                   }
                   return State.ProcessEnd;
               });

            _stateMachine.Configure(State.Correction)
              .SubstateOf(State.ProcessStarted)
              .OnEntryAsync(CorrectionAsync)
              .Ignore(Trigger.Teach)
              .PermitDynamic(Trigger.Next, () =>
              {
                  _subject.OnNext(new CheckPointOccured());
                  if (CutOffset != 0)
                  {
                      if (MsgBox.Ask($"Сместить следующие резы на {CutOffset} мм?", "Коррекция реза") == MessageBoxResult.OK)
                      {
                          _wafer.AddToSideShift(CutOffset);
                      }
                  }
                  var nearestNum = _wafer.GetNearestNum(_yActual - _machine.GetGeometry(Place.CameraChuckCenter, Ax.Y));
                  var thisSide = false;
                  if (_wafer.CurrentCutNum != nearestNum)
                  {
                      if (MsgBox.Ask($"Изменить номер реза на {(nearestNum + 1).ToOrdinalWords(GrammaticalGender.Masculine)}?", "Коррекция реза") == MessageBoxResult.OK)
                      {
                          thisSide = _wafer.SetCurrentCutNum(nearestNum/* + 1*/);
                      }
                      else
                      {
                          thisSide = _wafer.IncrementCut();
                      }
                  }
                  else
                  {
                      thisSide = _wafer.IncrementCut();
                  }
                  CutOffset = 0;
                  _machine.FreezeCameraImage();
                  _inspectX = _xActual;
                  _afterCorrection = true;
                  if (thisSide) return State.GoingTransferingZ;
                  if (_wafer.IncrementSide()) return State.TeachSides;
                  return State.ProcessEnd;
              });
            //-------------------------Ending and Interruption-------------------------
            _stateMachine.Configure(State.ProcessInterrupted)
                .OnEntryAsync(async () =>
                {
                    await GoTransferringHeightZAsync();
                    await _machine.GoThereAsync(Place.Loading);
                    _machine.OnAxisMotionStateChanged -= _machine_OnAxisMotionStateChanged;
                    _subject.OnCompleted();
                    //_subject.OnError(new ProcessInterruptedException($"The process was interrupted"));
                });

            _stateMachine.Configure(State.ProcessEnd)
                .OnEntryAsync(async () =>
                {
                    await GoTransferringHeightZAsync();
                    await _machine.GoThereAsync(Place.Loading);
                    _machine.OnAxisMotionStateChanged -= _machine_OnAxisMotionStateChanged;
                    _subject.OnCompleted();
                });
            //---------------------------------------------------------------------------

            _stateMachine.OnUnhandledTrigger((s, t) => { });
            _stateMachine.OnTransitioned(tr =>
            {
                if (tr.Source == State.TeachSides || tr.Source == State.Cutting)
                {
                    _currentScore++;
                    var linesCount = _wafer.TotalLinesCount();
                    ProcessPercentage = (int)((decimal)_currentScore).Map(0, 2 + linesCount * _technology.PassCount, 0, 100);
                }
                _subject.OnNext(new ProcessStateChanging(tr.Source, tr.Destination, tr.Trigger));
            });
            _stateMachine.OnTransitionCompleted(tr =>
            {
                if (tr.Source == State.Processing && tr.Destination == State.GoingTransferingZ)
                {
                    _inProcess = true;
                }
                _subject.OnNext(new ProcessStateChanged(tr.Source, tr.Destination, tr.Trigger));
            });

            await _stateMachine.ActivateAsync();
        }

        private async Task CorrectionAsync()
        {
            await GoTransferringHeightZAsync();
            await GoCameraPointXyzAsync();
            _machine.SwitchOnValve(Valves.Blowing);
            await Task.Delay(500);
            _machine.SwitchOffValve(Valves.Blowing);
            _machine.StartCamera(0);
        }
        private async Task GoCameraPointXyzAsync()
        {
            _machine.SetVelocity(Velocity.Service);
            var z = _machine.TranslateSpecCoor(Place.ZBladeTouch, _wafer.Thickness + _bladeTransferGapZ, Ax.Z);
            await _machine.MoveAxInPosAsync(Ax.Z, z);
            var y = _yActual + _machine.GetFeature(MFeatures.CameraBladeOffset);
            await _machine.MoveGpInPosAsync(Groups.XY, new double[] { _inspectX, y }, true);
            await Task.Delay(300);
            await _machine.MoveAxInPosAsync(Ax.Y, y);
            await _machine.MoveAxesInPlaceAsync(Place.ZFocus);
        }
        private async Task TakeThePhotoAsync()
        {
            _machine.StartCamera(0);
            await GoTransferringHeightZAsync();
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
        private async Task GoTransferringHeightZAsync()
        {
            await _machine.MoveAxInPosAsync(Ax.Z, _machine.GetFeature(MFeatures.ZBladeTouch) - _wafer.Thickness - _bladeTransferGapZ);
        }
        private async Task CuttingXAsync()
        {
            _machine.SwitchOnValve(Valves.ChuckVacuum);
            _machine.SwitchOnValve(Valves.Coolant);
            await Task.Delay(300);
            _machine.SetAxFeedSpeed(Ax.X, _technology.FeedSpeed);
            IsCutting = true;
            var xCurLineEnd = _wafer.GetCurrentCut().End.X;
            var x = _machine.TranslateSpecCoor(Place.BladeChuckCenter, -xCurLineEnd, 0);
            await _machine.MoveAxInPosAsync(Ax.X, x);
            IsCutting = false;
            _lastCutY = _yActual;
            _checkCut.addToCurrentCut();
            _machine.SwitchOffValve(Valves.Coolant);
        }
        public double GetLastCutY() => _lastCutY;
        private async Task GoNextDepthZAsync()
        {
            _machine.SetVelocity(Velocity.Service);
            var z = _machine.TranslateSpecCoor(Place.ZBladeTouch, _wafer[_zRatio] + _technology.UnterCut, Ax.Z);
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
            var resY = y + _machine.GetGeometry(Place.CameraChuckCenter, Ax.Y) - _machine.GetFeature(MFeatures.CameraBladeOffset);
            var xy = new double[] { arr.GetVal(Ax.X), /*arr.GetVal(Ax.Y)*/resY };
            await _machine.MoveGpInPosAsync(Groups.XY, xy, true);
            await Task.Delay(300);
            await _machine.MoveAxInPosAsync(Ax.Y, resY, true);
        }
        private async Task StartLearningAsync()
        {
            _machine.SetVelocity(Velocity.Service);
            //await _machine.MoveAxInPosAsync(Ax.Z, _machine.GetFeature(MFeatures.ZBladeTouch) - _wafer.Thickness - _bladeTransferGapZ);
            var y = _wafer.GetNearestY(0);
            var arr = _machine.TranslateActualCoors(Place.CameraChuckCenter, new (Ax, double)[] { (Ax.X, 0), (Ax.Y, -y) });
            var point = new double[] { arr.GetVal(Ax.X), arr.GetVal(Ax.Y) };
            //await _machine.MoveGpInPosAsync(Groups.XY, point);

            await Task.WhenAll(
                _machine.MoveAxInPosAsync(Ax.X, point[0]),
                _machine.MoveAxInPosAsync(Ax.Y, point[1], true));



            await _machine.MoveAxesInPlaceAsync(Place.ZFocus);
        }
        private Task LearningAsync()
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
        public async Task TriggerSingleCutState() => await _stateMachine.FireAsync(Trigger.DoSingleCut);
        public async Task EmergencyScript()
        {
            _machine.Stop(Ax.X);
            await _machine.MoveAxInPosAsync(Ax.Z, 0);
            _machine.StopSpindle();
            if (_repeatUntillCancell is not null) await _repeatUntillCancell.CancelAsync();
            await _stateMachine.FireAsync(Trigger.Deny);
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
            _isCancelled = true;
            if (_repeatUntillCancell is not null) await _repeatUntillCancell.CancelAsync();
            await _stateMachine.FireAsync(Trigger.Deny);
        }
        public async Task Next()
        {
            Guard.IsNotNull(_stateMachine, nameof(_stateMachine));
            if (_stateMachine.IsInState(State.ProcessStarted) && !_stateMachine.IsInState(State.TeachSides) && _isWaitingToTeach)
            {
                await _stateMachine.FireAsync(Trigger.Teach);
            }
            else if (_stateMachine.IsInState(State.TeachSides) & !_wafer.IsLastSide)
            {
                _repeatUntillCancell = new RepeatUntilCancel(() => _stateMachine.FireAsync(Trigger.Next));
                await _repeatUntillCancell.StartAsync();
            }
            else if (_stateMachine.IsInState(State.TeachSides) & _wafer.IsLastSide)
            {
                _repeatUntillCancell = new RepeatUntilCancel(() => _stateMachine.FireAsync(Trigger.Next));
                await _repeatUntillCancell.StartAsync();
            }
            else if (_stateMachine.IsInState(State.Processing))
            {
                _subject.OnNext(new ProcessMessage(MessageType.ToChangeCurrentStateTo, "Коррекция"));
                await _repeatUntillCancell.CancelAsync();
                await _stateMachine.FireAsync(Trigger.Correction);
            }
            else if (_inProcess || _stateMachine.IsInState(State.Correction))
            {
                _repeatUntillCancell = new RepeatUntilCancel(() => _stateMachine.FireAsync(Trigger.Next));
                await _repeatUntillCancell.StartAsync();
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


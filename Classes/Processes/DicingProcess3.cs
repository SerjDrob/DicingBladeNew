using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DicingBlade.Classes.Miscellaneous;
using DicingBlade.Classes.Technology;
using DicingBlade.Classes.WaferGrid;
using DicingBlade.Properties;
using DicingBlade.Utility;
using Humanizer;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using Microsoft.Toolkit.Diagnostics;
using Stateless;
using Stateless.Graph;
using MsgBox = HandyControl.Controls.MessageBox;

namespace DicingBlade.Classes.Processes;
internal class DicingProcess3 : IProcess
{
    private readonly DicingBladeMachine _machine;
    private readonly CutLines.CutLinesBuilder _cutLinesBuilder;
    private CutLines _cutLines;
    private readonly ITechnology _technology;
    private readonly double _materialThickness;
    private double _inspectX;
    private StateMachine<State, Trigger> _stateMachine;
    private bool _isWaitingToTeach;
    private readonly double _bladeTransferGapZ = 1;
    private readonly ISubject<IProcessNotify> _subject;
    public double CutOffset { get; set; } = 0;
    public int ProcessPercentage
    {
        get;
        private set;
    }

    public bool ProcessEndOrDenied
    {
        get => (_stateMachine?.IsInState(State.ProcessEnd) ?? false) || (_stateMachine?.IsInState(State.ProcessInterrupted) ?? false);
    }


    private CheckCutControl _checkCut;
    private RepeatUntilCancel _repeatUntilCancel;
    private double _xActual;
    private double _yActual;
    private double _zActual;
    private double _uActual;
    private bool _afterCorrection;
    private int _currentScore;
    private bool _inProcess;
    private bool _isCutting;
    private double _lastCutY;
    private bool _isCancelled;

    public DicingProcess3(DicingBladeMachine machine, CutLines.CutLinesBuilder cutLinesBuilder, 
        ITechnology technology, double materialThickness)
    {
        _machine = machine ?? throw new ProcessException("Не выбрана установка для процесса");
        _cutLinesBuilder = cutLinesBuilder;
        _technology = technology;
        _materialThickness = materialThickness;
        _subject = new Subject<IProcessNotify>();
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

                    var line = _cutLines.GetCurrentLine();
                    var yCoor = y - _machine.GetFeature(MFeatures.CameraBladeOffset);

                    await GoTransferringHeightZAsync();

                    await Task.WhenAll(
                        _machine.MoveAxInPosAsync(Ax.X, line.XStart, true),
                        _machine.MoveAxInPosAsync(Ax.Y, y, true)
                        );

                    var result = await _machine.ChangeSpindleFreqOnFlyAsync((ushort)_technology.SpindleFreq, 300);
                    await _machine.MoveAxInPosAsync(Ax.Z, line.Z);
                    await CuttingXAsync();

                    _machine.SetVelocity(Velocity.Service);
                    await GoTransferringHeightZAsync();

                    await Task.WhenAll(
                            _machine.MoveAxInPosAsync(Ax.X, x, true),
                            _machine.MoveAxInPosAsync(Ax.X, y, true)
                        );
                    await _machine.MoveAxesInPlaceAsync(Place.ZFocus);
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
                _subject.OnNext(new ProcessMessage(MessageType.Info, $"Укажите линию первого реза и нажмите продолжить"));
            })
            .OnExitAsync(async () =>
            {
                if (!_isCancelled)
                {
                    await LearningAsync();
                    _subject.OnNext(new NewCutLinesOccurred(_cutLines));
                    var result = _cutLines.NextLine();
                    //if (_wafer.IncrementSide())
                    //{
                    //    await GoTransferingHeightZAsync();
                    //    await MovingNextDirAsync();
                    //}
                }
            }, "Moving next direction for learning")
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
                if (_cutLines.NextLine())
                {
                    return State.GoingTransferingZ;
                }
                return State.ProcessEnd;
            }, () => !_checkCut.Check)
            .PermitIf(Trigger.Next, State.Inspection, () => _checkCut.Check && !_repeatUntilCancel.IsCancellationRequested);

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
               if (_cutLines.NextLine())
               {
                   return State.GoingTransferingZ;
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
                      _cutLines.AddYOffset(CutOffset);
                  }
              }
              var nearestStepNum = _cutLines.GetNearestStepNumber(_yActual);
              var thisSide = false;
              if (_cutLines.GetCurrentLine().StepNumber != nearestStepNum)
              {
                  if (MsgBox.Ask($"Изменить номер шага на {(nearestStepNum + 1).ToOrdinalWords(GrammaticalGender.Masculine)}?", "Коррекция реза") == MessageBoxResult.OK)
                  {
                      thisSide = _cutLines.SetStep(nearestStepNum);
                  }
              }
              if (!thisSide) thisSide = _cutLines.NextLine();
              CutOffset = 0;
              _machine.FreezeCameraImage();
              _inspectX = _xActual;
              _afterCorrection = true;
              if (thisSide) return State.GoingTransferingZ;
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
            });

        _stateMachine.Configure(State.ProcessEnd)
            .OnEntryAsync(async () =>
            {
                _machine.SetVelocity(Velocity.Service);
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
                ProcessPercentage = (int)((decimal)_currentScore).Map(0, 2 + _cutLines.LinesCount * _technology.PassCount, 0, 100);
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
    private Task LearningAsync()
    {
        var y = _yActual + Settings.Default.DiskShift;//TODO fix it
        _cutLines = _cutLinesBuilder.SetFirstY(y).Build();
        //_wafer.TeachSideShift(y);
        //_subject.OnNext(new WaferAligningChanged());
        //_wafer.TeachSideAngle(_uActual);
        return Task.CompletedTask;
    }
    private void _machine_OnAxisMotionStateChanged(object? sender, AxisStateEventArgs e)
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
        var z = _machine.TranslateSpecCoor(Place.ZBladeTouch, _cutLines.MaterialThickness + _bladeTransferGapZ, Ax.Z);
        await _machine.MoveAxInPosAsync(Ax.Z, z);
        var y = _yActual + _machine.GetFeature(MFeatures.CameraBladeOffset);
        await Task.WhenAll(
            _machine.MoveAxInPosAsync(Ax.X, _inspectX, true),
            _machine.MoveAxInPosAsync(Ax.Y, y, true)
            );
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
    private async Task GoTransferringHeightZAsync() => await _machine.MoveAxInPosAsync(Ax.Z, _machine.GetFeature(MFeatures.ZBladeTouch) - _materialThickness - _bladeTransferGapZ);
    private async Task CuttingXAsync()
    {
        var line = _cutLines.GetCurrentLine();
        _machine.SwitchOnValve(Valves.ChuckVacuum);
        _machine.SwitchOnValve(Valves.Coolant);
        await Task.Delay(300);
        _machine.SetAxFeedSpeed(Ax.X, line.FeedSpeed);
        _isCutting = true;
        var x = line.XStart + line.Length;
        await _machine.MoveAxInPosAsync(Ax.X, x);
        _isCutting = false;
        _lastCutY = _yActual;
        _machine.SwitchOffValve(Valves.Coolant);
    }
    private async Task GoNextDepthZAsync()
    {
        _machine.SetVelocity(Velocity.Service);
        var line = _cutLines.GetCurrentLine();
        var result = await _machine.ChangeSpindleFreqOnFlyAsync((ushort)line.RPM, 300);
        var z = line.Z;
        await _machine.MoveAxInPosAsync(Ax.Z, z);
    }
    private async Task GoNextCutXYAsync()
    {
        var line = _cutLines.GetCurrentLine();
        _machine.SetVelocity(Velocity.Service);
        var x = line.XStart;
        var y = line.Y;
        await Task.WhenAll(_machine.MoveAxInPosAsync(Ax.Y, y, true),
            _machine.MoveAxInPosAsync(Ax.X, x, true));
    }
    private async Task StartLearningAsync()
    {
        _machine.SetVelocity(Velocity.Service);
        var arr = _machine.TranslateActualCoors(Place.CameraChuckCenter, new (Ax, double)[] { (Ax.X, 0), (Ax.Y, 0) });
        var point = new double[] { arr.GetVal(Ax.X), arr.GetVal(Ax.Y) };
        await Task.WhenAll(
            _machine.MoveAxInPosAsync(Ax.X, point[0]),
            _machine.MoveAxInPosAsync(Ax.Y, point[1], true));
        await _machine.MoveAxesInPlaceAsync(Place.ZFocus);
    }
    public async Task TriggerSingleCutState() => await _stateMachine.FireAsync(Trigger.DoSingleCut);
    public async Task EmergencyScript()
    {
        _machine.Stop(Ax.X);
        await _machine.MoveAxInPosAsync(Ax.Z, 0);
        _machine.StopSpindle();
        if (_repeatUntilCancel is not null) await _repeatUntilCancel.CancelAsync();
        await _stateMachine.FireAsync(Trigger.Deny);
    }
    public async Task Deny()
    {
        _isCancelled = true;
        if (_repeatUntilCancel is not null) await _repeatUntilCancel.CancelAsync();
        await _stateMachine.FireAsync(Trigger.Deny);
    }
    public void ExcludeObject(IProcObject procObject) => throw new NotImplementedException();
    public void IncludeObject(IProcObject procObject) => throw new NotImplementedException();
    public async Task Next()
    {
        Guard.IsNotNull(_stateMachine, nameof(_stateMachine));
        if (_stateMachine.IsInState(State.ProcessStarted) && !_stateMachine.IsInState(State.TeachSides) && _isWaitingToTeach)
        {
            await _stateMachine.FireAsync(Trigger.Teach);
        }
        else if (_stateMachine.IsInState(State.TeachSides))
        {
            _repeatUntilCancel = new RepeatUntilCancel(() => _stateMachine.FireAsync(Trigger.Next));
            await _repeatUntilCancel.StartAsync();
        }
        else if (_stateMachine.IsInState(State.Processing))
        {
            _subject.OnNext(new ProcessMessage(MessageType.ToChangeCurrentStateTo, "Коррекция"));
            await _repeatUntilCancel.CancelAsync();
            await _stateMachine.FireAsync(Trigger.Correction);
        }
        else if (_inProcess || _stateMachine.IsInState(State.Correction))
        {
            _repeatUntilCancel = new RepeatUntilCancel(() => _stateMachine.FireAsync(Trigger.Next));
            await _repeatUntilCancel.StartAsync();
        }
    }
    public Task StartAsync() => throw new NotImplementedException();
    public Task StartAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    public IDisposable Subscribe(IObserver<IProcessNotify> observer) => _subject.Subscribe(observer);
    public override string ToString() => UmlDotGraph.Format(_stateMachine.GetInfo());
}

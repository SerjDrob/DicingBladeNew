using DicingBlade.Classes;
using DicingBlade.Classes.Processes;
using DicingBlade.Properties;
using DicingBlade.Utility;
using DicingBlade.Views;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Machine.MotionDevices;
using MachineControlsLibrary.Classes;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Growl = HandyControl.Controls.Growl;
using MsgBox = HandyControl.Controls.MessageBox;


namespace DicingBlade.ViewModels
{
    internal partial class MainViewModel
    {
        public ICommand? TestKeyCommand { get; protected set; }
        public bool IsMachineInProcess { get => !(_dicingProcess is null || _dicingProcess.ProcessEndOrDenied); }
        private bool _isReadyForAligning;
        private void InitCommands()
        {
            TestKeyCommand = new KeyProcessorCommands(parameter => true)
                .CreateAnyKeyDownCommand(moveAsync, () => true)
                .CreateAnyKeyUpCommand(stopAsync, () => true)
                .CreateKeyDownCommand(Key.T, Change, () => true)
                .CreateKeyDownCommand(Key.E, () =>
                {
                    _machine.SwitchOnValve(Valves.Light);
                    return Task.CompletedTask;
                }, () => true)
                .CreateKeyDownCommand(Key.Q, () =>
                {
                    if (_machine.GetValveState(Valves.ChuckVacuum))
                        _machine.SwitchOffValve(Valves.ChuckVacuum);
                    else
                        _machine.SwitchOnValve(Valves.ChuckVacuum);
                    return Task.CompletedTask;
                }, () => true)
                .CreateKeyDownCommand(Key.W, () =>
                {
                    if (_machine.GetValveState(Valves.Coolant))
                        _machine.SwitchOffValve(Valves.Coolant);
                    else
                        _machine.SwitchOnValve(Valves.Coolant);
                    return Task.CompletedTask;
                }, () => true)
                .CreateKeyDownCommand(Key.R, () =>
                {
                    if (_machine.GetValveState(Valves.Blowing))
                        _machine.SwitchOffValve(Valves.Blowing);
                    else
                        _machine.SwitchOnValve(Valves.Blowing);
                    return Task.CompletedTask;
                }, () => true)
                .CreateKeyDownCommand(Key.G, () => _machine.GoThereAsync(Place.Loading), () => true)
                .CreateKeyDownCommand(Key.Home, moveHomeAsync, () => !IsMachineInProcess)
                .CreateKeyDownCommand(Key.Add, changeVelocity, () => true)
                .CreateKeyDownCommand(Key.Subtract, setStepVelocity, () => true)
                .CreateKeyDownCommand(Key.F2, async () =>
                {
                    if (SpindleAccelarating | SpindleOnFreq) _machine.StopSpindle();
                    else
                    {
                        try
                        {
                            _machine.SetSpindleFreq(_technology.SpindleFreq);
                            await Task.Delay(100);
                            _machine.StartSpindle(Sensors.Air, Sensors.SpindleCoolant);
                        }
                        catch (MachineException ex)
                        {
                            MsgBox.Info(ex.Message, "Запуск шпинделя");
                        }
                        catch (Exception ex)
                        {
                            throw;
                        }
                    }

                }, () => true)
                .CreateKeyDownCommand(Key.J, async () =>
                {
                    var velocity = VelocityRegime != Velocity.Step ? VelocityRegime : Velocity.Slow;
                    _machine.SetVelocity(Velocity.Service);
                    var x = _machine.TranslateSpecCoor(Place.CameraChuckCenter, -Substrate.CurrentSideLength / 2, Ax.X);
                    await _machine.MoveAxInPosAsync(Ax.X, x);
                    _machine.SetVelocity(velocity);
                }, () => true)
                .CreateKeyDownCommand(Key.K, async () =>
                {
                    var velocity = VelocityRegime != Velocity.Step ? VelocityRegime : Velocity.Slow;
                    _machine.SetVelocity(Velocity.Service);
                    var x = _machine.TranslateSpecCoor(Place.CameraChuckCenter, 0, Ax.X);
                    await _machine.MoveAxInPosAsync(Ax.X, x);
                    _machine.SetVelocity(velocity);
                }, () => true)
                .CreateKeyDownCommand(Key.L, async () =>
                {
                    var velocity = VelocityRegime != Velocity.Step ? VelocityRegime : Velocity.Slow;
                    _machine.SetVelocity(Velocity.Service);
                    var x = _machine.TranslateSpecCoor(Place.CameraChuckCenter, Substrate.CurrentSideLength / 2, Ax.X);
                    await _machine.MoveAxInPosAsync(Ax.X, x);
                    _machine.SetVelocity(velocity);
                }, () => true)
                //.CreateKeyDownCommand(Key.Multiply, next, () => true)
                .CreateKeyDownCommand(Key.Escape, async () =>
                {
                    await _dicingProcess?.Deny();
                }, () => true)
                .CreateKeyDownCommand(Key.N, () =>
                {
                    _dicingProcess.CutOffset += 0.001;
                    CutOffsetView += 0.001;
                    return Task.CompletedTask;
                }, () => _isProcessInCorrection && _dicingProcess is not null)
                .CreateKeyDownCommand(Key.M, () =>
                {
                    CutOffsetView -= 0.001;
                    _dicingProcess.CutOffset -= 0.001;
                    return Task.CompletedTask;
                }, () => _isProcessInCorrection && _dicingProcess is not null)
                .CreateKeyDownCommand(Key.F, async () =>
                {
                    if (MsgBox.Ask("Сделать одиночный рез?", "Резка").HasFlag(MessageBoxResult.OK))
                    {
                        await _dicingProcess.TriggerSingleCutState();
                    }
                }, () => _isSingleCutAvaliable && _dicingProcess is not null)
                .CreateKeyDownCommand(Key.Divide, async () =>
                {
                    if (!IsMachineInProcess)
                    {
                        var blade = new Blade();
                        blade.Diameter = 55.6;
                        blade.Thickness = 0.11;
                        Substrate.ResetWafer();
                        _dicingProcess = new DicingProcess(_machine, Substrate, blade, _technology);
                        GetSubscriptions(_dicingProcess);
                        await _dicingProcess.CreateProcess();
                    }
                    else
                    {
                        await _dicingProcess.
                        Next();
                    }
                }, () => _homeDone)
                .CreateKeyDownCommand(Key.I, async () =>
                {
                    if (MsgBox.Ask("Завершить процесс?", "Процесс").HasFlag(MessageBoxResult.OK))
                    {
                        await _dicingProcess.Deny();
                        //Substrate = null;
                        //ResetWaferView();
                        //AjustWaferTechnology();
                    }
                }, () => IsMachineInProcess)
                .CreateKeyDownCommand(Key.OemMinus, async () => { await AlignWaferAsync(); }, () => _isReadyForAligning)
                .CreateKeyDownCommand(Key.Multiply, () => { UserConfirmation = true; return Task.CompletedTask; }, () => IsMachineInProcess)
                .CreateKeyDownCommand(Key.Oem6, async () => await _machine?.GoThereAsync(Place.CameraChuckCenter), () => !IsMachineInProcess)
                .CreateKeyDownCommand(Key.Oem4, async () => await _machine?.GoThereAsync(Place.BladeChuckCenter), () => !IsMachineInProcess)
                .CreateKeyDownCommand(Key.F11, async () =>
                {
                    await _dicingProcess.EmergencyScript();
                    //_dicingProcess.WaitProcDoneAsync().Wait();
                    _dicingProcess = null;
                    Substrate = null;
                    ResetWaferView();
                    AjustWaferTechnology();
                    Growl.Warning("Процесс экстренно прерван оператором.");
                }, () => IsMachineInProcess)
                .CreateKeyDownCommand(Key.H, () => { MachineSettings(); return Task.CompletedTask; }, () => true)
                .CreateKeyDownCommand(Key.F3, () => { WaferSettings(); return Task.CompletedTask; }, () => true)
                .CreateKeyDownCommand(Key.F4, () => { TechnologySettings(); return Task.CompletedTask; }, () => true)
                .CreateKeyDownCommand(Key.F6, () => { ToTeachCutShift(); return Task.CompletedTask; }, () => true)

                ;

            async Task moveAsync(KeyEventArgs key)
            {
                try
                {
                    var res = key.Key switch
                    {
                        Key.A => (Ax.Y, AxDir.Pos),
                        Key.Z => (Ax.Y, AxDir.Neg),
                        Key.X => (Ax.X, AxDir.Neg),
                        Key.C => (Ax.X, AxDir.Pos),
                        Key.V => (Ax.Z, AxDir.Pos),
                        Key.B => (Ax.Z, AxDir.Neg),
                        Key.S => (Ax.U, AxDir.Pos),
                        Key.D => (Ax.U, AxDir.Neg)
                    };

                    if (!key.IsRepeat)
                    {

                        switch (VelocityRegime)
                        {
                            case Velocity.Step when res.Item1 == Ax.Y:
                                {
                                    var velocity = VelocityRegime;
                                    _machine.SetVelocity(Velocity.Service);
                                    var index = res.Item2 == AxDir.Pos ? Substrate.CurrentIndex : -Substrate.CurrentIndex;
                                    await _machine.MoveAxInPosAsync(Ax.Y, YView + index, true);
                                    _machine.SetVelocity(velocity);
                                }
                                break;

                            case Velocity.Micro:
                                {
                                    var step = (res.Item2 == AxDir.Pos ? 1 : -1) * 0.005;
                                    await _machine.MoveAxRelativeAsync(res.Item1, step, false);
                                }
                                break;

                            case Velocity.Fast or Velocity.Slow or Velocity.Service or Velocity.Step:
                                {
                                    _machine.GoWhile(res.Item1, res.Item2);
                                }
                                break;

                            default:
                                break;
                        }

                        //                  if (VelocityRegime != Velocity.Step) _machine.GoWhile(res.Item1, res.Item2);
                        //if (VelocityRegime == Velocity.Step)
                        //{
                        //	var step = (res.Item2 == AxDir.Pos ? 1 : -1) * 0.005;
                        //	await _machine.MoveAxRelativeAsync(res.Item1, step, false);
                        //}
                    }
                    key.Handled = true;
                }
                catch (SwitchExpressionException)
                {
                    return;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            Task stopAsync(KeyEventArgs key)
            {
                try
                {
                    var axis = key.Key switch
                    {
                        Key.A or Key.Z => Ax.Y,
                        Key.X or Key.C => Ax.X,
                        Key.V or Key.B => Ax.Z,
                        Key.S or Key.D => Ax.U,
                    };
                    if (!(axis == Ax.Y && VelocityRegime == Velocity.Step)) _machine.Stop(axis);
                }
                catch (SwitchExpressionException)
                {
                    return Task.CompletedTask;
                }
                catch (Exception)
                {
                    throw;
                }
                return Task.CompletedTask;
            }
            async Task moveHomeAsync()
            {
                try
                {
                    await _machine.GoHomeAsync().ConfigureAwait(false);
                    //var loadPoint = new double[] { Settings.Default.XLoad, Settings.Default.YLoad };
                    //await _machine.MoveGpInPosAsync(Groups.XY, loadPoint).ConfigureAwait(false);
                    _homeDone = true;
                }
                catch (Exception ex)
                {

                    throw;
                }
                //techMessager.EraseMessage();
            }
            Task changeVelocity()
            {
                VelocityRegime = VelocityRegime switch
                {
                    Velocity.Slow => Velocity.Fast,
                    Velocity.Fast => Velocity.Slow,
                    _ => Velocity.Fast
                };
#if PCIInserted
                _machine.SetVelocity(VelocityRegime);
#endif
                return Task.CompletedTask;
            }
            Task setStepVelocity()
            {
                VelocityRegime = VelocityRegime switch
                {
                    Velocity.Step => Velocity.Micro,
                    Velocity.Micro => Velocity.Step,
                    _ => Velocity.Step
                };
#if PCIInserted
                _machine.SetVelocity(VelocityRegime);
#endif
                return Task.CompletedTask;
            }
        }


        private TempWafer2D _tempWafer2D = new();


        public async Task AlignWaferAsync()
        {
            if (!_tempWafer2D.FirstPointSet)
            {
                _tempWafer2D.Point1 = new double[] { XView, YView };
                _tempWafer2D.FirstPointSet = true;
                Growl.Info("Укажите вторую точку для выравнивания и нажмите _");
            }
            else
            {
                _tempWafer2D.Point2 = new double[] { XView, YView };
                _machine.SetVelocity(Velocity.Service);
                var angle = _tempWafer2D.GetAngle();
                await _machine.MoveAxInPosAsync(Ax.U, UView - angle);
                var rotation = new RotateTransform(-angle);
                rotation.CenterX = _machine.GetGeometry(Place.CameraChuckCenter, Ax.X);
                rotation.CenterY = _machine.GetGeometry(Place.CameraChuckCenter, Ax.Y);
                var point = rotation.Transform(new System.Windows.Point(_tempWafer2D.Point2[0], _tempWafer2D.Point2[1]));
                await _machine.MoveAxInPosAsync(Ax.Y, point.Y);
                _tempWafer2D.FirstPointSet = false;
                _machine.SetVelocity(VelocityRegime);
            }
        }


        private Task Change()
        {
            Cols = new[] { Cols[1], Cols[0] };
            Rows = new[] { Rows[1], Rows[0] };
            return Task.CompletedTask;
        }

        [ICommand]
        public async Task ToTeachVideoScale()
        {
            TeachVScaleMarkersVisibility = Visibility.Visible;
            ProcessMessage = "Подведите ориентир к одному из визиров и нажмите *";
            await WaitForConfirmationAsync();
            var y = YView;
            ProcessMessage = "Подведите ориентир ко второму визиру и нажмите *";
            await WaitForConfirmationAsync();
            CameraScale = TeachMarkersRatio * Math.Abs(y - YView);
            Settings.Default.CameraScale = CameraScale;
            Settings.Default.Save();
            ProcessMessage = "";
            TeachVScaleMarkersVisibility = Visibility.Hidden;
        }

        [ICommand]
        private async Task ToTeachCutShift()
        {
            if (MsgBox.Ask("Обучить смещение реза от ТВ?", "Обучение").HasFlag(MessageBoxResult.OK))
            {
                Growl.Info("Совместите горизонтальный визир с центром последнего реза и нажмите *");

                await WaitForConfirmationAsync();
                Settings.Default.DiskShift = -_dicingProcess.GetLastCutY() + YView;
                Settings.Default.Save();

                //Process?.TeachDiskShift();
                _machine.ConfigureGeometry(new Dictionary<Place, (Ax, double)[]>
                {
                    {
                        Place.BladeChuckCenter,
                        new[]
                        {
                            (Ax.X, Settings.Default.XDisk),
                            (Ax.Y, Settings.Default.YObjective + Settings.Default.DiskShift)
                        }
                    }
                });
                Growl.Info("Новое смещение реза от ТВ установленно");
            }
        }

        [ICommand]
        private async Task ToTeachChipSize()
        {
            var i = Substrate.CurrentSide;

            if (MsgBox.Ask("Обучить размер кристалла?", "Обучение").HasFlag(MessageBoxResult.OK))
            {
                ProcessMessage = "Подведите ориентир к перекрестию и нажмите *";
                await WaitForConfirmationAsync();
                var y = YView;
                ProcessMessage = "Подведите следующий ориентир к перекрестию и нажмите *";
                await WaitForConfirmationAsync();
                var size = Math.Round(Math.Abs(y - YView), 3);
                ProcessMessage = "";

                if (MsgBox.Ask($"\rНовый размер кристалла {size} мм", "Обучение").HasFlag(MessageBoxResult.OK))
                {
                    var tempwafer = await StatMethods
                        .DeSerializeObjectJsonAsync<TempWafer>(Settings.Default.WaferLastFile).ConfigureAwait(false);
                    //PropContainer.WaferTemp.CurrentSide = Substrate.CurrentSide;
                    //PropContainer.WaferTemp.SetCurrentIndex(size);//currentIndex?
                    Substrate.SetCurrentIndex(size);
                    tempwafer.CurrentSide = Substrate.CurrentSide;
                    tempwafer.SetCurrentIndex(size);
                    //new TempWafer(PropContainer.WaferTemp).SerializeObjectJson(Settings.Default.WaferLastFile);
                    await tempwafer.SerializeObjectJsonAsync(Settings.Default.WaferLastFile).ConfigureAwait(false);

                    Wafer = Wafer.GetWaferBuilder()
                        .AddDirection(0, tempwafer.Height, tempwafer.Width, tempwafer.IndexH)
                        .AddDirection(90, tempwafer.Width, tempwafer.Height, tempwafer.IndexW)
                        .Build(tempwafer.Thickness);

                    WaferView = Wafer.GetWaferView();
                    AjustWaferTechnology(Substrate.CurrentSide);
                }
            }
        }
        [ICommand]
        private void MachineSettings()
        {
            var settingsVM = new MachineSettingsViewModel(XView, YView, ZView);
            new MachineSettingsView
            {
                DataContext = settingsVM
            }.ShowDialog();


            var viewfinders = settingsVM.ScaleGridView;

            viewfinders.SerializeObject(ProjectPath.GetFilePathInFolder(ProjectFolders.APP_SETTINGS, "Viewfinders.json"));

            viewfinders = viewfinders.DivideDoubles(1000);
            ScaleGridView = viewfinders;
            RealCutWidthView = viewfinders.RealCutWidth;
            CutWidthView = viewfinders.CorrectingCutWidth;

            Settings.Default.Save();

            //Machine.RefreshSettings();
            try
            {
                ImplementMachineSettings();
            }
            catch (MotionException ex)
            {
                MsgBox.Info(ex.Message, "Настройка приводов");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public int PassCountTechnology { get; set; } = 1;
        public double FeedSpeedTechnology { get; set; } = 1.5;
        public string WaferFileName { get; set; }

        [ICommand]
        private void TechnologySettings()
        {
            var technologySettingsView = new TechnologySettingsView
            {
                DataContext = new TechnologySettingsViewModel()
            };

            technologySettingsView.ShowDialog();

            // TODO ASYNC
            _technology = StatMethods.DeSerializeObjectJson<Technology>(Settings.Default.TechnologyLastFile);

            PassCountTechnology = _technology.PassCount;
            FeedSpeedTechnology = _technology.FeedSpeed;

            _dicingProcess?.RefreshTechnology(_technology);
            //Process?.RefresfTechnology(_technology);
            Wafer?.SetPassCount(PropContainer.Technology.PassCount);
        }

        [ICommand]
        private void WaferSettings()
        {
            var waferSettingsView = new WaferSettingsView
            {
                DataContext = new WaferSettingsViewModel(_settingsService)
            };

            waferSettingsView.ShowDialog();
            AjustWaferTechnology();
        }

        [ICommand]
        private void OpenFile()
        {
            //var openFileDialog = new OpenFileDialog();
            //openFileDialog.Filter = "dxf files|*.dxf";
            //openFileDialog.Title = "Выберите файл";
            //if ((bool)openFileDialog.ShowDialog())
            //{
            //    string filePath = openFileDialog.FileName;
            //    var dxf = DxfDocument.Load(filePath);
            //    Wafer = new Wafer(1, dxf, "REZ");
            //}
            //Wafer = new Wafer(new Vector2(0, 0), 1, (0, 60000, 48000, 3100), (90, 48000, 60000, 5100));
            //Wafer = new Wafer(new Vector2(0, 0), 1, 5000, (0, 500), (60, 300));

            //Thickness = 1;
        }
    }
}

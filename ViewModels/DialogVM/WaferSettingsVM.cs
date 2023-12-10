using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using DicingBlade.Classes.WaferGeometry;
using DicingBlade.Utility;
using MachineControlsLibrary.CommonDialog;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Win32;
using PropertyChanged;

namespace DicingBlade.ViewModels.DialogVM
{

    [AddINotifyPropertyChangedInterface]
    public partial class WaferSettingsVM : CommonDialogResultable<WaferSettingsVM>, IWafer
    {
        private Action<IWafer> _actionWhenOpened;

        public bool IsRound
        {
            get; set;
        }
        public bool IsSquare
        {
            get => !IsRound;
        }
        public double Width
        {
            get; set;
        }
        public double Height
        {
            get; set;
        }
        public double Thickness
        {
            get; set;
        }
        public double IndexW
        {
            get; set;
        }
        public double IndexH
        {
            get; set;
        }
        public double Diameter
        {
            get; set;
        }
        public string FileName
        {
            get; set;
        }
        public bool NewFileCreated
        {
            get; private set;
        }
        public Wafer Wafer
        {
            get; set;
        }
        public WaferSettingsVM(string filename)
        {
            if (filename == string.Empty || !File.Exists(filename))
            {
                Width = 30;
                Height = 10;
                Thickness = 1;
                IndexH = 1;
                IndexW = 2;
                Diameter = 40;
            }
            else
            {
                FileName = filename;
                ((IWafer)StatMethods.DeSerializeObjectJson<TempWafer>(FileName)).CopyPropertiesTo(this);
            }
        }
        public WaferSettingsVM(IWafer wafer)
        {
            wafer.CopyPropertiesTo(this);
        }
        public void SetCurrentIndex(double index)
        {
            switch (CurrentSide)
            {
                case 0:
                    IndexH = index;
                    break;
                case 1:
                    IndexW = index;
                    break;
                default:
                    break;
            };
        }
        public int CurrentSide
        {
            get; set;
        }

        [ICommand]
        private void ChangeShape()
        {
            IsRound ^= true;
        }
        [ICommand]
        private void OpenFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Файлы пластины (*.waf)|*.waf",
            };

            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                FileName = dialog.FileName;
                ((IWafer)StatMethods.DeSerializeObjectJson<TempWafer>(FileName)).CopyPropertiesTo(this);
            }
        }

        [ICommand]
        private void SaveFileAs()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Файлы пластины (*.waf)|*.waf",
            };

            var result = dialog.ShowDialog();
            if (result ?? false)
            {
                FileName = dialog.FileName;
            }
            this.SerializeObjectJson(FileName);
        }
        [ICommand]
        private void SaveFile()
        {
            if (HandyControl.Controls.MessageBox.Ask($"Сохранить изменения в файле {FileName}?") == MessageBoxResult.OK)
            {
                this.SerializeObjectJson(FileName);
            }
        }

        public override void SetResult() => SetResult(this);
    }
}

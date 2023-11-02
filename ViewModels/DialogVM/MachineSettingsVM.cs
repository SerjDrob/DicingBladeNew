using System.Windows.Input;
using DicingBlade.Properties;
using MachineControlsLibrary.CommonDialog;
using Microsoft.Toolkit.Mvvm.Input;
using PropertyChanged;
namespace DicingBlade.ViewModels.DialogVM
{

    [AddINotifyPropertyChangedInterface]
    internal partial class MachineSettingsVM : CommonDialogResultable<ViewFindersVM>
    {

        private double _xCurrentPosition;
        private double _yCurrentPosition;
        private double _zCurrentPosition;
        public ViewFindersVM ScaleGridView { get; set; } = new();
        internal MachineSettingsVM(double x, double y, double z)
        {
            _xCurrentPosition = x;
            _yCurrentPosition = y;
            _zCurrentPosition = z;
        }
        [ICommand]
        private void ZObjectiveTeach()
        {
            Settings.Default.ZObjective = _zCurrentPosition;
        }
        [ICommand]
        private void XyObjectiveTeach()
        {
            Settings.Default.XObjective = _xCurrentPosition;
            Settings.Default.YObjective = _yCurrentPosition;
        }
        [ICommand]
        private void XyLoadTeach()
        {
            Settings.Default.XLoad = _xCurrentPosition;
            Settings.Default.YLoad = _yCurrentPosition;
        }
        [ICommand]
        private void XDiskTeach()
        {
            Settings.Default.XDisk = _xCurrentPosition;
        }

        public override void SetResult() => SetResult(ScaleGridView);
    }
}

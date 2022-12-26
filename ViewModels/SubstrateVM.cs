using DicingBlade.Classes.WaferGeometry;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DicingBlade.ViewModels
{
    [INotifyPropertyChanged]
    internal partial class SubstrateVM
    {
        public WaferView WaferView { get; set; } = new();
        public double Thickness { get; set; } = 1;
        public double XView { get; set; }
        public double YView { get; set; }
        public double UView { get; set; }

        public double CCCenterXView { get; set; }
        public double CCCenterYView { get; set; }
        public double BCCenterXView { get; set; }
        public double BCCenterYView { get; set; }
        public double WaferCurrentShiftView { get; set; }
        public double XTrace { get; set; }
        public double YTrace { get; set; }
        public double XTraceEnd { get; set; }

        public double WvAngle { get; set; }
        public bool WvRotate { get; set; }
        public bool ResetView { get; private set; }
        public double RotatingTime { get; set; } = 1;

        public event EventHandler<SubstrateClickedArgs> SubstrateClicked;

        [ICommand]
        private async Task ClickOnWafer(object o)
        {
            var args = ((Point[] points, bool isLeftButton))o;
            var points = args.points;
            var x = 0d;
            var y = 0d;
            var index = WaferView.ShapeSize[0] > WaferView.ShapeSize[1] ? 0 : 1;
            x = points[index].X * 1.4 * WaferView.ShapeSize[index];
            y = points[index].Y * 1.4 * WaferView.ShapeSize[index];

            SubstrateClicked?.Invoke(this, new()
            {
                IsByLeftButtonClicked = args.isLeftButton,
                X = x,
                Y = y
            });
        }
        public void ResetTrace()
        {
            XTrace = 0;
            YTrace = 0;
            XTraceEnd = 0;
        }
        public void ResetWaferView()
        {
            WvAngle = default;
            ResetView ^= true;
        }
    }
    internal class SubstrateClickedArgs : EventArgs
    {
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsByLeftButtonClicked { get; set; }
    }
}

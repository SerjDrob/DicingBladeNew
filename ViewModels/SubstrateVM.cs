using Advantech.Motion;
using DicingBlade.Classes.Miscellaneous;
using DicingBlade.Classes.WaferGeometry;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Machine;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

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
        public ObservableCollection<TraceLine> ControlPointsView { get; set; } = new();
        public ObservableCollection<TraceLine> TracesCollectionView { get; set; } = new();

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
        public void ResetWaferView()
        {
            WvAngle = default;
            ResetView ^= true;
        }
        public void AddControlPoint(double x, double y, double angle)
        {
            var rotateTransform = new RotateTransform(angle);
            var point = new TranslateTransform(-CCCenterXView, -CCCenterYView).Transform(new Point(x, y));
            var point1 = rotateTransform.Transform(new Point(point.X - 1, point.Y + WaferCurrentShiftView));
            var point2 = rotateTransform.Transform(new Point(point.X + 1, point.Y + WaferCurrentShiftView));

            List<TraceLine> temp = new(ControlPointsView);
            temp.ForEach(br => br.Brush = Brushes.Blue);
            temp.Add(new TraceLine()
            { XStart = point1.X, XEnd = point2.X, YStart = point1.Y, YEnd = point2.Y, Brush = Brushes.OrangeRed });
            ControlPointsView = new ObservableCollection<TraceLine>(temp);
        }
        public void ClearControlPoints() => ControlPointsView.Clear();
        public void ClearTrace() => TracesCollectionView.Clear();
        public void AddTrace(double sideAngle)
        {
            var rotateTransform = new RotateTransform(
                                    sideAngle,
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
            XTrace = 0;
            YTrace = 0;
            XTraceEnd = 0;
        }
        public async Task BladeTracingTaskAsync(CancellationToken cancellationToken)
        {
            XTrace = XView;
            YTrace = YView;

            await Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    XTraceEnd = XView;
                    await Task.Delay(100);
                }
            });

            cancellationToken.ThrowIfCancellationRequested();
        }
        public void OnAxisMotionStateChanged(object? sender, AxisStateEventArgs e)
        {
            try
            {
                var state = new AxisStateView(Math.Round(e.Position, 3), Math.Round(e.CmdPosition, 3), e.NLmt, e.PLmt, e.MotionDone, e.MotionStart);

                switch (e.Axis)
                {
                    case Ax.X:
                        XView = state.Position;
                        break;
                    case Ax.Y:
                        YView = state.Position;
                        break;
                    case Ax.U:
                        YView = state.Position;
                        break;
                }
            }
            catch (Exception)
            {
                //throw;
            }
        }
    }
    internal class SubstrateClickedArgs : EventArgs
    {
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsByLeftButtonClicked { get; set; }
    }
}

using DicingBlade.Classes.Miscellaneous;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Machine;
using MachineClassLibrary.VideoCapture;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DicingBlade.ViewModels
{
    [INotifyPropertyChanged]
    internal partial class CameraVM
    {
        public double CameraScale { get; set; } = 1;
        public BitmapImage Bi { get; set; } = new();
        public double CutWidthView { get; set; } = 0.05;
        public double RealCutWidthView { get; set; } = 0.13;
        public double TeachMarkersRatio { get; } = 2;
        public double CutOffsetView { get; set; }
        public ScaleGrid ScaleGridView { get; set; }=new();
        public bool TeachVScaleMarkersVisibility { get; set; }
        public event EventHandler<ImageClickedArgs> ImageClicked;

        [ICommand]
        private async Task ClickOnImage(object o)
        {
            var point = (Point)o;
            var x =   point.X * CameraScale;
            var y = - point.Y * CameraScale;
            ImageClicked?.Invoke(this, new ImageClickedArgs(x, y));
        }
        
        public void OnBitmapChanged(object? sender, VideoCaptureEventArgs e) => Bi = e.Image;
    }

    internal class ImageClickedArgs : EventArgs
    {
        public ImageClickedArgs(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; set; }
        public double Y { get; set; }
    }
}

using System;
using Advantech.Motion;
using DicingBlade.Classes.WaferGrid;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Machine;
using PropertyChanged;

namespace DicingBlade.ViewModels;

[AddINotifyPropertyChangedInterface]
internal class CutLinesVM
{
    public CutLines CutLines
    {
        get;
        init;
    }
    
    private double _xVideo;
    private double _yVideo;
    private double _currentX;
    private double _currentY;   
    public double XVideo
    {
        get => _currentX - _xVideo;
        set => _xVideo = value;
    }
    public double YVideo
    {
        get => _currentY - _yVideo;
        set => _yVideo = value;
    }
    private double _xBlade;
    private double _yBlade;
    public double XBlade
    {
        get => _currentX - _xBlade;
        set => _xBlade = value;
    }
    
    public double YBlade
    {
        get => _currentY - _yBlade;
        set => _yBlade = value;
    }
    public CutLinesVM(CutLines cutLines, double xVideo, double yVideo, double xBlade, double yBlade)
    {
        CutLines = cutLines;
        XVideo = xVideo;
        YVideo = yVideo;
        XBlade = xBlade;
        YBlade = yBlade;

        _currentX = 95;
        _currentY = 80;
        eventHandler = (o, e) =>
        {
            switch (e.Axis)
            {
                case Ax.X:
                    _currentX = e.Position;
                    break;
                case Ax.Y:
                    _currentY = e.Position;
                    break;
                default:
                    break;
            }
        };
        
    }

    public EventHandler<AxisStateEventArgs> eventHandler { get; set; } 
}

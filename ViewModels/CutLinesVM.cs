using System;
using Advantech.Motion;
using DicingBlade.Classes.WaferGrid;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Machine;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PropertyChanged;

namespace DicingBlade.ViewModels;

[AddINotifyPropertyChangedInterface]
public partial class CutLinesVM
{
    public CutLines CutLines
    {
        get;
        private set;
    }
    
    public double XVideoPos { get; init; }
    public double YVideoPos { get; init; }
    private double _currentX;
    private double _currentY;   
    public double XVideo
    {
        get;
        set;
    }
    public double YVideo
    {
        get;
        set;
    }
    private double _xBlade;
    private double _yBlade;
    public double XBlade
    {
        get;
        set;
    }
    
    public double YBlade
    {
        get;
        set;
    }
    public CutLinesVM(CutLines cutLines, double xVideo, double yVideo, double xBlade, double yBlade)
    {
        CutLines = cutLines;
        XVideoPos = xVideo;
        YVideoPos = yVideo;
        _xBlade = xBlade;
        _yBlade = yBlade;

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
            XBlade = _currentX - _xBlade;
            YBlade = _currentY - _yBlade;
            XVideo = _currentX - XVideoPos;
            YVideo = _currentY - YVideoPos;
        };
        
    }
    public void SetCutLines(CutLines cutLines) => CutLines = cutLines;
    public EventHandler<AxisStateEventArgs> eventHandler { get; set; } 
}

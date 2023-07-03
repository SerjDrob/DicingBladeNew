using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using netDxf;
using static System.Math;
//using netDxf.Entities;
using System.ComponentModel;
using System.Security;
using System.Windows;
using PropertyChanged;
using DicingBlade.Classes.WaferGeometry;
using DicingBlade;
using DicingBlade.Classes;

namespace DicingBlade.Classes.WaferGeometry
{
    [AddINotifyPropertyChangedInterface]
    public class Wafer
    {
        public bool IsRound { get; set; }
       
        public int CurrentAngleNum { get; private set; }
        private Grid Grid { get; set; }
        public double Thickness { get; set; }
        private List<(double angle, double actualAngle, double indexShift)> Directions { get; set; }
        //public double GetCurrentDiretionAngle
        //{
        //    get
        //    {
        //        return Directions[CurrentAngleNum].angle;
        //    }
        //}
        //public double GetCurrentDiretionActualAngle
        //{
        //    get
        //    {
        //        return Directions[CurrentAngleNum].actualAngle;
        //    }
        //}
        //public double GetPrevDiretionAngle
        //{
        //    get
        //    {
        //        return CurrentAngleNum != 0 ? Directions[CurrentAngleNum - 1].angle : Directions.Last().angle;
        //    }
        //}
        //public double GetPrevDiretionActualAngle
        //{
        //    get
        //    {
        //        return CurrentAngleNum != 0 ? Directions[CurrentAngleNum - 1].actualAngle : Directions.Last().actualAngle;
        //    }
        //}
        //public double SetCurrentDirectionAngle
        //{
        //    set
        //    {
        //        Directions[CurrentAngleNum] = (Directions[CurrentAngleNum].angle, value, Directions[CurrentAngleNum].indexShift);
        //    }
        //}
        //public double AddToCurrentDirectionIndexShift
        //{
        //    set
        //    {
        //        var shift = Directions[CurrentAngleNum].indexShift;
        //        Directions[CurrentAngleNum] = (Directions[CurrentAngleNum].angle, Directions[CurrentAngleNum].actualAngle, shift + value);
        //        GetCurrentDirectionIndexShift = Directions[CurrentAngleNum].indexShift;
        //    }
        //}
        double _currentShift;
        //public double GetCurrentDirectionIndexShift //{ get; set; }
        //{
        //    set { _currentShift = value; }
        //    get => - Directions[CurrentAngleNum].indexShift;
        //}
        //public Wafer() { }
        //public void ResetWafer()
        //{
        //    foreach (var key in Grid.Lines.Keys)
        //    {
        //        foreach (var cut in Grid.Lines[key])
        //        {
        //            cut.ResetCut();
        //        }
        //    }
        //    CurrentAngleNum = 0;
        //}
        public void SetPassCount(int passes)
        {
            foreach (var key in Grid.Lines.Keys)
            {
                foreach (var cut in Grid.Lines[key])
                {
                    cut.CutCount = passes;
                }
            }
        }
        //public Wafer(double thickness, DxfDocument dxf, string layer)
        //{
        //    Thickness = thickness;
        //    Grid = new Grid(dxf.Lines.Where(l => l.Layer.Name == layer));
        //    MakeDirections(new List<double>(Grid.Lines.Keys));
        //    CurrentAngleNum = 0;
        //}
        private void MakeDirections(List<double> list) => Directions = list.Select(item => (item, item, 0d)).ToList();

        private Wafer(Point origin, double thickness, params (double degree, double length, double side, double index)[] directions)
        {
            Thickness = thickness;
            Grid = new Grid(origin, directions);
            MakeDirections(new List<double>(Grid.Lines.Keys));
            CurrentAngleNum = 0;
            IsRound = false;
        }
        //public Wafer(Point origin, double thickness, double diameter, params (double degree, double index)[] directions)
        //{
        //    Thickness = thickness;
        //    Grid = new Grid(origin, diameter, directions);
        //    MakeDirections(new List<double>(Grid.Lines.Keys));
        //    CurrentAngleNum = 0;
        //    IsRound = true;
        //}
        public WaferView GetWaferView()
        {
            var tempView = Grid.MakeGridView();
            tempView.IsRound = IsRound;
            return tempView;
        }
        //public bool NextDir(bool reset = false)
        //{
        //    if (Directions.Count - 1 == CurrentAngleNum)
        //    {
        //        if(reset) CurrentAngleNum = 0;
        //        return false;
        //    }
        //    else
        //    {
        //        CurrentAngleNum++;
        //        return true;
        //    }
        //}
        //public bool PrevDir() 
        //{
        //    if (CurrentAngleNum == 0)
        //    {
        //        CurrentAngleNum = Directions.Count - 1;
        //        return false;
        //    }
        //    else
        //    {
        //        CurrentAngleNum--;
        //        return true;
        //    }
        //}
        //private void RotateWafer(double angle, Vector2 origin) => Grid.RotateRawLines(angle);
        //public Cut GetNearestCut(double y) 
        //{
        //    int index = 0;
        //    double diff = Math.Abs(Grid.Lines[Directions[CurrentAngleNum].angle][index].StartPoint.Y-y);

        //    for (int i = 0; i < Grid.Lines[Directions[CurrentAngleNum].angle].Count; i++)
        //    {                
        //        if (Math.Abs(Grid.Lines[Directions[CurrentAngleNum].angle][i].StartPoint.Y - y) < diff)
        //        {
        //            diff = (Math.Abs(Grid.Lines[Directions[CurrentAngleNum].angle][i].StartPoint.Y - y));
        //            index = i;
        //        }
        //    }
        //    return (Cut)Grid.Lines[Directions[CurrentAngleNum].angle][index];
        //}
        private Cut GetCurrentCut(int currentLine)
        {
            return Grid.Lines[Directions[CurrentAngleNum].angle][currentLine];
        }
        //public Line2D GetCurrentLine(int currentLine)
        //{
        //    var line = Grid.GetCenteredLine(Directions[CurrentAngleNum].angle, currentLine);
        //    line.Start.Y += Directions[CurrentAngleNum].indexShift;
        //    line.End.Y += Directions[CurrentAngleNum].indexShift;
        //    return line;
        //}
        //public double GetCurrentCutZ(int currentLine)
        //{
        //    double res = GetCurrentCut(currentLine).CutRatio;
        //    return (1 - res) * Thickness;
        //}

        //public bool CurrentCutIncrement(int currentLine) 
        //{            
        //    return Grid.Lines.GetItemByIndex(CurrentAngleNum)[currentLine].NextCut();
        //}

        public static WaferBuilder GetWaferBuilder() => new WaferBuilder();

        public class WaferBuilder
        {
            private List<(double degree, double length, double side, double index)> _directions;
            public WaferBuilder AddDirection(double degree, double length, double side, double index)
            {
                _directions ??= new();
                _directions.Add((degree, length, side, index));
                return this;
            }
            public Wafer Build(Point origin, double thickness)
            {
                return new Wafer(origin, thickness, _directions.ToArray());
            }
            public Wafer Build(double thickness)
            {
                return Build(new(), thickness);
            }
        }
    }

}

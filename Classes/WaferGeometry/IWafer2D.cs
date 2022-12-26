using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Interop;
using DicingBlade;
using DicingBlade.Classes;
using DicingBlade.Classes.WaferGeometry;

namespace DicingBlade.Classes.WaferGeometry
{
    public abstract class Wafer2D
    {
        protected IShape _shape;
        public double Thickness { get; protected set; }
        protected Dictionary<int, (double angle, double index, double sideshift, double realangle)> _directions;
        public int CurrentSide { get; private set; } = 0;
        public void SetChanges(double indexH, double indexW, double thickness, IShape shape)
        {
            Thickness = thickness;
            _shape = shape;
            _directions = new();
            _directions.Add(0, (0, indexH, 0, 0));
            _directions.Add(1, (90, indexW, 0, 90));
        }
        public bool XMirror { get; set; } = true;
        public int CurrentLinesCount
        {
            get
            {
                return (int)Math.Floor(_shape.GetIndexSide(CurrentSide) / CurrentIndex);
            }
        }
        public int TotalLinesCount()
        {
            var count = 0;
            for (int i = 0; i < _directions.Count; i++)
            {
                var side = _shape.GetIndexSide(i);
                var index = _directions[i].index;
                count += (int)Math.Floor(side / index) + 1;
            }
            return count;
        }
        public double CurrentIndex
        {
            get => _directions[CurrentSide].index;
        }
        public int SidesCount
        {
            get => _directions.Count;
        }
        public double CurrentSideAngle
        {
            get => _directions[CurrentSide].angle;
        }
        public double CurrentSideActualAngle
        {
            get => _directions[CurrentSide].realangle;
        }
        private double _prevSideAngle = 0;
        public double PrevSideAngle { get => _prevSideAngle; }
        private double _prevSideActualAngle = 0;
        public double PrevSideActualAngle { get => _prevSideActualAngle; }

        public bool IncrementSide()
        {
            if (CurrentSide == _directions.Count - 1)
            {
                return false;
            }
            else
            {
                SetSide(CurrentSide + 1);
                CurrentCutNum = 0;
                return true;
            }
        }
        public bool DecrementSide()
        {
            if (CurrentSide == 0)
            {
                return false;
            }
            else
            {
                SetSide(CurrentSide - 1);
                CurrentCutNum = 0;
                return true;
            }
        }
        public void SetSide(int side)
        {
            if (side < 0 | side > _directions.Count - 1)
            {
                throw new Exception("");
            }
            else
            {
                _prevSideAngle = _directions[CurrentSide].angle;
                _prevSideActualAngle = _directions[CurrentSide].realangle;
                CurrentSide = side;
            }
        }
        public void SetCurrentIndex(double index)
        {
            var tuple = _directions[CurrentSide];
            _directions[CurrentSide] = (tuple.angle, index, tuple.sideshift, tuple.realangle);
        }
        public double CurrentSideLength
        {
            get
            {
                return _shape.GetLengthSide(CurrentSide);
            }
        }
        public double CurrentShift
        {
            get
            {
                return _directions[CurrentSide].sideshift;
            }
        }
        public void SetShape(IShape shape)
        {
            _shape = shape;
        }
        public double GetNearestY(double y)
        {
            var side = _shape.GetIndexSide(CurrentSide);
            var index = _directions[CurrentSide].index;
            var bias = (side - Math.Floor(side / index) * index) / 2;

            var num = 0;
            if ((num = GetNearestNum(y)) != -1)
            {
                return num * index + bias - side / 2;
            }
            else
            {
                throw new Exception("");
            }
        }
        public Line2D GetNearestCut(double y)
        {
            var side = _shape.GetLengthSide(CurrentSide);
            var index = _directions[CurrentSide].index;
            var num = 0;
            if ((num = GetNearestNum(y)) != -1)
            {
                return this[num];
            }
            else
            {
                throw new Exception("");
            }
        }
        public int GetNearestNum(double y)
        {
            var side = _shape.GetIndexSide(CurrentSide);
            var index = _directions[CurrentSide].index;
            var bias = (side - Math.Floor(side / index) * index) / 2;

            var ypos = y + side / 2;
            var delta = side;
            var num = -1;
            for (int i = 0; i < side / index; i++)
            {
                var d = Math.Abs(ypos - i * index - bias);
                if (d <= delta)
                {
                    delta = d;
                    num = i;
                }
            }
            return num;
        }
        public void TeachSideShift(double y)
        {
            _directions[CurrentSide] = (_directions[CurrentSide].angle, _directions[CurrentSide].index, -y + GetNearestY(y), _directions[CurrentSide].realangle);
        }
        public void AddToSideShift(double delta)
        {
            _directions[CurrentSide] = (_directions[CurrentSide].angle, _directions[CurrentSide].index, _directions[CurrentSide].sideshift + delta, _directions[CurrentSide].realangle);
        }
        public void TeachSideAngle(double angle)
        {
            _directions[CurrentSide] = (_directions[CurrentSide].angle, _directions[CurrentSide].index, _directions[CurrentSide].sideshift, angle);
        }
        public int CurrentCutNum { get; private set; } = 0;
        public bool SetCurrentCutNum(int num)
        {
            if (num >= 0 && num <= CurrentLinesCount)
            {
                CurrentCutNum = num;
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool IncrementCut()
        {
            if (CurrentCutNum == CurrentLinesCount)
            {
                return false;
            }
            else
            {
                CurrentCutNum++;
                return true;
            }
        }
        public bool DecrementCut()
        {
            if (CurrentCutNum == 0)
            {
                return false;
            }
            else
            {
                CurrentCutNum--;
                return true;
            }
        }
        public Line2D GetCurrentCut() => this[CurrentCutNum];
        public Line2D this[int cutNum]
        {
            get
            {
                if (cutNum < 0) cutNum = 0;

                if (cutNum >= CurrentLinesCount)
                {
                    LastCutOfTheSide = true;
                    cutNum = CurrentLinesCount;
                }
                else
                {
                    LastCutOfTheSide = false;
                }
                var angle = _directions[CurrentSide].angle;
                var line = _shape.GetLine2D(CurrentIndex, cutNum, angle);
                var sign = XMirror ? -1 : 1;
                line.Start.X *= sign;
                line.End.X *= sign;
                return line;
            }
        }
        public bool LastCutOfTheSide { get; private set; } = false;
        public bool IsLastSide { get { return SidesCount - 1 == CurrentSide; } }
        public double this[double zratio]
        {
            get
            {
                return zratio * Thickness;
            }
        }
        public void ResetWafer()
        {
            CurrentCutNum = 0;
            CurrentSide = 0;
            LastCutOfTheSide = false;
        }
    }
}

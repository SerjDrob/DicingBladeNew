using System;
using System.Windows;
using DicingBlade;
using DicingBlade.Classes;
using DicingBlade.Classes.WaferGeometry;

namespace DicingBlade.Classes.WaferGeometry
{
    public class Circle2D : IShape
    {
        public double Diameter { get; private set; }

        public Circle2D(double diameter)
        {
            Diameter = diameter;
        }

        public Line2D GetLine2D(double index, int num, double angle)
        {
            var delta = (Diameter - Math.Floor(Diameter / index) * index) / 2;
            var zeroShift = index * num;
            var r = Diameter / 2;
            var y = zeroShift - r + delta;

            var xh = Math.Sqrt(r * r - y * y);

            return new Line2D(new Point(-xh, y), new Point(xh, y));
        }
        public bool InYArea(double zeroShift, double angle)
        {
            return angle switch
            {
                0 => zeroShift + Diameter / 2 < Diameter & zeroShift + Diameter / 2 > 0,
                90 => zeroShift + Diameter / 2 < Diameter & zeroShift + Diameter / 2 > 0
            };
        }
        public double GetLengthSide(int side)
        {
            return side switch
            {
                0 => Diameter,
                1 => Diameter
            };
        }
        public double GetIndexSide(int side)
        {
            return side switch
            {
                1 => Diameter,
                0 => Diameter
            };
        }
    }
}

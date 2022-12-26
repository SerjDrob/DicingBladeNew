using System;
using System.Windows;
using DicingBlade;
using DicingBlade.Classes;
using DicingBlade.Classes.WaferGeometry;

namespace DicingBlade.Classes.WaferGeometry
{
    public class Rectangle2D : IShape
    {
        public double Width { get; private set; }
        public double Height { get; private set; }
        public Rectangle2D(double width, double height)
        {
            Width = width;
            Height = height;
        }
        public Line2D GetLine2D(double index, int num, double angle)
        {
            var delta0 = (Height - Math.Floor(Height / index) * index) / 2;
            var delta90 = (Width - Math.Floor(Width / index) * index) / 2;
            var zeroShift = index * num;
            return angle switch
            {
                0 => new Line2D() { Start = new Point(-Width / 2, zeroShift - Height / 2 + delta0), End = new Point(Width / 2, zeroShift - Height / 2 + delta0) },
                90 => new Line2D() { Start = new Point(-Height / 2, zeroShift - Width / 2 + delta90), End = new Point(Height / 2, zeroShift - Width / 2 + delta90) }
            };
        }
        public bool InYArea(double zeroShift, double angle)
        {
            return angle switch
            {
                0 => zeroShift + Height / 2 < Height & zeroShift + Height / 2 > 0,
                90 => zeroShift + Width / 2 < Width & zeroShift + Width / 2 > 0
            };
        }
        public double GetLengthSide(int side)
        {
            return side switch
            {
                0 => Width,
                1 => Height
            };
        }
        public double GetIndexSide(int side)
        {
            return side switch
            {
                1 => Width,
                0 => Height
            };
        }
    }
}

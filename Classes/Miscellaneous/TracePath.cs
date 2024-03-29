﻿using DicingBlade;
using DicingBlade.Classes;
using DicingBlade.Classes.Miscellaneous;

namespace DicingBlade.Classes.Miscellaneous
{
    internal class TracePath
    {
        public TracePath(double y, double x, double xend, double initAngle)
        {
            Y = y;
            X = x;
            Xend = xend;
            InitAngle = initAngle;
        }
        public double Y { get; set; }
        public double X { get; set; }
        public double Xend { get; set; }
        public double InitAngle { get; set; }
    }
}
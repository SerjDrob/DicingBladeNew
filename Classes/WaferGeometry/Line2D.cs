using System.Windows;
using DicingBlade;
using DicingBlade.Classes;
using DicingBlade.Classes.WaferGeometry;

namespace DicingBlade.Classes.WaferGeometry
{
    public struct Line2D
    {
        public Line2D(Point start, Point end)
        {
            Start = start;
            End = end;
        }
        public Point Start;
        public Point End;
    }
}

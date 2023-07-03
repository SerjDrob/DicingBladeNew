using System.Collections.Generic;
using static System.Math;
//using netDxf.Entities;
using netDxf;
using System.Linq;
using System.Collections.ObjectModel;
using PropertyChanged;
//using static AForge.Math.FourierTransform;
using System.Windows;
using DicingBlade;
using DicingBlade.Classes;
using DicingBlade.Classes.WaferGeometry;

namespace DicingBlade.Classes.WaferGeometry
{
    [AddINotifyPropertyChangedInterface]
    public class Grid
    {
        #region Constructors
        //public Grid() { }
        public Grid(Point origin, params (double degree, double length, double side, double index)[] directions)
        {
            GridCenter = origin;
            _directions = directions;
            Lines = new Dictionary<double, List<Cut>>();
            GenerateLines();
        }
        #endregion
        #region Privates
        private (double degree, double length, double side, double index)[] _directions;
        private Point GridCenter { get; set; }

        #endregion
        #region Publics
        public Dictionary<double, List<Cut>> Lines { get; }
        #endregion
        #region Functions
        private Cut RotateLine(double angle, Line2D line, Point origin)
        {
            var translating = new Matrix3(1, 0, -origin.X, 0, 1, -origin.Y, 0, 0, 1);
            var returning = new Matrix3(1, 0, origin.X, 0, 1, origin.Y, 0, 0, 1);
            var rotating = new Matrix3(Cos(angle), -Sin(angle), 0, Sin(angle), Cos(angle), 0, 0, 0, 1);

            var startPoint = returning * rotating * translating * new Vector3(line.Start.X, line.Start.Y, 1);
            var endPoint = returning * rotating * translating * new Vector3(line.End.X, line.End.Y, 1);
            return new Cut(new(startPoint.X, startPoint.Y), new(endPoint.X, endPoint.Y));
        }
        private void GenerateLines()
        {
            foreach (var direction in _directions)
            {
                List<Cut> tempLines = new List<Cut>();
                int count = (int)Floor(direction.side / direction.index);
                double firstStep = (direction.side - direction.index * count) / 2;

                var dx = GridCenter.X - direction.length / 2;
                var dy = GridCenter.Y - direction.side / 2;
                for (int i = 0; i < count + 1; i++)
                {
                    var y = firstStep + direction.index * i + dy;
                    tempLines.Add(new Cut(new Point(dx, y), new Point(direction.length + dx, y)));
                }
                Lines.Add(direction.degree, tempLines);
            }
        }
        public WaferView MakeGridView()
        {
            var tempRaws = new List<Line2D>();
            foreach (var (degree, list) in Lines)
            {
                foreach (var cut in list)
                {
                    var tempLine = RotateLine(degree * PI / 180, new Line2D(cut.StartPoint, cut.EndPoint), GridCenter);
                    var startPoint = new Point(tempLine.StartPoint.X - GridCenter.X, tempLine.StartPoint.Y - GridCenter.Y);
                    var endPoint = new Point(tempLine.EndPoint.X - GridCenter.X, tempLine.EndPoint.Y - GridCenter.Y);
                    tempRaws.Add(new Line2D() { Start = startPoint, End = endPoint });
                }
            }
            return new WaferView(tempRaws);
        }
        #endregion
    }

   







}

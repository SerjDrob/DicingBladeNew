using System.Collections.Generic;
using static System.Math;
//using netDxf.Entities;
using netDxf;
using System.Linq;
using System.Collections.ObjectModel;
using PropertyChanged;
//using static AForge.Math.FourierTransform;
using System.Windows;

namespace DicingBlade.Classes
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
            //ShapeSize = GetSize();
        }
        //public Grid(Point origin, double diameter, params (double degree, double index)[] directions)
        //{
        //    GridCenter = origin;
        //    _directionsD = directions;
        //    _diameter = diameter;
        //    Lines = new Dictionary<double, List<Cut>>();
        //    GenerateLinesD();
        //    //ShapeSize = GetSize();
        //}
        //public Grid(IEnumerable<Line2D> rawLines)
        //{
        //    RawLines = new ObservableCollection<Line2D>(rawLines);

        //    var xmax = RawLines.Max(l => l.End.X) > RawLines.Max(l => l.Start.X) ? RawLines.Max(l => l.End.X) : RawLines.Max(l => l.Start.X);
        //    var ymax = RawLines.Max(l => l.End.Y) > RawLines.Max(l => l.Start.Y) ? RawLines.Max(l => l.End.Y) : RawLines.Max(l => l.Start.Y);
        //    var xmin = RawLines.Min(l => l.End.X) < RawLines.Min(l => l.Start.X) ? RawLines.Min(l => l.End.X) : RawLines.Min(l => l.Start.X);
        //    var ymin = RawLines.Min(l => l.End.Y) < RawLines.Min(l => l.Start.Y) ? RawLines.Min(l => l.End.Y) : RawLines.Min(l => l.Start.Y);
        //    GridCenter = new Point((xmax - xmin) / 2, (ymax - ymin) / 2);
        //    var lines = new List<(double degree, Cut line)>(RawLines.Count());

        //    Lines = new Dictionary<double, List<Cut>>();
        //    foreach (var line in RawLines)
        //    {
        //        lines.Add((GetAngle(line), RotateLine(-GetAngle(line), line, GridCenter)));
        //    }

        //    foreach (var angle in lines.OrderBy(d => d.degree).Select(d => d.degree).Distinct())
        //    {
        //        Lines.Add(angle, new List<Cut>(lines.Where(d => d.degree == angle).Select(l => l.line)));
        //    }
        //    //ShapeSize = GetSize();
        //}
        #endregion
        #region Privates
        private (double degree, double length, double side, double index)[] _directions;
        //private (double degree, double index)[] _directionsD;
        //private double _diameter;        
        private Point GridCenter { get; set; }

        #endregion
        #region Publics

        //public IEnumerable<Line2D> RawLines { get; set; }
        public Dictionary<double, List<Cut>> Lines { get; }

        //public Line2D GetCenteredLine(double angle, int line)
        //{
        //    //  if (!Lines.Keys.Contains(angle)) throw;
        //    var start = Lines[angle][line].StartPoint - GridCenter;
        //    var end = Lines[angle][line].EndPoint - GridCenter;
        //    return new Line2D((Point)start,(Point)end);
        //}
        #endregion
        #region Functions
        private Cut RotateLine(double angle, Line2D line, Point origin)
        {
            var translating = new Matrix3(1, 0, -origin.X, 0, 1, -origin.Y, 0, 0, 1);
            var returning = new Matrix3(1, 0, origin.X, 0, 1, origin.Y, 0, 0, 1);
            var rotating = new Matrix3(Cos(angle), -Sin(angle), 0, Sin(angle), Cos(angle), 0, 0, 0, 1);
                        
            var startPoint = returning * rotating * translating * new Vector3(line.Start.X, line.Start.Y, 1);
            var endPoint = returning * rotating * translating * new Vector3(line.End.X, line.End.Y, 1);
            return new Cut(new(startPoint.X,startPoint.Y), new(endPoint.X,endPoint.Y));
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="angle"></param>
        //public void RotateRawLines(double angle)
        //{
        //    List<Line> tempLines = new List<Line>(RawLines);
        //    var translating = new Matrix3(1, 0, -GridCenter.X, 0, 1, -GridCenter.Y, 0, 0, 1);
        //    var returning = new Matrix3(1, 0, GridCenter.X, 0, 1, GridCenter.Y, 0, 0, 1);
        //    var rotating = new Matrix3(Cos(angle), -Sin(angle), 0, Sin(angle), Cos(angle), 0, 0, 0, 1);
        //    //RawLines = new ObservableCollection<Line>();
        //    foreach (var line in tempLines)
        //    {
        //        line.StartPoint = returning * rotating * translating * new Vector3(line.StartPoint.X,line.StartPoint.Y,1);
        //        line.EndPoint = returning * rotating * translating * new Vector3(line.EndPoint.X,line.EndPoint.Y,1);
        //       // RawLines.Add(line);
        //    }
        //    RawLines = new ObservableCollection<Line>(tempLines);
        //}
        //private double GetAngle(Line2D line)
        //{
        //    return Atan2(line.End.Y - line.Start.Y, line.End.X - line.Start.X) * (180 / PI);
        //}

        //private double[] GetSize()
        //{
        //    return new double[]
        //    {
        //        RawLines.Max(l=>l.Start.X>l.End.X?l.Start.X:l.End.X)-RawLines.Min(l=>l.Start.X<l.End.X?l.Start.X:l.End.X),
        //        RawLines.Max(l=>l.Start.Y>l.End.Y?l.Start.Y:l.End.Y)-RawLines.Min(l=>l.Start.Y<l.End.Y?l.Start.Y:l.End.Y)
        //    };
        //}

        private void GenerateLines()
        {
            foreach (var direction in _directions)
            {
                List<Cut> tempLines = new List<Cut>();
                int count = (int)Floor(direction.side / direction.index);
                double firstStep = (direction.side - direction.index * count) / 2;

                var dx = GridCenter.X - direction.length / 2;
                var dy = GridCenter.Y - direction.side / 2;
                for (int i = 0; i < count+1; i++)
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
                    var startPoint = new System.Windows.Point(tempLine.StartPoint.X - GridCenter.X, tempLine.StartPoint.Y - GridCenter.Y);
                    var endPoint = new System.Windows.Point(tempLine.EndPoint.X - GridCenter.X, tempLine.EndPoint.Y - GridCenter.Y);
                    tempRaws.Add(new Line2D(){ Start = startPoint, End = endPoint});
                }
            }
            return new WaferView(tempRaws);
        }
        //private void GenerateLinesD()
        //{
        //    double c;
        //    double d;
        //    double x1;
        //    double x2;
        //    foreach (var (degree, index) in _directionsD)
        //    {
        //        int count = (int)Floor(_diameter / index);
        //        double firstStep = (_diameter - index * count) / 2;
        //        List<Cut> tempLines = new List<Cut>(count + 1);

        //        for (int i = 0; i < count + 1; i++)
        //        {
        //            c = firstStep + index * i;
        //            d = 4 * (_diameter * c - Pow(c, 2));

        //            if (d <= 0)
        //            {
        //                continue;
        //            }

        //            x1 = (_diameter - Sqrt(d)) / 2;
        //            x2 = (_diameter + Sqrt(d)) / 2;
        //            var dx = GridCenter.X - _diameter / 2;
        //            var dy = GridCenter.Y - _diameter / 2;
        //            tempLines.Add(new Cut(new Point(x1 + dx, dy + firstStep + index * i), new Point(x2 + dx, dy + firstStep + index * i)));
        //        }
        //        Lines.Add(degree, tempLines);
        //    }
        //}
        #endregion
    }


    public record Direction(double Angle, double Index);

    public class Grid1
    {
        private IEnumerable<Direction> _directions;
        private Point _origin;
    }
    






}

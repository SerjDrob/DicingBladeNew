using System.Windows;
using DicingBlade;
using DicingBlade.Classes;
using DicingBlade.Classes.WaferGeometry;

namespace DicingBlade.Classes.WaferGeometry
{
    public class Cut //: Line2D
    {
        public Cut(Cut cut)
        {
            //this.Clone = cut;
        }
        public Cut(Point startpoint, Point endpoint)
        {
            StartPoint = startpoint;
            EndPoint = endpoint;
            Status = true;
            CutCount = 1;
            Offset = 0;
            CutDirection = Directions.Direct;
        }

        public Cut(Line2D line) : this(line.Start, line.End)
        {

        }
        /// <summary>
        /// направление резки - встречная, попутная, встречно-попутная        
        /// </summary>
        public Directions CutDirection { get; set; }
        /// <summary>
        /// true - действует, false - отключен или выполнен
        /// </summary>
        public bool Status
        {
            get => CurrentCut / CutCount == 1 ? false : true;
            private set { }
        }
        public int CutCount { get; set; }
        private int CurrentCut { get; set; } = 0;
        public double Offset { get; set; }
        public Point StartPoint { get; private set; }
        public Point EndPoint { get; private set; }
    }
}

using System.Collections.Generic;
using System.Linq;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Windows.Media;
using DicingBlade.Classes.WaferGeometry;
using DicingBlade;
using DicingBlade.Classes;

namespace DicingBlade.Classes.WaferGeometry
{
    [AddINotifyPropertyChangedInterface]
    public class WaferView
    {
        public WaferView(ICollection<Line2D> rawLines)
        {
            RawLines = new ObservableCollection<Line2D>(rawLines);
            ShapeSize = GetSize();
        }
        public WaferView()
        {
            RawLines = new ObservableCollection<Line2D>();
        }
        public bool IsRound { get; set; }
        public ObservableCollection<Line2D> RawLines { get; set; }
        private double[] GetSize()
        {
            if (RawLines.Any())
            {
                return new double[]
                            {
                            RawLines.Max(l=>l.Start.X>l.End.X?l.Start.X:l.End.X)-RawLines.Min(l=>l.Start.X<l.End.X?l.Start.X:l.End.X),
                            RawLines.Max(l=>l.Start.Y>l.End.Y?l.Start.Y:l.End.Y)-RawLines.Min(l=>l.Start.Y<l.End.Y?l.Start.Y:l.End.Y)
                            };
            }
            else
            {
                return new double[] { 10, 10 };
            }
        }

        public double[] ShapeSize { get; set; }
        public void SetView(IWaferViewFactory concreteFactory)
        {
            RawLines = concreteFactory.GetWaferView();
            ShapeSize = GetSize();
        }
    }
}

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace DicingBlade.Classes.WaferGeometry
{
    public class WaferViewFactory : IWaferViewFactory
    {
        private Wafer2D _substrate;
        private int _side;
        public WaferViewFactory(Wafer2D wafer)
        {
            _substrate = wafer;
            _side = wafer.CurrentSide;
        }
        public ObservableCollection<Line2D> GetWaferView()
        {

            var rotation = new RotateTransform(0);
            var shift = 0;
            var tempLines = new List<Line2D>();
            for (int i = 0; i < _substrate.SidesCount; i++)
            {
                _substrate.SetSide(_substrate.CurrentSide + i + shift);
                for (int j = 0; j < _substrate.CurrentLinesCount + 1; j++)
                {
                    var pointStart = rotation.Transform(_substrate[j].Start);
                    var pointEnd = rotation.Transform(_substrate[j].End);
                    tempLines.Add(new Line2D() { Start = pointStart, End = pointEnd });
                }

                rotation.Angle += 90;
                if (_substrate.CurrentSide == _substrate.SidesCount - 1)
                {
                    shift = -(i + 2);
                }
            }
            _substrate.SetSide(_side);
            return new(tempLines);
        }
    }
}

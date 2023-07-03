using DicingBlade.Classes;
using DicingBlade.Classes.Miscellaneous;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DicingBlade.Converters
{
    internal class ScaleGridConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ScaleGrid scaleGrid)
            {
                var widthX = scaleGrid.SmallStrokeWidth / 2;
                var midWidthX = widthX * scaleGrid.MiddleStrokeWidthRatio;
                var bigWidthX = widthX * scaleGrid.BigStrokeWidthRatio;
                var indexY = scaleGrid.Index;
                var yOffset = scaleGrid.ScaleLength / 2;
                var count = (int)Math.Floor(scaleGrid.ScaleLength / scaleGrid.Index);
                count = count % 2 == 0 ? count + 1 : count;
                var result =  Enumerable.Range(0, count)
                    .Select(num =>
                    {
                        var y = num * indexY - yOffset;
                        if (num % scaleGrid.InBigStrokeCount == 0 && bigWidthX!=0) return new Line { X1 = -bigWidthX, Y1 = y, X2 = bigWidthX, Y2 = y };
                        if (num % scaleGrid.InMiddleStrokeCount == 0 && midWidthX!=0) return new Line { X1 = -midWidthX, Y1 = y, X2 = midWidthX, Y2 = y };
                        return new Line { X1 = -widthX, Y1 = y, X2 = widthX, Y2 = y };
                    })
                    .Select(line=>new LineGeometry(new(line.X1,line.Y1),new(line.X2,line.Y2)))
                    .ToArray();


                var path = new Path();
                var collection = new GeometryCollection(result);
                

                return collection;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

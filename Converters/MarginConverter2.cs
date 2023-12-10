using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DicingBlade.Converters
{
    internal class MarginConverter2 : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var actualWidth = (double)values[0];
                var actualHeight = (double)values[1];
                var leftRight = actualHeight > actualWidth ? 100 : (actualWidth - actualHeight) / 2 + 100;
                var topBot = leftRight > 100 ? 100 : (actualHeight - actualWidth) / 2 + 100;
                var margin = new Thickness(leftRight, topBot, leftRight, topBot);
                return margin;
            }
            catch
            {
                return new Thickness(100);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

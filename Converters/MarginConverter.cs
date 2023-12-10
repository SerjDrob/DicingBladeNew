using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DicingBlade.Converters
{
    internal class MarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var grid = value as Grid;
                if (grid != null)
                {
                    var leftRight = grid.ActualHeight > grid.ActualWidth ? 100 : (grid.ActualWidth - grid.ActualHeight)/2 + 100;
                    var topBot = leftRight > 100 ? 100 : (grid.ActualHeight - grid.ActualWidth) / 2 + 100 ;
                    var margin = new Thickness(leftRight,topBot,leftRight,topBot);
                    return margin;
                }
                return new Thickness(100);
            }
            catch 
            {
                return new Thickness(100);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Globalization;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DicingBlade.Converters
{
    internal class ConditionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Int32 mask = 0;
            int bit = 0;
            try
            {
                mask = System.Convert.ToInt32(values[0]);
                bit = System.Convert.ToInt32(values[1]);
            }
            catch { }
            return (mask & (1 << bit)) != 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

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
                    var topBot = leftRight > 100 ? (grid.ActualHeight - grid.ActualWidth) / 2 + 100 : 100;
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

    internal class ShareToValueConvereter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var tuple = (ValueTuple<int, int>)value;
                var itemNum = (int)parameter;
                if (itemNum == 2) return tuple.Item2;
                return tuple.Item1;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}

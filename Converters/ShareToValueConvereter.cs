using System;
using System.Globalization;
using System.Windows.Data;

namespace DicingBlade.Converters
{
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

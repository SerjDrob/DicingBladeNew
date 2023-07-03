using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace DicingBlade.Converters
{
    public class MulConvereter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return values.Cast<double>().Aggregate((prev, cur) => prev * cur);
            }
            catch (InvalidCastException ex)
            {
               // throw;
            }
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

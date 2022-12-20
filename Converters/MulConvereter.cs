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
                var result = values.Aggregate((prev, cur) =>
                {
                    var prevVal = System.Convert.ToDouble(prev);
                    var curVal = System.Convert.ToDouble(cur);
                    return prevVal * curVal;
                });
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using DicingBlade.Views.CutsProcessViews;
using System;
using System.Globalization;
using System.Windows.Data;

namespace DicingBlade.Converters
{
    internal class ShareToOffsetConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var share = (Share)values[0];
                var height = (double)values[1];
                return height * share.Total / 100;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

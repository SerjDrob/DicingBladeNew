using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using cursor = System.Windows.Forms.Cursor;

namespace DicingBlade.Converters
{
    internal class MouseArgsCanvasConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var args = value as MouseButtonEventArgs;
            var field = parameter as System.Windows.FrameworkElement;
            var point = args.GetPosition(parameter as IInputElement);
            

            //var point1 = new Point((point.X / field.ActualWidth) - 0.5, (point.Y / field.ActualHeight) - 0.5);
            //var point2 = new Point((point.X / field.ActualHeight) -0.5, (point.Y / field.ActualHeight) - 0.5);
            
            var point1 = new Point((point.X / field.ActualWidth) - 0.5, (point.Y - field.ActualHeight * 0.5) / field.ActualWidth);
            var point2 = new Point((point.X - 0.5 * field.ActualWidth) / field.ActualHeight, (point.Y / field.ActualHeight) - 0.5);
            return new Point[]{ point1, point2};
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    internal class MouseArgsCanvasConverter2 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var args = value as MouseButtonEventArgs;
            var field = parameter as System.Windows.FrameworkElement;
            var point = args.GetPosition(parameter as IInputElement);

            var isLeftButton = args.LeftButton.HasFlag(MouseButtonState.Pressed);
            var isRightButton = args.LeftButton.HasFlag(MouseButtonState.Pressed);


            var point1 = new Point((point.X / field.ActualWidth) - 0.5, (point.Y - field.ActualHeight * 0.5) / field.ActualWidth);
            var point2 = new Point((point.X - 0.5 * field.ActualWidth) / field.ActualHeight, (point.Y / field.ActualHeight) - 0.5);
            var points =  new Point[] { point1, point2 };
            return (points, isLeftButton);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

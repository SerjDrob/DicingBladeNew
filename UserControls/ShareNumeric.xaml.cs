using System.Windows;
using System.Windows.Controls;

namespace DicingBlade.UserControls
{
    /// <summary>
    /// Interaction logic for ShareNumeric.xaml
    /// </summary>
    public partial class ShareNumeric : UserControl
    {
        public ShareNumeric()
        {
            InitializeComponent();
        }



        public int Share
        {
            get { return (int)GetValue(ShareProperty); }
            set { SetValue(ShareProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Share.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShareProperty =
            DependencyProperty.Register("Share", typeof(int), typeof(ShareNumeric), new PropertyMetadata(0));




        public int TotalShare
        {
            get { return (int)GetValue(TotalShareProperty); }
            set { SetValue(TotalShareProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TotalShare.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TotalShareProperty =
            DependencyProperty.Register("TotalShare", typeof(int), typeof(ShareNumeric), new PropertyMetadata(0));


    }
}

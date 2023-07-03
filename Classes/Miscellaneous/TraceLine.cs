using System.Windows.Media;
using DicingBlade;
using DicingBlade.Classes;
using DicingBlade.Classes.Miscellaneous;
using PropertyChanged;

namespace DicingBlade.Classes.Miscellaneous
{
    [AddINotifyPropertyChangedInterface]
    public class TraceLine
    {
        public double XStart { get; set; }
        public double XEnd { get; set; }
        public double YStart { get; set; }
        public double YEnd { get; set; }
        public Brush Brush { get; set; }
    }
}
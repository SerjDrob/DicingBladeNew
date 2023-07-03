using DicingBlade;
using DicingBlade.Classes;
using DicingBlade.Classes.WaferGeometry;
using DicingBlade.ViewModels;

namespace DicingBlade.Classes.WaferGeometry
{
    public interface IWafer
    {
        bool IsRound { get; set; }
        double Width { get; set; }
        double Height { get; set; }
        double Thickness { get; set; }
        double IndexW { get; set; }
        double IndexH { get; set; }
        double Diameter { get; set; }
        string FileName { get; set; }
        public void SetCurrentIndex(double index);
        public int CurrentSide { get; set; }

    }
}

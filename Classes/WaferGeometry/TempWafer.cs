using DicingBlade;
using DicingBlade.Classes;
using DicingBlade.Classes.WaferGeometry;
using DicingBlade.Utility;

namespace DicingBlade.Classes.WaferGeometry
{
    public class TempWafer : IWafer
    {
        public bool IsRound { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Thickness { get; set; }
        public double IndexW { get; set; }
        public double IndexH { get; set; }
        public double Diameter { get; set; }
        public string FileName { get; set; }
        public void SetCurrentIndex(double index)
        {
            switch (CurrentSide)
            {
                case 0:
                    IndexH = index;

                    break;
                case 1:
                    IndexW = index;

                    break;
                default:
                    break;
            };
        }
        public int CurrentSide { get; set; }

        public TempWafer(IWafer wafer)
        {
            wafer.CopyPropertiesTo(this);
        }
        public TempWafer() { }
    }
}

using DicingBlade;
using DicingBlade.Classes;
using DicingBlade.Classes.WaferGeometry;

namespace DicingBlade.Classes.WaferGeometry
{
    public interface IShape
    {
        public bool InYArea(double zeroShift, double angle);
        public Line2D GetLine2D(double index, int num, double angle);
        public double GetLengthSide(int side);
        public double GetIndexSide(int side);
    }
}

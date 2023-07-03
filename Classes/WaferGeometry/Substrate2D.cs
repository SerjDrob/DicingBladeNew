using DicingBlade;
using DicingBlade.Classes;
using DicingBlade.Classes.WaferGeometry;

namespace DicingBlade.Classes.WaferGeometry
{
    public class Substrate2D : Wafer2D
    {
        public Substrate2D(double indexH, double indexW, double thickness, IShape shape)
        {
            SetChanges(indexH, indexW, thickness, shape);
        }

    }
}

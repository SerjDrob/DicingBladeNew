
//#nullable enable

using System;
using DicingBlade;
using DicingBlade.Classes;
using DicingBlade.Classes.WaferGeometry;

namespace DicingBlade.Classes.WaferGeometry
{
    /// <summary>
    /// Структура параметров процесса
    /// </summary>
    internal struct TempWafer2D
    {
        public bool FirstPointSet;
        public double[] Point1;
        public double[] Point2;
        public double GetAngle()
        {
            try
            {
                var tan = (Point2[1] - Point1[1]) / (Point2[0] - Point1[0]);
                var sign = Math.Sign(tan);
                var angle = Math.Atan(Math.Abs(tan)) * 180 / Math.PI;
                return sign * angle;
            }
            catch (Exception ex)
            {
                throw;
            }

        }
    }
}

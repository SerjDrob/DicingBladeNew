
//#nullable enable

using System;

namespace DicingBlade.Classes
{
    /// <summary>
    /// Структура параметров процесса
    /// </summary>
    internal struct TempWafer2D
    {
        //public bool Round;
        //public double XIndex;
        //public double XShift;
        //public double YIndex;
        //public double YShift;
        //public double XAngle;
        //public double YAngle;
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

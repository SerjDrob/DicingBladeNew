using System;

namespace DicingBlade.Classes.Processes
{
    public record RotationStarted(double Angle, TimeSpan Duration) : IProcessNotify;
}

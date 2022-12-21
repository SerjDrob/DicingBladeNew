using System;

namespace DicingBlade.Classes.Processes
{
    [Flags]
    public enum State
    {
        ProcessStarted = 1,
        TeachSides = 1<<1,
        Processing = 1<<2,
        Inspection = 1<<3,
        ProcessInterrupted = 1<<4,
        ProcessEnd = 1<<5,
        Cutting = 1<<6,
        GoingTransferingZ = 1<<7,
        GoingNextCutXY = 1<<8,
        GoingNextDepthZ = 1<<9,
        MovingNextSide = 1<<10,
        Correction = 1<<11,
        MovingTeachNextDirection = 1<<12,
        SingleCut = 1<<13
    }
}

﻿
//#nullable enable

using System;
using DicingBlade;
using DicingBlade.Classes;
using DicingBlade.Classes.Miscellaneous;

namespace DicingBlade.Classes.Miscellaneous
{
    public struct CheckCutControl
    {
        int startCut;
        int checkInterval;
        int currentCut;
        public bool Check;
        public void addToCurrentCut()
        {
            int res = 0;
            currentCut++;
            if (currentCut >= startCut)
            {
                Math.DivRem(currentCut - startCut, checkInterval, out res);
                Check = res == 0;
            }
            else
            {
                Check = false;
            }
        }
        public void Checked() => Check = false;
        public void Reset()
        {
            currentCut = 0;
        }
        public void Set(int start, int interval)
        {
            currentCut = 0;
            checkInterval = interval;
            startCut = start;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DicingBlade.Classes
{
    interface IWafer2
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double IndexW { get; set; }
        public double IndexH { get; set; }
        public double Thickness { get; set; }
    }
}

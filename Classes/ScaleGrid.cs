using System.ComponentModel;

namespace DicingBlade.Classes
{
    public class ScaleGrid
    {
        [Description("Ширина полосы корректировки, мкм")]
        public double Index { get; set; }
        public double SmallStrokeWidth { get; set; }
        public double StrokeThickness { get; set; }
        public int InMiddleStrokeCount { get; set; } = 5;
        public int InBigStrokeCount { get; set; } = 10;
        public int MiddleStrokeWidthRatio { get; set; } = 2;
        public int BigStrokeWidthRatio { get; set; } = 3;
        public double ScaleLength { get; set; }
    }
}
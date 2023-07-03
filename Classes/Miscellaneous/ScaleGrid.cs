using System.ComponentModel;
using System.Runtime.CompilerServices;
using DicingBlade;
using DicingBlade.Classes;
using DicingBlade.Classes.Miscellaneous;

namespace DicingBlade.Classes.Miscellaneous
{
    public class ScaleGrid
    {
        [Category("Измерительная шкала")]
        [DisplayName("Цена деления, мкм")]
        public double Index { get; set; } = 10;
        [Category("Измерительная шкала")]
        [DisplayName("Ширина малого деления, мкм")]
        public double SmallStrokeWidth { get; set; } = 10;
        [Category("Измерительная шкала")]
        [DisplayName("Толщина штриха, мкм")]
        public double StrokeThickness { get; set; } = 1;
        [Category("Измерительная шкала")]
        [DisplayName("Кратность среднего деления, шт")]
        public int InMiddleStrokeCount { get; set; } = 5;
        [Category("Измерительная шкала")]
        [DisplayName("Кратность большого деления, шт")]
        public int InBigStrokeCount { get; set; } = 10;
        [Category("Измерительная шкала")]
        [DisplayName("Относительная ширина среднего деления, шт")]
        public int MiddleStrokeWidthRatio { get; set; } = 2;
        [Category("Измерительная шкала")]
        [DisplayName("Относительная ширина большого деления, мкм")]
        public int BigStrokeWidthRatio { get; set; } = 3;
        [Category("Измерительная шкала")]
        [DisplayName("Длина шкалы, мкм")]
        public double ScaleLength { get; set; } = 200;
    }
}
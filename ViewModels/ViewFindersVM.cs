using System.ComponentModel;
using DicingBlade.Classes;
namespace DicingBlade.ViewModels
{
    internal class ViewFindersVM:ScaleGrid
    {
        [Category("Полосы")]
        [DisplayName("Ширина полосы реза, мкм")]
        public double RealCutWidth { get; set; } = 130;
        [Category("Полосы")]
        [DisplayName("Ширина полосы корректировки, мкм")]
        public double CorrectingCutWidth { get; set; } = 100;
    }
}

using PropertyChanged;
using System.Collections.ObjectModel;
using netDxf.Entities;
using DicingBlade;
using DicingBlade.Classes;
using DicingBlade.Classes.Miscellaneous;

namespace DicingBlade.Classes.Miscellaneous
{
    [AddINotifyPropertyChangedInterface]
    internal class TracesView
    {
        public TracesView()
        {
            Traces = new ObservableCollection<Line>();
        }
        public ObservableCollection<Line> Traces { get; set; }
    }
}

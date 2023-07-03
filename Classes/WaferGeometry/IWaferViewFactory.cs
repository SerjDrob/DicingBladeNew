using System.Collections.ObjectModel;

namespace DicingBlade.Classes.WaferGeometry
{
    public interface IWaferViewFactory
    {
        public ObservableCollection<Line2D> GetWaferView();
    }
}

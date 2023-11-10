using DicingBlade.Classes.WaferGrid;
using MachineControlsLibrary.CommonDialog;

namespace DicingBlade.ViewModels.DialogVM;
internal class CutSetVM : CommonDialogResultable<bool>
{
    public CutSet CutSet
    {
        get;
        set;
    }
    public CutSetVM()
    {
        
    }
    public CutSetVM(CutSet cutSet)
    {
        CutSet = cutSet;
    }

    public override void SetResult() => SetResult(true);
}

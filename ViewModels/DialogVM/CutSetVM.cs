using DicingBlade.Classes.WaferGrid;
using MachineControlsLibrary.CommonDialog;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace DicingBlade.ViewModels.DialogVM;
[INotifyPropertyChanged]
internal partial class CutSetVM : CommonDialogResultable<bool>
{
    public CutSet CutSet
    {
        get;
        set;
    }
    public CutSetVM()
    {
        
    }
    [ICommand]
    private void AddPass(CuttingStep step)
    {
        step.AddPass(new Pass() { DepthShare = 10, FeedSpeed = 33, PassNumber = 4, RPM = 45000 });
    }
    public CutSetVM(CutSet cutSet)
    {
        CutSet = cutSet;
    }

    public override void SetResult() => SetResult(true);
}

using System.Collections.ObjectModel;
using System.Linq;
using DicingBlade.Classes.WaferGrid;
using MachineControlsLibrary.CommonDialog;
using Microsoft.Toolkit.Mvvm.Input;
using PropertyChanged;

namespace DicingBlade.ViewModels.DialogVM;
[AddINotifyPropertyChangedInterface]
internal partial class CutSetVM : CommonDialogResultable<bool>
{
    public CutSet CutSet
    {
        get;
        set;
    }
    public ObservableCollection<CuttingStep> Steps
    {
        get; set;
    }


    public ObservableCollection<Pass> TestPasses
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
        var passes = step.Passes as ObservableCollection<Pass>;
        if (passes is not null) passes.Add(new Pass() { DepthShare = 10, FeedSpeed = 33, PassNumber = 4, RPM = 45000 });
    }
    [ICommand]
    private void RemovePass(CuttingStep step)
    {
        var passes = step.Passes as ObservableCollection<Pass>;
        if (passes is not null && passes.Count > 1) passes.RemoveAt(passes.Count - 1);
    }
    public CutSetVM(CutSet cutSet)
    {
        CutSet = cutSet;
        Steps = new ObservableCollection<CuttingStep>(cutSet.CuttingSteps.Select(s => new CuttingStep()
        {
            StepNumber = s.StepNumber,
            Count = s.Count,
            Index = s.Index,
            Length = s.Length,
            Passes = new ObservableCollection<Pass>(s.Passes),
        }));

        TestPasses = new ObservableCollection<Pass>(Steps[0].Passes);
    }

    public override void SetResult() => SetResult(true);
}

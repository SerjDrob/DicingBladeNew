using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DicingBlade.Classes.WaferGrid;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using MachineControlsLibrary.CommonDialog;
using Microsoft.Toolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using PropertyChanged;

namespace DicingBlade.ViewModels.DialogVM;
[AddINotifyPropertyChangedInterface]
internal partial class CutSetVM : CommonDialogResultable<CutSet>
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
   
    public CutSetVM()
    {

    }
    [ICommand]
    private async Task AddPass(CuttingStep step)
    {
        var result = await Dialog.Show<CommonDialog>()
            .SetDialogTitle("Распределение проходов")
            .SetDataContext(new CrudPassesVM(step.Passes), vm => { })
            .GetCommonResultAsync<IEnumerable<Pass>>();

        if (result.Success)
        {
            var passes = step.Passes as ObservableCollection<Pass>;
            if (passes is not null)
            {
                passes.Clear();
                result.CommonResult.ToList()
                    .ForEach(pass => passes.Add(pass)); 
            }
        }
    }
    [ICommand]
    private void RemovePass(CuttingStep step)
    {
        var passes = step.Passes as ObservableCollection<Pass>;
        if (passes is not null && passes.Count > 1) passes.RemoveAt(passes.Count - 1);
    }
    [ICommand]
    private void AddStep()
    {
        var lastStep = Steps.Last();
        var addedStep = new CuttingStep
        {
            Count = lastStep.Count,
            Index = lastStep.Index,
            StepNumber = ++lastStep.StepNumber,
            Length = lastStep.Length,
            Passes = new ObservableCollection<Pass>(lastStep.Passes.ToList()),
        };
        Steps.Add(addedStep);
    }
    public CutSetVM(CutSet cutSet)
    {
        CutSet = cutSet;
        Steps = new ObservableCollection<CuttingStep>(cutSet.CuttingSteps);
    }

    public override void SetResult() => SetResult(CutSet);
}

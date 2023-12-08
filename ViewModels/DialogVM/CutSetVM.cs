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
    private async Task AddPass(CuttingStep step)
    {
        var result = await Dialog.Show<CommonDialog>()
            .SetDialogTitle("Проходы")
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

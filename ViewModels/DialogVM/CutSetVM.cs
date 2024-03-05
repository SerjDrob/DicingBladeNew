using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DicingBlade.Classes.WaferGrid;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using MachineControlsLibrary.CommonDialog;
using Microsoft.Toolkit.Mvvm.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PropertyChanged;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using DialogResult = System.Windows.Forms.DialogResult;
using System.IO;

namespace DicingBlade.ViewModels.DialogVM;
[AddINotifyPropertyChangedInterface]
internal partial class CutSetVM : CommonDialogResultable<CutSet>
{
    public CutSet CutSet { get; set; }
    public ObservableCollection<CuttingStep> Steps { get; set; }
    public string FileName { get; set; }

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
    [ICommand]
    private void RemoveStep(CuttingStep step)
    {
        if (Steps.Count > 1) Steps.Remove(step);
    }
    public CutSetVM(CutSet cutSet)
    {
        CutSet = cutSet;
        Steps = new ObservableCollection<CuttingStep>(cutSet.CuttingSteps);
    }

    public override void SetResult() 
    {
        CutSet.CuttingSteps = Steps;
        SetResult(CutSet);
    }

    [ICommand]
    private async Task OpenStepSet()
    {
        var openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Файлы раскроя (*.csf)|*.csf";
        openFileDialog.DefaultExt = "*.csf";
        openFileDialog.Title = "Выберите файл";
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var filePath = openFileDialog.FileName;
                var json = File.ReadAllText(filePath);
                var result = JsonConvert.DeserializeObject<CutSet>(json);
                if (result != null)
                {
                    FileName = filePath;
                    CutSet = result;
                    Steps = new ObservableCollection<CuttingStep>(CutSet.CuttingSteps);
                }
            }
            catch (Exception)
            {

                //throw;
            }
        }
    }

    [ICommand]
    private async Task SaveStepSet()
    {
        var serialized = JsonConvert.SerializeObject(CutSet);

        var saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "Файлы раскроя (*.csf)|*.csf";
        saveFileDialog.DefaultExt = "*.csf";
        saveFileDialog.FileName = "";
        saveFileDialog.AddExtension = true;
        if (saveFileDialog.ShowDialog() == DialogResult.OK)
        {
            File.WriteAllText(saveFileDialog.FileName, serialized);
            FileName = saveFileDialog.FileName;
        }
    }
}

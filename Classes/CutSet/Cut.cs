using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls.Primitives;
using DicingBlade.Classes.Technology;

namespace DicingBlade.Classes.WaferGrid;
internal class Pass
{
    public int PassNumber
    {
        get;
        set;
    }
    /// <summary>
    /// mm/s
    /// </summary>
    public double FeedSpeed
    {
        get;
        set;
    }
    public int RPM
    {
        get;
        set;
    }
    /// <summary>
    /// %
    /// </summary>
    public int DepthShare
    {
        get;
        set;
    }
}

internal class CuttingStep
{
    public int StepNumber
    {
        get;
        set;
    }
    /// <summary>
    /// mm
    /// </summary>
    public double Length
    {
        get;
        set;
    }
    /// <summary>
    /// mm
    /// </summary>
    public double Index
    {
        get;
        set;
    }
    public int Count
    {
        get;
        set;
    }
    public IEnumerable<Pass> Passes
    {
        get;
        set;
    }
}

internal class CutSet
{
    /// <summary>
    /// Summary thickness including wafer's and film's. mm
    /// </summary>
    public double Thickness
    {
        get;
        set;
    }
    /// <summary>
    /// mm
    /// </summary>
    public double UnderCut
    {
        get;
        set;
    }
    public IEnumerable<CuttingStep> CuttingSteps
    {
        get;
        set;
    }
}

internal record CutLine(int LineNumber, int StepNumber, double FeedSpeed, int RPM, double Y, double XStart, double XEnd, double Z);

internal class CutLines : IEnumerable<CutLine>
{
    private readonly List<CutLine> _cutlines;
    private readonly int _lastStepNum;
    private double _yOffset = 0d;
    private int _currentLineNumber = -1;
    private int _currentStepNumber = 0;

    public double MaterialThickness
    {
        get; init;
    }
    public int LinesCount
    {
        get;
        init;
    }
    public int StepCount
    {
        get;
        init;
    }
    public bool NextLine()
    {
        if (_currentLineNumber < _cutlines.Count - 1)
        {
            _currentLineNumber++;
            _currentStepNumber = _cutlines.Where(c => c.LineNumber == _currentLineNumber).First().StepNumber;
            return true;
        }
        else
        {
            _currentLineNumber = -1;
            _currentStepNumber = 0;
            return false;
        }
    }
    public bool PrevLine()
    {
        if (_currentLineNumber > 0)
        {
            _currentLineNumber--;
            _currentStepNumber = _cutlines.Where(c => c.LineNumber == _currentLineNumber).First().StepNumber;
            return true;
        }
        else { return false; }  
    }

    public void RewindLines()
    {
        _currentLineNumber = -1;
        _currentStepNumber = 0;
    }
    public bool NextStep()
    {
        if (_currentStepNumber < _lastStepNum)
        {
            _currentStepNumber++;
            ChangeLineNumByStep();
            return true;
        }
        else
        {
            _currentStepNumber = 0;
            _currentLineNumber = -1;
            return false;
        }
    }
    public bool PrevStep()
    {
        if (_currentStepNumber > 0)
        {
            _currentStepNumber--;
            ChangeLineNumByStep();
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool SetStep(int num)
    {
        if (num >=0 || num <=_lastStepNum)
        {
            _currentStepNumber = num;
            ChangeLineNumByStep() ;
            return true;
        }
        else
        {
            return false;
        }
    }
    private void ChangeLineNumByStep() => 
        _currentLineNumber = _cutlines.Where(c=>c.StepNumber == _currentStepNumber).MinBy(c=>c.LineNumber)?.LineNumber ?? throw new KeyNotFoundException();
    public CutLine GetCurrentLine() => this[_currentLineNumber];
    /// <summary>
    /// Get nearest to the y step number
    /// </summary>
    /// <param name="y">coordinate</param>
    /// <returns>step number. -1 if not found</returns>
    public int GetNearestStepNumber(double y) => _cutlines.MinBy(c => c.Y - y)?.StepNumber ?? -1;
    public CutLine this[int lineNum]
    {
        get
        {
            var line = _cutlines[lineNum];
            var y = line.Y + _yOffset;
            return line with { Y = y };
        }        
    }
    public CutLines(List<CutLine> cutlines, double materialThickness, int linesCount, int stepCount)
    {
        _cutlines = cutlines;
        MaterialThickness = materialThickness;
        _lastStepNum = cutlines.Max(c => c.LineNumber);
        LinesCount = linesCount;
        StepCount = stepCount;
    }
    public void AddYOffset(double offset) => _yOffset += offset;
    public IEnumerator<CutLine> GetEnumerator()
    {
        foreach (var item in _cutlines)
        {
            var y = item.Y + _yOffset;
            yield return item with {  Y = y };
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal class CutLinesFactory
{
    public CutLines GetCutLines(CutSet cutSet, double yFirst, double xCenter, double zBase, Blade blade)
    {
        var cutLines = new List<CutLine>();
        var currentY = yFirst;
        var bodyThickness = cutSet.Thickness - cutSet.UnderCut;
        var delta = blade.XGap(bodyThickness);
        var lineNumber = 0;
        var stepCount = 0;
        foreach (var step in cutSet.CuttingSteps)
        {
            for (var i = 0; i < step.Count; i++)
            {
                if(step.StepNumber!=0) currentY += step.Index;
                var currentShare = 0;
                foreach (var pass in step.Passes)
                {
                    currentShare += pass.DepthShare;
                    var z = zBase - cutSet.Thickness + bodyThickness * pass.DepthShare / 100;
                    var line = new CutLine(lineNumber, step.StepNumber, pass.FeedSpeed, pass.RPM, currentY, xCenter - step.Length / 2 + delta, xCenter + step.Length / 2, z);
                    cutLines.Add(line);
                    lineNumber++;
                }
                stepCount++;
            }
        }
        return new CutLines(cutLines, cutSet.Thickness, lineNumber + 1, stepCount);
    }
}

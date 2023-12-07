﻿using DicingBlade.Classes.WaferGrid;
using DicingBlade.UserControls;
using DicingBlade.Utility;
using HandyControl.Controls;
using MachineControlsLibrary.Classes;
using MachineControlsLibrary.Controls;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Xceed.Wpf.AvalonDock.Controls;

namespace DicingBlade.Views.CutsProcessViews;
/// <summary>
/// Interaction logic for PassItemView.xaml
/// </summary>
[INotifyPropertyChanged]
public partial class PassItemView : UserControl
{
    public PassItemView()
    {
        InitializeComponent();
        mainCanvas.DataContext = this;
        //Shares = new();
        //Shares.Add(new(10, 10));
        //Shares.Add(new(20, 30));
        //Shares.Add(new(30, 60));

    }

    public ObservableCollection<Pass> Passes
    {
        get => (ObservableCollection<Pass>)GetValue(PassesProperty);
        set => SetValue(PassesProperty, value);
    }


    public static readonly DependencyProperty PassesProperty =
        DependencyProperty.Register("Passes", typeof(ObservableCollection<Pass>), typeof(PassItemView),
            new PropertyMetadata(null, new PropertyChangedCallback(PassesChanged)));

    private static void PassesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PassItemView view)
        {
            var passes = view.Passes;

            var shares = passes.Select(p => new Share(p.DepthShare, p.DepthShare));
            var firstShare = shares.First();
            var seed = new List<Share>();
            seed.Add(firstShare);
            var accShares = shares.Skip(1).Aggregate(seed, (acc, cur) =>
            {
                var last = acc.Last();
                var result = new Share(cur.Part, last.Total + cur.Part);
                acc.Add(result);
                return acc;
            });    
            view.Shares = new(accShares);
            view.LinkLines = new(Enumerable.Repeat(new LinkLine(), view.Shares.Count));
        }
    }

    private void RefreshNumerics()
    {
        var nmrcs = numerics.FindVisualChildren<ShareNumeric>().ToList();
        for (int i = 0; i < Shares.Count; i++)
        {
            SetLinkLine(i);
        }
    }

    public ObservableCollection<Share> Shares { get; set; }
    public ObservableCollection<LinkLine> LinkLines { get; set; }

    void onDragDelta(object sender, DragDeltaEventArgs e)
    {
        var thumb = sender as Thumb;
        var y = Canvas.GetTop(thumb) + e.VerticalChange;
        if (y > 0 & y < SubstrateSection.ActualHeight - 1)
        {
            var ptIndex = pointers.FindVisualChildren<Thumb>().FindIndex(th => th.Equals(thumb));
            RecountShares(y, ptIndex);
            SetLinkLine(ptIndex);
            Share.IsChanged = true;
        }
    }

    private void SetLinkLine(int index)
    {
        LinkLines[index] = new()
        {
            X1 = SubstrateSection.ActualWidth,
            Y1 = SubstrateSection.ActualHeight * Shares[index].Total / 100,
            X2 = Canvas.GetLeft(numerics),
            Y2 = Canvas.GetTop(numerics) + (2 * index + 1) * numerics.ActualHeight / (2 * Shares.Count)
        };
    }

    private void RecountShares(double y, int index)
    {
        var indexSummary = (int)Math.Round(y * 100 / SubstrateSection.ActualHeight);
        if (index == 0)
        {
            if (Shares.Count > index + 1 & Shares[index + 1].Total - indexSummary <= 5) return;
            Shares[index] = new(indexSummary, indexSummary);
        }
        else if (index > 0)
        {            
            var summary = Shares[index - 1].Total;
            if (indexSummary - summary <= 5) return;
            if (Shares.Count > index + 1 && Shares[index + 1].Total - indexSummary <= 5) return;
            Shares[index] = new(indexSummary - summary, indexSummary);
        }
        if (Shares.Count > index + 1)
        {
            var summary = Shares[index + 1].Total;
            Shares[index + 1] = new(summary - Shares[index].Total, summary);
        }
    }


    public class LinkLine
    {
        public LinkLine()
        {

        }
        public LinkLine(double x1, double y1, double x2, double y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }
        public double Y1 { get; set; }
        public double Y2 { get; set; }
        public double X1 { get; set; }
        public double X2 { get; set; }
    }




    private void numerics_Loaded(object sender, System.Windows.RoutedEventArgs e) => RefreshNumerics();

    private void NumericUpDown_ValueChanged(object sender, HandyControl.Data.FunctionEventArgs<double> e)
    {
        //if (Share.IsChanged)
        //{
        //    Share.IsChanged = false;
        //    return;
        //}
        //Shares[0] = new(Shares[0].Part, Shares[0].Part);
        //if (Shares.Count > 1)
        //    for (var i = 1; i < Shares.Count; i++)
        //    {
        //        Shares[i] = new(Shares[i].Part, Shares[i].Part + Shares[i - 1].Total);
        //    }
    }
}


public partial class Share
{
    public Share(int part, int total)
    {
        Part = part;
        Total = total;
    }
    public int Part { get; set; }
    public int Total { get; set; }
    public static bool IsChanged { get; set; }
}

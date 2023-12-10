using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DicingBlade.UserControls;
using DicingBlade.Utility;
using Microsoft.Toolkit.Mvvm.ComponentModel;
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
    }


    private void RefreshNumerics()
    {
        var nmrcs = numerics.FindVisualChildren<ShareNumeric>().ToList();
        for (int i = 0; i < Shares.Count; i++)
        {
            SetLinkLine(i);
        }
    }

    public int TotalDepth { get; set; } = 99;

    public ObservableCollection<Share> Shares
    {
        get
        {
            return (ObservableCollection<Share>)GetValue(SharesProperty);
        }
        set
        {
            SetValue(SharesProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for Shares.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SharesProperty =
        DependencyProperty.Register("Shares", typeof(ObservableCollection<Share>), typeof(PassItemView),
            new PropertyMetadata(new ObservableCollection<Share>(), new PropertyChangedCallback(OnSharesChanged)));

    private static void OnSharesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PassItemView view)
        {
            view.LinkLines = new(Enumerable.Repeat(new LinkLine(), view.Shares.Count));
        }
    }

    public ObservableCollection<LinkLine> LinkLines
    {
        get; set;
    }

    void onDragDelta(object sender, DragDeltaEventArgs e)
    {
        var thumb = sender as Thumb;
        var y = Canvas.GetTop(thumb) + e.VerticalChange;
        if (y > 0 & y < SubstrateSection.ActualHeight)
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
            X1 = SubstrateSection.ActualWidth + 10,
            Y1 = SubstrateSection.ActualHeight * Shares[index].Total / 100,
            X2 = Canvas.GetLeft(numerics),
            Y2 = Canvas.GetTop(numerics) + (2 * index + 1) * numerics.ActualHeight / (2 * Shares.Count)
        };
        TotalDepth = Shares.Last().Total;
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
        public double Y1
        {
            get; set;
        }
        public double Y2
        {
            get; set;
        }
        public double X1
        {
            get; set;
        }
        public double X2
        {
            get; set;
        }
    }




    private void numerics_Loaded(object sender, System.Windows.RoutedEventArgs e) => RefreshNumerics();

    private void NumericUpDown_ValueChanged(object sender, HandyControl.Data.FunctionEventArgs<double> e)
    {
    }
}

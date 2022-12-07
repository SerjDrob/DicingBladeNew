using System;
using System.Windows;
using DicingBlade.ViewModels;
using HandyControl.Controls;

namespace DicingBlade.Views
{
    /// <summary>
    /// Логика взаимодействия для MainWindowView.xaml
    /// </summary>
    public partial class MainWindowView : GlowWindow
    {
        public MainWindowView(IMainViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Environment.Exit(0);
        }

    }
}

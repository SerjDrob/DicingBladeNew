using System;
using DicingBlade.ViewModels;
using HandyControl.Controls;
using Microsoft.Extensions.Logging;

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
            var context = DataContext as IMainViewModel;
            context?.LogMessage(LogLevel.Information, "Application closed");
            base.OnClosed(e);
            Environment.Exit(0);
        }

    }
}

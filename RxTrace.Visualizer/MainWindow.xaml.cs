using System.Windows;
using ReactiveUI;

namespace RxTrace.Visualizer;

public partial class MainWindow : Window, IViewFor<ViewModels.MainViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = value as ViewModels.MainViewModel;
    }

    public ViewModels.MainViewModel? ViewModel { get; set; }
}
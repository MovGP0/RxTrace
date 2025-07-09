using ReactiveUI;
using SourceChord.FluentWPF;

namespace RxTrace.Visualizer;

public partial class MainWindow : AcrylicWindow, IViewFor<ViewModels.MainViewModel>
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
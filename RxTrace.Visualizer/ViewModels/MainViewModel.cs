using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using Microsoft.Msagl.Drawing;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RxTrace.Visualizer.Behaviors;
using Splat;

namespace RxTrace.Visualizer.ViewModels;

public sealed partial class MainViewModel: ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public void Dispose() => _disposables.Dispose();

    public MainViewModel()
    {
        foreach (var behavior in Locator.Current.GetServices<IBehavior<MainViewModel>>())
        {
            behavior.Activate(this, _disposables);
        }
    }

    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ObservableCollection<EdgeViewModel> Edges { get; } = new();

    [Reactive]
    private string _url = "http://localhost:5000/rxtrace";

    [Reactive]
    private ReactiveCommand<Unit, Unit> _start = DisabledCommand.Instance;

    [Reactive]
    private ReactiveCommand<Unit, Unit> _stop = DisabledCommand.Instance;

    [Reactive]
    private ReactiveCommand<Unit, Unit> _clear = DisabledCommand.Instance;

    [Reactive]
    private Graph _graph = new();
}
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using Microsoft.Msagl.Drawing;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RxTrace.Visualizer.Behaviors;
using DynamicData;
using System.Reactive.Linq;
using Splat;

namespace RxTrace.Visualizer.ViewModels;

public sealed partial class MainViewModel: ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public void Dispose() => _disposables.Dispose();

    public MainViewModel(): this(
        Locator.Current.GetServices<IBehavior<MainViewModel>>())
    {
    }

    private MainViewModel(IEnumerable<IBehavior<MainViewModel>> behaviors)
    {
        NodesCache
            .Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out var nodesReadOnly)
            .Subscribe()
            .DisposeWith(_disposables);
        Nodes = nodesReadOnly;

        EdgesCache
            .Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out var edgesReadOnly)
            .Subscribe()
            .DisposeWith(_disposables);
        Edges = edgesReadOnly;

        foreach (var behavior in behaviors)
        {
            behavior.Activate(this, _disposables);
        }
    }

    public SourceCache<NodeViewModel, string> NodesCache { get; }
        = new(n => n.Id);

    public SourceCache<EdgeViewModel, (string SourceId, string TargetId)> EdgesCache { get; }
        = new(e => (e.SourceId, e.TargetId));

    public ReadOnlyObservableCollection<NodeViewModel> Nodes { get; }
    public ReadOnlyObservableCollection<EdgeViewModel> Edges { get; }

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
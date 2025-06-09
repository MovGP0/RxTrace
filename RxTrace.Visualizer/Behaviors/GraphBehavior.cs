using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveMarbles.ObservableEvents;

namespace RxTrace.Visualizer.Behaviors;

public sealed class GraphBehavior(TimeProvider timeProvider) : IBehavior<ViewModels.MainViewModel>
{
    private TimeProvider TimeProvider { get; } = timeProvider;

    public void Activate(ViewModels.MainViewModel vm, CompositeDisposable disposables)
    {
        // Watch both collections
        var nodesChanged = vm.Nodes.Events().CollectionChanged;
        var edgesChanged = vm.Edges.Events().CollectionChanged;

        nodesChanged.Merge(edgesChanged)
            .Select(_ => Unit.Default)
            .StartWith(Unit.Default)
            .Subscribe(_ => RebuildGraph())
            .DisposeWith(disposables);
        return;

        void RebuildGraph()
        {
            var utcNow = TimeProvider.GetUtcNow();
            var graph = new Microsoft.Msagl.Drawing.Graph();

            // add nodes
            foreach (var n in vm.Nodes)
            {
                graph.AddNode(n.Id).LabelText = n.Id;
            }

            // add edges
            foreach (var e in vm.Edges)
            {
                var edge = graph.AddEdge(e.SourceId, e.TargetId);
                var wpf = ColorFromAge(utcNow, e.LastTriggered);
                edge.Attr.Color = new(wpf.R, wpf.G, wpf.B);
            }

            vm.Graph = graph;
        }

        System.Windows.Media.Color ColorFromAge(DateTimeOffset utcNow, DateTimeOffset timestamp)
        {
            var ageSec = (utcNow - timestamp).TotalSeconds;
            byte intensity = (byte)Math.Max(0, 255 - ageSec * 5);
            return System.Windows.Media.Color.FromRgb(intensity, 0, (byte)(255 - intensity));
        }
    }
}

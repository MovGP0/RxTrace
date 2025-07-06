using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Msagl.Drawing;
using ReactiveMarbles.ObservableEvents;
using RxTrace.Visualizer.ViewModels;

namespace RxTrace.Visualizer.Behaviors;

public sealed class GraphBehavior(TimeProvider timeProvider) : IBehavior<MainViewModel>
{
    private TimeProvider TimeProvider { get; } = timeProvider;

    public void Activate(MainViewModel vm, CompositeDisposable disposables)
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
            var graph = CloneGraph(vm);

            var existingIds = new HashSet<string>(graph.Nodes.Select(n => n.Id));

            var nodesToCreate = vm.Nodes
                .Where(n => !existingIds.Contains(n.Id));

            foreach (var n in nodesToCreate)
            {
                var node = graph.AddNode(n.Id);
                node.LabelText = n.Id;
            }

            // add or update edges
            var existingKeys = graph.Edges
                .Select(e => (e.Source, e.Target))
                .ToHashSet();

            var toAdd = vm.Edges
                .Where(e => !existingKeys.Contains((e.SourceId, e.TargetId)));

            foreach (var vmEdge in toAdd)
            {
                var c = ColorFromAge(utcNow, vmEdge.LastTriggered);
                var added = graph.AddEdge(vmEdge.SourceId, vmEdge.TargetId);
                added.Attr.Color = new(c.R, c.G, c.B);
            }

            var toUpdate = vm.Edges
                .Where(e => existingKeys.Contains((e.SourceId, e.TargetId)));

            foreach (var vmEdge in toUpdate)
            {
                var c = ColorFromAge(utcNow, vmEdge.LastTriggered);
                var existing = graph.Edges
                    .First(e => e.Source == vmEdge.SourceId && e.Target == vmEdge.TargetId);
                existing.Attr.Color = new(c.R, c.G, c.B);
            }

            vm.Graph = graph;
        }

        System.Windows.Media.Color ColorFromAge(DateTimeOffset utcNow, DateTimeOffset timestamp)
        {
            var ageSec = (utcNow - timestamp).TotalSeconds;

            float tau = 20.0f; // decay factor in [s]
            byte intensity = (byte)Math.Max(0, 255 - ageSec * tau);

            return System.Windows.Media.Color.FromRgb(intensity, 0, (byte)(255 - intensity));
        }
    }

    private static Graph CloneGraph(MainViewModel vm)
    {
        var existingGraph = vm.Graph;
        var graph = new Graph
        {
            Directed = existingGraph.Directed,
            Attr = existingGraph.Attr,
            Label = existingGraph.Label,
            UserData = existingGraph.UserData,
            GeometryObject = existingGraph.GeometryObject,
            RootSubgraph = existingGraph.RootSubgraph,
        };

        foreach (var node in existingGraph.Nodes)
        {
            graph.AddNode(node);
        }

        foreach (var edge in graph.Edges)
        {
            graph.AddPrecalculatedEdge(edge);
        }

        return graph;
    }
}

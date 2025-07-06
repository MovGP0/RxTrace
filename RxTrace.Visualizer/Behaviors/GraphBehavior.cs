using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Msagl.Drawing;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using RxTrace.Visualizer.ViewModels;

namespace RxTrace.Visualizer.Behaviors;

public sealed class GraphBehavior(TimeProvider timeProvider) : IBehavior<MainViewModel>
{
    private TimeProvider TimeProvider { get; } = timeProvider;

    public void Activate(MainViewModel vm, CompositeDisposable disposables)
    {
        var nodesChanged = vm.Nodes.Events().CollectionChanged;
        var edgesChanged = vm.Edges.Events().CollectionChanged;

        nodesChanged
            .Merge(edgesChanged)
            .Select(_ => Unit.Default)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .StartWith(Unit.Default)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => SyncGraph(vm))
            .DisposeWith(disposables);
    }

    private void SyncGraph(MainViewModel vm)
    {
        var utcNow = TimeProvider.GetUtcNow();
        var graph = CloneGraph(vm.Graph);  // update in-place

        var vmNodeIds = new HashSet<string>(vm.Nodes.Select(n => n.Id));
        var vmEdgeKeys = vm.Edges
            .Select(e => (e.SourceId, e.TargetId))
            .ToHashSet();

        var graphNodeIds = graph.Nodes.Select(n => n.Id).ToList();
        foreach (var id in graphNodeIds.Except(vmNodeIds))
        {
            var node = graph.Nodes.FirstOrDefault(n => n.Id == id);
            graph.RemoveNode(node);
        }

        foreach (var n in vm.Nodes.Where(n => !graphNodeIds.Contains(n.Id)))
        {
            var node = graph.AddNode(n.Id);
            node.LabelText = n.Id;
        }

        var graphEdgeKeys = graph.Edges
            .Select(e => (e.Source, e.Target))
            .ToList();

        foreach (var (source, target) in graphEdgeKeys.Except(vmEdgeKeys))
        {
            var edge = graph.Edges.FirstOrDefault(e => e.Source == source && e.Target == target);
            graph.RemoveEdge(edge);
        }

        var graphEdgeMap = graph.Edges
            .ToDictionary(e => (e.Source, e.Target));

        foreach (var vmEdge in vm.Edges)
        {
            var key = (vmEdge.SourceId, vmEdge.TargetId);
            var color = ColorFromAge(utcNow, vmEdge.LastTriggered);

            if (graphEdgeMap.TryGetValue(key, out var existingEdge))
            {
                // update
                existingEdge.Attr.Color = new(color.R, color.G, color.B);
            }
            else
            {
                // add
                var added = graph.AddEdge(vmEdge.SourceId, vmEdge.TargetId);
                added.Attr.Color = new(color.R, color.G, color.B);
            }
        }

        vm.Graph = graph;
    }

    private static System.Windows.Media.Color ColorFromAge(DateTimeOffset utcNow, DateTimeOffset timestamp)
    {
        var ageSec = (utcNow - timestamp).TotalSeconds;
        float decayPerSec = 20.0f;    // intensity decay
        byte intensity = (byte)Math.Max(0, 255 - ageSec * decayPerSec);
        return System.Windows.Media.Color.FromRgb(intensity, 0, (byte)(255 - intensity));
    }

    private static Graph CloneGraph(Graph graph)
    {
        var newGraph = new Graph
        {
            Directed = graph.Directed,
            Attr = graph.Attr,
            Label = graph.Label,
            UserData = graph.UserData,
            GeometryObject = graph.GeometryObject,
            RootSubgraph = graph.RootSubgraph,
        };

        foreach (var node in graph.Nodes)
        {
            newGraph.AddNode(node);
        }

        foreach (var edge in newGraph.Edges)
        {
            newGraph.AddPrecalculatedEdge(edge);
        }

        return newGraph;
    }
}

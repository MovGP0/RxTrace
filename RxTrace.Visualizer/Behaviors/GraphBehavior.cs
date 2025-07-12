using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Msagl.Drawing;
using ReactiveUI;
using RxTrace.Visualizer.ViewModels;

namespace RxTrace.Visualizer.Behaviors;

public sealed class GraphBehavior(
    TimeProvider timeProvider,
    ILogger<GraphBehavior> logger) : IBehavior<MainViewModel>
{
    public void Activate(MainViewModel vm, CompositeDisposable disposables)
    {
        var nodesChanged = vm.NodesCache.Connect().Select(_ => Unit.Default);
        var edgesChanged = vm.EdgesCache.Connect().Select(_ => Unit.Default);

        nodesChanged
            .Merge(edgesChanged)
            .SubscribeWithoutOverlap(
                () => SyncGraphAsync(vm),
                RxApp.MainThreadScheduler)
            .DisposeWith(disposables);
    }

    private Task SyncGraphAsync(MainViewModel vm)
    {
        var startTicks = 0L;
        if (logger.IsEnabled(LogLevel.Debug))
        {
            startTicks = Stopwatch.GetTimestamp();
        }

        var utcNow = timeProvider.GetUtcNow();

        var graph = CloneGraph(vm.Graph);

        // Snapshot ids
        var vmNodeIds = new HashSet<string>(vm.Nodes.Select(n => n.Id));
        var vmEdgeKeys = vm.Edges.Select(e => (e.SourceId, e.TargetId)).ToHashSet();

        // Remove old nodes (snapshot first)
        var nodesToRemove = graph.Nodes
            .Where(n => !vmNodeIds.Contains(n.Id))
            .ToList();

        foreach (var node in nodesToRemove)
        {
            graph.RemoveNode(node);
        }

        // Add missing nodes
        foreach (var id in vmNodeIds.Except(graph.Nodes.Select(n => n.Id)))
        {
            var node = graph.AddNode(id);
            node.LabelText = id;
        }

        // Remove old edges (snapshot first)
        var edgesToRemove = graph.Edges
            .Where(e => !vmEdgeKeys.Contains((e.Source, e.Target)))
            .ToList();

        foreach (var edge in edgesToRemove)
        {
            graph.RemoveEdge(edge);
        }

        // Update existing or add new edges
        var graphEdgeMap = graph.Edges
            .ToDictionary(e => (e.Source, e.Target));

        foreach (var vmEdge in vm.Edges)
        {
            var key = (vmEdge.SourceId, vmEdge.TargetId);
            var color = ColorFromAge(utcNow, vmEdge.LastTriggered);

            if (graphEdgeMap.TryGetValue(key, out var existing))
            {
                // update
                existing.Attr.Color = new(color.R, color.G, color.B);
            }
            else
            {
                // add
                var edge = graph.AddEdge(key.SourceId, key.TargetId);
                edge.Attr.Color = new(color.R, color.G, color.B);
            }
        }

        // One‐time assignment
        vm.Graph = graph;

        if (logger.IsEnabled(LogLevel.Debug))
        {
            TimeSpan duration = Stopwatch.GetElapsedTime(startTicks);
            logger.LogDebug($"Graph synced in {duration.TotalMilliseconds} ms");
        }

        return Task.CompletedTask;
    }

    private static System.Windows.Media.Color ColorFromAge(DateTimeOffset utcNow, DateTimeOffset timestamp)
    {
        var ageSec = (utcNow - timestamp).TotalSeconds;
        float decayPerSec = 20.0f; // intensity decay
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

        foreach (var edge in graph.Edges)
        {
            newGraph.AddPrecalculatedEdge(edge);
        }

        return newGraph;
    }
}

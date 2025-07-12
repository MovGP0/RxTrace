using System.Reactive.Disposables;
using DynamicData;
using RxTrace.Visualizer.Models;
using RxTrace.Visualizer.ViewModels;

namespace RxTrace.Visualizer.Behaviors;

public sealed class ProcessEventsBehavior(
    GlobalState globalState): IBehavior<MainViewModel>
{
    public void Activate(MainViewModel viewModel, CompositeDisposable disposables)
    {
        var reader = globalState.EventRecordChannel.Reader;
        var cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            var events = new List<RxEventRecord>();
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));

            try
            {
                while (await timer.WaitForNextTickAsync(cts.Token))
                {
                    while (reader.TryRead(out var rec))
                    {
                        events.Add(rec);
                    }

                    if (events.Count > 0)
                    {
                        ApplyBatch(viewModel, events);
                        events.Clear();
                    }
                }
            }
            catch (OperationCanceledException) { }
        }, cts.Token);

        disposables.Add(Disposable.Create(() => cts.Cancel()));
    }

    private static void ApplyBatch(MainViewModel vm, ICollection<RxEventRecord> events)
    {
        var sources = events.Select(r => r.Source)
            .Concat(events.Select(r => r.Target))
            .Distinct();

        var missingNodes = sources
            .Where(id => vm.Nodes.All(n => n.Id != id))
            .Select(id => new NodeViewModel(id));

        vm.NodesCache.AddOrUpdate(missingNodes);

        vm.EdgesCache.Edit(cache =>
        {
            var edges = vm.Edges.ToList();
            foreach (var record in events)
            {
                var match = edges.FirstOrDefault(e => 
                    e.SourceId == record.Source && e.TargetId == record.Target);

                if (match == null)
                {
                    cache.AddOrUpdate(new EdgeViewModel(record.Source, record.Target)
                    {
                        LastTriggered = record.Timestamp
                    });
                }
                else
                {
                    match.LastTriggered = record.Timestamp;
                }
            }
        });
    }
}
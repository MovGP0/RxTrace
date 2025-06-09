using System.Net.Http;
using System.Reactive.Disposables;
using System.Text.Json;
using DynamicData.Kernel;
using ReactiveUI;
using RxTrace.Visualizer.ViewModels;

namespace RxTrace.Visualizer.Behaviors;

public sealed class CommandsBehavior(TimeProvider timeProvider) : IBehavior<MainViewModel>
{
    private TimeProvider TimeProvider { get; } = timeProvider;

    private CancellationTokenSource CurrentCts { get; set; } = new();

    public void Activate(MainViewModel viewModel, CompositeDisposable disposables)
    {
        // Start
        viewModel.Start = ReactiveCommand.CreateFromTask(async () =>
            {
                CurrentCts.Dispose();

                var cts = new CancellationTokenSource();
                CurrentCts = cts;

                var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
                var req    = new HttpRequestMessage(HttpMethod.Get, viewModel.Url);
                var resp   = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);

                using var stream = await resp.Content.ReadAsStreamAsync(cts.Token);
                using var reader = new System.IO.StreamReader(stream);

                while (!reader.EndOfStream && !cts.Token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cts.Token);
                    if (line?.StartsWith("data:") == true)
                    {
                        var json = line["data:".Length..].Trim();
                        var ev = JsonSerializer.Deserialize<RxEventRecord>(json);
                        ProcessEventRecord(viewModel, ev);
                    }
                }
            },
            viewModel.WhenAnyValue(m => m.Url, url => !string.IsNullOrWhiteSpace(url)))
            .DisposeWith(disposables);

        // Stop
        viewModel.Stop = ReactiveCommand.Create(() =>
            {
                CurrentCts.Cancel();
                CurrentCts.Dispose();
            },
            viewModel.Start.IsExecuting)
        .DisposeWith(disposables);

        // Clear
        viewModel.Clear = ReactiveCommand.Create(() =>
            {
                viewModel.Edges.Clear();
                viewModel.Nodes.Clear();
            })
            .DisposeWith(disposables);

        Disposable.Create(() => viewModel.Start = DisabledCommand.Instance).DisposeWith(disposables);
        Disposable.Create(() => viewModel.Stop = DisabledCommand.Instance).DisposeWith(disposables);
        Disposable.Create(() => viewModel.Clear = DisabledCommand.Instance).DisposeWith(disposables);
    }

    private void ProcessEventRecord(MainViewModel viewModel, RxEventRecord eventRecord)
    {
        // if source does not exist as node, create it
        if (viewModel.Nodes.All(n => n.Id != eventRecord.Source))
        {
            viewModel.Nodes.Add(new(eventRecord.Source));
        }

        // if target does not exist as node, create it
        if (viewModel.Nodes.All(n => n.Id != eventRecord.Target))
        {
            viewModel.Nodes.Add(new(eventRecord.Target));
        }

        // if edge does not exist, create it
        var edge = viewModel.Edges.FirstOrOptional(IsMatch);

        // if edge exists, update the LastTriggered property, otherwise create a new edge
        if (!edge.HasValue)
        {
            var newEdge = new EdgeViewModel(eventRecord.Source, eventRecord.Target)
            {
                LastTriggered = TimeProvider.GetUtcNow()
            };

            viewModel.Edges.Add(newEdge);
        }
        else
        {
            edge.Value.LastTriggered = TimeProvider.GetUtcNow();
        }

        return;

        bool IsMatch(EdgeViewModel edgeViewModel)
            => edgeViewModel.SourceId == eventRecord.Source
            && edgeViewModel.TargetId == eventRecord.Target;
    }
}
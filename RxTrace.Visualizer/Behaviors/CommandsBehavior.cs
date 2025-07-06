using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using ReactiveUI;
using RxTrace.Visualizer.ViewModels;

namespace RxTrace.Visualizer.Behaviors;

public sealed class CommandsBehavior(
    TimeProvider timeProvider,
    IHttpClientFactory httpClientFactory) : IBehavior<MainViewModel>
{
    private TimeProvider TimeProvider { get; } = timeProvider;
    private IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;
    private CancellationTokenSource CurrentCts { get; set; } = new();

    public void Activate(MainViewModel viewModel, CompositeDisposable disposables)
    {
        BehaviorSubject<bool> isProcessingResponses = new(false);

        // Start
        var canStart = viewModel
            .WhenAnyValue(m => m.Url, url => !string.IsNullOrWhiteSpace(url)) // URL typed
            .CombineLatest(isProcessingResponses, (urlOk, running) => urlOk && !running) // not running
            .ObserveOn(RxApp.MainThreadScheduler);

        viewModel.Start = ReactiveCommand
            .CreateFromTask(StartAsync, canStart)
            .DisposeWith(disposables);

        viewModel.Start.IsExecuting
            .Subscribe(isProcessingResponses)
            .DisposeWith(disposables);

        viewModel.Start.ThrownExceptions
            .Subscribe(ex => Debug.WriteLine($"Start-command terminated: {ex}"))
            .DisposeWith(disposables);

        // Stop
        var canStop = isProcessingResponses
            .ObserveOn(RxApp.MainThreadScheduler);

        viewModel.Stop = ReactiveCommand
            .Create(Stop, canStop, RxApp.MainThreadScheduler)
            .DisposeWith(disposables);

        // Clear
        var canClear = Observable.Return(true)
            .ObserveOn(RxApp.MainThreadScheduler);

        viewModel.Clear = ReactiveCommand
            .Create(Clear, canClear, RxApp.MainThreadScheduler)
            .DisposeWith(disposables);

        Disposable.Create(() => viewModel.Start = DisabledCommand.Instance).DisposeWith(disposables);
        Disposable.Create(() => viewModel.Stop = DisabledCommand.Instance).DisposeWith(disposables);
        Disposable.Create(() => viewModel.Clear = DisabledCommand.Instance).DisposeWith(disposables);

        void Stop()
        {
            CurrentCts.Cancel();
            CurrentCts.Dispose();
            isProcessingResponses.OnNext(false);
        }

        void Clear()
        {
            viewModel.Edges.Clear();
            viewModel.Nodes.Clear();
        }

        Task StartAsync(CancellationToken ct)
        {
            // Schedule on task pool
            return Task.Run(() => ProcessResponsesAsync(viewModel, ct), ct);
        }
    }

    private async Task ProcessResponsesAsync(MainViewModel viewModel, CancellationToken cancellationToken)
    {
        CurrentCts.Dispose();
        CurrentCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ct = CurrentCts.Token;

        // Named client automatically carries the Polly retry handler
        var client = HttpClientFactory.CreateClient(HttpClientNames.EventStream);
        using var req = new HttpRequestMessage(HttpMethod.Get, viewModel.Url);

        var resp = await client.SendAsync(
            req,
            HttpCompletionOption.ResponseHeadersRead,
            ct); // retry policy respects this token

        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line?.StartsWith("data:") == true)
            {
                var json = line["data:".Length..].Trim();
                var eventRecord = JsonSerializer.Deserialize<RxEventRecord>(json);
                if (eventRecord is null)
                {
                    continue;
                }

                RxApp.MainThreadScheduler.Schedule(eventRecord, (_, record) => viewModel.ProcessEventRecord(record, TimeProvider));
            }
        }
    }
}
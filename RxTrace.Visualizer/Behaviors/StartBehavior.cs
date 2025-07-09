using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using ReactiveUI;
using RxTrace.Visualizer.Models;
using RxTrace.Visualizer.ViewModels;

namespace RxTrace.Visualizer.Behaviors;

/// <summary>
/// Installs the *Start* command and orchestrates the HTTP event‑stream.
/// </summary>
public sealed class StartBehavior(
    TimeProvider timeProvider,
    IHttpClientFactory httpClientFactory,
    CommandState state) : IBehavior<MainViewModel>
{
    public void Activate(MainViewModel viewModel, CompositeDisposable d)
    {
        // Can execute when the URL textbox contains text *and* we are not already running.
        var canStart = viewModel
            .WhenAnyValue(m => m.Url, url => !string.IsNullOrWhiteSpace(url))
            .CombineLatest(state.IsProcessingResponses, (urlOk, running) => urlOk && !running)
            .ObserveOn(RxApp.MainThreadScheduler);

        viewModel.Start = ReactiveCommand
            .CreateFromTask(ExecuteStartAsync, canStart)
            .DisposeWith(d);

        // Propagate IsExecuting into the shared state so Stop() can observe it.
        viewModel.Start.IsExecuting
            .Subscribe(state.IsProcessingResponses)
            .DisposeWith(d);

        viewModel.Start.ThrownExceptions
            .Subscribe(ex => Debug.WriteLine($"Start‑command terminated: {ex}"))
            .DisposeWith(d);

        Disposable.Create(() => viewModel.Start = DisabledCommand.Instance).DisposeWith(d);
        return;

        Task ExecuteStartAsync(CancellationToken ct)
        {
            // Execute the long‑running work on the thread‑pool to avoid blocking the UI scheduler.
            return Task.Run(() => ProcessResponsesAsync(viewModel, ct), ct);
        }
    }

    private async Task ProcessResponsesAsync(MainViewModel viewModel, CancellationToken external)
    {
        var ct = state.RefreshToken(external);

        // Named client automatically carries the Polly retry handler.
        var client = httpClientFactory.CreateClient(HttpClientNames.EventStream);

        using var req = new HttpRequestMessage(HttpMethod.Get, viewModel.Url);
        var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line?.StartsWith("data:") == true)
            {
                var json = line["data:".Length..].Trim();
                var record = JsonSerializer.Deserialize<RxEventRecord>(json);
                if (record is null) continue;

                RxApp.MainThreadScheduler.Schedule(record, (_, r) => viewModel.ProcessEventRecord(r, timeProvider));
            }
        }
    }
}

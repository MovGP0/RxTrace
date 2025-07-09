using System.Reactive.Subjects;
using ReactiveUI;

namespace RxTrace.Visualizer.Models;

/// <summary>
/// Shared mutable state for the command‑related behaviors.
/// </summary>
public sealed class CommandState : IDisposable
{
    private CancellationTokenSource _currentCts = new();

    /// <summary>
    /// Indicates whether we are currently processing the event‑stream.  Observed by the Stop‑command and
    /// updated by the Start‑command via <see cref="ReactiveCommand.IsExecuting"/>.
    /// </summary>
    public BehaviorSubject<bool> IsProcessingResponses { get; } = new(false);

    /// <summary>
    /// Obtains a fresh <see cref="CancellationToken"/> linked to the caller supplied token – replacing the
    /// previous CTS in the process.
    /// </summary>
    public CancellationToken RefreshToken(CancellationToken external)
    {
        // Replace the CTS atomically and link it to the external token supplied by ReactiveCommand.
        var linked = CancellationTokenSource.CreateLinkedTokenSource(external);
        var old = Interlocked.Exchange(ref _currentCts, linked);
        old.Dispose();
        return linked.Token;
    }

    /// <summary>
    /// Cancels the running operation (if any) and resets the execution flag.
    /// </summary>
    public void Cancel()
    {
        _currentCts.Cancel();
        // A cancelled CTS cannot be reused – create a fresh one so the next Start() doesn’t throw.
        var old = Interlocked.Exchange(ref _currentCts, new());
        old.Dispose();
        IsProcessingResponses.OnNext(false);
    }

    public void Dispose()
    {
        _currentCts.Dispose();
        IsProcessingResponses.Dispose();
    }
}
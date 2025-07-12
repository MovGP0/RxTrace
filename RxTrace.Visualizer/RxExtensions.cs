using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Threading;

namespace RxTrace.Visualizer;

public static class RxExtensions
{
    /// <summary>
    /// Subscribes to the source, ensuring only one invocation of the asyncAction
    /// runs at a time. While work is in-flight, new events are coalesced down
    /// to at most one pending invocation. After the action completes and
    /// yields, the next pending signal (if any) is processed.
    /// </summary>
    public static IDisposable SubscribeWithoutOverlap<T>(
        this IObservable<T> source,
        Func<Task> asyncAction,
        IScheduler? scheduler = null)
    {
        scheduler ??= Scheduler.Default;

        var replayed = source.Replay(1);

        var gate = new Subject<Unit>();

        var subscription = gate
            .Select(_ => replayed.Take(1))
            .Switch()
            .ObserveOn(scheduler)
            .SelectMany(async _ =>
            {
                await asyncAction().ConfigureAwait(true);
                await Dispatcher.Yield(DispatcherPriority.Background);
                gate.OnNext(Unit.Default);
                return Unit.Default;
            })
            .Subscribe(
                _ => { },
                ex => Console.Error.WriteLine($"Error in SubscribeWithoutOverlap: {ex}")
            );

        var connection = replayed.Connect();

        gate.OnNext(Unit.Default);

        return new CompositeDisposable(connection, subscription, gate);
    }
}
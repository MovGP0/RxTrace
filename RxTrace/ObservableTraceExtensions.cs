using OneOf;
using Splat;
using System.Reactive.Linq;

namespace RxTrace;

public static class ObservableTraceExtensions
{
    /// <summary>
    /// Trace each item of an observable sequence by publishing an EventRecord.
    /// </summary>
    public static IObservable<T> Trace<T>(
        this IObservable<T> observable,
        OneOf<string, Type, Func<T>> source,
        OneOf<string, Type, Func<T>> target)
    {
        if (!RxTraceOptions._IsEnabled)
        {
            return observable;
        }

        var tracer = Locator.Current.GetService<RxEventTracer>()
                     ?? throw new InvalidOperationException("EventTracer not registered");

        return observable.Do(item =>
        {
            var srcName = ToName(source);
            var tgtName = ToName(target);
            tracer.Trace(srcName, tgtName, item);
        });
    }

    private static string ToName<T>(OneOf<string, Type, Func<T>> target)
    {
        return target.Match(
            str => str,
            typ => typ.Name,
            func =>
            {
                var val = func();
                return val?.GetType().Name ?? string.Empty;
            });
    }
}

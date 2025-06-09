using System.Globalization;
using MessagePipe;

namespace RxTrace;

public sealed class RxEventTracer(
    IAsyncPublisher<RxEventRecord> events,
    TimeProvider dateTimeProvider) : IRxEventTracer
{
    public void Trace(
        string source,
        string target,
        object? payload = null)
    {
        if (!RxTraceOptions._IsEnabled) return;

        var now = dateTimeProvider.GetUtcNow();
        events.Publish(new(source, target, Format(payload), now));
    }

    public async Task TraceAsync(
        string source,
        string target,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        if (!RxTraceOptions._IsEnabled) return;

        var now = dateTimeProvider.GetUtcNow();
        await events.PublishAsync(new(source, target, Format(payload), now), cancellationToken);
    }

    private static string Format(object? payload)
    {
        if (payload is null)
            return string.Empty;

        if (payload is string str)
            return str;

        // TODO: if the object has debugger display string, use that

        if (payload is IFormattable formattable)
            return formattable.ToString(null, CultureInfo.InvariantCulture);

        return payload.ToString() ?? string.Empty;
    }
}
namespace RxTrace;

public interface IRxEventTracer
{
    void Trace(
        string source,
        string target,
        object? payload = null);

    Task TraceAsync(
        string source,
        string target,
        object? payload = null,
        CancellationToken cancellationToken = default);
}
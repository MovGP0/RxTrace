using System.Text.Json;
using MessagePipe;
using Microsoft.AspNetCore.Http;

namespace RxTrace;

public static class RxTraceMiddleware
{
    public static async Task InvokeAsync(
        HttpContext context,
        IAsyncSubscriber<RxEventRecord> tracer,
        CancellationToken requestAborted)
    {
        // SSE requires this header to be set on each response
        context.Response.Headers.Add("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");
        context.Response.Headers.Append("X-Accel-Buffering", "no"); // nginx
        context.Response.Headers.Append("Content-Encoding",  "identity"); // disable gzip
        await context.Response.Body.FlushAsync(requestAborted);

        // Subscribe to the tracer observable and push events as they come
        using var subscription = tracer
            .Subscribe(async (RxEventRecord ev, CancellationToken ct) =>
            {
                var json = JsonSerializer.Serialize(ev);
                await context.Response.WriteAsync($"data: {json}\n\n", context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
            });

        // Keep the connection open until client disconnects
        var disconnectTcs = new TaskCompletionSource<object?>();
        context.RequestAborted.Register(() => disconnectTcs.TrySetResult(null));
        await disconnectTcs.Task;
    }
}
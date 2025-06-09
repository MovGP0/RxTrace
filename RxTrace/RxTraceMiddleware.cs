using System.Text.Json;
using MessagePipe;
using Microsoft.AspNetCore.Http;

namespace RxTrace;

public sealed class RxTraceMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAsyncSubscriber<RxEventRecord> tracer)
    {
        if (!context.Request.Path.Equals("/rxtrace", StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        // SSE requires this header to be set on each response  
        context.Response.Headers.Add("Content-Type", "text/event-stream");

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
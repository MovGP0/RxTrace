using System.Net.Http;
using System.Net.Sockets;
using Polly;

namespace RxTrace.Visualizer;

public static class RetryPolicyFactory
{
    public static IAsyncPolicy<HttpResponseMessage> CreateHttpRetryPolicy() =>
        Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<SocketException>()
            .Or<TaskCanceledException>(ex => !ex.CancellationToken.IsCancellationRequested)
            .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Min(Math.Pow(2, retryAttempt), 30)));

}
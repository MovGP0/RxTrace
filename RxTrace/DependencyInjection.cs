using Microsoft.Extensions.DependencyInjection;

namespace RxTrace;

public static class DependencyInjection
{
    public static IServiceCollection AddRxTrace(this IServiceCollection services)
    {
        services.AddSingleton<IRxEventTracer, RxEventTracer>();
        services.AddOptions<RxTraceOptions>();
        return services;
    }
}
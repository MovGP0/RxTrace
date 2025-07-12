using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using RxTrace.Visualizer.Behaviors;
using RxTrace.Visualizer.Models;

namespace RxTrace.Visualizer;

public static class DependencyInjection
{
    public static IServiceCollection AddVisualizer(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddTransient<ViewModels.MainViewModel>();
        services.AddTransient<IViewFor<ViewModels.MainViewModel>, MainWindow>();
        services.AddTransient<IBehavior<ViewModels.MainViewModel>, GraphBehavior>();
        services.AddTransient<IBehavior<ViewModels.MainViewModel>, StartBehavior>();
        services.AddTransient<IBehavior<ViewModels.MainViewModel>, StopBehavior>();
        services.AddTransient<IBehavior<ViewModels.MainViewModel>, ClearBehavior>();
        services.AddTransient<IBehavior<ViewModels.MainViewModel>, ProcessEventsBehavior>();
        services.AddSingleton<GlobalState>();
        services.AddMessagePipe();

        services
            .AddHttpClient(HttpClientNames.EventStream)
            .AddPolicyHandler(RetryPolicyFactory.CreateHttpRetryPolicy());

        return services;
    }
}
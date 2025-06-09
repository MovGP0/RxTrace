using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using RxTrace.Visualizer.Behaviors;

namespace RxTrace.Visualizer;

public static class DependencyInjection
{
    public static IServiceCollection AddVisualizer(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ViewModels.MainViewModel>();
        services.AddSingleton<IViewFor<ViewModels.MainViewModel>, MainWindow>();
        services.AddSingleton<IBehavior<ViewModels.MainViewModel>, GraphBehavior>();
        services.AddSingleton<IBehavior<ViewModels.MainViewModel>, CommandsBehavior>();
        services.AddMessagePipe();
        return services;
    }
}
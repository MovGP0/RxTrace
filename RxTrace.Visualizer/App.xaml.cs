using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using RxTrace.Visualizer.ViewModels;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using static Microsoft.Extensions.Logging.LogLevel;

namespace RxTrace.Visualizer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        services.AddVisualizer();
        services.AddLogging(logger =>
        {
            logger.SetMinimumLevel(Debug);
            logger.AddConsole();
        });
        services.UseMicrosoftDependencyResolver();

        var mainWindow = Locator.Current.GetService<IViewFor<MainViewModel>>();
        if (mainWindow is Window window)
        {
            window.Show();
        }
    }
}

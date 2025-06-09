using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace RxTrace.Visualizer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // setup service location
        var services = new ServiceCollection();
        services.AddVisualizer();
        services.UseMicrosoftDependencyResolver(); 
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Clean up resources if necessary
        base.OnExit(e);
    }
}


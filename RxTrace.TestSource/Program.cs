using System.Reactive.Linq;
using RxTrace;
using Splat.Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Starting RxTrace.TestSource...");

// Build the Host + DI
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        // MessagePipe registrations (publisher/subscriber for RxEventRecord)
        services.AddMessagePipe();
        services.AddSingleton(TimeProvider.System);

        // Your tracer registration and options
        services.AddRxTrace();
        services.Configure<RxTraceOptions>(opts =>
            ctx.Configuration.GetSection("RxTrace").Bind(opts));

        // Splat: let MessagePipe & your tracer use Splat for locating services
        services.UseMicrosoftDependencyResolver(); 
    })
    .ConfigureWebHostDefaults(web =>
    {
        web.Configure(app =>
        {
            // 1. Wire up your custom SSE middleware
            app.UseRxTrace();
        })
        .UseUrls("http://*:5000");
    })
    .Build();

// Start the web server in the background
var webTask = host.RunAsync();

// Resolve your tracer (publisher) from the container
var tracer = host.Services.GetRequiredService<IRxEventTracer>();

// Create a simple IObservable sequence (one event per second)
var sourceStream = Observable
    .Interval(TimeSpan.FromSeconds(1))
    .Select(i => new RxEventRecord(
        Source: "TestSource",
        Target: "TestTarget",
        Payload: $"Message #{i}",
        Timestamp: DateTime.UtcNow
    ));

// Subscribe the tracer to that sequence
using (var _ = sourceStream.Subscribe(ev =>
       {
           tracer.Trace(ev.Source, ev.Target, ev.Payload);
           Console.WriteLine($"Traced: {ev.Payload}");
       }))
{
    Console.WriteLine("Press ENTER to stop sending events and shut down.");
    Console.ReadLine();
}

// shut down web server
await host.StopAsync();
await webTask;
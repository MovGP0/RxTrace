using System.Reactive.Linq;
using RxTrace;
using Splat.Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Starting RxTrace.TestSource...");

// Build the Host + DI
var builder = WebApplication.CreateSlimBuilder(args);

var services = builder.Services;

// MessagePipe registrations (publisher/subscriber for RxEventRecord)
services.AddMessagePipe();
services.AddSingleton(TimeProvider.System);

// Your tracer registration and options
services.AddRxTrace();
services.Configure<RxTraceOptions>(opts =>
    builder.Configuration.GetSection("RxTrace").Bind(opts));

// Splat: let MessagePipe & your tracer use Splat for locating services
services.UseMicrosoftDependencyResolver();

builder.WebHost.UseUrls("http://localhost:5000", "http://*:5000");

var app = builder.Build();

app.MapGet("/rxtrace", RxTraceMiddleware.InvokeAsync);
app.UseRouting();

// Start the web server in the background
var webTask = app.RunAsync();

// Resolve your tracer (publisher) from the container
var tracer = app.Services.GetRequiredService<IRxEventTracer>();

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
await app.StopAsync();
await webTask;
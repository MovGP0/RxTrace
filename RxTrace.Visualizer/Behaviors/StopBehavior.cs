using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using RxTrace.Visualizer.Models;
using RxTrace.Visualizer.ViewModels;

namespace RxTrace.Visualizer.Behaviors;

/// <summary>
/// Installs the "Stop" command that cancels the in‑flight HTTP request.
/// </summary>
public sealed class StopBehavior(CommandState state) : IBehavior<MainViewModel>
{
    public void Activate(MainViewModel vm, CompositeDisposable d)
    {
        var canStop = state.IsProcessingResponses.ObserveOn(RxApp.MainThreadScheduler);

        vm.Stop = ReactiveCommand
            .Create(state.Cancel, canStop, RxApp.MainThreadScheduler)
            .DisposeWith(d);

        Disposable.Create(() => vm.Stop = DisabledCommand.Instance).DisposeWith(d);
    }
}
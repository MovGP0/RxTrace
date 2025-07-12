using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using RxTrace.Visualizer.ViewModels;

namespace RxTrace.Visualizer.Behaviors;

/// <summary>
/// Installs the always‑enabled "Clear" command.
/// </summary>
public sealed class ClearBehavior : IBehavior<MainViewModel>
{
    public void Activate(MainViewModel vm, CompositeDisposable d)
    {
        var canClear = Observable.Return(true).ObserveOn(RxApp.MainThreadScheduler);

        vm.Clear = ReactiveCommand
            .Create(() =>
            {
                vm.EdgesCache.Clear();
                vm.NodesCache.Clear();
            }, canClear, RxApp.MainThreadScheduler)
            .DisposeWith(d);

        Disposable.Create(() => vm.Clear = DisabledCommand.Instance).DisposeWith(d);
    }
}
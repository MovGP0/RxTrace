using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;

namespace RxTrace.Visualizer;

public static class DisabledCommand
{
    public static ReactiveCommand<Unit, Unit> Instance { get; }
        = ReactiveCommand.Create(() => { }, Observable.Return(false));
}
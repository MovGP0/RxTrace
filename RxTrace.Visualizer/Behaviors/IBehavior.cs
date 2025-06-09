using System.Reactive.Disposables;

namespace RxTrace.Visualizer.Behaviors;

public interface IBehavior<in T>
{
    public void Activate(T viewModel, CompositeDisposable disposables);
}
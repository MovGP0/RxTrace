using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace RxTrace.Visualizer.ViewModels;

public sealed partial class NodeViewModel(string id) : ReactiveObject
{
    [Reactive]
    private string _id = id;
}
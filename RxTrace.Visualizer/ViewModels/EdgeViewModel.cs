using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace RxTrace.Visualizer.ViewModels;

public sealed partial class EdgeViewModel(string sourceId, string targetId) : ReactiveObject
{
    public string SourceId { get; } = sourceId;

    public string TargetId { get; } = targetId;

    [Reactive]
    private DateTimeOffset _lastTriggered = DateTimeOffset.MinValue;
}
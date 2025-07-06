using DynamicData.Kernel;

namespace RxTrace.Visualizer.ViewModels;

public static class MainViewModelExtensions
{
    public static Task ProcessEventRecord(
        this MainViewModel viewModel,
        RxEventRecord eventRecord,
        TimeProvider timeProvider)
    {
        // if source does not exist as node, create it
        if (viewModel.Nodes.All(n => n.Id != eventRecord.Source))
        {
            viewModel.Nodes.Add(new(eventRecord.Source));
        }

        // if target does not exist as node, create it
        if (viewModel.Nodes.All(n => n.Id != eventRecord.Target))
        {
            viewModel.Nodes.Add(new(eventRecord.Target));
        }

        // if edge does not exist, create it
        var edge = viewModel.Edges.FirstOrOptional(IsMatch);

        // if edge exists, update the LastTriggered property, otherwise create a new edge
        if (!edge.HasValue)
        {
            var newEdge = new EdgeViewModel(eventRecord.Source, eventRecord.Target)
            {
                LastTriggered = timeProvider.GetUtcNow()
            };

            viewModel.Edges.Add(newEdge);
        }
        else
        {
            edge.Value.LastTriggered = timeProvider.GetUtcNow();
        }

        return Task.CompletedTask;

        bool IsMatch(EdgeViewModel edgeViewModel)
            => edgeViewModel.SourceId == eventRecord.Source
               && edgeViewModel.TargetId == eventRecord.Target;
    }
}
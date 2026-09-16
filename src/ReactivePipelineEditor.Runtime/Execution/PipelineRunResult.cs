using ReactivePipelineEditor.Domain.Common;

namespace ReactivePipelineEditor.Runtime.Execution;

public sealed class PipelineRunResult
{
    public PipelineRunStatus Status { get; }
    public TimeSpan Duration { get; }
    public IReadOnlyDictionary<NodeId, NodeStats> NodeStats { get; }
    public Exception? Exception { get; }

    public PipelineRunResult(
        PipelineRunStatus status,
        TimeSpan duration,
        IReadOnlyDictionary<NodeId, NodeStats> nodeStats,
        Exception? exception = null)
    {
        Status = status;
        Duration = duration;
        NodeStats = nodeStats;
        Exception = exception;
    }
}

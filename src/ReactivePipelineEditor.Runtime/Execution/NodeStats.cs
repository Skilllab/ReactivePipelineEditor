using ReactivePipelineEditor.Domain.Common;

namespace ReactivePipelineEditor.Runtime.Execution;

public sealed class NodeStats
{
    public NodeId NodeId { get; }
    public string DisplayName { get; }
    public long ProcessedCount { get; private set; }
    public long ErrorCount { get; private set; }
    public TimeSpan? Duration { get; private set; }
    public Exception? Error { get; private set; }

    public NodeStats(NodeId nodeId, string displayName)
    {
        NodeId = nodeId;
        DisplayName = displayName;
    }

    public void MarkCompleted() => Duration = TimeSpan.Zero;
    public void MarkCancelled() => Duration = TimeSpan.Zero;
    public void MarkFailed(Exception ex) { Error = ex; ErrorCount++; }
    public void IncrementProcessed() => ProcessedCount++;
}

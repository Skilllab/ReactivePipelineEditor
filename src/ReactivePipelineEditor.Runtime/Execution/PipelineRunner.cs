using System.Threading.Channels;

using Microsoft.Extensions.Logging;

using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Nodes;
using ReactivePipelineEditor.Domain.Pipelines;
using ReactivePipelineEditor.Domain.Ports;
using ReactivePipelineEditor.Runtime.Metrics;
using ReactivePipelineEditor.Runtime.Retry;

namespace ReactivePipelineEditor.Runtime.Execution;

/// <summary>
/// Запускает ExecutionPlan: создаёт каналы между нодами, инстанцирует
/// executor'ы, ждёт завершения или отмены.
///
/// Ключевая идея: PipelineRunner — «дирижёр». Executor'ы — «музыканты».
/// Runner не знает, что делают ноды, — только как их запустить и связать.
/// </summary>
public sealed class PipelineRunner
{
    private readonly IServiceProvider _services;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMetricsCollector _metrics;
    private readonly RetryPolicy _retry;

    public PipelineRunner(
        IServiceProvider services,
        ILoggerFactory loggerFactory,
        IMetricsCollector metrics,
        RetryPolicy retry)
    {
        _services = services;
        _loggerFactory = loggerFactory;
        _metrics = metrics;
        _retry = retry;
    }

    /// <summary>
    /// Запускает план и возвращает результат после завершения или отмены.
    /// </summary>
    public async Task<PipelineRunResult> RunAsync(
        ExecutionPlan plan,
        RunOptions options,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var nodeTasks = new List<Task>();
        var nodeStats = new Dictionary<NodeId, NodeStats>();
        var channels = CreateChannels(plan, options.ChannelCapacity);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            foreach (var nodeId in plan.ExecutionOrder)
            {
                var node = plan.Nodes[nodeId];
                var executor = CreateExecutor(node);
                var context = CreateContext(node, plan, channels, nodeStats);

                var task = RunNodeAsync(node, executor, context, nodeStats, linkedCts.Token);
                nodeTasks.Add(task);
            }

            var allNodes = Task.WhenAll(nodeTasks);
            var completed = await Task.WhenAny(allNodes, Task.Delay(Timeout.Infinite, linkedCts.Token));

            if (completed != allNodes)
            {
                // Отмена: даём нодам DrainTimeout на завершение
                linkedCts.CancelAfter(options.DrainTimeout);
                try { await allNodes; } catch (OperationCanceledException) { }
            }
            else
            {
                await allNodes;
            }

            stopwatch.Stop();
            return new PipelineRunResult(
                linkedCts.IsCancellationRequested ? PipelineRunStatus.Cancelled : PipelineRunStatus.Completed,
                stopwatch.Elapsed,
                nodeStats);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            linkedCts.Cancel();
            return new PipelineRunResult(
                PipelineRunStatus.Failed,
                stopwatch.Elapsed,
                nodeStats,
                ex);
        }
    }

    private async Task RunNodeAsync(
        Node node,
        INodeExecutor executor,
        ExecutionContext context,
        Dictionary<NodeId, NodeStats> nodeStats,
        CancellationToken cancellationToken)
    {
        var logger = _loggerFactory.CreateLogger(node.GetType().Name);
        var stats = new NodeStats(node.Id, node.DisplayName);
        nodeStats[node.Id] = stats;

        using var timer = _metrics.StartTimer(
            "pipeline.node.duration_ms",
            new KeyValuePair<string, object?>("node_id", node.Id.ToString()),
            new KeyValuePair<string, object?>("node_type", node.GetType().Name));

        try
        {
            await executor.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            stats.MarkCompleted();
        }
        catch (OperationCanceledException)
        {
            stats.MarkCancelled();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Node {NodeId} failed", node.Id);
            stats.MarkFailed(ex);
            _metrics.IncrementCounter(
                "pipeline.node.errors",
                tags: new[] { new KeyValuePair<string, object?>("node_id", node.Id.ToString()) });
            throw;
        }
    }

    private INodeExecutor CreateExecutor(Node node)
    {
        return node switch
        {
            CsvSourceNode csv => new Executors.CsvSourceNodeExecutor(csv),
            FilterNode filter => new Executors.FilterNodeExecutor(filter),
            MapNode map => new Executors.MapNodeExecutor(map),
            ValidateNode validate => new Executors.ValidateNodeExecutor(validate),
            AggregateNode aggregate => new Executors.AggregateNodeExecutor(aggregate),
            JsonSinkNode jsonSink => new Executors.JsonSinkNodeExecutor(jsonSink),
            DeadLetterSinkNode deadLetter => new Executors.DeadLetterSinkNodeExecutor(deadLetter),
            _ => throw new NotSupportedException($"No executor for node type {node.GetType().Name}")
        };
    }

    private ExecutionContext CreateContext(
        Node node,
        ExecutionPlan plan,
        Dictionary<(NodeId, PortName), Channel<object>> channels,
        Dictionary<NodeId, NodeStats> nodeStats)
    {
        var inputs = new Dictionary<PortName, ChannelReader<object>>();
        var outputs = new Dictionary<PortName, ChannelWriter<object>>();

        foreach (var port in node.Inputs)
        {
            var incoming = plan.Connections.FirstOrDefault(c => c.To.NodeId == node.Id && c.To.PortName == port.Name);
            if (incoming is not null)
            {
                var key = (incoming.From.NodeId, incoming.From.PortName);
                if (channels.TryGetValue(key, out var channel))
                    inputs[port.Name] = channel.Reader;
            }
        }

        foreach (var port in node.Outputs)
        {
            var key = (node.Id, port.Name);
            if (channels.TryGetValue(key, out var channel))
                outputs[port.Name] = channel.Writer;
        }

        var logger = _loggerFactory.CreateLogger(node.GetType().Name);
        return new ExecutionContext(inputs, outputs, _metrics, logger, _retry);
    }

    private static Dictionary<(NodeId, PortName), Channel<object>> CreateChannels(
        ExecutionPlan plan,
        int capacity)
    {
        var channels = new Dictionary<(NodeId, PortName), Channel<object>>();

        foreach (var connection in plan.Connections)
        {
            var key = (connection.From.NodeId, connection.From.PortName);
            if (!channels.ContainsKey(key))
            {
                channels[key] = Channel.CreateBounded<object>(new BoundedChannelOptions(capacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = false
                });
            }
        }

        return channels;
    }


}

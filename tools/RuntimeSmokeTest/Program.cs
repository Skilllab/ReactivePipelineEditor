using Microsoft.Extensions.Logging;

using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Nodes;
using ReactivePipelineEditor.Domain.Pipelines;
using ReactivePipelineEditor.Domain.Ports;
using ReactivePipelineEditor.Runtime.Execution;
using ReactivePipelineEditor.Runtime.Metrics;
using ReactivePipelineEditor.Runtime.Retry;

// Подготовка данных
File.WriteAllText("input.csv", "Id,Amount,Category\n1,1500,A\n2,500,B\n3,2000,A\n4,800,C\n");

// Сборка pipeline
var pipeline = new Pipeline(PipelineId.New(), "smoke");
var csv = new CsvSourceNode(NodeId.New(), new Point2D(0, 0), "input.csv");
var filter = new FilterNode(NodeId.New(), new Point2D(200, 0), "Amount > 1000");
var map = new MapNode(NodeId.New(), new Point2D(400, 0), "new { Id, Amount }");
var aggregate = new AggregateNode(NodeId.New(), new Point2D(600, 0), "Id", "Sum(Amount)");
var sink = new JsonSinkNode(NodeId.New(), new Point2D(800, 0), "output.json");

pipeline.AddNode(csv);
pipeline.AddNode(filter);
pipeline.AddNode(map);
pipeline.AddNode(aggregate);
pipeline.AddNode(sink);

// Helper для проверки результата Connect
static void Connect(Pipeline p, PortRef from, PortRef to)
{
    var result = p.Connect(from, to);
    if (!result.IsSuccess)
        throw new InvalidOperationException(
            $"Failed to connect {from} → {to}: {result.Error!.Value.Message}");
}

Connect(pipeline, new PortRef(csv.Id, CsvSourceNode.OutPortName), new PortRef(filter.Id, FilterNode.InPortName));
Connect(pipeline, new PortRef(filter.Id, FilterNode.PassedPortName), new PortRef(map.Id, MapNode.InPortName));
Connect(pipeline, new PortRef(map.Id, MapNode.OutPortName), new PortRef(aggregate.Id, AggregateNode.InPortName));
Connect(pipeline, new PortRef(aggregate.Id, AggregateNode.OutPortName), new PortRef(sink.Id, JsonSinkNode.InPortName));

var planResult = pipeline.BuildPlan();
if (!planResult.IsSuccess)
{
    Console.WriteLine($"BuildPlan failed: {planResult.Error!.Value.Message}");
    return;
}

// Запуск
using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
using var metrics = new RuntimeMetrics();

var runner = new PipelineRunner(
    services: null!,
    loggerFactory: loggerFactory,
    metrics: metrics,
    retry: new RetryPolicy());

var result = await runner.RunAsync(
    planResult.Value,
    new RunOptions { ChannelCapacity = 10 },
    CancellationToken.None);

Console.WriteLine($"Status: {result.Status}");
Console.WriteLine($"Duration: {result.Duration}");
foreach (var (id, stats) in result.NodeStats)
    Console.WriteLine($"  {stats.DisplayName}: processed={stats.ProcessedCount}, errors={stats.ErrorCount}");

Console.WriteLine($"\nOutput file exists: {File.Exists("output.json")}");

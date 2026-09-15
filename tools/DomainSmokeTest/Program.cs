using System.Text;

using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Nodes;
using ReactivePipelineEditor.Domain.Pipelines;
using ReactivePipelineEditor.Domain.Ports;

Console.OutputEncoding = Encoding.UTF8;
var pipeline = new Pipeline(PipelineId.New(), "smoke test");

var csv = new CsvSourceNode(NodeId.New(), new Point2D(0, 0), "input.csv");
var filter = new FilterNode(NodeId.New(), new Point2D(200, 0), "Amount > 1000");
var map = new MapNode(NodeId.New(), new Point2D(400, 0), "new { Amount = decimal.Parse(Amount) }");
var aggregate = new AggregateNode(NodeId.New(), new Point2D(600, 0), "Country", "Sum(Amount)");
var sink = new JsonSinkNode(NodeId.New(), new Point2D(800, 0), "output.json");

pipeline.AddNode(csv);
pipeline.AddNode(filter);
pipeline.AddNode(map);
pipeline.AddNode(aggregate);
pipeline.AddNode(sink);

// RawRecord -> RawRecord
var r1 = pipeline.Connect(
    new PortRef(csv.Id, CsvSourceNode.OutPortName),
    new PortRef(filter.Id, FilterNode.InPortName));
Console.WriteLine($"Connect csv → filter: {r1.IsSuccess}");

// RawRecord -> RawRecord: Map is what turns a raw CSV row into a typed Record
var r2 = pipeline.Connect(
    new PortRef(filter.Id, FilterNode.PassedPortName),
    new PortRef(map.Id, MapNode.InPortName));
Console.WriteLine($"Connect filter → map: {r2.IsSuccess}");

// Record -> Record: Aggregate requires typed fields, so it must follow Map
var r3 = pipeline.Connect(
    new PortRef(map.Id, MapNode.OutPortName),
    new PortRef(aggregate.Id, AggregateNode.InPortName));
Console.WriteLine($"Connect map → aggregate: {r3.IsSuccess}");

// Aggregate -> Aggregate: the sink declares the most derived input type
var r4 = pipeline.Connect(
    new PortRef(aggregate.Id, AggregateNode.OutPortName),
    new PortRef(sink.Id, JsonSinkNode.InPortName));
Console.WriteLine($"Connect aggregate → sink: {r4.IsSuccess}");

var planResult = pipeline.BuildPlan();
if (planResult.IsSuccess)
{
    Console.WriteLine("Execution order:");
    foreach (var id in planResult.Value.ExecutionOrder)
        Console.WriteLine($"  {planResult.Value.Nodes[id].DisplayName}");
}
else
{
    Console.WriteLine($"Failed: {planResult.Error!.Value.Message}");
}


using System;
using System.Linq;

using FluentAssertions;

using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Nodes;
using ReactivePipelineEditor.Domain.Pipelines;
using ReactivePipelineEditor.Domain.Ports;

using Xunit;

namespace ReactivePipelineEditor.Domain.Tests;

public class BuildPlanTests
{
    private sealed class TestNode : Node
    {
        private readonly Port[] _inputs;
        private readonly Port[] _outputs;

        public TestNode(NodeId id, string displayName, Point2D position, Port[]? inputs = null, Port[]? outputs = null)
            : base(id, displayName, position)
        {
            _inputs = inputs ?? Array.Empty<Port>();
            _outputs = outputs ?? Array.Empty<Port>();
        }

        public override IReadOnlyList<Port> Inputs => _inputs;
        public override IReadOnlyList<Port> Outputs => _outputs;
    }

    [Fact]
    public void LinearGraph_A_B_C_ProducesOrder_A_B_C()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "linear");
        var a = new TestNode(NodeId.New(), "A", new Point2D(0, 0),
            outputs: new[] { new Port(PortName.From("o"), PortDirection.Output, DataType.Record) },
            inputs: Array.Empty<Port>());
        var b = new TestNode(NodeId.New(), "B", new Point2D(1, 0),
            inputs: new[] { new Port(PortName.From("i"), PortDirection.Input, DataType.RawRecord) },
            outputs: new[] { new Port(PortName.From("o"), PortDirection.Output, DataType.Record) });
        var c = new TestNode(NodeId.New(), "C", new Point2D(2, 0),
            inputs: new[] { new Port(PortName.From("i"), PortDirection.Input, DataType.RawRecord) });

        pipeline.AddNode(a).IsSuccess.Should().BeTrue();
        pipeline.AddNode(b).IsSuccess.Should().BeTrue();
        pipeline.AddNode(c).IsSuccess.Should().BeTrue();

        pipeline.Connect(new PortRef(a.Id, PortName.From("o")), new PortRef(b.Id, PortName.From("i"))).IsSuccess.Should().BeTrue();
        pipeline.Connect(new PortRef(b.Id, PortName.From("o")), new PortRef(c.Id, PortName.From("i"))).IsSuccess.Should().BeTrue();

        // Act
        var planResult = pipeline.BuildPlan();

        // Assert
        planResult.IsSuccess.Should().BeTrue();
        var order = planResult.Value.ExecutionOrder;
        order.Should().HaveCount(3);
        order[0].Should().Be(a.Id);
        order[1].Should().Be(b.Id);
        order[2].Should().Be(c.Id);
    }

    [Fact]
    public void DiamondGraph_A_B_C_D_ProducesA_First_D_Last_BandCBetween()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "diamond");
        var a = new TestNode(NodeId.New(), "A", new Point2D(0, 0),
            outputs: new[] { new Port(PortName.From("o"), PortDirection.Output, DataType.Record) });
        var b = new TestNode(NodeId.New(), "B", new Point2D(1, 1),
            inputs: new[] { new Port(PortName.From("i"), PortDirection.Input, DataType.RawRecord) },
            outputs: new[] { new Port(PortName.From("o"), PortDirection.Output, DataType.Record) });
        var c = new TestNode(NodeId.New(), "C", new Point2D(1, -1),
            inputs: new[] { new Port(PortName.From("i"), PortDirection.Input, DataType.RawRecord) },
            outputs: new[] { new Port(PortName.From("o"), PortDirection.Output, DataType.Record) });
        var d = new TestNode(NodeId.New(), "D", new Point2D(2, 0),
            inputs: new[] { new Port(PortName.From("i1"), PortDirection.Input, DataType.RawRecord),
                            new Port(PortName.From("i2"), PortDirection.Input, DataType.RawRecord) });

        pipeline.AddNode(a).IsSuccess.Should().BeTrue();
        pipeline.AddNode(b).IsSuccess.Should().BeTrue();
        pipeline.AddNode(c).IsSuccess.Should().BeTrue();
        pipeline.AddNode(d).IsSuccess.Should().BeTrue();

        pipeline.Connect(new PortRef(a.Id, PortName.From("o")), new PortRef(b.Id, PortName.From("i"))).IsSuccess.Should().BeTrue();
        pipeline.Connect(new PortRef(a.Id, PortName.From("o")), new PortRef(c.Id, PortName.From("i"))).IsSuccess.Should().BeTrue();
        pipeline.Connect(new PortRef(b.Id, PortName.From("o")), new PortRef(d.Id, PortName.From("i1"))).IsSuccess.Should().BeTrue();
        pipeline.Connect(new PortRef(c.Id, PortName.From("o")), new PortRef(d.Id, PortName.From("i2"))).IsSuccess.Should().BeTrue();

        // Act
        var planResult = pipeline.BuildPlan();

        // Assert
        planResult.IsSuccess.Should().BeTrue();
        var order = planResult.Value.ExecutionOrder.ToList();
        order.Should().Contain(new[] { a.Id, b.Id, c.Id, d.Id });
        order.First().Should().Be(a.Id);
        order.Last().Should().Be(d.Id);

        // B and C must be after A and before D
        var idxA = order.IndexOf(a.Id);
        var idxB = order.IndexOf(b.Id);
        var idxC = order.IndexOf(c.Id);
        var idxD = order.IndexOf(d.Id);

        idxA.Should().BeLessThan(idxB);
        idxA.Should().BeLessThan(idxC);
        idxB.Should().BeLessThan(idxD);
        idxC.Should().BeLessThan(idxD);
    }

    [Fact]
    public void Cycle_A_B_A_ProducesConflict()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "cycle");
        var a = new TestNode(NodeId.New(), "A", new Point2D(0, 0),
            outputs: new[] { new Port(PortName.From("o"), PortDirection.Output, DataType.Record) },
            inputs: new[] { new Port(PortName.From("i"), PortDirection.Input, DataType.RawRecord) });
        var b = new TestNode(NodeId.New(), "B", new Point2D(1, 0),
            inputs: new[] { new Port(PortName.From("i"), PortDirection.Input, DataType.RawRecord) },
            outputs: new[] { new Port(PortName.From("o"), PortDirection.Output, DataType.Record) });

        pipeline.AddNode(a).IsSuccess.Should().BeTrue();
        pipeline.AddNode(b).IsSuccess.Should().BeTrue();

        pipeline.Connect(new PortRef(a.Id, PortName.From("o")), new PortRef(b.Id, PortName.From("i"))).IsSuccess.Should().BeTrue();
        pipeline.Connect(new PortRef(b.Id, PortName.From("o")), new PortRef(a.Id, PortName.From("i"))).IsSuccess.Should().BeTrue();

        // Act
        var planResult = pipeline.BuildPlan();

        // Assert
        planResult.IsSuccess.Should().BeFalse();
        planResult.Error.Should().NotBeNull();
        planResult.Error!.Value.Code.Should().Be("conflict");
    }

    [Fact]
    public void ComplexDAG_MultipleBranches_ProducesValidTopologicalOrder()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "complex");
        // Build a small DAG with several branches
        var a = new TestNode(NodeId.New(), "A", new Point2D(0, 0),
            outputs: new[] { new Port(PortName.From("o"), PortDirection.Output, DataType.Record) });
        var b = new TestNode(NodeId.New(), "B", new Point2D(1, 0),
            inputs: new[] { new Port(PortName.From("i"), PortDirection.Input, DataType.RawRecord) },
            outputs: new[] { new Port(PortName.From("o"), PortDirection.Output, DataType.Record) });
        var c = new TestNode(NodeId.New(), "C", new Point2D(1, 1),
            inputs: new[] { new Port(PortName.From("i"), PortDirection.Input, DataType.RawRecord) },
            outputs: new[] { new Port(PortName.From("o"), PortDirection.Output, DataType.Record) });
        var d = new TestNode(NodeId.New(), "D", new Point2D(2, 0),
            inputs: new[] { new Port(PortName.From("i1"), PortDirection.Input, DataType.RawRecord),
                            new Port(PortName.From("i2"), PortDirection.Input, DataType.RawRecord) },
            outputs: new[] { new Port(PortName.From("o"), PortDirection.Output, DataType.Record) });
        var e = new TestNode(NodeId.New(), "E", new Point2D(3, 0),
            inputs: new[] { new Port(PortName.From("i"), PortDirection.Input, DataType.RawRecord) });

        pipeline.AddNode(a).IsSuccess.Should().BeTrue();
        pipeline.AddNode(b).IsSuccess.Should().BeTrue();
        pipeline.AddNode(c).IsSuccess.Should().BeTrue();
        pipeline.AddNode(d).IsSuccess.Should().BeTrue();
        pipeline.AddNode(e).IsSuccess.Should().BeTrue();

        pipeline.Connect(new PortRef(a.Id, PortName.From("o")), new PortRef(b.Id, PortName.From("i"))).IsSuccess.Should().BeTrue();
        pipeline.Connect(new PortRef(a.Id, PortName.From("o")), new PortRef(c.Id, PortName.From("i"))).IsSuccess.Should().BeTrue();
        pipeline.Connect(new PortRef(b.Id, PortName.From("o")), new PortRef(d.Id, PortName.From("i1"))).IsSuccess.Should().BeTrue();
        pipeline.Connect(new PortRef(c.Id, PortName.From("o")), new PortRef(d.Id, PortName.From("i2"))).IsSuccess.Should().BeTrue();
        pipeline.Connect(new PortRef(d.Id, PortName.From("o")), new PortRef(e.Id, PortName.From("i"))).IsSuccess.Should().BeTrue();

        // Act
        var planResult = pipeline.BuildPlan();

        // Assert
        planResult.IsSuccess.Should().BeTrue();
        var order = planResult.Value.ExecutionOrder.ToList();

        // Validate that for every connection index(from) < index(to)
        foreach (var conn in planResult.Value.Connections)
        {
            var idxFrom = order.IndexOf(conn.From.NodeId);
            var idxTo = order.IndexOf(conn.To.NodeId);
            idxFrom.Should().BeLessThan(idxTo);
        }
    }

    [Fact]
    public void EmptyPipeline_BuildPlan_ReturnsValidationError()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "empty");

        // Act
        var planResult = pipeline.BuildPlan();

        // Assert
        planResult.IsSuccess.Should().BeFalse();
        planResult.Error.Should().NotBeNull();
        planResult.Error!.Value.Code.Should().Be("validation");
    }

    [Fact]
    public void SingleSourceNode_BuildPlan_Succeeds()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "single");
        var n = new TestNode(NodeId.New(), "only", new Point2D(0, 0),
            outputs: new[] { new Port(PortName.From("o"), PortDirection.Output, DataType.Record) });
        pipeline.AddNode(n).IsSuccess.Should().BeTrue();

        // Act
        var planResult = pipeline.BuildPlan();

        // Assert
        planResult.IsSuccess.Should().BeTrue();
        planResult.Value.ExecutionOrder.Should().ContainSingle().Which.Should().Be(n.Id);
    }

    [Fact]
    public void DanglingInput_Port_Unconnected_FailsValidation()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "dangling");
        var source = new TestNode(NodeId.New(), "src", new Point2D(0, 0),
            outputs: new[] { new Port(PortName.From("o"), PortDirection.Output, DataType.Record) });
        var target = new TestNode(NodeId.New(), "t", new Point2D(1, 0),
            inputs: new[] { new Port(PortName.From("in"), PortDirection.Input, DataType.RawRecord) });

        pipeline.AddNode(source).IsSuccess.Should().BeTrue();
        pipeline.AddNode(target).IsSuccess.Should().BeTrue();

        // Note: we do NOT connect source -> target, so input is dangling

        // Act
        var planResult = pipeline.BuildPlan();

        // Assert
        planResult.IsSuccess.Should().BeFalse();
        planResult.Error.Should().NotBeNull();
        planResult.Error!.Value.Code.Should().Be("validation");
        planResult.Error!.Value.Message.Should().Contain("Unconnected inputs");
    }

    // Additional meaningful tests

    [Fact]
    public void TwoIndependentChains_ProducesValidOrderContainingAllNodes()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "two-chains");

        var a1 = new TestNode(NodeId.New(), "A1", new Point2D(0, 0),
            outputs: new[] { new Port(PortName.From("o"), PortDirection.Output, DataType.Record) });
        var b1 = new TestNode(NodeId.New(), "B1", new Point2D(1, 0),
            inputs: new[] { new Port(PortName.From("i"), PortDirection.Input, DataType.RawRecord) });

        var a2 = new TestNode(NodeId.New(), "A2", new Point2D(0, 1),
            outputs: new[] { new Port(PortName.From("o"), PortDirection.Output, DataType.Record) });
        var b2 = new TestNode(NodeId.New(), "B2", new Point2D(1, 1),
            inputs: new[] { new Port(PortName.From("i"), PortDirection.Input, DataType.RawRecord) });

        pipeline.AddNode(a1);
        pipeline.AddNode(b1);
        pipeline.AddNode(a2);
        pipeline.AddNode(b2);

        pipeline.Connect(new PortRef(a1.Id, PortName.From("o")), new PortRef(b1.Id, PortName.From("i"))).IsSuccess.Should().BeTrue();
        pipeline.Connect(new PortRef(a2.Id, PortName.From("o")), new PortRef(b2.Id, PortName.From("i"))).IsSuccess.Should().BeTrue();

        // Act
        var planResult = pipeline.BuildPlan();

        // Assert
        planResult.IsSuccess.Should().BeTrue();
        var order = planResult.Value.ExecutionOrder.ToList();
        order.Should().HaveCount(4);
        // Ensure each connection's from index < to index
        foreach (var conn in planResult.Value.Connections)
        {
            var idxFrom = order.IndexOf(conn.From.NodeId);
            var idxTo = order.IndexOf(conn.To.NodeId);
            idxFrom.Should().BeLessThan(idxTo);
        }
    }

    [Fact]
    public void BuildPlan_IncludesAllNodesAndConnections()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "include-check");
        var a = new TestNode(NodeId.New(), "A", new Point2D(0, 0),
            outputs: new[] { new Port(PortName.From("o"), PortDirection.Output, DataType.Record) });
        var b = new TestNode(NodeId.New(), "B", new Point2D(1, 0),
            inputs: new[] { new Port(PortName.From("i"), PortDirection.Input, DataType.RawRecord) });

        pipeline.AddNode(a).IsSuccess.Should().BeTrue();
        pipeline.AddNode(b).IsSuccess.Should().BeTrue();

        var connectRes = pipeline.Connect(new PortRef(a.Id, PortName.From("o")), new PortRef(b.Id, PortName.From("i")));
        connectRes.IsSuccess.Should().BeTrue();

        // Act
        var planResult = pipeline.BuildPlan();

        // Assert
        planResult.IsSuccess.Should().BeTrue();
        var plan = planResult.Value;
        plan.Nodes.Keys.Should().BeEquivalentTo(pipeline.Nodes.Select(n => n.Id));
        plan.Connections.Should().BeEquivalentTo(pipeline.Connections);
    }

    [Fact]
    public void BuildPlan_RepeatedCalls_ReturnSameOrder_WhenStructureUnchanged()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "idempotent");
        var a = new TestNode(NodeId.New(), "A", new Point2D(0, 0),
            outputs: new[] { new Port(PortName.From("o"), PortDirection.Output, DataType.Record) });
        var b = new TestNode(NodeId.New(), "B", new Point2D(1, 0),
            inputs: new[] { new Port(PortName.From("i"), PortDirection.Input, DataType.RawRecord) });

        pipeline.AddNode(a).IsSuccess.Should().BeTrue();
        pipeline.AddNode(b).IsSuccess.Should().BeTrue();
        pipeline.Connect(new PortRef(a.Id, PortName.From("o")), new PortRef(b.Id, PortName.From("i"))).IsSuccess.Should().BeTrue();

        // Act
        var plan1 = pipeline.BuildPlan();
        var plan2 = pipeline.BuildPlan();

        // Assert
        plan1.IsSuccess.Should().BeTrue();
        plan2.IsSuccess.Should().BeTrue();
        plan1.Value.ExecutionOrder.Should().Equal(plan2.Value.ExecutionOrder);
    }
}

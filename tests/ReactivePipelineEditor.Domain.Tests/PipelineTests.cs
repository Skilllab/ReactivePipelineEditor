using FluentAssertions;

using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Nodes;
using ReactivePipelineEditor.Domain.Pipelines;
using ReactivePipelineEditor.Domain.Ports;

namespace ReactivePipelineEditor.Domain.Tests;

public sealed class PipelineTests
{
    private readonly Pipeline _pipeline = new(PipelineId.New(), "test");

    [Fact]
    public void AddNode_succeeds_for_new_node()
    {
        var node = new CsvSourceNode(NodeId.New(), new Point2D(0, 0), "file.csv");
        var result = _pipeline.AddNode(node);

        result.IsSuccess.Should().BeTrue();
        _pipeline.Nodes.Should().ContainSingle();
    }

    [Fact]
    public void AddNode_fails_for_duplicate_id()
    {
        var id = NodeId.New();
        var node1 = new CsvSourceNode(id, new Point2D(0, 0), "file1.csv");
        var node2 = new CsvSourceNode(id, new Point2D(100, 0), "file2.csv");

        _pipeline.AddNode(node1);
        var result = _pipeline.AddNode(node2);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("conflict");
    }

    [Fact]
    public void RemoveNode_succeeds_for_existing()
    {
        var node = new CsvSourceNode(NodeId.New(), new Point2D(0, 0), "file.csv");
        _pipeline.AddNode(node);

        var result = _pipeline.RemoveNode(node.Id);

        result.IsSuccess.Should().BeTrue();
        _pipeline.Nodes.Should().BeEmpty();
    }

    [Fact]
    public void RemoveNode_fails_for_missing()
    {
        var result = _pipeline.RemoveNode(NodeId.New());
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("not_found");
    }

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
    public void Connect_Succeeds_For_CompatibleTypes()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "p");
        var source = new TestNode(NodeId.New(), "src", new Point2D(0, 0),
            outputs: new[] { new Port(PortName.From("out"), PortDirection.Output, DataType.Record) });
        var target = new TestNode(NodeId.New(), "dst", new Point2D(1, 1),
            inputs: new[] { new Port(PortName.From("in"), PortDirection.Input, DataType.RawRecord) });

        pipeline.AddNode(source).IsSuccess.Should().BeTrue();
        pipeline.AddNode(target).IsSuccess.Should().BeTrue();

        var from = new PortRef(source.Id, PortName.From("out"));
        var to = new PortRef(target.Id, PortName.From("in"));

        // Act
        var result = pipeline.Connect(from, to);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var connectionId = result.Value;
        pipeline.Connections.Should().ContainSingle(c => c.Id == connectionId && c.From == from && c.To == to);
    }

    [Fact]
    public void Connect_Fails_When_SourceNodeNotFound()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "p");
        var target = new TestNode(NodeId.New(), "dst", new Point2D(0, 0),
            inputs: new[] { new Port(PortName.From("in"), PortDirection.Input, DataType.RawRecord) });
        pipeline.AddNode(target);

        var from = new PortRef(NodeId.New(), PortName.From("out")); // not added
        var to = new PortRef(target.Id, PortName.From("in"));

        // Act
        var result = pipeline.Connect(from, to);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("not_found");
    }

    [Fact]
    public void Connect_Fails_When_TargetNodeNotFound()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "p");
        var source = new TestNode(NodeId.New(), "src", new Point2D(0, 0),
            outputs: new[] { new Port(PortName.From("out"), PortDirection.Output, DataType.Record) });
        pipeline.AddNode(source);

        var from = new PortRef(source.Id, PortName.From("out"));
        var to = new PortRef(NodeId.New(), PortName.From("in")); // not added

        // Act
        var result = pipeline.Connect(from, to);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("not_found");
    }

    [Fact]
    public void Connect_Fails_When_OutputPortNotFound()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "p");
        var source = new TestNode(NodeId.New(), "src", new Point2D(0, 0),
            outputs: new[] { new Port(PortName.From("other"), PortDirection.Output, DataType.Record) });
        var target = new TestNode(NodeId.New(), "dst", new Point2D(1, 1),
            inputs: new[] { new Port(PortName.From("in"), PortDirection.Input, DataType.RawRecord) });
        pipeline.AddNode(source);
        pipeline.AddNode(target);

        var from = new PortRef(source.Id, PortName.From("out")); // not present
        var to = new PortRef(target.Id, PortName.From("in"));

        // Act
        var result = pipeline.Connect(from, to);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("not_found");
    }

    [Fact]
    public void Connect_Fails_When_InputPortNotFound()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "p");
        var source = new TestNode(NodeId.New(), "src", new Point2D(0, 0),
            outputs: new[] { new Port(PortName.From("out"), PortDirection.Output, DataType.Record) });
        var target = new TestNode(NodeId.New(), "dst", new Point2D(1, 1),
            inputs: new[] { new Port(PortName.From("other"), PortDirection.Input, DataType.RawRecord) });
        pipeline.AddNode(source);
        pipeline.AddNode(target);

        var from = new PortRef(source.Id, PortName.From("out"));
        var to = new PortRef(target.Id, PortName.From("in")); // not present

        // Act
        var result = pipeline.Connect(from, to);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("not_found");
    }

    [Fact]
    public void Connect_Fails_For_IncompatibleTypes_RawRecordOutput_To_AggregateInput()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "p");
        var source = new TestNode(NodeId.New(), "src", new Point2D(0, 0),
            outputs: new[] { new Port(PortName.From("out"), PortDirection.Output, DataType.RawRecord) });
        var target = new TestNode(NodeId.New(), "dst", new Point2D(1, 1),
            inputs: new[] { new Port(PortName.From("in"), PortDirection.Input, DataType.Aggregate) });
        pipeline.AddNode(source);
        pipeline.AddNode(target);

        var from = new PortRef(source.Id, PortName.From("out"));
        var to = new PortRef(target.Id, PortName.From("in"));

        // Act
        var result = pipeline.Connect(from, to);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("validation");
    }

    [Fact]
    public void Connect_Fails_When_InputPort_AlreadyHasConnection()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "p");
        var src1 = new TestNode(NodeId.New(), "s1", new Point2D(0, 0),
            outputs: new[] { new Port(PortName.From("out1"), PortDirection.Output, DataType.Record) });
        var src2 = new TestNode(NodeId.New(), "s2", new Point2D(1, 0),
            outputs: new[] { new Port(PortName.From("out2"), PortDirection.Output, DataType.Record) });
        var dst = new TestNode(NodeId.New(), "dst", new Point2D(2, 0),
            inputs: new[] { new Port(PortName.From("in"), PortDirection.Input, DataType.RawRecord) });
        pipeline.AddNode(src1);
        pipeline.AddNode(src2);
        pipeline.AddNode(dst);

        var first = pipeline.Connect(new PortRef(src1.Id, PortName.From("out1")), new PortRef(dst.Id, PortName.From("in")));
        first.IsSuccess.Should().BeTrue();
        var second = pipeline.Connect(new PortRef(src2.Id, PortName.From("out2")), new PortRef(dst.Id, PortName.From("in")));

        // Assert
        second.IsSuccess.Should().BeFalse();
        second.Error!.Value.Code.Should().Be("conflict");
    }

    [Fact]
    public void Connect_Fails_When_Connecting_Node_To_Itself()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "p");
        var node = new TestNode(NodeId.New(), "n", new Point2D(0, 0),
            inputs: new[] { new Port(PortName.From("in"), PortDirection.Input, DataType.RawRecord) },
            outputs: new[] { new Port(PortName.From("out"), PortDirection.Output, DataType.Record) });
        pipeline.AddNode(node);

        var from = new PortRef(node.Id, PortName.From("out"));
        var to = new PortRef(node.Id, PortName.From("in"));

        // Act
        var result = pipeline.Connect(from, to);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("validation");
    }

    [Fact]
    public void Disconnect_Removes_ExistingConnection()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "p");
        var src = new TestNode(NodeId.New(), "s", new Point2D(0, 0),
            outputs: new[] { new Port(PortName.From("out"), PortDirection.Output, DataType.Record) });
        var dst = new TestNode(NodeId.New(), "d", new Point2D(1, 0),
            inputs: new[] { new Port(PortName.From("in"), PortDirection.Input, DataType.RawRecord) });
        pipeline.AddNode(src);
        pipeline.AddNode(dst);

        var connect = pipeline.Connect(new PortRef(src.Id, PortName.From("out")), new PortRef(dst.Id, PortName.From("in")));
        connect.IsSuccess.Should().BeTrue();
        var connId = connect.Value;

        // Act
        var disconnect = pipeline.Disconnect(connId);

        // Assert
        disconnect.IsSuccess.Should().BeTrue();
        pipeline.Connections.Should().NotContain(c => c.Id == connId);
    }

    [Fact]
    public void Disconnect_NonExisting_ReturnsNotFound()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "p");

        // Act
        var result = pipeline.Disconnect(ConnectionId.New());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("not_found");
    }

    [Fact]
    public void Connect_Succeeds_For_Record_To_RawRecord_Covariance()
    {
        // Arrange
        var pipeline = new Pipeline(PipelineId.New(), "p");
        var source = new TestNode(NodeId.New(), "src", new Point2D(0, 0),
            outputs: new[] { new Port(PortName.From("out"), PortDirection.Output, DataType.Record) });
        var target = new TestNode(NodeId.New(), "dst", new Point2D(1, 1),
            inputs: new[] { new Port(PortName.From("in"), PortDirection.Input, DataType.RawRecord) });
        pipeline.AddNode(source);
        pipeline.AddNode(target);

        var from = new PortRef(source.Id, PortName.From("out"));
        var to = new PortRef(target.Id, PortName.From("in"));

        // Act
        var result = pipeline.Connect(from, to);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }


}

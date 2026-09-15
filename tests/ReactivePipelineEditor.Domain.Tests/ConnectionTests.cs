using FluentAssertions;

using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Ports;

namespace ReactivePipelineEditor.Domain.Tests;

public class ConnectionTests
{
    [Fact]
    public void Connection_Create_HasCorrectComponents()
    {
        // Arrange
        var id = ConnectionId.New();
        var from = new PortRef(NodeId.New(), PortName.From("out"));
        var to = new PortRef(NodeId.New(), PortName.From("in"));

        // Act
        var connection = new Connection(id, from, to);

        // Assert
        connection.Id.Should().Be(id);
        connection.From.Should().Be(from);
        connection.To.Should().Be(to);
    }

    [Fact]
    public void Connection_Deconstruct_Works()
    {
        // Arrange
        var id = ConnectionId.New();
        var from = new PortRef(NodeId.New(), PortName.From("a"));
        var to = new PortRef(NodeId.New(), PortName.From("b"));
        var connection = new Connection(id, from, to);

        // Act
        connection.Deconstruct(out var gotId, out var gotFrom, out var gotTo);

        // Assert
        gotId.Should().Be(id);
        gotFrom.Should().Be(from);
        gotTo.Should().Be(to);
    }

    [Fact]
    public void Connection_Equality_SameValues_AreEqual()
    {
        // Arrange
        var id = ConnectionId.New();
        var from = new PortRef(NodeId.New(), PortName.From("p"));
        var to = new PortRef(NodeId.New(), PortName.From("q"));

        // Act
        var a = new Connection(id, from, to);
        var b = new Connection(id, from, to);

        // Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Connection_NotEqual_WhenDifferentId()
    {
        // Arrange
        var from = new PortRef(NodeId.New(), PortName.From("p"));
        var to = new PortRef(NodeId.New(), PortName.From("q"));

        // Act
        var a = new Connection(ConnectionId.New(), from, to);
        var b = new Connection(ConnectionId.New(), from, to);

        // Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void Connection_With_CreatesModifiedCopy()
    {
        // Arrange
        var id = ConnectionId.New();
        var from = new PortRef(NodeId.New(), PortName.From("out"));
        var to = new PortRef(NodeId.New(), PortName.From("in"));
        var connection = new Connection(id, from, to);

        // Act
        var newTo = new PortRef(NodeId.New(), PortName.From("in2"));
        var modified = connection with { To = newTo };

        // Assert
        connection.To.Should().Be(to); // original unchanged
        modified.To.Should().Be(newTo);
        modified.Id.Should().Be(connection.Id);
        modified.From.Should().Be(connection.From);
    }

    [Fact]
    public void Connection_ToString_ContainsComponents()
    {
        // Arrange
        var id = ConnectionId.From(Guid.Parse("11111111-2222-3333-4444-555555555555"));
        var from = new PortRef(NodeId.From(Guid.Parse("00000000-0000-0000-0000-000000000001")), PortName.From("out"));
        var to = new PortRef(NodeId.From(Guid.Parse("00000000-0000-0000-0000-000000000002")), PortName.From("in"));
        var connection = new Connection(id, from, to);

        // Act
        var s = connection.ToString();

        // Assert
        s.Should().Contain(id.ToString());
        s.Should().Contain(from.NodeId.ToString());
        s.Should().Contain(from.PortName.Value);
        s.Should().Contain(to.NodeId.ToString());
        s.Should().Contain(to.PortName.Value);
    }

    [Fact]
    public void Connection_Allows_SameFromAndTo()
    {
        // Arrange
        var id = ConnectionId.New();
        var nodeId = NodeId.New();
        var port = PortName.From("dup");
        var pr = new PortRef(nodeId, port);

        // Act
        var connection = new Connection(id, pr, pr);

        // Assert
        connection.From.Should().Be(connection.To);
    }

}

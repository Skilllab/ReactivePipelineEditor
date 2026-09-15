using FluentAssertions;

using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Ports;

namespace ReactivePipelineEditor.Domain.Tests;

public class PortRefTests
{
    [Fact]
    public void PortRef_Create_HasCorrectComponents()
    {
        // Arrange
        var nodeId = NodeId.New();
        var name = PortName.From("input");

        // Act
        var portRef = new PortRef(nodeId, name);

        // Assert
        portRef.NodeId.Should().Be(nodeId);
        portRef.PortName.Should().Be(name);
    }

    [Fact]
    public void PortRef_Deconstruct_Works()
    {
        // Arrange
        var nodeId = NodeId.New();
        var name = PortName.From("output");
        var portRef = new PortRef(nodeId, name);

        // Act
        portRef.Deconstruct(out var gotNodeId, out var gotName);

        // Assert
        gotNodeId.Should().Be(nodeId);
        gotName.Should().Be(name);
    }

    [Fact]
    public void PortRef_Equals_ForSameValues()
    {
        // Arrange
        var nodeId = NodeId.New();
        var name = PortName.From("port");
        var a = new PortRef(nodeId, name);

        // Act
        var b = new PortRef(nodeId, name);

        // Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void PortRef_NotEquals_WhenDifferentNodeOrName()
    {
        // Arrange
        var n1 = NodeId.New();
        var n2 = NodeId.New();
        var name1 = PortName.From("a");
        var name2 = PortName.From("b");

        // Act
        var byNode = new PortRef(n1, name1);
        var byNodeDiff = new PortRef(n2, name1);
        var byNameDiff = new PortRef(n1, name2);

        // Assert
        byNode.Should().NotBe(byNodeDiff);
        byNode.Should().NotBe(byNameDiff);
    }

    [Fact]
    public void PortRef_ToString_UsesNodeIdAndPortName()
    {
        // Arrange
        var nodeId = NodeId.From(Guid.Parse("11111111-2222-3333-4444-555555555555"));
        var name = PortName.From("in");
        var portRef = new PortRef(nodeId, name);

        // Act
        var s = portRef.ToString();

        // Assert
        s.Should().Be($"{nodeId}.{name}");
    }
}

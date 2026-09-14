using FluentAssertions;

using ReactivePipelineEditor.Domain.Common;

namespace ReactivePipelineEditor.Domain.Tests;

public class IdTypesTests
{
    [Fact]
    [Trait("Category", "NodeId")]

    public void NodeId_New_IsNotEmpty()
    {
        // Arrange and Act
        var id = NodeId.New();

        // Assert
        id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Category", "NodeId")]
    public void NodeId_From_Empty_ThrowsArgumentException()
    {
        // Arrange and Act
        Action act = () => NodeId.From(Guid.Empty);

        // Assert
        var ex = Assert.Throws<ArgumentException>(act);
        ex.ParamName.Should().Be("value");
    }

    [Fact]
    [Trait("Category", "NodeId")]
    public void NodeId_From_PreservesValue_And_Equals()
    {
        // Arrange
        var g = Guid.NewGuid();

        // Act
        var a = NodeId.From(g);
        var b = NodeId.From(g);

        // Assert
        a.Value.Should().Be(g);
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.ToString().Should().Be(g.ToString());
    }

    [Fact]
    [Trait("Category", "NodeId")]
    public void NodeId_DifferentValues_AreNotEqual()
    {
        // Arrange
        var a = NodeId.From(Guid.NewGuid());
        var b = NodeId.From(Guid.NewGuid());

        // Act

        // Assert
        a.Should().NotBe(b);
    }

    [Fact]
    [Trait("Category", "NodeId")]
    public void NodeId_Deconstruct_Works()
    {
        // Arrange
        var g = Guid.NewGuid();
        var id = NodeId.From(g);

        // Act
        id.Deconstruct(out var value);

        // Assert
        value.Should().Be(g);
    }

    [Fact]
    [Trait("Category", "NodeId")]
    public void NodeId_Boxing_EqualsObject()
    {
        // Arrange
        var g = Guid.NewGuid();
        var id = NodeId.From(g);

        // Act
        object boxed = id;

        // Assert
        id.Equals(boxed).Should().BeTrue();
        boxed.Equals(id).Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "NodeId")]
    public void DefaultNodeId_IsEmpty()
    {
        // Arrange and Act
        var def = default(NodeId);

        // Assert
        def.Value.Should().Be(Guid.Empty);
    }

    [Fact]
    [Trait("Category", "PipelineId")]
    public void PipelineId_New_IsNotEmpty()
    {
        // Arrange and Act
        var id = PipelineId.New();

        // Assert
        id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Category", "PipelineId")]
    public void PipelineId_From_Empty_ThrowsArgumentException()
    {
        // Arrange and Act
        Action act = () => PipelineId.From(Guid.Empty);

        // Assert
        var ex = Assert.Throws<ArgumentException>(act);
        ex.ParamName.Should().Be("value");
    }

    [Fact]
    [Trait("Category", "PipelineId")]
    public void PipelineId_From_PreservesValue_And_Equals()
    {
        // Arrange
        var g = Guid.NewGuid();

        // Act
        var a = PipelineId.From(g);
        var b = PipelineId.From(g);

        // Assert
        a.Value.Should().Be(g);
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.ToString().Should().Be(g.ToString());
    }

    [Fact]
    [Trait("Category", "PipelineId")]
    public void PipelineId_DifferentValues_AreNotEqual()
    {
        // Arrange
        var a = PipelineId.From(Guid.NewGuid());
        var b = PipelineId.From(Guid.NewGuid());

        // Act

        // Assert
        a.Should().NotBe(b);
    }

    [Fact]
    [Trait("Category", "PipelineId")]
    public void PipelineId_Deconstruct_Works()
    {
        // Arrange
        var g = Guid.NewGuid();
        var id = PipelineId.From(g);

        // Act
        id.Deconstruct(out var value);

        // Assert
        value.Should().Be(g);
    }

    [Fact]
    [Trait("Category", "ConnectionId")]
    public void ConnectionId_New_IsNotEmpty()
    {
        // Arrange

        // Act
        var id = ConnectionId.New();

        // Assert
        id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Category", "ConnectionId")]
    public void ConnectionId_From_Empty_ThrowsArgumentException()
    {
        // Arrange

        // Act
        Action act = () => ConnectionId.From(Guid.Empty);

        // Assert
        var ex = Assert.Throws<ArgumentException>(act);
        ex.ParamName.Should().Be("value");
    }

    [Fact]
    [Trait("Category", "ConnectionId")]
    public void ConnectionId_From_PreservesValue_And_Equals()
    {
        // Arrange
        var g = Guid.NewGuid();

        // Act
        var a = ConnectionId.From(g);
        var b = ConnectionId.From(g);

        // Assert
        a.Value.Should().Be(g);
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.ToString().Should().Be(g.ToString());
    }

    [Fact]
    [Trait("Category", "ConnectionId")]
    public void ConnectionId_DifferentValues_AreNotEqual()
    {
        // Arrange
        var a = ConnectionId.From(Guid.NewGuid());
        var b = ConnectionId.From(Guid.NewGuid());

        // Act

        // Assert
        a.Should().NotBe(b);
    }

    [Fact]
    [Trait("Category", "ConnectionId")]
    public void ConnectionId_Deconstruct_Works()
    {
        // Arrange
        var g = Guid.NewGuid();
        var id = ConnectionId.From(g);

        // Act
        id.Deconstruct(out var value);

        // Assert
        value.Should().Be(g);
    }

    [Fact]
    [Trait("Category", "All")]
    public void SameGuid_DifferentIdTypes_AreNotEqual()
    {
        // Arrange
        var g = Guid.NewGuid();
        var n = NodeId.From(g);
        var p = PipelineId.From(g);
        var c = ConnectionId.From(g);

        // Act

        // Assert
        n.Equals(p).Should().BeFalse();
        n.Equals(c).Should().BeFalse();
        p.Equals(c).Should().BeFalse();

        n.GetType().Should().NotBe(p.GetType());
        p.GetType().Should().NotBe(c.GetType());
    }
}

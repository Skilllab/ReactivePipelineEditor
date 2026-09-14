using FluentAssertions;

using ReactivePipelineEditor.Domain.Ports;

namespace ReactivePipelineEditor.Domain.Tests;

public class PortNameTests
{
    [Theory]
    [InlineData("In")]
    [InlineData("Out")]
    [InlineData("input123")]
    [InlineData("input_port_123")]
    [InlineData("a")]
    public void PortName_From_ValidName_Succeeds(string name)
    {
        // Arrange Act
        var result = PortName.From(name);

        // Assert
        result.Value.Should().Be(name);
        result.ToString().Should().Be(name);
    }

    [Fact]
    public void PortName_From_MaxLength64_Succeeds()
    {
        // Arrange
        var name = new string('a', 64);

        // Act
        var result = PortName.From(name);

        // Assert
        result.Value.Should().Be(name);
        result.Value.Length.Should().Be(64);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\t")]
    [InlineData("1input")]
    [InlineData("_input")]
    [InlineData("input-port")]
    [InlineData("input.port")]
    [InlineData("input@port")]
    [InlineData("input port")]
    public void PortName_From_Null_Throws(string? name)
    {
        // Arrange and Act
        Action act = () => PortName.From(name!);

        // Assert
        var ex = Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void PortName_From_TrimsWhitespace()
    {
        // Arrange
        var name = "  input  ";

        // Act
        var result = PortName.From(name);

        // Assert
        result.Value.Should().Be("input");
    }

    [Fact]
    public void PortName_From_LongerThan64_Throws()
    {
        // Arrange
        var name = new string('a', 65);

        // Act
        Action act = () => PortName.From(name);

        // Assert
        var ex = Assert.Throws<ArgumentException>(act);
        ex.ParamName.Should().Be("value");
    }

    [Fact]
    public void PortName_EqualNames_AreEqual()
    {
        // Arrange
        var a = PortName.From("input");
        var b = PortName.From("input");

        // Act

        // Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void PortName_DifferentNames_AreNotEqual()
    {
        // Arrange
        var a = PortName.From("input");
        var b = PortName.From("output");

        // Act

        // Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void PortName_ImplicitConversion_ToString()
    {
        // Arrange
        var name = PortName.From("input");

        // Act
        string str = name;

        // Assert
        str.Should().Be("input");
    }

    [Fact]
    public void PortName_ToString_ReturnsValue()
    {
        // Arrange
        var name = PortName.From("output");

        // Act
        var str = name.ToString();

        // Assert
        str.Should().Be("output");
    }

    [Fact]
    public void PortName_BoxedValues_AreEqual()
    {
        // Arrange
        var name = PortName.From("port");

        // Act
        object boxed = name;

        // Assert
        name.Equals(boxed).Should().BeTrue();
        boxed.Equals(name).Should().BeTrue();
    }
}

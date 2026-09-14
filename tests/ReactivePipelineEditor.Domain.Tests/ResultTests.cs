using FluentAssertions;

using ReactivePipelineEditor.Domain.Common;

namespace ReactivePipelineEditor.Domain.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Ok_result_has_value()
    {
        // Arrange and Act
        var result = Result<int>.Ok(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failed_result_has_error()
    {
        // Arrange
        var error = Error.Validation("Ошибка");

        //Act
        var result = Result<int>.Fail(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Accessing_value_of_failed_result_throws()
    {
        //Arrange
        var result = Result<int>.Fail(Error.Validation("Ошибка"));

        //Act
        Action act = () => _ = result.Value;

        //Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Ok_result_with_null_reference_value_throws()
    {
        // Arrange
        Action act = () => Result<string>.Ok(null!);

        // Act and Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("value");
    }

    [Fact]
    public void Accessing_value_of_failed_result_of_reference_type_throws()
    {
        // Arrange
        Result<string> result = Result<string>.Fail(Error.Conflict("Конфликт"));

        // Act
        Action act = () => _ = result.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Default_result_is_not_success_and_has_no_error()
    {
        // Arrange
        Result<int> result = default;

        // Act and Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Accessing_value_of_default_result_throws()
    {
        // Arrange
        Result<int> result = default;

        // Act
        Action act = () => _ = result.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Failed_result_preserves_error_code_and_message()
    {
        // Arrange
        Result<int> result = Result<int>.Fail(Error.NotFound("Pipeline"));

        // Act and Assert
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("not_found");
        result.Error!.Value.Message.Should().Be("Pipeline не найден");
    }


    [Fact]
    public void Ok_results_with_equal_values_are_equal()
    {
        // Arrange
        Result<int> first = Result<int>.Ok(42);
        Result<int> second = Result<int>.Ok(42);

        // Act and Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void Ok_result_keeps_the_exact_instance_of_a_reference_value()
    {
        // Arrange
        var payload = new object();

        // Act
        Result<object> result = Result<object>.Ok(payload);

        // Assert
        result.Value.Should().BeSameAs(payload);
    }

    [Fact]
    public void Ok_result_with_nullable_value_type_returns_value()
    {
        // Arrange — T == int?, where 'null' is a legal T but still rejected as Ok
        Result<int?> result = Result<int?>.Ok(5);

        // Act and Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5);
        result.Error.Should().BeNull();
    }


}

using FluentAssertions;

using ReactivePipelineEditor.Domain.Ports;

namespace ReactivePipelineEditor.Domain.Tests;

public class DataTypeTests
{
    [Fact]
    public void DataType_Create_WithName_Only()
    {
        // Arrange
        var name = "CustomType";

        // Act
        var dataType = new DataType(name);

        // Assert
        dataType.Name.Should().Be(name);
        dataType.BaseType.Should().BeNull();
    }

    [Fact]
    public void DataType_Create_WithNameAndBaseType()
    {
        // Arrange
        var basetype = new DataType("Base");
        var name = "Derived";

        // Act
        var dataType = new DataType(name, basetype);

        // Assert
        dataType.Name.Should().Be(name);
        dataType.BaseType.Should().Be(basetype);
    }

    [Fact]
    public void DataType_PredefinedTypes_Exist()
    {
        // Arrange
        // Act
        // Assert
        DataType.RawRecord.Should().NotBeNull();
        DataType.RawRecord.Name.Should().Be("RawRecord");
        DataType.RawRecord.BaseType.Should().BeNull();

        DataType.Record.Should().NotBeNull();
        DataType.Record.Name.Should().Be("Record");

        DataType.Aggregate.Should().NotBeNull();
        DataType.Aggregate.Name.Should().Be("Aggregate");
    }

    [Fact]
    public void DataType_Hierarchy_IsCorrect()
    {
        // Arrange
        // Act
        // Assert
        DataType.Record.BaseType.Should().Be(DataType.RawRecord);
        DataType.Aggregate.BaseType.Should().Be(DataType.Record);
    }

    [Fact]
    public void IsAssignableFrom_SameType_ReturnsTrue()
    {
        // Arrange
        var type1 = new DataType("MyType");
        var type2 = new DataType("MyType");

        // Act
        var result = type1.IsAssignableFrom(type2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAssignableFrom_DirectDerivedType_ReturnsTrue()
    {
        // Arrange
        var baseType = new DataType("Base");
        var derivedType = new DataType("Derived", baseType);

        // Act
        var result = baseType.IsAssignableFrom(derivedType);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAssignableFrom_IndirectDerivedType_ReturnsTrue()
    {
        // Arrange
        var level1 = new DataType("Level1");
        var level2 = new DataType("Level2", level1);
        var level3 = new DataType("Level3", level2);

        // Act
        var result = level1.IsAssignableFrom(level3);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAssignableFrom_RawRecord_AcceptsRecord()
    {
        // Arrange
        // Act
        var result = DataType.RawRecord.IsAssignableFrom(DataType.Record);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAssignableFrom_RawRecord_AcceptsAggregate()
    {
        // Arrange
        // Act
        var result = DataType.RawRecord.IsAssignableFrom(DataType.Aggregate);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAssignableFrom_Record_AcceptsAggregate()
    {
        // Arrange
        // Act
        var result = DataType.Record.IsAssignableFrom(DataType.Aggregate);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAssignableFrom_DerivedToBase_ReturnsFalse()
    {
        // Arrange
        var baseType = new DataType("Base");
        var derivedType = new DataType("Derived", baseType);

        // Act
        var result = derivedType.IsAssignableFrom(baseType);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAssignableFrom_Aggregate_DoesNotAcceptRecord()
    {
        // Arrange

        // Act
        var result = DataType.Aggregate.IsAssignableFrom(DataType.Record);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAssignableFrom_UnrelatedTypes_ReturnsFalse()
    {
        // Arrange
        var type1 = new DataType("Type1");
        var type2 = new DataType("Type2");

        // Act
        var result = type1.IsAssignableFrom(type2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        // Arrange
        var name = "MyCustomType";
        var dataType = new DataType(name);

        // Act
        var result = dataType.ToString();

        // Assert
        result.Should().Be(name);
    }

    [Fact]
    public void EqualDataTypes_AreEqual()
    {
        // Arrange
        var dataType1 = new DataType("TestType");
        var dataType2 = new DataType("TestType");

        // Act

        // Assert
        dataType1.Should().Be(dataType2);
        dataType1.GetHashCode().Should().Be(dataType2.GetHashCode());
    }

    [Fact]
    public void DifferentDataTypes_AreNotEqual()
    {
        // Arrange
        var dataType1 = new DataType("Type1");
        var dataType2 = new DataType("Type2");

        // Act

        // Assert
        dataType1.Should().NotBe(dataType2);
    }

    [Fact]
    public void DataTypesWithDifferentBaseTypes_AreNotEqual()
    {
        // Arrange
        var base1 = new DataType("Base1");
        var base2 = new DataType("Base2");
        var dataType1 = new DataType("Derived", base1);
        var dataType2 = new DataType("Derived", base2);

        // Act

        // Assert
        dataType1.Should().NotBe(dataType2);
    }

    [Fact]
    public void DataTypesWithSameNameAndBaseType_AreEqual()
    {
        // Arrange
        var baseType = new DataType("Base");
        var dataType1 = new DataType("Derived", baseType);
        var dataType2 = new DataType("Derived", baseType);

        // Act

        // Assert
        dataType1.Should().Be(dataType2);
    }

    [Fact]
    public void BoxedDataType_EqualsOriginal()
    {
        // Arrange
        var dataType = new DataType("MyType");

        // Act
        object boxed = dataType;

        // Assert
        dataType.Equals(boxed).Should().BeTrue();
        boxed.Equals(dataType).Should().BeTrue();
    }

    [Fact]
    public void PredefinedTypes_AreConsistent()
    {
        // Arrange

        // Act
        var rawRecord = DataType.RawRecord;
        var record = DataType.Record;
        var aggregate = DataType.Aggregate;

        // Assert
        record.IsAssignableFrom(rawRecord).Should().BeFalse();
        rawRecord.IsAssignableFrom(record).Should().BeTrue();
        rawRecord.IsAssignableFrom(aggregate).Should().BeTrue();
        record.IsAssignableFrom(aggregate).Should().BeTrue();
        aggregate.IsAssignableFrom(record).Should().BeFalse();
        aggregate.IsAssignableFrom(rawRecord).Should().BeFalse();
    }

    [Fact]
    public void IsAssignableFrom_WithDeeplyNestedHierarchy_ReturnsTrue()
    {
        // Arrange
        var level0 = new DataType("Level0");
        var level1 = new DataType("Level1", level0);
        var level2 = new DataType("Level2", level1);
        var level3 = new DataType("Level3", level2);
        var level4 = new DataType("Level4", level3);

        // Act
        var result = level0.IsAssignableFrom(level4);

        // Assert
        result.Should().BeTrue();
    }
}

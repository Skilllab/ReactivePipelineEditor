namespace ReactivePipelineEditor.Domain.Ports;

/// <summary>
/// Тип данных с именем и необязательным базовым типом, используемый для представления ковариантной иерархии типов
/// </summary>   
/// <param name="Name">Имя типа</param>
/// <param name="BaseType">Необязательный базовый тип, задающий ковариантную иерархию</param>
public sealed record DataType(string Name, DataType? BaseType = null)
{
    public static readonly DataType RawRecord = new("RawRecord");
    public static readonly DataType Record = new("Record", RawRecord);
    public static readonly DataType Aggregate = new("Aggregate", Record);

    /// <summary>
    /// Проверяет, можно ли присвоить значение типа <paramref name="other"/>
    /// переменной текущего типа. Учитывает ковариантность по BaseType.
    /// </summary>
    public bool IsAssignableFrom(DataType other)
    {
        var current = other;
        while (current is not null)
        {
            if (current == this) return true;
            current = current.BaseType;
        }
        return false;
    }

    public override string ToString() => Name;
}

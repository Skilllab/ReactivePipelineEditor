namespace ReactivePipelineEditor.Domain.Common;

/// <summary>
/// Представляет идентификатор узла в конвейере
/// </summary>
public readonly record struct NodeId
{
    /// <summary>
    /// GUID, представляющий идентификатор узла
    /// </summary>
    public Guid Value { get; }

    private NodeId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("NodeId не может быть пустым", nameof(value));
        Value = value;
    }

    /// <summary>
    /// Создает новый уникальный идентификатор узла
    /// </summary>
    public static NodeId New() => new(Guid.NewGuid());

    /// <summary>
    /// Создает идентификатор узла из существующего GUID
    /// </summary>
    /// <param name="value">GUID, представляющий идентификатор узла; не может быть пустым</param>
    public static NodeId From(Guid value) => new(value);

    /// <summary>
    /// Возвращает строковое представление идентификатора узла
    /// </summary>
    public override string ToString() => Value.ToString();
}

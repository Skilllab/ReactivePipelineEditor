namespace ReactivePipelineEditor.Domain.Common;

/// <summary>
/// Представляет идентификатор конвейера
/// </summary>
public readonly record struct PipelineId
{
    /// <summary>
    /// GUID, представляющий идентификатор конвейера
    /// </summary>
    public Guid Value { get; }

    private PipelineId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PipelineId не может быть пустым", nameof(value));
        Value = value;
    }

    /// <summary>
    /// Создает новый уникальный идентификатор конвейера
    /// </summary>
    public static PipelineId New() => new(Guid.NewGuid());

    /// <summary>
    /// Создает идентификатор конвейера из существующего GUID
    /// </summary>
    /// <param name="value">GUID, представляющий идентификатор конвейера; не может быть пустым</param>
    public static PipelineId From(Guid value) => new(value);

    /// <summary>
    /// Возвращает строковое представление идентификатора конвейера
    /// </summary>
    public override string ToString() => Value.ToString();
}

namespace ReactivePipelineEditor.Runtime.Executors;

/// <summary>
/// Типизированная запись после MapNode. Содержит словарь
/// имя-поля → значение. Значения хранятся как object, потому что
/// конкретный тип (int, double, DateTime, string) определяется
/// выражением маппинга в runtime.
/// </summary>
public sealed record Record(int LineNumber, IReadOnlyDictionary<string, object?> Fields)
{
    /// <summary>
    /// Возвращает значение поля по имени. null, если поля нет.
    /// </summary>
    public object? GetField(string name) =>
        Fields.TryGetValue(name, out var value) ? value : null;

    /// <summary>
    /// Возвращает значение поля, приведённое к типу T.
    /// Если поле отсутствует или не приводится — возвращает default(T).
    /// </summary>
    public T? GetField<T>(string name)
    {
        var value = GetField(name);
        if (value is null) return default;
        if (value is T typed) return typed;

        try
        {
            return (T) Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}

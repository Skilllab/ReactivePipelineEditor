namespace ReactivePipelineEditor.Runtime.Executors;

/// <summary>
/// Результат агрегации. Содержит ключ группировки и агрегированное значение.
/// Например: { Key = "Category A", Value = 15000.0 }.
/// </summary>
public sealed record Aggregate(string Key, object? Value);

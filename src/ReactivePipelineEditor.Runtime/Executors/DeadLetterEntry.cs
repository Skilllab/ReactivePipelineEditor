namespace ReactivePipelineEditor.Runtime.Executors;

/// <summary>
/// Запись в dead-letter: отвергнутая запись + причина отклонения.
/// Формат — JSONL: одна запись на строку.
/// </summary>
public sealed record DeadLetterEntry(
    DateTimeOffset Timestamp,
    string NodeDisplayName,
    string Reason,
    RawRecord? SourceData);

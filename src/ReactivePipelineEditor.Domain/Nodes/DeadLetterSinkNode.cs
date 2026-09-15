using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Ports;

namespace ReactivePipelineEditor.Domain.Nodes;

/// <summary>
/// Приёмник «мёртвых писем» (dead-letter): сохраняет записи,
/// отвергнутые другими нодами, вместе с причиной отклонения.
///
/// Вход: RawRecord. Принимает отвергнутое от FilterNode (порт Rejected)
/// или ValidateNode (порт Invalid). RawRecord, а не Record, потому что
/// некоторые отказы происходят ДО типизации — например, FilterNode
/// работает с сырыми записями.
///
/// Формат вывода — JSONL (JSON Lines): по одной JSON-записи на строку.
/// Удобно стримить, парсить построчно, обрабатывать большими файлами
/// без загрузки всего в память.
///
/// Dead-letter — не «мусор». Это данные для разбора:
/// - Почему запись отклонена?
/// - Что в ней было не так?
/// - Не сломался ли наш pipeline?
///
/// Без DeadLetterSink отвергнутые записи теряются, и разбор проблем
/// превращается в гадание.
/// </summary>
public sealed class DeadLetterSinkNode : Node
{
    public static readonly PortName InPortName = PortName.From("In");

    /// <summary>
    /// Путь к выходному файлу (JSONL-формат).
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// Включать ли исходные данные записи в dead-letter запись.
    /// true (по умолчанию) — сохраняем payload, можно посмотреть,
    /// что именно не прошло.
    /// false — сохраняем только метаданные (кто отклонил, причина,
    /// время). Меньше размер файла, но сложнее разбирать инциденты.
    /// </summary>
    public bool IncludeSourceData { get; set; } = true;

    public DeadLetterSinkNode(NodeId id, Point2D position, string filePath)
        : base(id, "Dead Letter Sink", position)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Вход: RawRecord. Принимаем отвергнутое с фильтров и валидаторов.
    /// Если запись отвергнута на этапе Map (например, ошибка парсинга),
    /// она тоже приходит сюда как RawRecord — потому что на момент
    /// ошибки она ещё не была типизирована.
    /// </summary>
    public override IReadOnlyList<Port> Inputs => new[]
    {
        new Port(InPortName, PortDirection.Input, DataType.RawRecord)
    };

    /// <summary>
    /// Выходов нет. Конечная точка.
    /// </summary>
    public override IReadOnlyList<Port> Outputs => Array.Empty<Port>();
}

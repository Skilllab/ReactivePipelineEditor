using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Ports;

namespace ReactivePipelineEditor.Domain.Nodes;

/// <summary>
/// Приемник данных: пишет входящие записи в JSON-файл.
/// Вход: Aggregate (результат агрегации).
/// Выходов нет — приёмник является конечной точкой pipeline.
/// </summary>
public sealed class JsonSinkNode : Node
{
    public static readonly PortName InPortName = PortName.From("In");

    /// <summary>
    /// Путь к выходному JSON-файлу
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// Форматировать ли JSON с отступами (indented)
    /// </summary>
    public bool Indented { get; set; } = true;

    /// <summary>
    /// Режим записи: перезапись или дозапись
    /// </summary>
    public WriteMode WriteMode { get; set; } = WriteMode.Overwrite;

    public JsonSinkNode(NodeId id, Point2D position, string filePath)
        : base(id, "Json Sink", position)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Вход: Aggregate. Приемник принимает самый обогащенный тип данных,
    /// который получается после агрегации.
    /// </summary>
    public override IReadOnlyList<Port> Inputs => new[]
    {
        new Port(InPortName, PortDirection.Input, DataType.Aggregate)
    };

    /// <summary>
    /// Выходов нет. Приемник — конечная точка.
    /// </summary>
    public override IReadOnlyList<Port> Outputs => Array.Empty<Port>();
}

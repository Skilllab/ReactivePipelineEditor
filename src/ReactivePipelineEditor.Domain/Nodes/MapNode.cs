using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Ports;

namespace ReactivePipelineEditor.Domain.Nodes;

/// <summary>
/// Применяет выражение-трансформацию к каждой входящей записи.
/// </summary>
public sealed class MapNode : Node
{
    /// <summary>
    /// Имя входного порта
    /// </summary>
    public static readonly PortName InPortName = PortName.From("In");

    /// <summary>
    /// Имя выходного порта
    /// </summary>
    public static readonly PortName OutPortName = PortName.From("Out");

    /// <summary>
    /// Выражение маппинга. Хранится строкой
    /// </summary>
    public string Expression { get; set; }

    /// <summary>
    /// Степень параллелизма при обработке записей
    /// </summary>
    public int DegreeOfParallelism { get; set; } = 1;

    public MapNode(NodeId id, Point2D position, string expression)
        : base(id, "Map", position)
    {
        Expression = expression;
    }

    /// <summary>
    /// Входной порт: одна запись типа RawRecord
    /// </summary>
    public override IReadOnlyList<Port> Inputs => new[]
    {
        new Port(InPortName, PortDirection.Input, DataType.RawRecord)
    };

    /// <summary>
    /// Выходной порт: типизированная запись Record.
    /// </summary>
    public override IReadOnlyList<Port> Outputs => new[]
    {
        new Port(OutPortName, PortDirection.Output, DataType.Record)
    };
}

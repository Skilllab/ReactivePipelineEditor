using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Ports;

namespace ReactivePipelineEditor.Domain.Nodes;

/// <summary>
/// Фильтр: применяет предикат к каждой записи.
/// Записи, удовлетворяющие предикату, идут в порт "Passed".
/// Остальные в порт "Rejected".
/// </summary>
public sealed class FilterNode : Node
{
    /// <summary>
    /// Имя входного порта
    /// </summary>
    public static readonly PortName InPortName = PortName.From("In");

    /// <summary>
    /// Имя порта для записей, прошедших фильтр
    /// </summary>
    public static readonly PortName PassedPortName = PortName.From("Passed");

    /// <summary>
    /// Имя порта для отвергнутых записей
    /// </summary>
    public static readonly PortName RejectedPortName = PortName.From("Rejected");

    /// <summary>
    /// Выражение предиката. Хранится строкой
    /// </summary>
    public string Predicate { get; set; }

    public FilterNode(NodeId id, Point2D position, string predicate)
        : base(id, "Filter", position)
    {
        Predicate = predicate;
    }

    /// <summary>
    /// Один входной порт: In типа RawRecord.
    /// </summary>
    public override IReadOnlyList<Port> Inputs => new[]
    {
        new Port(InPortName, PortDirection.Input, DataType.RawRecord)
    };

    /// <summary>
    /// Два выходных порта: Passed и Rejected.
    /// </summary>
    public override IReadOnlyList<Port> Outputs => new[]
    {
        new Port(PassedPortName, PortDirection.Output, DataType.RawRecord),
        new Port(RejectedPortName, PortDirection.Output, DataType.RawRecord)
    };
}

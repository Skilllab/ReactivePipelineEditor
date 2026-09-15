using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Ports;

namespace ReactivePipelineEditor.Domain.Nodes;

/// <summary>
/// Проверяет каждую запись по именованной схеме валидации.
/// Валидные записи идут в порт "Valid".
/// Невалидные — в порт "Invalid" (для дальнейшего разбора).
/// </summary>
public sealed class ValidateNode : Node
{
    public static readonly PortName InPortName = PortName.From("In");
    public static readonly PortName ValidPortName = PortName.From("Valid");
    public static readonly PortName InvalidPortName = PortName.From("Invalid");

    /// <summary>
    /// Имя схемы валидации
    /// </summary>
    public string SchemaName { get; set; }

    public ValidateNode(NodeId id, Point2D position, string schemaName)
        : base(id, "Validate", position)
    {
        SchemaName = schemaName;
    }

    /// <summary>
    /// Вход: типизированная запись
    /// </summary>
    public override IReadOnlyList<Port> Inputs => new[]
    {
        new Port(InPortName, PortDirection.Input, DataType.Record)
    };

    /// <summary>
    /// Два выхода: валидные и невалидные записи
    /// </summary>
    public override IReadOnlyList<Port> Outputs => new[]
    {
        new Port(ValidPortName, PortDirection.Output, DataType.Record),
        new Port(InvalidPortName, PortDirection.Output, DataType.Record)
    };
}

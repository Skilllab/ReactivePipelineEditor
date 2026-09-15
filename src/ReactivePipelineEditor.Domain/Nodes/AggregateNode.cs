using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Ports;

namespace ReactivePipelineEditor.Domain.Nodes;

/// <summary>
/// Группирует входящие записи по ключу и применяет агрегатную функцию.
/// Вход: Record (типизированная запись).
/// Выход: Aggregate (результат агрегации).
/// </summary>
public sealed class AggregateNode : Node
{
    public static readonly PortName InPortName = PortName.From("In");
    public static readonly PortName OutPortName = PortName.From("Out");

    /// <summary>
    /// Выражение ключа группировки. Строка, потому что домен не знает
    /// про парсер выражений. Может быть:
    /// - именем поля: "Category"
    /// - составным ключом: "new { Country, Year }"
    /// - вычисляемым: "Year(Date)"
    /// </summary>
    public string GroupBy { get; set; }

    /// <summary>
    /// Выражение агрегатной функции. Строка по тем же причинам.
    /// Примеры: "Sum(Amount)", "Count()", "Average(Price)", "Min(Date)".
    /// </summary>
    public string Aggregation { get; set; }

    public AggregateNode(NodeId id, Point2D position, string groupBy, string aggregation)
        : base(id, "Aggregate", position)
    {
        GroupBy = groupBy;
        Aggregation = aggregation;
    }

    /// <summary>
    /// Вход: типизированная запись. Агрегация требует именованных полей.
    /// </summary>
    public override IReadOnlyList<Port> Inputs => new[]
    {
        new Port(InPortName, PortDirection.Input, DataType.Record)
    };

    /// <summary>
    /// Выход: Aggregate. Это НОВЫЙ тип данных, отличный от Record.
    /// Он наследуется от Record (через цепочку Aggregate -> Record -> RawRecord),
    /// поэтому выход AggregateNode можно подключить к нодам, ожидающим
    /// Record или RawRecord. Но НЕ наоборот: Record нельзя подключить
    /// туда, где ожидается Aggregate — это позволяет отсекать
    /// некорректные соединения на уровне графа.
    /// </summary>
    public override IReadOnlyList<Port> Outputs => new[]
    {
        new Port(OutPortName, PortDirection.Output, DataType.Aggregate)
    };
}

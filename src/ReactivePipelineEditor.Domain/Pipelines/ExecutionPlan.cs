using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Nodes;
using ReactivePipelineEditor.Domain.Ports;

namespace ReactivePipelineEditor.Domain.Pipelines;

/// <summary>
/// План выполнения, задающий упорядоченный список идентификаторов узлов, словарь определений узлов и список соединений
/// между ними.
/// </summary>
public sealed class ExecutionPlan
{
    /// <summary>
    /// Идентификаторы узлов в порядке их выполнения
    /// </summary>
    public IReadOnlyList<NodeId> ExecutionOrder { get; }

    /// <summary>
    /// Коллекция узлов, доступная по идентификатору узла
    /// </summary>
    public IReadOnlyDictionary<NodeId, Node> Nodes { get; }

    /// <summary>
    /// Получает коллекцию подключений, доступную только для чтения
    /// </summary>
    public IReadOnlyList<Connection> Connections { get; }

    /// <summary>
    /// Инициализирует новый экземпляр ExecutionPlan с заданным порядком выполнения, коллекцией узлов и списком
    /// соединений.
    /// </summary>
    /// <param name="executionOrder">Порядок выполнения узлов.</param>
    /// <param name="nodes">Словарь идентификаторов узлов и соответствующих объектов Node.</param>
    /// <param name="connections">Список соединений между узлами.</param>
    public ExecutionPlan(
        IReadOnlyList<NodeId> executionOrder,
        IReadOnlyDictionary<NodeId, Node> nodes,
        IReadOnlyList<Connection> connections)
    {
        ExecutionOrder = executionOrder;
        Nodes = nodes;
        Connections = connections;
    }
}

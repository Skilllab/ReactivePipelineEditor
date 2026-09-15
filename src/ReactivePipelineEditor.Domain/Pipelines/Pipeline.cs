using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Nodes;
using ReactivePipelineEditor.Domain.Ports;

namespace ReactivePipelineEditor.Domain.Pipelines;

/// <summary>
/// Представляет конвейер обработки, содержащий набор узлов (Node) и предоставляющий операции добавления, удаления и
/// поиска узлов. Содержит идентификатор и имя конвейера
/// </summary>
public sealed class Pipeline
{
    private readonly Dictionary<NodeId, Node> _nodes = new();

    public PipelineId Id { get; }
    public string Name { get; private set; }

    public Pipeline(PipelineId id, string name)
    {
        Id = id;
        Name = name;
    }

    public IReadOnlyCollection<Node> Nodes => _nodes.Values;

    public Result<NodeId> AddNode(Node node)
    {
        if (_nodes.ContainsKey(node.Id))
            return Result<NodeId>.Fail(Error.Conflict($"Node {node.Id} already exists"));

        _nodes[node.Id] = node;
        return Result<NodeId>.Ok(node.Id);
    }

    public Result RemoveNode(NodeId id)
    {
        if (!_nodes.ContainsKey(id))
            return Result.Fail(Error.NotFound($"Node {id}"));

        _nodes.Remove(id);
        return Result.Ok();
    }

    public Node? FindNode(NodeId id) => _nodes.GetValueOrDefault(id);

    private readonly Dictionary<ConnectionId, Connection> _connections = new();

    public IReadOnlyCollection<Connection> Connections => _connections.Values;

    public Result<ConnectionId> Connect(PortRef from, PortRef to)
    {
        // Обе ноды должны существовать
        if (!_nodes.TryGetValue(from.NodeId, out var fromNode))
            return Result<ConnectionId>.Fail(Error.NotFound($"Node {from.NodeId}"));

        if (!_nodes.TryGetValue(to.NodeId, out var toNode))
            return Result<ConnectionId>.Fail(Error.NotFound($"Node {to.NodeId}"));

        // Порты должны существовать на нодах
        var fromPort = fromNode.Outputs.FirstOrDefault(p => p.Name == from.PortName);
        if (fromPort is null)
            return Result<ConnectionId>.Fail(
                Error.NotFound($"Output port '{from.PortName}' on node {from.NodeId}"));

        var toPort = toNode.Inputs.FirstOrDefault(p => p.Name == to.PortName);
        if (toPort is null)
            return Result<ConnectionId>.Fail(
                Error.NotFound($"Input port '{to.PortName}' on node {to.NodeId}"));

        // Типы должны быть совместимы
        if (!toPort.Type.IsAssignableFrom(fromPort.Type))
            return Result<ConnectionId>.Fail(Error.Validation(
                $"Cannot connect {fromPort.Type} to {toPort.Type}"));

        // У input-порта не должно быть входящего соединения
        if (_connections.Values.Any(c => c.To == to))
            return Result<ConnectionId>.Fail(Error.Conflict(
                $"Input port {to} is already connected"));

        // Нельзя соединять ноду саму с собой
        if (from.NodeId == to.NodeId)
            return Result<ConnectionId>.Fail(Error.Validation(
                "Cannot connect a node to itself"));

        // Создаем соединение
        var connection = new Connection(ConnectionId.New(), from, to);
        _connections[connection.Id] = connection;
        return Result<ConnectionId>.Ok(connection.Id);
    }

    public Result Disconnect(ConnectionId id)
    {
        if (!_connections.Remove(id))
            return Result.Fail(Error.NotFound($"Connection {id}"));

        return Result.Ok();
    }
}

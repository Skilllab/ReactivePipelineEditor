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


    public Result<ExecutionPlan> BuildPlan()
    {
        if (_nodes.Count == 0)
            return Result<ExecutionPlan>.Fail(Error.Validation("Pipeline is empty"));

        // Проверяем, что все input-порты (кроме источников) подключены
        var dangling = FindDanglingInputs();
        if (dangling.Count > 0)
            return Result<ExecutionPlan>.Fail(Error.Validation(
                $"Unconnected inputs: {string.Join(", ", dangling.Select(d => d.ToString()))}"));

        // Топосорт
        var order = TopologicalSort();
        if (order is null)
            return Result<ExecutionPlan>.Fail(Error.Conflict("Pipeline contains cycles"));

        var plan = new ExecutionPlan(
            executionOrder: order,
            nodes: new Dictionary<NodeId, Node>(_nodes),
            connections: _connections.Values.ToArray());

        return Result<ExecutionPlan>.Ok(plan);
    }

    private List<PortRef> FindDanglingInputs()
    {
        var connectedInputs = _connections.Values.Select(c => c.To).ToHashSet();
        var result = new List<PortRef>();

        foreach (var node in _nodes.Values)
            foreach (var port in node.Inputs)
            {
                var portRef = new PortRef(node.Id, port.Name);
                if (!connectedInputs.Contains(portRef))
                    result.Add(portRef);
            }

        return result;
    }

    private List<NodeId>? TopologicalSort()
    {
        var inDegree = _nodes.Keys.ToDictionary(id => id, _ => 0);
        var adjacency = _nodes.Keys.ToDictionary(id => id, _ => new List<NodeId>());

        foreach (var connection in _connections.Values)
        {
            var from = connection.From.NodeId;
            var to = connection.To.NodeId;
            if (from == to) continue;

            adjacency[from].Add(to);
            inDegree[to]++;
        }

        var queue = new Queue<NodeId>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var order = new List<NodeId>(_nodes.Count);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            order.Add(node);

            foreach (var next in adjacency[node])
                if (--inDegree[next] == 0)
                    queue.Enqueue(next);
        }

        return order.Count == _nodes.Count ? order : null;
    }


}

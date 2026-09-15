using ReactivePipelineEditor.Domain.Common;
using ReactivePipelineEditor.Domain.Ports;

namespace ReactivePipelineEditor.Domain.Nodes;

/// <summary>
///Абстрактный узел графа
/// </summary>
public abstract class Node
{
    /// <summary>
    /// Идентификатор узла
    /// </summary>
    public NodeId Id { get; }

    /// <summary>
    /// Имя узла
    /// </summary>
    public string DisplayName { get; private set; }

    /// <summary>
    /// Позиция узла
    /// </summary>
    public Point2D Position { get; set; }

    /// <summary>
    /// Создает экземпляр узла
    /// </summary>
    /// <param name="id">Идентификатор узла</param>
    /// <param name="displayName">Отображаемое имя узла</param>
    /// <param name="position">Позиция узла</param>
    protected Node(NodeId id, string displayName, Point2D position)
    {
        Id = id;
        DisplayName = displayName;
        Position = position;
    }

    // любая нода в системе имеет список портов


    public abstract IReadOnlyList<Port> Inputs { get; }
    public abstract IReadOnlyList<Port> Outputs { get; }
}

using ReactivePipelineEditor.Domain.Common;

namespace ReactivePipelineEditor.Domain.Ports;

/// <summary>
/// Представляет ссылку на порт узла, содержащую идентификатор узла и имя порта
/// </summary>
/// <param name="NodeId">Идентификатор узла, которому принадлежит порт</param>
/// <param name="PortName">Имя порта на указанном узле</param>
public readonly record struct PortRef(NodeId NodeId, PortName PortName)
{
    public override string ToString() => $"{NodeId}.{PortName}";
}

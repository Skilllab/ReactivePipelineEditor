using ReactivePipelineEditor.Domain.Common;

namespace ReactivePipelineEditor.Domain.Ports;

/// <summary>
/// Представляет неизменяемое соединение между двумя портами с уникальным идентификатором
/// </summary>
/// <param name="Id">Уникальный идентификатор соединения.</param>
/// <param name="From">Исходный порт соединения.</param>
/// <param name="To">Целевой (принимающий) порт соединения.</param>
public sealed record Connection(ConnectionId Id, PortRef From, PortRef To);

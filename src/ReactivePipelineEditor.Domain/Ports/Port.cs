namespace ReactivePipelineEditor.Domain.Ports;

/// <summary>
/// Представляет порт с именем, направлением и типом данных
/// </summary>
/// <remarks>record — неизменяемый тип со структурным равенством</remarks>
/// <param name="Name">Имя порта</param>
/// <param name="Direction">Направление порта (например, входной или выходной)</param>
/// <param name="Type">Тип данных, передаваемых через порт</param>
public sealed record Port(PortName Name, PortDirection Direction, DataType Type);

namespace ReactivePipelineEditor.Runtime.Execution;

/// <summary>
/// Опции запуска pipeline.
/// </summary>
public sealed class RunOptions
{
    /// <summary>
    /// Ёмкость bounded-каналов между нодами.
    /// Меньше — сильнее backpressure, меньше памяти.
    /// Больше — слабее backpressure, больше памяти.
    /// </summary>
    public int ChannelCapacity { get; init; } = 100;

    /// <summary>
    /// Таймаут graceful drain при остановке.
    /// После Stop ноды должны завершить текущую работу за это время.
    /// </summary>
    public TimeSpan DrainTimeout { get; init; } = TimeSpan.FromSeconds(5);
}

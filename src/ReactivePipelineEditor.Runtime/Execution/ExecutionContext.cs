using System.Threading.Channels;

using ReactivePipelineEditor.Domain.Ports;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using ReactivePipelineEditor.Domain.Ports;
using ReactivePipelineEditor.Runtime.Metrics;
using ReactivePipelineEditor.Runtime.Retry;

namespace ReactivePipelineEditor.Runtime.Execution;

/// <summary>
/// Контекст выполнения ноды. Даёт доступ к:
/// - входным каналам (откуда читать данные)
/// - выходным каналам (куда писать данные)
/// - метрикам (для observability)
/// - логгеру (для структурированных логов)
/// - retry-политике (для обработки временных сбоев)
///
/// Контекст иммутабелен: после создания его нельзя изменить.
/// Это гарантирует, что executor не подсунет данные не туда.
/// </summary>
public sealed class ExecutionContext
{
    /// <summary>
    /// Входные каналы. Ключ — имя порта (например, "In").
    /// </summary>
    public IReadOnlyDictionary<PortName, ChannelReader<object>> Inputs { get; }

    /// <summary>
    /// Выходные каналы. Ключ — имя порта (например, "Out", "Passed", "Rejected").
    /// </summary>
    public IReadOnlyDictionary<PortName, ChannelWriter<object>> Outputs { get; }

    /// <summary>
    /// Коллектор метрик. Executor вызывает StartTimer, IncrementCounter и т.д.
    /// </summary>
    public IMetricsCollector Metrics { get; }

    /// <summary>
    /// Логгер для структурированных сообщений.
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Политика retry для операций, которые могут временно упасть.
    /// </summary>
    public RetryPolicy Retry { get; }

    public ExecutionContext(
        IReadOnlyDictionary<PortName, ChannelReader<object>> inputs,
        IReadOnlyDictionary<PortName, ChannelWriter<object>> outputs,
        IMetricsCollector metrics,
        ILogger logger,
        RetryPolicy retry)
    {
        Inputs = inputs;
        Outputs = outputs;
        Metrics = metrics;
        Logger = logger;
        Retry = retry;
    }

    /// <summary>
    /// Удобный доступ к входному каналу по имени порта.
    /// Бросает KeyNotFoundException, если порт не найден — это баг конфигурации.
    /// </summary>
    public ChannelReader<object> Input(string portName) =>
        Inputs[PortName.From(portName)];

    /// <summary>
    /// Удобный доступ к выходному каналу по имени порта.
    /// </summary>
    public ChannelWriter<object> Output(string portName) =>
        Outputs[PortName.From(portName)];
}

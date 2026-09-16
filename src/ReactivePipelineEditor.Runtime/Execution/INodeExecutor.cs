namespace ReactivePipelineEditor.Runtime.Execution;

/// <summary>
/// Контракт исполнителя ноды. Каждая конкретная нода имеет свой executor,
/// который знает, как читать из входных каналов и писать в выходные.
/// </summary>
public interface INodeExecutor
{
    /// <summary>
    /// Выполняет ноду до завершения входного потока или отмены.
    /// Отмена кооперативная: executor должен периодически проверять
    /// cancellationToken и корректно завершаться.
    /// </summary>
    Task ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken);
}

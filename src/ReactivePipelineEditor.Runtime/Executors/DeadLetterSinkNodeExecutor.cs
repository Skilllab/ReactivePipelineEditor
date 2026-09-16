using System.Text.Json;

using ReactivePipelineEditor.Domain.Nodes;
using ReactivePipelineEditor.Runtime.Execution;

using ExecutionContext = ReactivePipelineEditor.Runtime.Execution.ExecutionContext;

namespace ReactivePipelineEditor.Runtime.Executors;

/// <summary>
/// Executor для DeadLetterSinkNode: читает RawRecord, пишет в JSONL-файл.
///
/// Формат JSONL (JSON Lines): по одной JSON-записи на строку.
/// Не массив — каждая строка самостоятельна. Это позволяет:
/// - стримить большие файлы построчно
/// - дозаписывать без перезаписи существующего
/// - парсить параллельно
///
/// Каждая dead-letter запись содержит:
/// - timestamp (когда отвергли)
/// - имя ноды, которая отвергла
/// - причину (если есть)
/// - исходные данные (если IncludeSourceData = true)
/// </summary>
public sealed class DeadLetterSinkNodeExecutor : INodeExecutor
{
    private readonly DeadLetterSinkNode _node;
    private readonly string _nodeDisplayName;

    public DeadLetterSinkNodeExecutor(DeadLetterSinkNode node)
    {
        _node = node;
        _nodeDisplayName = node.DisplayName;
    }

    public async Task ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken)
    {
        var input = context.Input(DeadLetterSinkNode.InPortName);

        var directory = Path.GetDirectoryName(_node.FilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using var stream = new FileStream(
            _node.FilePath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read);

        await using var writer = new StreamWriter(stream);

        await foreach (var item in input.ReadAllAsync(cancellationToken))
        {
            var entry = BuildEntry(item);

            var json = JsonSerializer.Serialize(entry);
            await writer.WriteLineAsync(json);
        }

        await writer.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Строит DeadLetterEntry из полученного элемента.
    /// Если IncludeSourceData = true — включает исходную запись.
    /// Причину берём из поля __validation_error, если оно есть
    /// (его добавляет ValidateNode), иначе — generic сообщение.
    /// </summary>
    private DeadLetterEntry BuildEntry(object item)
    {
        var reason = ExtractReason(item);
        var sourceData = _node.IncludeSourceData ? ConvertToRawRecord(item) : null;

        return new DeadLetterEntry(
            Timestamp: DateTimeOffset.UtcNow,
            NodeDisplayName: _nodeDisplayName,
            Reason: reason,
            SourceData: sourceData);
    }

    private static string ExtractReason(object item)
    {
        // ValidateNode добавляет __validation_error
        if (item is Record record)
        {
            var error = record.GetField<string>("__validation_error");
            if (!string.IsNullOrEmpty(error))
                return error;
        }

        // FilterNode отвергает без причины — пишем generic
        return "Rejected by filter";
    }

    private static RawRecord? ConvertToRawRecord(object item)
    {
        return item switch
        {
            RawRecord raw => raw,
            Record record => new RawRecord(
                record.LineNumber,
                record.Fields.Select(kv => $"{kv.Key}={kv.Value}").ToArray(),
                Headers: null),
            _ => null
        };
    }
}

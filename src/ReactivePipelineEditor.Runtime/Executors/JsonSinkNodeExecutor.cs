using System.Text.Json;

using ReactivePipelineEditor.Domain.Nodes;
using ReactivePipelineEditor.Runtime.Execution;

using ExecutionContext = ReactivePipelineEditor.Runtime.Execution.ExecutionContext;

namespace ReactivePipelineEditor.Runtime.Executors;

/// <summary>
/// Executor для JsonSinkNode: читает Aggregate, пишет в JSON-файл.
///
/// Режим Overwrite — создаёт файл заново.
/// Режим Append — дописывает в конец существующего файла
/// (в этом случае формат — JSON Lines, по одному объекту на строку).
///
/// При Indented = true и Overwrite — пишет массив JSON с отступами.
/// При Indented = false — компактный массив без отступов.
/// </summary>
public sealed class JsonSinkNodeExecutor : INodeExecutor
{
    private readonly JsonSinkNode _node;

    public JsonSinkNodeExecutor(JsonSinkNode node) => _node = node;

    public async Task ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken)
    {
        var input = context.Input(JsonSinkNode.InPortName);

        var directory = Path.GetDirectoryName(_node.FilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var append = _node.WriteMode == WriteMode.Append;

        await using var stream = new FileStream(
            _node.FilePath,
            append ? FileMode.Append : FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        await using var writer = new StreamWriter(stream);

        if (append)
        {
            // JSONL: по одному объекту на строку, без общей структуры
            await foreach (var item in input.ReadAllAsync(cancellationToken))
            {
                var aggregate = (Aggregate) item;
                var json = JsonSerializer.Serialize(aggregate);
                await writer.WriteLineAsync(json);
            }
        }
        else
        {
            // JSON-массив
            var options = new JsonSerializerOptions
            {
                WriteIndented = _node.Indented
            };

            await writer.WriteAsync("[");
            var first = true;

            await foreach (var item in input.ReadAllAsync(cancellationToken))
            {
                if (!first)
                    await writer.WriteAsync(",");

                if (_node.Indented)
                    await writer.WriteAsync("\n  ");

                var aggregate = (Aggregate) item;
                var json = JsonSerializer.Serialize(aggregate, options);
                await writer.WriteAsync(json);
                first = false;
            }

            if (_node.Indented && !first)
                await writer.WriteAsync("\n");

            await writer.WriteAsync("]");
        }

        await writer.FlushAsync(cancellationToken);
    }
}

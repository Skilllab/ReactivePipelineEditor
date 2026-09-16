using System.Text;

using ReactivePipelineEditor.Domain.Nodes;
using ReactivePipelineEditor.Runtime.Execution;

using ExecutionContext = ReactivePipelineEditor.Runtime.Execution.ExecutionContext;

namespace ReactivePipelineEditor.Runtime.Executors;

/// <summary>
/// Executor для CsvSourceNode: читает CSV-файл построчно и эмитит
/// RawRecord в выходной канал.
///
/// Особенности:
/// - Поддерживает кодировку (по умолчанию UTF-8)
/// - Поддерживает произвольный разделитель (запятая, точка с запятой, таб)
/// - Читает батчами по BatchSize строк, чтобы не создавать слишком
///   много объектов за раз и снизить нагрузку на GC
/// - Если HasHeader = true — первая строка считается заголовком,
///   поля получают имена; заголовки пробрасываются в каждый RawRecord
/// - Корректно закрывает выходной канал в finally, даже при отмене
///   или исключении — это гарантирует, что downstream не зависнет
///
/// Нода НЕ валидирует содержимое CSV. Если строка битая — просто
/// эмитим как есть с массивом полей. Валидация — задача ValidateNode.
/// </summary>
public sealed class CsvSourceNodeExecutor : INodeExecutor
{
    private readonly CsvSourceNode _node;

    public CsvSourceNodeExecutor(CsvSourceNode node) => _node = node;

    public async Task ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken)
    {
        var output = context.Output(CsvSourceNode.OutPortName);

        try
        {
            await using var stream = File.OpenRead(_node.FilePath);
            using var reader = new StreamReader(stream, Encoding.GetEncoding(_node.Encoding));

            // Если HasHeader = true, первая строка — это имена полей.
            var headers = _node.HasHeader
                ? (await reader.ReadLineAsync(cancellationToken))?.Split(_node.Delimiter)
                : null;

            var batch = new List<RawRecord>(_node.BatchSize);
            var lineNumber = _node.HasHeader ? 1 : 0;

            // Основной цикл: читаем строки асинхронно, пока не дойдем до конца файла (null)
            // и пока не затребована отмена операции.
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null)
                    break; // Достигнут конец файла (EOF)

                lineNumber++;

                var fields = line.Split(_node.Delimiter);
                batch.Add(new RawRecord(lineNumber, fields, headers));

                // Если набрали полный батч — эмитим его и очищаем буфер.
                if (batch.Count >= _node.BatchSize)
                {
                    foreach (var record in batch)
                        await output.WriteAsync(record, cancellationToken);

                    batch.Clear();
                }
            }

            // Эмитим остаток батча (последняя неполная порция), если не было отмены.
            if (!cancellationToken.IsCancellationRequested && batch.Count > 0)
            {
                foreach (var record in batch)
                    await output.WriteAsync(record, cancellationToken);
            }
        }
        finally
        {
            output.Complete();
        }
    }
}

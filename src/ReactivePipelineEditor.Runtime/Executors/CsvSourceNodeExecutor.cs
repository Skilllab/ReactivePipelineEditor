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
            // Читаем её сразу, до основного цикла.
            // null означает, что файл пустой — заголовков нет.
            var headers = _node.HasHeader
                ? (await reader.ReadLineAsync(cancellationToken))?.Split(_node.Delimiter)
                : null;

            // Буфер для батча. Создаём заранее с ёмкостью BatchSize,
            // чтобы избежать реаллокаций при добавлении элементов.
            var batch = new List<RawRecord>(_node.BatchSize);

            // Номер строки. Если был заголовок — начинаем с 1,
            // потому что следующая строка файла имеет номер 2.
            // Если заголовка нет — начинаем с 0, следующая строка = 1.
            var lineNumber = _node.HasHeader ? 1 : 0;

            // Основной цикл: читаем до конца файла или до отмены.
            // Проверяем EndOfStream и cancellationToken на каждой итерации.
            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null) break;

                lineNumber++;

                // Разбиваем строку по разделителю.
                // Split без StringSplitOptions.RemoveEmptyEntries,
                // потому что пустые поля — это валидные данные
                // (например, "1,,A" — три поля, второе пустое).
                var fields = line.Split(_node.Delimiter);

                // Создаём RawRecord. Headers может быть null — это ок,
                // RawRecord.GetField корректно обработает этот случай.
                batch.Add(new RawRecord(lineNumber, fields, headers));

                // Если набрали полный батч — эмитим его и очищаем буфер.
                // Эмитим по одному, потому что выходной канал — поток,
                // а не коллекция. Каждый RawRecord попадает в канал отдельно.
                if (batch.Count >= _node.BatchSize)
                {
                    foreach (var record in batch)
                        await output.WriteAsync(record, cancellationToken);

                    batch.Clear();
                }
            }

            // Эмитим остаток батча (последняя неполная порция).
            // Проверяем, что не отменили — если отмена, не пишем.
            if (!cancellationToken.IsCancellationRequested)
            {
                foreach (var record in batch)
                    await output.WriteAsync(record, cancellationToken);
            }
        }
        finally
        {
            // Закрываем канал в finally — гарантированно, даже при
            // исключении или отмене. Downstream увидит завершение потока
            // и сможет корректно завершиться сам.
            output.Complete();
        }
    }
}

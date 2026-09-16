using System.Globalization;

using ReactivePipelineEditor.Domain.Nodes;
using ReactivePipelineEditor.Runtime.Execution;

using ExecutionContext = ReactivePipelineEditor.Runtime.Execution.ExecutionContext;

namespace ReactivePipelineEditor.Runtime.Executors;

/// <summary>
/// Executor для AggregateNode: буферизует все входящие Record,
/// группирует по ключу, применяет агрегатную функцию, эмитит Aggregate.
///
/// ВАЖНО: буферизует ВСЁ в памяти. Для больших потоков данных
/// это проблема. Позже добавим оконные агрегаты (tumbling, sliding)
/// и внешнее хранилище для state.
///
/// Поддерживаемые агрегаты: Sum, Count, Average, Min, Max.
/// Выражение вида "Sum(Amount)" парсится в (operation, field).
/// </summary>
public sealed class AggregateNodeExecutor : INodeExecutor
{
    private readonly AggregateNode _node;

    public AggregateNodeExecutor(AggregateNode node) => _node = node;

    public async Task ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken)
    {
        var input = context.Input(AggregateNode.InPortName);
        var output = context.Output(AggregateNode.OutPortName);

        try
        {
            // Фаза 1: буферизация всех записей по группам.
            var groups = new Dictionary<string, List<Record>>(StringComparer.OrdinalIgnoreCase);

            await foreach (var item in input.ReadAllAsync(cancellationToken))
            {
                var record = (Record) item;
                var key = ExtractGroupKey(record, _node.GroupBy);

                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<Record>();
                    groups[key] = list;
                }

                list.Add(record);
            }

            // Фаза 2: агрегация и эмит.
            var (operation, fieldName) = ParseAggregation(_node.Aggregation);

            foreach (var (key, records) in groups)
            {
                var value = ApplyAggregation(records, operation, fieldName);
                await output.WriteAsync(new Aggregate(key, value), cancellationToken);
            }
        }
        finally
        {
            output.Complete();
        }
    }

    /// <summary>
    /// Извлекает ключ группировки из записи.
    /// Поддерживает: "FieldName", "Field1+Field2".
    /// </summary>
    private static string ExtractGroupKey(Record record, string groupBy)
    {
        // Составной ключ: "Country+Year"
        if (groupBy.Contains('+'))
        {
            var parts = groupBy.Split('+', StringSplitOptions.TrimEntries);
            var values = parts.Select(p => record.GetField(p)?.ToString() ?? "").ToArray();
            return string.Join("|", values);
        }

        // Простое поле
        return record.GetField(groupBy)?.ToString() ?? "";
    }

    /// <summary>
    /// Парсит выражение агрегата: "Sum(Amount)" → ("Sum", "Amount").
    /// </summary>
    private static (string Operation, string Field) ParseAggregation(string aggregation)
    {
        var openParen = aggregation.IndexOf('(');
        var closeParen = aggregation.IndexOf(')');

        if (openParen < 0 || closeParen < 0 || closeParen <= openParen)
            return ("Count", "");

        var operation = aggregation.Substring(0, openParen).Trim();
        var field = aggregation.Substring(openParen + 1, closeParen - openParen - 1).Trim();

        return (operation, field);
    }

    /// <summary>
    /// Применяет агрегатную функцию к списку записей.
    /// </summary>
    private static object? ApplyAggregation(List<Record> records, string operation, string fieldName)
    {
        return operation.ToUpperInvariant() switch
        {
            "COUNT" => (long) records.Count,
            "SUM" => records.Select(r => r.GetField<double?>(fieldName) ?? 0).Sum(),
            "AVERAGE" => records.Count == 0
                ? 0.0
                : records.Select(r => r.GetField<double?>(fieldName) ?? 0).Average(),
            "MIN" => records.Select(r => r.GetField<double?>(fieldName) ?? double.MaxValue).Min(),
            "MAX" => records.Select(r => r.GetField<double?>(fieldName) ?? double.MinValue).Max(),
            _ => null
        };
    }
}

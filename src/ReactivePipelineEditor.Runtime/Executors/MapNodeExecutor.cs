using System.Globalization;

using ReactivePipelineEditor.Domain.Nodes;
using ReactivePipelineEditor.Runtime.Execution;

using ExecutionContext = ReactivePipelineEditor.Runtime.Execution.ExecutionContext;

namespace ReactivePipelineEditor.Runtime.Executors;

/// <summary>
/// Executor для MapNode: применяет выражение-трансформацию
/// к каждой входящей RawRecord и пишет типизированную Record.
///
/// Упрощённая реализация: поддерживает выражения вида
/// "new { Id, Total = Price * Quantity }" через простой парсер.
///
/// Параллелизм через DegreeOfParallelism не реализован в этой версии —
/// обрабатываем последовательно. Параллельная версия появится, когда
/// будем оптимизировать throughput.
/// </summary>
public sealed class MapNodeExecutor : INodeExecutor
{
    private readonly MapNode _node;

    public MapNodeExecutor(MapNode node) => _node = node;

    public async Task ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken)
    {
        var input = context.Input(MapNode.InPortName);
        var output = context.Output(MapNode.OutPortName);

        try
        {
            await foreach (var item in input.ReadAllAsync(cancellationToken))
            {
                var rawRecord = (RawRecord) item;

                using var timer = context.Metrics.StartTimer(
                    "pipeline.map.duration_ms",
                    new KeyValuePair<string, object?>("node_id", _node.Id.ToString()));

                var mapped = ApplyExpression(rawRecord, _node.Expression);
                await output.WriteAsync(mapped, cancellationToken);
            }
        }
        finally
        {
            output.Complete();
        }
    }

    /// <summary>
    /// Упрощённый парсер выражений. Поддерживает:
    /// - "new { Field1, Field2 }" — выбор полей по имени
    /// - "new { Alias = Field }" — переименование поля
    /// - "new { Alias = Field1 * Field2 }" — простое арифметическое выражение
    ///
    /// Всё, что сложнее — не поддерживается. Runtime логирует warning
    /// и возвращает пустую Record.
    /// </summary>
    private static Record ApplyExpression(RawRecord source, string expression)
    {
        var fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // Ожидаем "new { ... }"
        var trimmed = expression.Trim();
        if (!trimmed.StartsWith("new", StringComparison.OrdinalIgnoreCase))
            return new Record(source.LineNumber, fields);

        var openBrace = trimmed.IndexOf('{');
        var closeBrace = trimmed.LastIndexOf('}');
        if (openBrace < 0 || closeBrace < 0 || closeBrace <= openBrace)
            return new Record(source.LineNumber, fields);

        var body = trimmed.Substring(openBrace + 1, closeBrace - openBrace - 1);
        var assignments = body.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var assignment in assignments)
        {
            var eqIndex = assignment.IndexOf('=');
            if (eqIndex < 0)
            {
                // Просто поле: "Id"
                var fieldName = assignment.Trim();
                fields[fieldName] = ParseFieldValue(source.GetField(fieldName));
            }
            else
            {
                // Переименование или вычисление: "Alias = ..."
                var alias = assignment.Substring(0, eqIndex).Trim();
                var expr = assignment.Substring(eqIndex + 1).Trim();

                var value = EvaluateExpression(source, expr);
                fields[alias] = value;
            }
        }

        return new Record(source.LineNumber, fields);
    }

    /// <summary>
    /// Вычисляет выражение. Поддерживает:
    /// - имя поля: "Amount"
    /// - арифметику: "Price * Quantity", "Amount + Tax"
    /// </summary>
    private static object? EvaluateExpression(RawRecord source, string expr)
    {
        // Арифметика: Field1 OP Field2
        foreach (var op in new[] { '*', '+', '-', '/' })
        {
            var opIndex = expr.IndexOf(op);
            if (opIndex > 0 && opIndex < expr.Length - 1)
            {
                var left = expr.Substring(0, opIndex).Trim();
                var right = expr.Substring(opIndex + 1).Trim();

                var leftVal = ParseDouble(source.GetField(left));
                var rightVal = ParseDouble(source.GetField(right));

                if (leftVal.HasValue && rightVal.HasValue)
                {
                    return op switch
                    {
                        '*' => leftVal.Value * rightVal.Value,
                        '+' => leftVal.Value + rightVal.Value,
                        '-' => leftVal.Value - rightVal.Value,
                        '/' when rightVal.Value != 0 => leftVal.Value / rightVal.Value,
                        _ => null
                    };
                }
            }
        }

        // Просто поле
        return ParseFieldValue(source.GetField(expr));
    }

    /// <summary>
    /// Парсит значение поля: сначала как число, потом как строку.
    /// </summary>
    private static object? ParseFieldValue(string? raw)
    {
        if (raw is null) return null;

        if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
            return number;

        return raw;
    }

    private static double? ParseDouble(string? raw)
    {
        if (raw is null) return null;
        return double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
    }
}

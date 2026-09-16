using System.Globalization;

using ReactivePipelineEditor.Domain.Nodes;
using ReactivePipelineEditor.Runtime.Execution;

using ExecutionContext = ReactivePipelineEditor.Runtime.Execution.ExecutionContext;

namespace ReactivePipelineEditor.Runtime.Executors;

/// <summary>
/// Executor для FilterNode: применяет предикат к каждой RawRecord.
/// Записи, прошедшие предикат, идут в "Passed", остальные — в "Rejected".
///
/// Особенности:
/// - Два выходных канала, оба закрываются в finally
/// - Упрощённый парсер предикатов (подробнее в EvaluatePredicate)
/// - Если предикат не распознан — запись считается прошедшей
///   (fail-open). Это осознанный выбор: лучше пропустить всё,
///   чем отбросить всё из-за опечатки в выражении
///
/// Нода НЕ модифицирует записи — только распределяет их по каналам.
/// Модификация — задача MapNode.
/// </summary>
public sealed class FilterNodeExecutor : INodeExecutor
{
    private readonly FilterNode _node;

    public FilterNodeExecutor(FilterNode node) => _node = node;

    public async Task ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken)
    {
        var input = context.Input(FilterNode.InPortName);
        var passed = context.Output(FilterNode.PassedPortName);
        var rejected = context.Output(FilterNode.RejectedPortName);

        try
        {
            // ReadAllAsync — extension-метод из System.Threading.Channels,
            // превращает ChannelReader<T> в IAsyncEnumerable<T>.
            // Это позволяет использовать await foreach вместо
            // ручного цикла с WaitToReadAsync + TryRead.
            await foreach (var item in input.ReadAllAsync(cancellationToken))
            {
                // item имеет тип object, потому что канал типизирован как object.
                // Приводим к RawRecord — это контракт: FilterNode принимает
                // только RawRecord (и его наследников, но у нас их нет).
                var record = (RawRecord) item;

                // Оборачиваем вычисление предиката в таймер метрики.
                // using гарантирует, что метрика запишется даже при исключении.
                using var timer = context.Metrics.StartTimer(
                    "pipeline.filter.duration_ms",
                    new KeyValuePair<string, object?>("node_id", _node.Id.ToString()));

                var isPassed = EvaluatePredicate(record, _node.Predicate);

                // Распределяем запись в соответствующий канал.
                // await WriteAsync гарантирует, что запись попала в канал
                // до того, как читаем следующую. Если канал полон —
                // ждём, пока downstream освободит место (backpressure).
                if (isPassed)
                    await passed.WriteAsync(record, cancellationToken);
                else
                    await rejected.WriteAsync(record, cancellationToken);
            }
        }
        finally
        {
            // Закрываем ОБА выходных канала. Если один из них не закрыть,
            // соответствующая ветка downstream зависнет навсегда.
            // Порядок закрытия не важен, но оба обязательны.
            passed.Complete();
            rejected.Complete();
        }
    }

    /// <summary>
    /// Упрощённый парсер предикатов. Поддерживает только следующие форматы:
    /// - Field &gt; number
    /// - Field &lt; number
    /// - Field &gt;= number
    /// - Field &lt;= number
    /// Для всех остальных случаев поведение — fail-open: предикат возвращает true.
    /// Это осознанное решение: если пользователь допустил опечатку в предикате,
    /// система пропустит записи, а не отбросит их молча. Сигнал к действию
    /// (например, проверка логов) появится в dead-letter: там будет отмечено,
    /// что все записи по какой-то причине не прошли фильтр. Так проще заметить
    /// и разобрать проблему.
    /// </summary>
    private static bool EvaluatePredicate(RawRecord record, string predicate)
    {
        if (string.IsNullOrWhiteSpace(predicate))
            return true;

        // Ищем оператор в порядке от длинных к коротким.
        // Это важно: ">=" должно проверяться раньше ">",
        // "==" и "!=" — раньше "=" (которого у нас нет, но всё же).
        foreach (var op in new[] { ">=", "<=", "==", "!=", ">", "<" })
        {
            var opIndex = predicate.IndexOf(op, StringComparison.Ordinal);
            if (opIndex <= 0) continue;

            var left = predicate.Substring(0, opIndex).Trim();
            var right = predicate.Substring(opIndex + op.Length).Trim();

            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
                continue;

            var leftValue = record.GetField(left);
            if (leftValue is null)
                continue;

            // Пробуем сравнить как числа. Если не получилось — как строки.
            if (double.TryParse(leftValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var leftNum)
                && double.TryParse(StripQuotes(right), NumberStyles.Any, CultureInfo.InvariantCulture, out var rightNum))
            {
                return CompareNumbers(leftNum, rightNum, op);
            }

            // Сравнение строк (с учётом кавычек в правой части).
            return CompareStrings(leftValue, StripQuotes(right), op);
        }

        // Не распознали предикат — fail-open.
        return true;
    }

    /// <summary>
    /// Убирает обрамляющие кавычки из строкового литерала.
    /// "RU" → RU. Нужно для предикатов вида Country == "RU".
    /// </summary>
    private static string StripQuotes(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            return value.Substring(1, value.Length - 2);
        return value;
    }

    private static bool CompareNumbers(double left, double right, string op) => op switch
    {
        ">=" => left >= right,
        "<=" => left <= right,
        "==" => Math.Abs(left - right) < double.Epsilon,
        "!=" => Math.Abs(left - right) >= double.Epsilon,
        ">" => left > right,
        "<" => left < right,
        _ => true
    };

    private static bool CompareStrings(string left, string right, string op) => op switch
    {
        "==" => string.Equals(left, right, StringComparison.Ordinal),
        "!=" => !string.Equals(left, right, StringComparison.Ordinal),
        // Для строк > и < имеют мало смысла, но пусть будут —
        // лексикографическое сравнение.
        ">" => string.CompareOrdinal(left, right) > 0,
        "<" => string.CompareOrdinal(left, right) < 0,
        ">=" => string.CompareOrdinal(left, right) >= 0,
        "<=" => string.CompareOrdinal(left, right) <= 0,
        _ => true
    };
}

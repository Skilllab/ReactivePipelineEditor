using ReactivePipelineEditor.Domain.Nodes;
using ReactivePipelineEditor.Runtime.Execution;

using ExecutionContext = ReactivePipelineEditor.Runtime.Execution.ExecutionContext;

namespace ReactivePipelineEditor.Runtime.Executors;

/// <summary>
/// Executor для ValidateNode: проверяет каждую Record по схеме.
///
/// Схема задаётся именем (SchemaName). В этой версии поддерживается
/// одна встроенная схема "OrderSchema" с правилами:
/// - поле Id обязательно и не пустое
/// - поле Amount обязательно и > 0
///
/// Валидные записи идут в "Valid", невалидные — в "Invalid"
/// с добавлением поля "__validation_error".
/// </summary>
public sealed class ValidateNodeExecutor : INodeExecutor
{
    private readonly ValidateNode _node;

    public ValidateNodeExecutor(ValidateNode node) => _node = node;

    public async Task ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken)
    {
        var input = context.Input(ValidateNode.InPortName);
        var valid = context.Output(ValidateNode.ValidPortName);
        var invalid = context.Output(ValidateNode.InvalidPortName);

        try
        {
            await foreach (var item in input.ReadAllAsync(cancellationToken))
            {
                var record = (Record) item;

                using var timer = context.Metrics.StartTimer(
                    "pipeline.validate.duration_ms",
                    new KeyValuePair<string, object?>("node_id", _node.Id.ToString()));

                var result = Validate(record, _node.SchemaName);

                if (result.IsValid)
                {
                    await valid.WriteAsync(record, cancellationToken);
                }
                else
                {
                    var annotated = AnnotateWithError(record, result.Error!);
                    await invalid.WriteAsync(annotated, cancellationToken);
                }
            }
        }
        finally
        {
            valid.Complete();
            invalid.Complete();
        }
    }

    private static ValidationResult Validate(Record record, string schemaName)
    {
        // Пока одна встроенная схема. Позже — реестр схем.
        if (schemaName == "OrderSchema")
        {
            var id = record.GetField<string>("Id");
            if (string.IsNullOrWhiteSpace(id))
                return ValidationResult.Fail("Field 'Id' is required");

            var amount = record.GetField<double?>("Amount");
            if (amount is null)
                return ValidationResult.Fail("Field 'Amount' is required");

            if (amount <= 0)
                return ValidationResult.Fail($"Field 'Amount' must be positive, got {amount}");

            return ValidationResult.Pass();
        }

        // Неизвестная схема — считаем валидной (fail-open).
        // Альтернатива — fail-closed, но это заблокирует pipeline
        // при опечатке в имени схемы. Выбор осознанный.
        return ValidationResult.Pass();
    }

    private static Record AnnotateWithError(Record record, string error)
    {
        var fields = new Dictionary<string, object?>(record.Fields, StringComparer.OrdinalIgnoreCase)
        {
            ["__validation_error"] = error
        };
        return new Record(record.LineNumber, fields);
    }

    private readonly record struct ValidationResult(bool IsValid, string? Error)
    {
        public static ValidationResult Pass() => new(true, null);
        public static ValidationResult Fail(string error) => new(false, error);
    }
}

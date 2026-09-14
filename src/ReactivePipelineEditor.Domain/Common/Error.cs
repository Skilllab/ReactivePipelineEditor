namespace ReactivePipelineEditor.Domain.Common;

/// <summary>
/// Представляет ошибку, которая может возникнуть при выполнении операции
/// </summary>
/// <param name="Code">Код ошибки</param>
/// <param name="Message">Сообщение об ошибке</param>
public readonly record struct Error(string Code, string Message)
{
    public static Error NotFound(string what) => new("not_found", $"{what} не найден");
    public static Error Conflict(string message) => new("conflict", message);
    public static Error Validation(string message) => new("validation", message);
}

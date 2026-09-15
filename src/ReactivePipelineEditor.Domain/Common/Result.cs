namespace ReactivePipelineEditor.Domain.Common;

/// <summary>
/// Представляет результат операции, указывающий, выполнена ли она успешно, и содержащий информацию об ошибке при
/// неудаче.
/// </summary>
public readonly struct Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }

    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Ok() => new(true, null);
    public static Result Fail(Error error) => new(false, error);
}

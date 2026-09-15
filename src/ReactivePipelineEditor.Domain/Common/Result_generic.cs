namespace ReactivePipelineEditor.Domain.Common;

/// <summary>
/// Представляет результат операции, которая может завершиться успешно или с ошибкой
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct Result<T>
{
    private readonly T? _value;

    /// <summary>
    /// Возвращает true, если операция завершилась успешно, иначе false
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Возвращает объект ошибки, если операция завершилась с ошибкой, иначе null
    /// </summary>
    public Error? Error { get; }

    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    /// <summary>
    /// Создает успешный результат с указанным значением
    /// </summary>
    /// <param name="value">Значение результата</param>
    public static Result<T> Ok(T value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value), "Успешный результат обязан содержать значение");

        return new(true, value, null);
    }


    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Отсутствует значение ошибки");

    /// <summary>
    /// Создает неуспешный результат с указанной ошибкой
    /// </summary>
    /// <param name="error">Описание ошибки</param>
    public static Result<T> Fail(Error error) => new(false, default, error);
}

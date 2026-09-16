using Microsoft.Extensions.Logging;

namespace ReactivePipelineEditor.Runtime.Retry;

/// <summary>
/// Политика повторных попыток с exponential backoff.
///
/// Использует TimeProvider, а не Task.Delay напрямую — это позволяет
/// тестировать retry с FakeTimeProvider без реальных задержек.
/// </summary>
public sealed class RetryPolicy
{
    /// <summary>
    /// Максимальное число попыток (включая первую).
    /// MaxAttempts = 1 означает «не повторять».
    /// </summary>
    public int MaxAttempts { get; }

    /// <summary>
    /// Базовая задержка перед первой повторной попыткой.
    /// </summary>
    public TimeSpan BaseDelay { get; }

    /// <summary>
    /// Множитель для exponential backoff.
    /// Задержка перед n-й попыткой = BaseDelay * BackoffMultiplier^(n-1).
    /// </summary>
    public double BackoffMultiplier { get; }

    /// <summary>
    /// Максимальная задержка. Ограничивает рост exponential backoff.
    /// </summary>
    public TimeSpan MaxDelay { get; }

    private readonly TimeProvider _timeProvider;

    public RetryPolicy(
        int maxAttempts = 3,
        TimeSpan? baseDelay = null,
        double backoffMultiplier = 2.0,
        TimeSpan? maxDelay = null,
        TimeProvider? timeProvider = null)
    {
        if (maxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "MaxAttempts must be >= 1");
        if (backoffMultiplier < 1.0)
            throw new ArgumentOutOfRangeException(nameof(backoffMultiplier), "BackoffMultiplier must be >= 1.0");

        MaxAttempts = maxAttempts;
        BaseDelay = baseDelay ?? TimeSpan.FromMilliseconds(200);
        BackoffMultiplier = backoffMultiplier;
        MaxDelay = maxDelay ?? TimeSpan.FromSeconds(30);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Вычисляет задержку перед попыткой с номером attempt (1-based).
    /// attempt = 1 — первая попытка, задержка не нужна.
    /// attempt = 2 — первая повторная, задержка = BaseDelay.
    /// attempt = 3 — вторая повторная, задержка = BaseDelay * BackoffMultiplier.
    /// </summary>
    public TimeSpan GetDelay(int attempt)
    {
        if (attempt <= 1) return TimeSpan.Zero;

        var delay = BaseDelay * Math.Pow(BackoffMultiplier, attempt - 2);
        return delay > MaxDelay ? MaxDelay : delay;
    }

    /// <summary>
    /// Выполняет операцию с retry. Логирует каждую неудачную попытку.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                logger.LogWarning(
                    ex,
                    "Attempt {Attempt}/{MaxAttempts} failed: {Message}",
                    attempt, MaxAttempts, ex.Message);

                if (attempt < MaxAttempts)
                {
                    var delay = GetDelay(attempt + 1);
                    await Task.Delay(delay, _timeProvider, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        throw new InvalidOperationException(
            $"Operation failed after {MaxAttempts} attempts", lastException);
    }
}

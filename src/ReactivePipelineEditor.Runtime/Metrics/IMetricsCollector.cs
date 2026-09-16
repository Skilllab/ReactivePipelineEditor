namespace ReactivePipelineEditor.Runtime.Metrics;

/// <summary>
/// Коллектор метрик. Executor вызывает методы по мере выполнения.
///
/// Интерфейс абстрагирует конкретную реализацию: в продакшене — Meter,
/// в тестах — in-memory коллектор без оверхеда.
/// </summary>
public interface IMetricsCollector
{
    /// <summary>
    /// Начинает таймер. Dispose возвращает длительность.
    /// </summary>
    IDisposable StartTimer(string name, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Инкрементирует счётчик.
    /// </summary>
    void IncrementCounter(string name, long delta = 1, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Устанавливает текущее значение для gauge (например, queue depth).
    /// </summary>
    void SetGauge(string name, long value, params KeyValuePair<string, object?>[] tags);
}

namespace ReactivePipelineEditor.Runtime.Metrics;

/// <summary>
/// No-op коллектор для тестов. Не делает ничего, нулевой overhead.
/// </summary>
public sealed class NullMetricsCollector : IMetricsCollector
{
    public static readonly NullMetricsCollector Instance = new();

    public IDisposable StartTimer(string name, params KeyValuePair<string, object?>[] tags)
        => NullScope.Instance;

    public void IncrementCounter(string name, long delta = 1, params KeyValuePair<string, object?>[] tags) { }

    public void SetGauge(string name, long value, params KeyValuePair<string, object?>[] tags) { }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

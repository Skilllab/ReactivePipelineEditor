using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ReactivePipelineEditor.Runtime.Metrics;

/// <summary>
/// Реализация IMetricsCollector на базе System.Diagnostics.Metrics.Meter.
///
/// Meter — стандарт .NET для метрик. Совместим с OpenTelemetry,
/// Prometheus, Application Insights и другими экспортёрами.
/// </summary>
public sealed class RuntimeMetrics : IMetricsCollector, IDisposable
{
    private readonly Meter _meter;
    private readonly Dictionary<string, Counter<long>> _counters = new();
    private readonly Dictionary<string, Histogram<double>> _histograms = new();
    private readonly Dictionary<string, UpDownCounter<long>> _gauges = new();

    public RuntimeMetrics(string meterName = "ReactivePipelineEditor")
    {
        _meter = new Meter(meterName);
    }

    public IDisposable StartTimer(string name, params KeyValuePair<string, object?>[] tags)
    {
        if (!_histograms.TryGetValue(name, out var histogram))
        {
            histogram = _meter.CreateHistogram<double>(name, unit: "ms");
            _histograms[name] = histogram;
        }

        return new TimerScope(histogram, tags);
    }

    public void IncrementCounter(string name, long delta = 1, params KeyValuePair<string, object?>[] tags)
    {
        if (!_counters.TryGetValue(name, out var counter))
        {
            counter = _meter.CreateCounter<long>(name);
            _counters[name] = counter;
        }

        counter.Add(delta, tags);
    }

    public void SetGauge(string name, long value, params KeyValuePair<string, object?>[] tags)
    {
        if (!_gauges.TryGetValue(name, out var gauge))
        {
            gauge = _meter.CreateUpDownCounter<long>(name);
            _gauges[name] = gauge;
        }

        gauge.Add(value, tags);
    }

    public void Dispose() => _meter.Dispose();

    private sealed class TimerScope : IDisposable
    {
        private readonly Histogram<double> _histogram;
        private readonly KeyValuePair<string, object?>[] _tags;
        private readonly long _startTimestamp;

        public TimerScope(Histogram<double> histogram, KeyValuePair<string, object?>[] tags)
        {
            _histogram = histogram;
            _tags = tags;
            _startTimestamp = Stopwatch.GetTimestamp();
        }

        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            _histogram.Record(elapsed.TotalMilliseconds, _tags);
        }
    }
}

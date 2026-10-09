using PadLab.Core.Model;

namespace PadLab.Core.Analysis;

public sealed record PollingStats(double Hz, double MeanIntervalMs, double MaxIntervalMs, double JitterMs, int Samples);

/// <summary>直近 window 内のタイムスタンプからポーリングレートとジッターを出す。</summary>
public sealed class PollingRateMeter : IAnalyzer
{
    private readonly object _gate = new();
    private readonly Queue<long> _stamps = new();
    private readonly long _windowNs;

    public PollingRateMeter(long windowNs = 1_000_000_000) => _windowNs = windowNs;

    public void Feed(GamepadSnapshot snapshot)
    {
        lock (_gate)
        {
            _stamps.Enqueue(snapshot.TimestampNs);
            while (_stamps.Count > 0 && snapshot.TimestampNs - _stamps.Peek() > _windowNs) _stamps.Dequeue();
        }
    }

    public void Reset() { lock (_gate) _stamps.Clear(); }

    public PollingStats Stats
    {
        get
        {
            long[] t;
            lock (_gate) t = _stamps.ToArray();
            if (t.Length < 2) return new PollingStats(0, 0, 0, 0, t.Length);
            var n = t.Length - 1;
            double sum = 0, max = 0;
            for (var i = 0; i < n; i++) { var d = Clock.ToMs(t[i + 1] - t[i]); sum += d; if (d > max) max = d; }
            var mean = sum / n;
            double var = 0;
            for (var i = 0; i < n; i++) { var d = Clock.ToMs(t[i + 1] - t[i]) - mean; var += d * d; }
            return new PollingStats(1000.0 / mean, mean, max, Math.Sqrt(var / n), t.Length);
        }
    }
}

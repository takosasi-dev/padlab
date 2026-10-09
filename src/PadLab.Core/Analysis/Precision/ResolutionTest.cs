using PadLab.Core.Model;

namespace PadLab.Core.Analysis.Precision;

public sealed record ResolutionResult(TestPhase Phase, float MinStep, float NoiseLevel, int DistinctLevels, double ElapsedSeconds);

/// <summary>
/// 一定時間の |v| 列から分解能の目安を出す。
/// MinStep: 連続差 (>0) の最小値。NoiseLevel: 一次差分の標準偏差。DistinctLevels: 1/4096 で量子化した種類数。
/// </summary>
public sealed class ResolutionTest : IAnalyzer
{
    private readonly object _gate = new();
    private readonly StickId _stick;
    private readonly double _seconds;
    private readonly List<float> _diffs = new();
    private readonly HashSet<int> _levels = new();
    private TestPhase _phase;
    private long _startNs = -1, _lastNs;
    private float _prev = float.NaN, _minStep = float.MaxValue;

    public ResolutionTest(StickId stick, double seconds = 5)
    {
        _stick = stick;
        _seconds = seconds;
    }

    public void Start() { lock (_gate) { ResetCore(); _phase = TestPhase.Running; } }

    public void Feed(GamepadSnapshot snapshot)
    {
        var r = snapshot.Stick(_stick).Length;
        lock (_gate)
        {
            if (_phase != TestPhase.Running) return;
            if (_startNs < 0) _startNs = snapshot.TimestampNs;
            _lastNs = snapshot.TimestampNs;
            _levels.Add((int)MathF.Round(r * 4096));
            if (!float.IsNaN(_prev))
            {
                var d = r - _prev;
                _diffs.Add(d);
                var ad = MathF.Abs(d);
                if (ad > 0 && ad < _minStep) _minStep = ad;
            }
            _prev = r;
            if ((_lastNs - _startNs) / 1e9 >= _seconds) _phase = TestPhase.Complete;
        }
    }

    public void Reset() { lock (_gate) ResetCore(); }

    private void ResetCore()
    {
        _phase = TestPhase.Idle;
        _diffs.Clear(); _levels.Clear();
        _startNs = -1; _lastNs = 0;
        _prev = float.NaN; _minStep = float.MaxValue;
    }

    public ResolutionResult Result
    {
        get
        {
            lock (_gate)
            {
                float noise = 0;
                if (_diffs.Count > 1)
                {
                    var mean = _diffs.Average();
                    noise = MathF.Sqrt(_diffs.Sum(d => (d - mean) * (d - mean)) / _diffs.Count);
                }
                return new ResolutionResult(_phase, _minStep == float.MaxValue ? 0 : _minStep, noise, _levels.Count,
                    _startNs < 0 ? 0 : (_lastNs - _startNs) / 1e9);
            }
        }
    }
}

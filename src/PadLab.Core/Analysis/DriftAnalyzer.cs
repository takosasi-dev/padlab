using PadLab.Core.Model;

namespace PadLab.Core.Analysis;

public sealed record DriftThresholds(float MinorDistance = 0.03f, float NotableDistance = 0.08f, double CalibrationSeconds = 3);

public enum DriftPhase { Idle, Calibrating, Monitoring }

public sealed record DriftResult(
    DriftPhase Phase,
    Vector2F Center,
    float MaxDistance,
    float MeanDistance,
    float DominantAngleDeg,
    int ExceedCount,
    Verdict Verdict,
    IReadOnlyList<Vector2F> Points,
    double ElapsedSeconds);

/// <summary>キャリブレーション (平均で基準点を決める) → 監視 (基準点からのズレを集計)。</summary>
public sealed class DriftAnalyzer : IAnalyzer
{
    private readonly object _gate = new();
    private readonly StickId _stick;
    private readonly int _maxPoints;
    private readonly Queue<Vector2F> _points = new();
    private DriftThresholds _thresholds;

    private DriftPhase _phase;
    private long _phaseStartNs = -1;
    private double _sumX, _sumY;
    private int _calSamples;
    private Vector2F _center;
    private float _max, _dominantAngle;
    private double _distSum;
    private int _monSamples, _exceed;
    private bool _exceeding;
    private long _lastNs;

    public DriftAnalyzer(StickId stick, DriftThresholds? thresholds = null, int maxPoints = 3000)
    {
        _stick = stick;
        _thresholds = thresholds ?? new DriftThresholds();
        _maxPoints = maxPoints;
    }

    public DriftThresholds Thresholds
    {
        get { lock (_gate) return _thresholds; }
        set { lock (_gate) _thresholds = value; }
    }

    public void StartCalibration()
    {
        lock (_gate)
        {
            ResetCore();
            _phase = DriftPhase.Calibrating;
        }
    }

    public void Feed(GamepadSnapshot snapshot)
    {
        var v = snapshot.Stick(_stick);
        lock (_gate)
        {
            if (_phase == DriftPhase.Idle) return;
            if (_phaseStartNs < 0) _phaseStartNs = snapshot.TimestampNs;
            _lastNs = snapshot.TimestampNs;

            if (_phase == DriftPhase.Calibrating)
            {
                var elapsed = (snapshot.TimestampNs - _phaseStartNs) / 1e9;
                if (elapsed < _thresholds.CalibrationSeconds || _calSamples == 0)
                {
                    _sumX += v.X; _sumY += v.Y; _calSamples++;
                    return;
                }
                _center = new Vector2F((float)(_sumX / _calSamples), (float)(_sumY / _calSamples));
                _phase = DriftPhase.Monitoring;
                _phaseStartNs = snapshot.TimestampNs;
            }

            var off = v - _center;
            var d = off.Length;
            _monSamples++;
            _distSum += d;
            if (d > _max) { _max = d; _dominantAngle = off.AngleDeg; }
            var over = d >= _thresholds.MinorDistance;
            if (over && !_exceeding) _exceed++;
            _exceeding = over;
            _points.Enqueue(off);
            while (_points.Count > _maxPoints) _points.Dequeue();
        }
    }

    public void Reset() { lock (_gate) ResetCore(); }

    private void ResetCore()
    {
        _phase = DriftPhase.Idle;
        _phaseStartNs = -1; _lastNs = 0;
        _sumX = _sumY = 0; _calSamples = 0;
        _center = Vector2F.Zero;
        _max = _dominantAngle = 0; _distSum = 0;
        _monSamples = _exceed = 0; _exceeding = false;
        _points.Clear();
    }

    public DriftResult Result
    {
        get
        {
            lock (_gate)
            {
                var verdict = _max >= _thresholds.NotableDistance ? Verdict.Notable
                    : _max >= _thresholds.MinorDistance ? Verdict.Minor : Verdict.None;
                var elapsed = _phase == DriftPhase.Monitoring && _phaseStartNs >= 0 ? (_lastNs - _phaseStartNs) / 1e9 : 0;
                return new DriftResult(_phase, _center, _max, _monSamples == 0 ? 0 : (float)(_distSum / _monSamples),
                    _dominantAngle, _exceed, verdict, _points.ToArray(), elapsed);
            }
        }
    }
}

using PadLab.Core.Model;

namespace PadLab.Core.Analysis.Precision;

public sealed record SnapbackSample(double ReturnMs, float Overshoot, float ReleaseAngleDeg);
public sealed record SnapbackResult(TestPhase Phase, int Releases, int TargetReleases, double MeanReturnMs, double MaxReturnMs, float MaxOvershoot, IReadOnlyList<SnapbackSample> Samples);

/// <summary>
/// 倒して離した時の戻り時間とオーバーシュート。
/// |v|>0.8 で「倒し」、その後 |v|<0.15 になるか倒し方向と逆向きになった瞬間を「解放」、
/// 解放から |v|<0.1 が 50ms 続いた時点までを戻り時間とする。
/// </summary>
public sealed class SnapbackTest : IAnalyzer
{
    private const float DeflectRadius = 0.8f, ReleaseRadius = 0.15f, SettleRadius = 0.1f;
    private const long SettleNs = 50 * Clock.NsPerMs;
    private enum State { Idle, Deflected, Returning }

    private readonly object _gate = new();
    private readonly StickId _stick;
    private readonly int _target;
    private readonly List<SnapbackSample> _samples = new();
    private TestPhase _phase;
    private State _state;
    private Vector2F _dir;
    private float _releaseAngle, _overshoot;
    private long _releaseNs, _settleStartNs = -1;

    public SnapbackTest(StickId stick, int targetReleases = 5)
    {
        _stick = stick;
        _target = targetReleases;
    }

    public void Start() { lock (_gate) { ResetCore(); _phase = TestPhase.Running; } }

    public void Feed(GamepadSnapshot snapshot)
    {
        var v = snapshot.Stick(_stick);
        var r = v.Length;
        var ts = snapshot.TimestampNs;
        lock (_gate)
        {
            if (_phase != TestPhase.Running) return;
            if (_state == State.Idle)
            {
                if (r > DeflectRadius) { _state = State.Deflected; SetDir(v, r); }
                return;
            }
            var dot = v.X * _dir.X + v.Y * _dir.Y;
            if (_state == State.Deflected)
            {
                if (r > DeflectRadius) { SetDir(v, r); return; }
                if (r >= ReleaseRadius && dot >= 0) return;
                _state = State.Returning;
                _releaseNs = ts;
                _overshoot = 0;
                _settleStartNs = -1;
            }
            // Returning
            if (r > DeflectRadius) { _state = State.Deflected; SetDir(v, r); return; }
            if (-dot > _overshoot) _overshoot = -dot;
            if (r >= SettleRadius) { _settleStartNs = -1; return; }
            if (_settleStartNs < 0) _settleStartNs = ts;
            if (ts - _settleStartNs < SettleNs) return;
            _samples.Add(new SnapbackSample(Clock.ToMs(_settleStartNs - _releaseNs), _overshoot, _releaseAngle));
            _state = State.Idle;
            if (_samples.Count >= _target) _phase = TestPhase.Complete;
        }
    }

    private void SetDir(Vector2F v, float r)
    {
        _dir = new Vector2F(v.X / r, v.Y / r);
        _releaseAngle = v.AngleDeg;
    }

    public void Reset() { lock (_gate) ResetCore(); }

    private void ResetCore()
    {
        _phase = TestPhase.Idle;
        _state = State.Idle;
        _samples.Clear();
        _settleStartNs = -1;
        _overshoot = 0;
    }

    public SnapbackResult Result
    {
        get
        {
            lock (_gate)
            {
                var s = _samples.ToArray();
                return new SnapbackResult(_phase, s.Length, _target,
                    s.Length == 0 ? 0 : s.Average(x => x.ReturnMs),
                    s.Length == 0 ? 0 : s.Max(x => x.ReturnMs),
                    s.Length == 0 ? 0 : s.Max(x => x.Overshoot), s);
            }
        }
    }
}

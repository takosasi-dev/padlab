using PadLab.Core.Model;

namespace PadLab.Core.Analysis.Precision;

public sealed record LinearityResult(TestPhase Phase, IReadOnlyList<Vector2F> Curve, int JumpCount, int ReversalCount, float MaxJump);

/// <summary>
/// ゆっくり倒した時の応答カーブ。|v|>0.05 で記録開始、|v|>0.95 で完了。
/// 連続サンプルの |v| 差が jumpThreshold 超で Jump、0.01 超の減少で Reversal。
/// </summary>
public sealed class LinearityTest : IAnalyzer
{
    private const float StartRadius = 0.05f, EndRadius = 0.95f, NoiseFloor = 0.01f;
    private readonly object _gate = new();
    private readonly StickId _stick;
    private readonly float _jumpThreshold;
    private readonly List<(long Ns, float R)> _curve = new();
    private TestPhase _phase;
    private int _jumps, _reversals;
    private float _maxJump;

    public LinearityTest(StickId stick, float jumpThreshold = 0.12f)
    {
        _stick = stick;
        _jumpThreshold = jumpThreshold;
    }

    public void Start() { lock (_gate) { ResetCore(); _phase = TestPhase.Running; } }

    public void Feed(GamepadSnapshot snapshot)
    {
        var r = snapshot.Stick(_stick).Length;
        lock (_gate)
        {
            if (_phase != TestPhase.Running) return;
            if (_curve.Count == 0)
            {
                if (r > StartRadius) _curve.Add((snapshot.TimestampNs, r));
                return;
            }
            var d = r - _curve[^1].R;
            _curve.Add((snapshot.TimestampNs, r));
            if (d > _jumpThreshold) _jumps++;
            if (d > _maxJump) _maxJump = d;
            if (d < -NoiseFloor) _reversals++;
            if (r > EndRadius) _phase = TestPhase.Complete;
        }
    }

    public void Reset() { lock (_gate) ResetCore(); }

    private void ResetCore()
    {
        _phase = TestPhase.Idle;
        _curve.Clear();
        _jumps = _reversals = 0;
        _maxJump = 0;
    }

    public LinearityResult Result
    {
        get
        {
            lock (_gate)
            {
                var curve = new Vector2F[_curve.Count];
                if (curve.Length > 0)
                {
                    long t0 = _curve[0].Ns, span = _curve[^1].Ns - t0;
                    for (var i = 0; i < curve.Length; i++)
                        curve[i] = new Vector2F(span == 0 ? 0 : (float)((_curve[i].Ns - t0) / (double)span), _curve[i].R);
                }
                return new LinearityResult(_phase, curve, _jumps, _reversals, _maxJump);
            }
        }
    }
}

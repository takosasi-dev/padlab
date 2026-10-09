using PadLab.Core.Model;

namespace PadLab.Core.Analysis.Precision;

public enum TestPhase { Idle, Running, Complete }

public sealed record CircleResult(TestPhase Phase, float[] RadiusBySector, float MeanRadius, float MinRadius, float MaxRadius,
    float Roundness, float AsymmetryX, float AsymmetryY, int TurnsDetected,
    IReadOnlyList<Vector2F> Trail);

/// <summary>外周を回した時の角度別到達半径。requiredTurns 周 (累積角度) で完了。</summary>
public sealed class CircleTest : IAnalyzer
{
    private const int TrailCapacity = 2000;
    private readonly object _gate = new();
    private readonly StickId _stick;
    private readonly int _requiredTurns;
    private readonly float[] _radius;
    private readonly Queue<Vector2F> _trail = new();
    private TestPhase _phase;
    private float _prevAngle = float.NaN;
    private double _cumulativeDeg;

    public CircleTest(StickId stick, int sectors = 36, int requiredTurns = 1)
    {
        _stick = stick;
        _requiredTurns = requiredTurns;
        _radius = new float[sectors];
    }

    public void Start() { lock (_gate) { ResetCore(); _phase = TestPhase.Running; } }

    public void Feed(GamepadSnapshot snapshot)
    {
        var v = snapshot.Stick(_stick);
        lock (_gate)
        {
            if (_phase != TestPhase.Running) return;
            _trail.Enqueue(v);
            while (_trail.Count > TrailCapacity) _trail.Dequeue();
            var r = v.Length;
            if (r < 0.5f) return; // 中心付近は角度が不安定なので周回判定に使わない
            var a = v.AngleDeg;
            var s = Math.Min((int)(a / (360f / _radius.Length)), _radius.Length - 1);
            if (r > _radius[s]) _radius[s] = r;
            if (!float.IsNaN(_prevAngle)) _cumulativeDeg += ((a - _prevAngle + 540f) % 360f) - 180f;
            _prevAngle = a;
            if (Turns() >= _requiredTurns) _phase = TestPhase.Complete;
        }
    }

    // 最後の 1 セクター分手前で 1 周とみなす (サンプル列の末尾が 360° にぴったり届かないため)
    private int Turns() => (int)((Math.Abs(_cumulativeDeg) + 360.0 / _radius.Length) / 360.0);

    public void Reset() { lock (_gate) ResetCore(); }

    private void ResetCore()
    {
        _phase = TestPhase.Idle;
        Array.Clear(_radius);
        _trail.Clear();
        _prevAngle = float.NaN;
        _cumulativeDeg = 0;
    }

    public CircleResult Result
    {
        get
        {
            lock (_gate)
            {
                var by = (float[])_radius.Clone();
                var hit = by.Where(x => x > 0).ToArray();
                float mean = hit.Length == 0 ? 0 : hit.Average(), min = hit.Length == 0 ? 0 : hit.Min(), max = hit.Length == 0 ? 0 : hit.Max();
                float At(float deg) => by[Math.Min((int)(deg / (360f / by.Length)), by.Length - 1)];
                var ax = mean > 0 ? (At(0) - At(180)) / mean : 0;
                var ay = mean > 0 ? (At(90) - At(270)) / mean : 0;
                return new CircleResult(_phase, by, mean, min, max, max > 0 ? min / max : 0, ax, ay, Turns(), _trail.ToArray());
            }
        }
    }
}

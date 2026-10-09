using PadLab.Core.Model;

namespace PadLab.Core.Analysis;

/// <summary>スティックの角度別最大到達半径。</summary>
public sealed class StickRange : IAnalyzer
{
    private readonly object _gate = new();
    private readonly StickId _stick;
    private readonly float[] _max;
    private Vector2F _current;

    public StickRange(StickId stick, int sectors = 36)
    {
        _stick = stick;
        _max = new float[sectors];
    }

    public void Feed(GamepadSnapshot snapshot)
    {
        var v = snapshot.Stick(_stick);
        var r = v.Length;
        lock (_gate)
        {
            _current = v;
            if (r <= 0) return;
            var s = Math.Min((int)(v.AngleDeg / (360f / _max.Length)), _max.Length - 1);
            if (r > _max[s]) _max[s] = r;
        }
    }

    public void Reset() { lock (_gate) { Array.Clear(_max); _current = Vector2F.Zero; } }

    public float[] MaxRadiusBySector { get { lock (_gate) return (float[])_max.Clone(); } }
    public float MaxRadius { get { lock (_gate) return _max.Length == 0 ? 0 : _max.Max(); } }
    public Vector2F Current { get { lock (_gate) return _current; } }
}

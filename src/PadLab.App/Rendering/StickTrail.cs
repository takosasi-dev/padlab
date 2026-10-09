using PadLab.Core.Model;
using SkiaSharp;

namespace PadLab.App.Rendering;

/// <summary>直近 fadeSeconds の軌跡を残像として描く。motion=false なら現在点のみ。</summary>
public sealed class StickTrail
{
    private readonly long _fadeNs;
    private readonly Queue<(Vector2F P, long Ns)> _points = new();
    private readonly object _gate = new();
    private readonly SKPaint _line = Theme.Stroke(Theme.Accent, 2f);
    private readonly SKPaint _dot = Theme.Fill(Theme.Accent);
    private readonly SKPaint _dotOutline = Theme.Stroke(Theme.Background, 1.5f);

    public StickTrail(double fadeSeconds = 0.3) => _fadeNs = (long)(fadeSeconds * 1_000_000_000);

    public void Push(Vector2F p, long ns)
    {
        lock (_gate)
        {
            _points.Enqueue((p, ns));
            while (_points.Count > 0 && ns - _points.Peek().Ns > _fadeNs) _points.Dequeue();
        }
    }

    public void Draw(SKCanvas c, SKPoint center, float radius, bool motion)
    {
        (Vector2F P, long Ns)[] pts;
        lock (_gate) pts = _points.ToArray();
        if (pts.Length == 0) return;
        var last = pts[^1];

        if (motion && pts.Length > 1)
        {
            for (var i = 1; i < pts.Length; i++)
            {
                var age = (float)(last.Ns - pts[i].Ns) / _fadeNs;   // 0=今, 1=消える
                var alpha = (byte)(255 * (1f - Math.Clamp(age, 0f, 1f)) * 0.8f);
                _line.Color = Theme.Accent.WithAlpha(alpha);
                c.DrawLine(Map(pts[i - 1].P, center, radius), Map(pts[i].P, center, radius), _line);
            }
        }
        var cur = Map(last.P, center, radius);
        _dot.Color = Theme.Accent;
        c.DrawCircle(cur, 5f, _dot);
        c.DrawCircle(cur, 5f, _dotOutline);
    }

    public static SKPoint Map(Vector2F v, SKPoint center, float radius) =>
        new(center.X + v.X * radius, center.Y - v.Y * radius);   // Y は上が +
}

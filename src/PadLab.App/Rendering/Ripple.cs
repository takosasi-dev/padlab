using SkiaSharp;

namespace PadLab.App.Rendering;

/// <summary>振動の波紋。Emit ごとに輪が広がって消える。motion=false なら強度に比例した静止した輪 1 本。</summary>
public sealed class Ripple
{
    private const double LifeSeconds = 0.6;
    private readonly List<(SKPoint O, float S, double Age)> _live = new();
    private readonly Dictionary<SKPoint, float> _static = new();
    private readonly SKPaint _paint = Theme.Stroke(Theme.Accent, 2f);

    public void Emit(SKPoint origin, float strength)
    {
        strength = Math.Clamp(strength, 0f, 1f);
        _static[origin] = strength;
        if (strength > 0) _live.Add((origin, strength, 0));
    }

    public void Draw(SKCanvas c, double dt, bool motion)
    {
        if (!motion)
        {
            _live.Clear();
            foreach (var (o, s) in _static)
            {
                if (s <= 0) continue;
                _paint.Color = Theme.Accent.WithAlpha(180);
                _paint.StrokeWidth = 2f + 4f * s;
                c.DrawCircle(o, 12f + 40f * s, _paint);
            }
            return;
        }
        for (var i = _live.Count - 1; i >= 0; i--)
        {
            var (o, s, age) = _live[i];
            age += dt;
            if (age >= LifeSeconds) { _live.RemoveAt(i); continue; }
            _live[i] = (o, s, age);
            var t = (float)(age / LifeSeconds);
            _paint.Color = Theme.Accent.WithAlpha((byte)(200 * (1 - t) * s));
            _paint.StrokeWidth = 1f + 3f * s;
            c.DrawCircle(o, 8f + (20f + 60f * s) * t, _paint);
        }
    }
}

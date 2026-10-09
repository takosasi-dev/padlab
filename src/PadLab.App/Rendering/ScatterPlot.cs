using PadLab.Core.Model;
using SkiaSharp;

namespace PadLab.App.Rendering;

/// <summary>ドリフト散布図。点は半透明で積み上がり、偏りがヒートマップのように浮かぶ。Minor/Notable 円を併記。</summary>
public sealed class ScatterPlot
{
    private readonly SKPaint _bg = Theme.Fill(Theme.Panel);
    private readonly SKPaint _ring = Theme.Stroke(Theme.Outline, 1f);
    private readonly SKPaint _minor = Theme.Stroke(Theme.Warn, 1f) ;
    private readonly SKPaint _notable = Theme.Stroke(Theme.Alert, 1f);
    private readonly SKPaint _pts = new() { Color = Theme.Accent.WithAlpha(40), IsAntialias = true, StrokeWidth = 3f, StrokeCap = SKStrokeCap.Round, Style = SKPaintStyle.Stroke };
    private SKPoint[] _buf = Array.Empty<SKPoint>();

    /// <summary>zoom: 1.0 で半径=1.0 が円周。ドリフト用は 5〜10 倍に拡大して使う。</summary>
    public float Zoom { get; set; } = 1f;

    public void Draw(SKCanvas c, SKPoint center, float radius, IReadOnlyList<Vector2F> points, float minorR, float notableR)
    {
        _bg.Color = Theme.Panel; _ring.Color = Theme.Outline;
        c.DrawCircle(center, radius, _bg);
        c.DrawCircle(center, radius, _ring);
        c.DrawLine(center.X - radius, center.Y, center.X + radius, center.Y, _ring);
        c.DrawLine(center.X, center.Y - radius, center.X, center.Y + radius, _ring);
        var scale = radius * Zoom;
        _minor.PathEffect ??= SKPathEffect.CreateDash(new[] { 4f, 4f }, 0);
        _notable.PathEffect ??= SKPathEffect.CreateDash(new[] { 4f, 4f }, 0);
        if (minorR > 0) c.DrawCircle(center, minorR * scale, _minor);
        if (notableR > 0) c.DrawCircle(center, notableR * scale, _notable);

        if (points.Count == 0) return;
        if (_buf.Length < points.Count) _buf = new SKPoint[points.Count];
        for (var i = 0; i < points.Count; i++)
        {
            var p = points[i];
            _buf[i] = new SKPoint(center.X + Math.Clamp(p.X * Zoom, -1f, 1f) * radius, center.Y - Math.Clamp(p.Y * Zoom, -1f, 1f) * radius);
        }
        c.Save();
        c.ClipRect(new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius));
        c.DrawPoints(SKPointMode.Points, _buf.AsSpan(0, points.Count).ToArray(), _pts);
        c.Restore();
    }
}

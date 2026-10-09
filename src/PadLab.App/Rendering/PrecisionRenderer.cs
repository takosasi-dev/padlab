using PadLab.Core.Analysis.Precision;
using PadLab.Core.Model;
using SkiaSharp;

namespace PadLab.App.Rendering;

/// <summary>精度テストのグラフ。Circle は極座標、Snapback は棒、Linearity は曲線、Resolution は数値。</summary>
public sealed class PrecisionRenderer : IFrameRenderer
{
    public enum View { None, Circle, Snapback, Linearity, Resolution, All }

    public View Mode;
    public CircleTest? Circle;
    public SnapbackTest? Snapback;
    public LinearityTest? Linearity;
    public ResolutionTest? Resolution;
    public Func<Vector2F>? LiveStick;

    private readonly SKPaint _panel = Theme.Fill(Theme.Panel);
    private readonly SKPaint _ring = Theme.Stroke(Theme.Outline, 1f);
    private readonly SKPaint _accent = Theme.Stroke(Theme.Accent, 2f);
    private readonly SKPaint _accentFill = Theme.Fill(Theme.Accent);
    private readonly SKPaint _trail = new() { Color = Theme.Accent.WithAlpha(50), IsAntialias = true, StrokeWidth = 2f, StrokeCap = SKStrokeCap.Round, Style = SKPaintStyle.Stroke };
    private readonly SKPaint _warn = Theme.Fill(Theme.Alert);
    private readonly SKPaint _text = Theme.Fill(Theme.Text);
    private readonly SKPaint _dim = Theme.Fill(Theme.TextDim);
    private readonly SKFont _font = Theme.JpFont(13);
    private readonly SKFont _mono = Theme.MonoFont(13);
    private readonly SKPathBuilder _path = new();

    public void Render(SKCanvas c, SKRect bounds, double dt, bool motion)
    {
        _panel.Color = Theme.Panel; _ring.Color = Theme.Outline; _text.Color = Theme.Text; _dim.Color = Theme.TextDim;
        var b = SKRect.Inflate(bounds, -12, -12);
        switch (Mode)
        {
            case View.Circle: DrawCircle(c, b); break;
            case View.Snapback: DrawSnapback(c, b); break;
            case View.Linearity: DrawLinearity(c, b); break;
            case View.Resolution: DrawResolution(c, b); break;
            case View.None: Frame(c, b, Strings.NavPrecision); Empty(c, b); break;
            case View.All:
                var w = b.Width / 2 - 6; var h = b.Height / 2 - 6;
                DrawCircle(c, new SKRect(b.Left, b.Top, b.Left + w, b.Top + h));
                DrawSnapback(c, new SKRect(b.Right - w, b.Top, b.Right, b.Top + h));
                DrawLinearity(c, new SKRect(b.Left, b.Bottom - h, b.Left + w, b.Bottom));
                DrawResolution(c, new SKRect(b.Right - w, b.Bottom - h, b.Right, b.Bottom));
                break;
        }
    }

    private void Frame(SKCanvas c, SKRect r, string title)
    {
        c.DrawRoundRect(r, 6, 6, _panel);
        c.DrawRoundRect(r, 6, 6, _ring);
        c.DrawText(title, r.Left + 10, r.Top + 18, SKTextAlign.Left, _font, _dim);
    }

    private void Empty(SKCanvas c, SKRect r) => c.DrawText(Strings.PrecNotDone, r.MidX, r.MidY, SKTextAlign.Center, _font, _dim);

    private void DrawCircle(SKCanvas c, SKRect r)
    {
        Frame(c, r, Strings.PrecStepCircle);
        var res = Circle?.Result;
        var rad = Math.Min(r.Width, r.Height - 24) / 2 - 16;
        var center = new SKPoint(r.MidX, r.MidY + 10);
        c.DrawCircle(center, rad, _ring);
        c.DrawLine(center.X - rad, center.Y, center.X + rad, center.Y, _ring);
        c.DrawLine(center.X, center.Y - rad, center.X, center.Y + rad, _ring);
        if (res is null) { Empty(c, r); return; }

        if (res.Trail.Count > 1)
        {
            _path.Reset();
            for (var i = 0; i < res.Trail.Count; i++)
            {
                var p = StickTrail.Map(res.Trail[i], center, rad);
                if (i == 0) _path.MoveTo(p); else _path.LineTo(p);
            }
            using var tp = _path.Detach();
            c.DrawPath(tp, _trail);
        }
        var n = res.RadiusBySector.Length;
        if (res.RadiusBySector.Any(x => x > 0))
        {
            _path.Reset();
            var first = true;
            for (var i = 0; i < n; i++)
            {
                var a = (i + 0.5f) * 2 * MathF.PI / n;
                var rr = res.RadiusBySector[i];
                if (rr <= 0) continue;
                var p = new SKPoint(center.X + MathF.Cos(a) * rr * rad, center.Y - MathF.Sin(a) * rr * rad);
                if (first) { _path.MoveTo(p); first = false; } else _path.LineTo(p);
            }
            _path.Close();
            using var pp = _path.Detach();
            _accent.Color = Theme.Accent;
            c.DrawPath(pp, _accent);
        }
        if (LiveStick is not null && res.Phase == TestPhase.Running)
        {
            _accentFill.Color = Theme.Accent;
            c.DrawCircle(StickTrail.Map(LiveStick(), center, rad), 5, _accentFill);
        }
        c.DrawText($"{Strings.PrecRoundness} {res.Roundness,5:0.00}  {Strings.PrecMaxRadius} {res.MaxRadius,5:0.00}", r.Left + 10, r.Bottom - 8, SKTextAlign.Left, _mono, _text);
    }

    private void DrawSnapback(SKCanvas c, SKRect r)
    {
        Frame(c, r, Strings.PrecStepSnapback);
        var res = Snapback?.Result;
        if (res is null) { Empty(c, r); return; }
        var plot = new SKRect(r.Left + 40, r.Top + 28, r.Right - 12, r.Bottom - 28);
        c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, _ring);
        var maxMs = Math.Max(50.0, res.Samples.Count == 0 ? 0 : res.Samples.Max(s => s.ReturnMs) * 1.2);
        c.DrawText($"{maxMs,4:0}ms", r.Left + 4, plot.Top + 12, SKTextAlign.Left, _mono, _dim);
        var slots = Math.Max(res.TargetReleases, res.Samples.Count);
        var bw = plot.Width / slots;
        for (var i = 0; i < res.Samples.Count; i++)
        {
            var s = res.Samples[i];
            var h = (float)(s.ReturnMs / maxMs) * plot.Height;
            var x = plot.Left + i * bw + bw * 0.2f;
            _accentFill.Color = Theme.Accent;
            c.DrawRect(new SKRect(x, plot.Bottom - h, x + bw * 0.6f, plot.Bottom), _accentFill);
            if (s.Overshoot > 0.01f)
                c.DrawRect(new SKRect(x, plot.Bottom - h - s.Overshoot * plot.Height * 0.5f, x + bw * 0.6f, plot.Bottom - h), _warn);
            c.DrawText($"{s.ReturnMs:0}", x + bw * 0.3f, plot.Bottom + 14, SKTextAlign.Center, _mono, _dim);
        }
        c.DrawText($"{Strings.PrecReturnMs} {res.MeanReturnMs,6:0.0} ms  {Strings.PrecOvershoot} {res.MaxOvershoot,5:0.00}", r.Left + 10, r.Bottom - 8, SKTextAlign.Left, _mono, _text);
    }

    private void DrawLinearity(SKCanvas c, SKRect r)
    {
        Frame(c, r, Strings.PrecStepLinearity);
        var res = Linearity?.Result;
        if (res is null) { Empty(c, r); return; }
        var plot = new SKRect(r.Left + 16, r.Top + 28, r.Right - 16, r.Bottom - 28);
        c.DrawRect(plot, _ring);
        c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Top, _ring);   // 理想線
        if (res.Curve.Count > 1)
        {
            _path.Reset();
            for (var i = 0; i < res.Curve.Count; i++)
            {
                var p = res.Curve[i];
                var pt = new SKPoint(plot.Left + p.X * plot.Width, plot.Bottom - Math.Clamp(p.Y, 0, 1) * plot.Height);
                if (i == 0) _path.MoveTo(pt); else _path.LineTo(pt);
            }
            using var lp = _path.Detach();
            _accent.Color = Theme.Accent;
            c.DrawPath(lp, _accent);
        }
        c.DrawText($"{Strings.PrecJumps} {res.JumpCount,3}  {Strings.PrecReversals} {res.ReversalCount,3}  {Strings.PrecMaxJump} {res.MaxJump,5:0.00}", r.Left + 10, r.Bottom - 8, SKTextAlign.Left, _mono, _text);
    }

    private void DrawResolution(SKCanvas c, SKRect r)
    {
        Frame(c, r, Strings.PrecStepResolution);
        var res = Resolution?.Result;
        if (res is null) { Empty(c, r); return; }
        var y = r.Top + 50;
        foreach (var line in new[]
        {
            $"{Strings.PrecMinStep,-12} {res.MinStep,8:0.0000}",
            $"{Strings.PrecNoise,-12} {res.NoiseLevel,8:0.0000}",
            $"{Strings.PrecLevels,-12} {res.DistinctLevels,8}",
            $"{Strings.PrecElapsedLabel,-12} {res.ElapsedSeconds,8:0.0} s",
        })
        {
            c.DrawText(line, r.Left + 16, y, SKTextAlign.Left, _mono, _text);
            y += 22;
        }
    }
}

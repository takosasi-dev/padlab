using SkiaSharp;

namespace PadLab.App.Rendering;

/// <summary>時系列の折れ線。右端が最新、windowNs 分を表示。</summary>
public sealed class LineChart
{
    private readonly SKPaint _bg = Theme.Fill(Theme.Panel);
    private readonly SKPaint _frame = Theme.Stroke(Theme.Outline, 1f);
    private readonly SKPaint _line = Theme.Stroke(Theme.Accent, 1.5f);
    private readonly SKPathBuilder _path = new();

    public SKColor Color { get; set; } = Theme.Accent;

    public void Draw(SKCanvas c, SKRect r, IReadOnlyList<(long ns, float v)> data, long windowNs, float min, float max)
    {
        _bg.Color = Theme.Panel; _frame.Color = Theme.Outline; _line.Color = Color;
        c.DrawRoundRect(r, 4, 4, _bg);
        c.DrawRoundRect(r, 4, 4, _frame);
        if (data.Count < 2 || max <= min || windowNs <= 0) return;

        var end = data[^1].ns;
        var start = end - windowNs;
        var plot = SKRect.Inflate(r, -2, -3);   // 0 と 1 の線が枠線に埋もれないよう内側に描く
        _path.Reset();
        var first = true;
        foreach (var (ns, v) in data)
        {
            if (ns < start) continue;
            var x = plot.Left + (float)(ns - start) / windowNs * plot.Width;
            var y = plot.Bottom - Math.Clamp((v - min) / (max - min), 0f, 1f) * plot.Height;
            if (first) { _path.MoveTo(x, y); first = false; } else _path.LineTo(x, y);
        }
        using var path = _path.Detach();
        c.Save();
        c.ClipRect(r);
        c.DrawPath(path, _line);
        c.Restore();
    }
}

using PadLab.App.Services;
using PadLab.Core.Analysis;
using SkiaSharp;

namespace PadLab.App.Rendering;

/// <summary>ドリフト画面: 左右 2 つの散布図 (Minor/Notable 円付き)。点は DriftAnalyzer.Result.Points をそのまま描く。</summary>
public sealed class DriftRenderer : IFrameRenderer
{
    private readonly DeviceSession _s;
    private readonly ScatterPlot _left = new() { Zoom = 6f }, _right = new() { Zoom = 6f };
    private readonly SKPaint _text = Theme.Fill(Theme.TextDim);
    private readonly SKFont _font = Theme.JpFont(13);

    public DriftRenderer(DeviceSession session) => _s = session;

    public void Render(SKCanvas c, SKRect bounds, double dt, bool motion)
    {
        _text.Color = Theme.TextDim;
        var pad = 16f;
        var r = Math.Min((bounds.Width - pad * 3) / 4, (bounds.Height - pad * 2 - 24) / 2);
        var y = bounds.Top + pad + 20 + r;
        var lc = new SKPoint(bounds.Left + pad + r, y);
        var rc = new SKPoint(lc.X + r * 2 + pad, y);
        Draw(c, _left, lc, r, _s.LeftDrift, Strings.LeftStick);
        Draw(c, _right, rc, r, _s.RightDrift, Strings.RightStick);
    }

    private void Draw(SKCanvas c, ScatterPlot plot, SKPoint center, float r, DriftAnalyzer a, string label)
    {
        var th = a.Thresholds;
        var res = a.Result;
        plot.Draw(c, center, r, res.Points, th.MinorDistance, th.NotableDistance);
        c.DrawText(label, center.X, center.Y - r - 6, SKTextAlign.Center, _font, _text);
    }
}

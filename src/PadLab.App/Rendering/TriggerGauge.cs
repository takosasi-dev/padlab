using SkiaSharp;

namespace PadLab.App.Rendering;

/// <summary>縦ゲージ。押し込みに応じて満ち、押し切りでスナップ (Glow)。最大到達値の目盛りを残す。</summary>
public sealed class TriggerGauge
{
    private readonly SKPaint _bg = Theme.Fill(Theme.Panel);
    private readonly SKPaint _frame = Theme.Stroke(Theme.Outline, 1f);
    private readonly SKPaint _fill = Theme.Fill(Theme.Accent);
    private readonly SKPaint _mark = Theme.Stroke(Theme.Warn, 2f);
    private readonly SKPaint _text = Theme.Fill(Theme.TextDim);
    private readonly SKFont _font = Theme.MonoFont(12);
    private bool _wasFull;

    public string Label { get; set; } = "";

    public void Draw(SKCanvas c, SKRect r, float value, float max, Glow snap, bool motion)
    {
        _bg.Color = Theme.Panel; _frame.Color = Theme.Outline; _text.Color = Theme.TextDim;
        c.DrawRoundRect(r, 4, 4, _bg);

        var full = value >= 0.98f;
        if (full && !_wasFull) snap.Trigger();
        _wasFull = full;
        var s = snap.Value(1.0 / 60, motion);

        var h = r.Height * Math.Clamp(value, 0f, 1f);
        var fillRect = new SKRect(r.Left, r.Bottom - h, r.Right, r.Bottom);
        _fill.Color = full ? Theme.Ok : Theme.Accent;
        c.DrawRoundRect(fillRect, 3, 3, _fill);
        if (s > 0)
        {
            var grow = 3f * s;
            using var glow = Theme.Stroke(Theme.Ok.WithAlpha((byte)(200 * s)), 2f + grow);
            c.DrawRoundRect(SKRect.Inflate(r, grow, grow), 5, 5, glow);
        }
        c.DrawRoundRect(r, 4, 4, _frame);

        if (max > 0)
        {
            var y = r.Bottom - r.Height * Math.Clamp(max, 0f, 1f);
            c.DrawLine(r.Left - 4, y, r.Right + 4, y, _mark);
        }
        c.DrawText($"{value * 100,3:0}", r.MidX, r.Bottom + 14, SKTextAlign.Center, _font, _text);
        if (Label.Length > 0) c.DrawText(Label, r.MidX, r.Top - 4, SKTextAlign.Center, _font, _text);
    }
}

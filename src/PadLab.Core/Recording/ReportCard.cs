using SkiaSharp;

namespace PadLab.Core.Recording;

/// <summary>セッションを 1 枚の PNG にまとめる。高さは行数で決まる。</summary>
public static class ReportCard
{
    private static readonly SKColor Background = new(0x1E, 0x1E, 0x24);
    private static readonly SKColor Text = new(0xF0, 0xF0, 0xF0);
    private static readonly SKColor Dim = new(0xA0, 0xA0, 0xA8);
    private static readonly SKColor Accent = new(0x4C, 0xC2, 0xFF);
    private static readonly SKColor Ok = new(0x5C, 0xD6, 0x8A);
    private static readonly SKColor Warn = new(0xF5, 0xC5, 0x42);
    private static readonly SKColor Alert = new(0xF0, 0x6A, 0x6A);

    private readonly record struct Line(string Text, float Size, SKColor Color, float Indent = 0, float Gap = 0);

    public static byte[] RenderPng(DiagnosticSession session, int width = 1200)
    {
        var lines = Compose(session);
        const float margin = 48f;
        var height = margin * 2 + lines.Sum(l => l.Size * 1.5f + l.Gap);

        using var typeface = SKFontManager.Default.MatchCharacter('あ') ?? SKTypeface.Default;
        using var bitmap = new SKBitmap(width, (int)MathF.Ceiling(height));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(Background);
        using var paint = new SKPaint { IsAntialias = true };
        using var font = new SKFont(typeface);

        var y = margin;
        foreach (var l in lines)
        {
            y += l.Gap;
            font.Size = l.Size;
            paint.Color = l.Color;
            y += l.Size;
            canvas.DrawText(l.Text, margin + l.Indent, y, SKTextAlign.Left, font, paint);
            y += l.Size * 0.5f;
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static List<Line> Compose(DiagnosticSession s)
    {
        var r = s.Results;
        var lines = new List<Line>
        {
            new("PadLab レポートカード", 40, Accent),
            new($"{s.Device.Type}  /  {s.Device.Connection}  /  {s.MeasuredAt:yyyy-MM-dd HH:mm}  /  PadLab {s.AppVersion}", 20, Dim),
        };

        Section(lines, "ボタン反応");
        var pressed = r.Buttons.Count(b => b.Pressed);
        lines.Add(new($"{pressed} / {r.Buttons.Length} 個に反応を確認", 20, Text, 24));
        // ponytail: 1行に5個ずつ並べるだけ。列揃えは等幅でないので厳密でない
        foreach (var chunk in r.Buttons.Chunk(5))
            lines.Add(new(string.Join("   ", chunk.Select(b => $"{(b.Pressed ? "✓" : "－")} {b.Label}")), 20, Text, 24));

        Section(lines, "スティック");
        foreach (var st in r.Sticks)
        {
            lines.Add(new($"{SessionStore.StickName(st.Stick)}スティック: {SessionStore.VerdictText(st.Verdict)}", 22, VerdictColor(st.Verdict), 24));
            lines.Add(new($"最大ズレ {st.MaxDrift:F3}   平均 {st.MeanDrift:F3}   方向 {st.DominantAngle:F0}°   到達半径 {st.MaxRadius:F2}   真円度 {SessionStore.Opt(st.Roundness, "F2")}   戻り {SessionStore.Opt(st.MeanReturnMs, "F1", " ms")}", 18, Dim, 48));
        }

        Section(lines, "チャタリング");
        if (r.Chatter.Length == 0) lines.Add(new("未実施", 20, Dim, 24));
        foreach (var c in r.Chatter)
            lines.Add(new($"{c.Button}: 押下 {c.Presses} 回 / 疑い {c.Suspects} 回   ({c.DetectionLimitMs:F1} ms 未満は検出できません)", 20, c.Suspects > 0 ? Warn : Text, 24));

        Section(lines, "振動");
        foreach (var chunk in r.Vibration.Chunk(2))
            lines.Add(new(string.Join("      ", chunk.Select(v => $"{v.Motor}: {(v.Tested ? "実施" : "未実施")}{(v.UserConfirmedFelt is { } f ? (f ? " (体感あり)" : " (体感なし)") : "")}")), 20, Text, 24));

        Section(lines, "デッドゾーン推奨");
        if (r.DeadzoneRecommendedPercent is { } dz)
        {
            lines.Add(new($"{dz}%", 26, Accent, 24));
            if (!string.IsNullOrEmpty(r.DeadzoneRationale)) lines.Add(new(r.DeadzoneRationale, 18, Dim, 24));
        }
        else lines.Add(new("未確認", 20, Dim, 24));

        lines.Add(new(SessionStore.Disclaimer, 18, Dim, 0, 36));
        return lines;
    }

    private static void Section(List<Line> lines, string title) => lines.Add(new(title, 24, Accent, 0, 20));

    private static SKColor VerdictColor(string verdict) => verdict switch
    {
        "None" => Ok,
        "Minor" => Warn,
        "Notable" => Alert,
        _ => Dim,
    };
}

using SkiaSharp;

namespace PadLab.App.Rendering;

/// <summary>描画色とフォント。アクセント 1 色 + 状態色 3 色。状態色は必ず記号/文言と併用する。</summary>
public static class Theme
{
    public static SKColor Background = new(0x12, 0x14, 0x18);
    public static SKColor Panel = new(0x1E, 0x22, 0x28);
    public static SKColor Outline = new(0x3A, 0x40, 0x4A);
    public static SKColor Accent = new(0x4C, 0xC2, 0xFF);
    public static SKColor Ok = new(0x5C, 0xD6, 0x8A);
    public static SKColor Warn = new(0xF2, 0xC0, 0x4C);
    public static SKColor Alert = new(0xF0, 0x6A, 0x6A);
    public static SKColor Text = new(0xE8, 0xEA, 0xEE);
    public static SKColor TextDim = new(0x8A, 0x90, 0x9A);

    public static readonly SKTypeface Jp =
        SKFontManager.Default.MatchCharacter('あ') ?? SKTypeface.Default;
    public static readonly SKTypeface Mono =
        SKFontManager.Default.MatchFamily("Consolas") ?? SKFontManager.Default.MatchFamily("Cascadia Mono") ?? SKTypeface.Default;

    public static void Apply(bool light)
    {
        if (light)
        {
            Background = new(0xF4, 0xF5, 0xF7);
            Panel = new(0xE4, 0xE7, 0xEB);
            Outline = new(0xB8, 0xBE, 0xC8);
            Text = new(0x1A, 0x1C, 0x20);
            TextDim = new(0x5C, 0x62, 0x6C);
        }
        else
        {
            Background = new(0x12, 0x14, 0x18);
            Panel = new(0x1E, 0x22, 0x28);
            Outline = new(0x3A, 0x40, 0x4A);
            Text = new(0xE8, 0xEA, 0xEE);
            TextDim = new(0x8A, 0x90, 0x9A);
        }
    }

    public static SKFont JpFont(float size) => new(Jp, size) { Edging = SKFontEdging.SubpixelAntialias };
    public static SKFont MonoFont(float size) => new(Mono, size) { Edging = SKFontEdging.SubpixelAntialias };

    public static SKPaint Fill(SKColor c) => new() { Color = c, IsAntialias = true, Style = SKPaintStyle.Fill };
    public static SKPaint Stroke(SKColor c, float width = 1.5f) =>
        new() { Color = c, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = width, StrokeCap = SKStrokeCap.Round };
}

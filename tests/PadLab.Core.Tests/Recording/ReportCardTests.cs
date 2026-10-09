using PadLab.Core.Recording;
using SkiaSharp;

namespace PadLab.Core.Tests.Recording;

public class ReportCardTests
{
    [Fact]
    public void Renders_decodable_png()
    {
        var png = ReportCard.RenderPng(TestSessions.Minimal());
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, png.Take(8).ToArray());
        using var bmp = SKBitmap.Decode(png);
        Assert.NotNull(bmp);
        Assert.Equal(1200, bmp.Width);
        Assert.True(bmp.Height > 400, $"height={bmp.Height}");
    }

    [Fact]
    public void Custom_width_is_respected()
    {
        using var bmp = SKBitmap.Decode(ReportCard.RenderPng(TestSessions.Minimal(), 800));
        Assert.Equal(800, bmp.Width);
    }
}

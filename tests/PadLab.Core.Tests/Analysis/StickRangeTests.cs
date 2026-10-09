using PadLab.Core.Analysis;
using PadLab.Core.Fake;
using PadLab.Core.Model;

namespace PadLab.Core.Tests.Analysis;

public class StickRangeTests
{
    [Fact]
    public void Circle_FillsAllSectors()
    {
        var r = new StickRange(StickId.Left);
        r.Feed(ScriptedPattern.Circle(1, 0.9f));
        var by = r.MaxRadiusBySector;
        Assert.Equal(36, by.Length);
        Assert.All(by, v => Assert.InRange(v, 0.85f, 0.95f));
        Assert.InRange(r.MaxRadius, 0.89f, 0.91f);
        r.Reset();
        Assert.Equal(0, r.MaxRadius);
    }

    [Fact]
    public void Current_IsLatest()
    {
        var r = new StickRange(StickId.Right);
        r.Feed(Seq.At(0) with { RightStick = new Vector2F(0.3f, 0.4f) });
        Assert.Equal(new Vector2F(0.3f, 0.4f), r.Current);
        Assert.Equal(0.5f, r.MaxRadius, 3);
    }
}

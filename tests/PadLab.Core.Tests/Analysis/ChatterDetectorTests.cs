using PadLab.Core.Analysis;
using PadLab.Core.Fake;
using PadLab.Core.Model;

namespace PadLab.Core.Tests.Analysis;

public class ChatterDetectorTests
{
    [Fact]
    public void ChatterPattern_IsSuspected()
    {
        var c = new ChatterDetector();
        c.Feed(ScriptedPattern.Chatter(ButtonId.A, 5, gapMs: 4, hz: 1000));
        var s = c.Result.Buttons[ButtonId.A];
        Assert.Equal(10, s.Presses);
        Assert.Equal(5, s.Suspects);
        Assert.Equal(5, s.SuspectGapsMs.Count);
        Assert.Equal(5, c.Result.TotalSuspects);
    }

    [Fact]
    public void HumanTapping_IsNotSuspected()
    {
        var c = new ChatterDetector();
        c.Feed(ScriptedPattern.Taps(ButtonId.A, 50, intervalMs: 60, holdMs: 30, hz: 1000).SkipLast(1)); // Taps は末尾に次の押下1サンプルを含む
        var s = c.Result.Buttons[ButtonId.A];
        Assert.Equal(50, s.Presses);
        Assert.Equal(0, s.Suspects);
        Assert.NotNull(s.ShortestNormalGapMs);
        Assert.InRange(s.ShortestNormalGapMs!.Value, 29, 31);
    }

    [Fact]
    public void DetectionLimit_IsTwiceSampleInterval()
    {
        var c = new ChatterDetector();
        c.Feed(ScriptedPattern.Idle(1, hz: 125));
        Assert.InRange(c.Result.DetectionLimitMs, 15.9, 16.1);
        c.Reset();
        Assert.Empty(c.Result.Buttons);
        Assert.Equal(0, c.Result.DetectionLimitMs);
    }
}

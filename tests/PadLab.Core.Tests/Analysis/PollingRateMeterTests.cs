using PadLab.Core.Analysis;
using PadLab.Core.Fake;

namespace PadLab.Core.Tests.Analysis;

public class PollingRateMeterTests
{
    [Fact]
    public void Idle250Hz_Reports250()
    {
        var m = new PollingRateMeter();
        m.Feed(ScriptedPattern.Idle(2, hz: 250));
        var s = m.Stats;
        Assert.InRange(s.Hz, 240, 260);
        Assert.True(s.JitterMs < 0.01, $"Jitter={s.JitterMs}");
        Assert.InRange(s.MeanIntervalMs, 3.9, 4.1);
    }

    [Fact]
    public void Idle60Hz_Reports60()
    {
        var m = new PollingRateMeter();
        m.Feed(ScriptedPattern.Idle(1, hz: 60));
        Assert.InRange(m.Stats.Hz, 55, 65);
    }

    [Fact]
    public void Reset_ClearsSamples()
    {
        var m = new PollingRateMeter();
        m.Feed(ScriptedPattern.Idle(1, hz: 60));
        m.Reset();
        Assert.Equal(0, m.Stats.Samples);
        Assert.Equal(0, m.Stats.Hz);
    }
}

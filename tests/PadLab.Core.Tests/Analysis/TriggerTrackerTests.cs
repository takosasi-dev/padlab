using PadLab.Core.Analysis;
using PadLab.Core.Model;

namespace PadLab.Core.Tests.Analysis;

public class TriggerTrackerTests
{
    [Fact]
    public void TracksMaxCurrentHistory()
    {
        var t = new TriggerTracker(TriggerId.Left);
        var i = 0;
        foreach (var v in new[] { 0f, 0.5f, 1.0f, 0.2f }) t.Feed(Seq.At(i++ * 4, lt: v));
        Assert.Equal(1.0f, t.Max);
        Assert.Equal(0.2f, t.Current);
        Assert.Equal(4, t.History.Count);
        Assert.Equal(0.5f, t.History[1].Value);
        Assert.True(t.History[0].TimestampNs < t.History[3].TimestampNs);
    }

    [Fact]
    public void Capacity_DropsOldest()
    {
        var t = new TriggerTracker(TriggerId.Right, capacity: 3);
        for (var i = 0; i < 5; i++) t.Feed(Seq.At(i * 4) with { RightTrigger = i * 0.1f });
        Assert.Equal(3, t.History.Count);
        Assert.Equal(0.2f, t.History[0].Value, 3);
        t.Reset();
        Assert.Empty(t.History);
    }
}

using PadLab.Core.Analysis.Precision;
using PadLab.Core.Model;

namespace PadLab.Core.Tests.Analysis;

public class SnapbackTestTests
{
    /// <summary>1000Hz で (x,0) を ms 間保持する列。</summary>
    private static IEnumerable<GamepadSnapshot> Hold(ref double t, float x, double ms)
    {
        var list = new List<GamepadSnapshot>();
        for (var i = 0; i < ms; i++, t++) list.Add(Seq.At(t, left: new Vector2F(x, 0)));
        return list;
    }

    [Fact]
    public void FiveCleanReleases_Complete()
    {
        var s = new SnapbackTest(StickId.Left);
        s.Start();
        double t = 0;
        for (var i = 0; i < 5; i++) { s.Feed(Hold(ref t, 1f, 300)); s.Feed(Hold(ref t, 0f, 300)); }
        var r = s.Result;
        Assert.Equal(TestPhase.Complete, r.Phase);
        Assert.Equal(5, r.Releases);
        Assert.Equal(5, r.TargetReleases);
        Assert.True(r.MeanReturnMs < 5, $"mean={r.MeanReturnMs}");
        Assert.Equal(5, r.Samples.Count);
        Assert.InRange(r.Samples[0].ReleaseAngleDeg, -1f, 1f);
    }

    [Fact]
    public void Overshoot_IsMeasured()
    {
        var s = new SnapbackTest(StickId.Left, targetReleases: 1);
        s.Start();
        double t = 0;
        s.Feed(Hold(ref t, 1f, 300));
        s.Feed(Hold(ref t, -0.2f, 20));
        s.Feed(Hold(ref t, 0f, 300));
        var r = s.Result;
        Assert.Equal(1, r.Releases);
        Assert.InRange(r.MaxOvershoot, 0.19f, 0.21f);
        Assert.InRange(r.MaxReturnMs, 15, 25);
    }

    [Fact]
    public void Reset_ReturnsToIdle()
    {
        var s = new SnapbackTest(StickId.Left, 1);
        s.Start();
        double t = 0;
        s.Feed(Hold(ref t, 1f, 300)); s.Feed(Hold(ref t, 0f, 300));
        s.Reset();
        Assert.Equal(TestPhase.Idle, s.Result.Phase);
        Assert.Equal(0, s.Result.Releases);
    }
}

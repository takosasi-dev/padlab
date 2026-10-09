using PadLab.Core.Analysis.Precision;
using PadLab.Core.Model;

namespace PadLab.Core.Tests.Analysis;

public class LinearityTestTests
{
    private static IEnumerable<GamepadSnapshot> Ramp(IEnumerable<float> values)
    {
        var i = 0;
        foreach (var v in values) yield return Seq.At(i++ * 4, left: new Vector2F(v, 0));
    }

    [Fact]
    public void LinearRamp_NoJumps()
    {
        var t = new LinearityTest(StickId.Left);
        t.Start();
        t.Feed(Ramp(Enumerable.Range(0, 500).Select(i => i / 499f)));
        var r = t.Result;
        Assert.Equal(TestPhase.Complete, r.Phase);
        Assert.Equal(0, r.JumpCount);
        Assert.Equal(0, r.ReversalCount);
        Assert.True(r.Curve.Count > 100);
        Assert.InRange(r.Curve[0].X, 0f, 0.001f);
        Assert.InRange(r.Curve[^1].X, 0.999f, 1f);
        Assert.True(r.Curve[^1].Y > 0.95f);
    }

    [Fact]
    public void SkippedRange_CountsJump()
    {
        var t = new LinearityTest(StickId.Left);
        t.Start();
        var values = Enumerable.Range(0, 500).Select(i => i / 499f).Where(v => v < 0.4f || v > 0.6f);
        t.Feed(Ramp(values));
        var r = t.Result;
        Assert.Equal(1, r.JumpCount);
        Assert.InRange(r.MaxJump, 0.19f, 0.22f);
    }

    [Fact]
    public void Decrease_CountsReversal()
    {
        var t = new LinearityTest(StickId.Left);
        t.Start();
        t.Feed(Ramp(new[] { 0f, 0.1f, 0.2f, 0.3f, 0.25f, 0.4f, 0.5f, 1f }));
        Assert.Equal(1, t.Result.ReversalCount);
        Assert.Equal(TestPhase.Complete, t.Result.Phase);
    }
}

using PadLab.Core.Analysis.Precision;
using PadLab.Core.Fake;
using PadLab.Core.Model;

namespace PadLab.Core.Tests.Analysis;

public class CircleTestTests
{
    [Fact]
    public void TwoTurns_Completes()
    {
        var t = new CircleTest(StickId.Left, requiredTurns: 2);
        Assert.Equal(TestPhase.Idle, t.Result.Phase);
        t.Start();
        Assert.Equal(TestPhase.Running, t.Result.Phase);
        t.Feed(ScriptedPattern.Circle(2, 0.9f, turns: 2));
        var r = t.Result;
        Assert.Equal(TestPhase.Complete, r.Phase);
        Assert.Equal(2, r.TurnsDetected);
        Assert.True(r.Roundness > 0.95f, $"roundness={r.Roundness}");
        Assert.InRange(r.MeanRadius, 0.88f, 0.92f);
        Assert.True(r.Trail.Count > 0);
        Assert.Equal(36, r.RadiusBySector.Length);
    }

    [Fact]
    public void Ellipse_ReportsRoundnessAndSymmetry()
    {
        var t = new CircleTest(StickId.Left);
        t.Start();
        t.Feed(ScriptedPattern.Circle(1, 0.9f).Select(s => s with { LeftStick = new Vector2F(s.LeftStick.X * 0.7f, s.LeftStick.Y) }));
        var r = t.Result;
        Assert.Equal(TestPhase.Complete, r.Phase);
        Assert.InRange(r.Roundness, 0.65f, 0.75f);
        Assert.InRange(r.AsymmetryX, -0.05f, 0.05f);
        Assert.InRange(r.AsymmetryY, -0.05f, 0.05f);
    }

    [Fact]
    public void WithoutStart_IgnoresInput()
    {
        var t = new CircleTest(StickId.Left);
        t.Feed(ScriptedPattern.Circle(1, 0.9f));
        Assert.Equal(TestPhase.Idle, t.Result.Phase);
        Assert.Equal(0, t.Result.TurnsDetected);
    }
}

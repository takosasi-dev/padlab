using PadLab.Core.Analysis;
using PadLab.Core.Fake;
using PadLab.Core.Model;

namespace PadLab.Core.Tests.Analysis;

public class DriftAnalyzerTests
{
    private const long Sec = 1_000_000_000L;

    private static float AngleDiff(float a, float b) => MathF.Abs(((a - b + 540f) % 360f) - 180f);

    [Fact]
    public void IdleWithoutStart_StaysIdle()
    {
        var d = new DriftAnalyzer(StickId.Left);
        d.Feed(ScriptedPattern.Drift(1, 0.1f, 0));
        Assert.Equal(DriftPhase.Idle, d.Result.Phase);
    }

    [Fact]
    public void Calibration_ThenMinor_ThenNotable()
    {
        var d = new DriftAnalyzer(StickId.Left);
        d.StartCalibration();
        Assert.Equal(DriftPhase.Calibrating, d.Result.Phase);
        d.Feed(ScriptedPattern.Drift(3.5, 0.01f, 0.01f));
        var r = d.Result;
        Assert.Equal(DriftPhase.Monitoring, r.Phase);
        Assert.InRange(r.Center.X, 0.005f, 0.015f);
        Assert.InRange(r.Center.Y, 0.005f, 0.015f);
        Assert.Equal(Verdict.None, r.Verdict);

        d.Feed(ScriptedPattern.Drift(2, 0.06f, 0, startNs: 4 * Sec));
        r = d.Result;
        Assert.Equal(Verdict.Minor, r.Verdict);
        Assert.True(AngleDiff(r.DominantAngleDeg, 0) < 20, $"angle={r.DominantAngleDeg}");
        Assert.True(r.ExceedCount >= 1);
        Assert.True(r.Points.Count > 0);

        d.Feed(ScriptedPattern.Drift(2, 0, 0.1f, startNs: 7 * Sec));
        r = d.Result;
        Assert.Equal(Verdict.Notable, r.Verdict);
        Assert.True(AngleDiff(r.DominantAngleDeg, 90) < 15, $"angle={r.DominantAngleDeg}");
        Assert.InRange(r.MaxDistance, 0.08f, 0.11f);
        Assert.True(r.ElapsedSeconds > 5);

        d.Reset();
        Assert.Equal(DriftPhase.Idle, d.Result.Phase);
    }

    [Fact]
    public void Points_AreCappedAtMaxPoints()
    {
        var d = new DriftAnalyzer(StickId.Left, maxPoints: 100);
        d.StartCalibration();
        d.Feed(ScriptedPattern.Drift(6, 0, 0));
        Assert.Equal(100, d.Result.Points.Count);
    }
}

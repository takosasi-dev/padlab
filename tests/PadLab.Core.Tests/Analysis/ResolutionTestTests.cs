using PadLab.Core.Analysis.Precision;
using PadLab.Core.Model;

namespace PadLab.Core.Tests.Analysis;

public class ResolutionTestTests
{
    [Fact]
    public void FineSteps_ReportMinStep()
    {
        var t = new ResolutionTest(StickId.Left, seconds: 1);
        t.Start();
        // 0.001 刻みで 0→0.5 (500 サンプル、4ms 間隔 = 2s)
        for (var i = 0; i <= 500; i++) t.Feed(Seq.At(i * 4, left: new Vector2F(i * 0.001f, 0)));
        var r = t.Result;
        Assert.Equal(TestPhase.Complete, r.Phase);
        Assert.InRange(r.MinStep, 0.0009f, 0.0011f);
        Assert.True(r.NoiseLevel < 0.001f, $"noise={r.NoiseLevel}");
        Assert.InRange(r.DistinctLevels, 200, 260);
        Assert.InRange(r.ElapsedSeconds, 0.99, 1.01);
    }

    [Fact]
    public void BeforeStart_Idle()
    {
        var t = new ResolutionTest(StickId.Left);
        t.Feed(Seq.At(0, left: new Vector2F(0.5f, 0)));
        Assert.Equal(TestPhase.Idle, t.Result.Phase);
        Assert.Equal(0, t.Result.DistinctLevels);
    }
}

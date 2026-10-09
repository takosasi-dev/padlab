using PadLab.Core.Analysis;
using PadLab.Core.Fake;
using PadLab.Core.Model;

namespace PadLab.Core.Tests.Analysis;

public class PaddleMapperTests
{
    [Fact]
    public void FullFlow()
    {
        var p = new PaddleMapper(paddleCount: 3);
        Assert.Equal(PaddlePhase.Idle, p.State.Phase);
        p.Begin();
        Assert.Equal(PaddlePhase.Waiting, p.State.Phase);
        Assert.Equal(1, p.State.CurrentPaddle);

        // P1: A 押下 → 100ms の取り込み窓が閉じて Captured
        p.Feed(ScriptedPattern.Taps(ButtonId.A, 1, 300).SkipLast(1));
        Assert.Equal(PaddlePhase.Captured, p.State.Phase);
        Assert.Equal(new[] { ButtonId.A }, p.State.Mappings[0].Mapped);
        Assert.False(p.State.Mappings[0].PhysicalSeen);

        // P2: B と X を 50ms 内に
        p.Next();
        Assert.Equal(2, p.State.CurrentPaddle);
        Assert.Equal(PaddlePhase.Waiting, p.State.Phase);
        p.Feed(Seq.At(1000));
        p.Feed(Seq.At(1010, Seq.Bits(ButtonId.B)));
        p.Feed(Seq.At(1050, Seq.Bits(ButtonId.B, ButtonId.X)));
        p.Feed(Seq.At(1200));
        Assert.Equal(PaddlePhase.Captured, p.State.Phase);
        Assert.Equal(new[] { ButtonId.B, ButtonId.X }, p.State.Mappings[1].Mapped);

        // P3: 反応なし
        p.Next();
        Assert.Equal(3, p.State.CurrentPaddle);
        p.Feed(ScriptedPattern.Idle(11, startNs: 2_000_000_000));
        Assert.Equal(PaddlePhase.NoResponse, p.State.Phase);
        Assert.True(p.State.WaitedSeconds >= 10);
        Assert.Empty(p.State.Mappings[2].Mapped);

        // やり直し → 物理ビット P3 と A
        p.Retry();
        Assert.Equal(PaddlePhase.Waiting, p.State.Phase);
        Assert.Equal(2, p.State.Mappings.Count);
        p.Feed(Seq.At(20_000, Seq.Bits(ButtonId.P3, ButtonId.A)));
        p.Feed(Seq.At(20_200));
        Assert.Equal(PaddlePhase.Captured, p.State.Phase);
        Assert.True(p.State.Mappings[2].PhysicalSeen);
        Assert.Equal(new[] { ButtonId.A }, p.State.Mappings[2].Mapped);

        p.Next();
        Assert.Equal(PaddlePhase.Done, p.State.Phase);
        Assert.Equal(3, p.State.Mappings.Count);
    }

    [Fact]
    public void SameButtonTwice_IsInvalid()
    {
        var p = new PaddleMapper(1);
        p.Begin();
        p.Feed(Seq.At(0, Seq.Bits(ButtonId.A)));
        p.Feed(Seq.At(10));
        p.Feed(Seq.At(20, Seq.Bits(ButtonId.A)));
        Assert.Equal(PaddlePhase.Invalid, p.State.Phase);
        p.Retry();
        Assert.Equal(PaddlePhase.Waiting, p.State.Phase);
    }

    [Fact]
    public void ThreeStandardButtons_IsInvalid()
    {
        var p = new PaddleMapper(1);
        p.Begin();
        p.Feed(Seq.At(0, Seq.Bits(ButtonId.A, ButtonId.B, ButtonId.X)));
        Assert.Equal(PaddlePhase.Invalid, p.State.Phase);
        p.Reset();
        Assert.Equal(PaddlePhase.Idle, p.State.Phase);
    }
}

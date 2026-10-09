using PadLab.Core.Analysis;
using PadLab.Core.Fake;
using PadLab.Core.Model;

namespace PadLab.Core.Tests.Analysis;

public class IdleMonitorTests
{
    private const long Sec = 1_000_000_000L;

    [Fact]
    public void Idle_NoEvents()
    {
        var m = new IdleMonitor();
        m.Start(Vector2F.Zero, Vector2F.Zero);
        m.Feed(ScriptedPattern.Idle(1));
        var s = m.Summary;
        Assert.True(s.Running);
        Assert.False(s.Finished);
        Assert.Empty(s.Events);
        Assert.InRange(s.ElapsedSeconds, 0.9, 1.1);
    }

    [Fact]
    public void NotStarted_IgnoresInput()
    {
        var m = new IdleMonitor();
        m.Feed(ScriptedPattern.Drift(0.5, 0.2f, 0));
        Assert.False(m.Summary.Running);
        Assert.Empty(m.Summary.Events);
    }

    [Fact]
    public void Drift_Buttons_Gap_AreCounted()
    {
        var m = new IdleMonitor();
        m.Start(Vector2F.Zero, Vector2F.Zero);
        m.Feed(ScriptedPattern.Idle(1));
        m.Feed(ScriptedPattern.Drift(0.5, 0.08f, 0, startNs: 1 * Sec));
        var s = m.Summary;
        Assert.Equal(1, s.DriftEvents);
        Assert.InRange(s.MaxLeftDeviation, 0.07f, 0.09f);
        Assert.Equal(IdleEventKind.StickDrift, s.Events[0].Kind);
        Assert.Contains("左スティック", s.Events[0].Detail);

        m.Feed(ScriptedPattern.Taps(ButtonId.B, 2, 300, startNs: 1_500_000_000).SkipLast(1));
        Assert.Equal(2, m.Summary.ButtonEvents);
        Assert.Contains("B", m.Summary.Events.First(e => e.Kind == IdleEventKind.PhantomButton).Detail);

        // 200ms のタイムスタンプ飛び
        m.Feed(ScriptedPattern.Idle(0.1, startNs: 2_100_000_000));
        m.Feed(ScriptedPattern.Idle(0.1, startNs: 2_400_000_000));
        Assert.Equal(1, m.Summary.GapEvents);

        m.NoteDisconnected(4 * Sec);
        m.NoteReconnected(5 * Sec);
        Assert.Equal(2, m.Summary.DisconnectEvents);
        Assert.Equal(4 * Sec, m.Summary.Events.First(e => e.Kind == IdleEventKind.Disconnected).OffsetNs);
    }

    [Fact]
    public void Trigger_Wobble_IsCounted()
    {
        var m = new IdleMonitor();
        m.Start(Vector2F.Zero, Vector2F.Zero);
        m.Feed(Seq.At(0));
        m.Feed(Seq.At(4, lt: 0.2f));
        m.Feed(Seq.At(8, lt: 0.3f));
        Assert.Equal(1, m.Summary.TriggerEvents);
    }

    [Fact]
    public void Duration_Finishes()
    {
        var m = new IdleMonitor(new IdleMonitorSettings(DurationSeconds: 1));
        m.Start(Vector2F.Zero, Vector2F.Zero);
        m.Feed(ScriptedPattern.Idle(1.5));
        Assert.True(m.Summary.Finished);
        Assert.False(m.Summary.Running);
        m.Reset();
        Assert.False(m.Summary.Finished);
    }

    [Fact]
    public void Stop_EndsRun()
    {
        var m = new IdleMonitor();
        m.Start(Vector2F.Zero, Vector2F.Zero);
        m.Feed(ScriptedPattern.Idle(0.5));
        m.Stop();
        Assert.False(m.Summary.Running);
        Assert.True(m.Summary.Finished);
    }
}

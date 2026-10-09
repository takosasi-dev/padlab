using PadLab.Core.Analysis;
using PadLab.Core.Fake;
using PadLab.Core.Model;
using PadLab.Input;

namespace PadLab.Core.Tests.Input;

public class PollingLoopTests
{
    [Fact]
    public void FakeGamepad_100件_全部_Hub_に流れる()
    {
        var fake = new FakeGamepad();
        var seq = ScriptedPattern.Idle(0.4, hz: 250).ToList(); // 100 件
        fake.Push(seq);
        var hub = new SnapshotHub();
        using var loop = new PollingLoop(fake, hub);
        loop.Start();
        Assert.True(loop.IsRunning);
        Thread.Sleep(200);
        loop.Stop();
        Assert.False(loop.IsRunning);

        var got = new List<GamepadSnapshot>();
        while (hub.Reader.TryRead(out var s)) got.Add(s);
        Assert.Equal(100, got.Count);
        Assert.Equal(seq[^1], hub.Latest);
    }

    private sealed class ThrowingSource : IGamepadSource
    {
        public DeviceDescriptor Descriptor { get; } = new("t:0", 0, 0, "throw", ConnectionType.Unknown, DeviceKind.Fake);
        public Capabilities Capabilities => Capabilities.None;
        public bool TryRead(out GamepadSnapshot snapshot) => throw new InvalidOperationException("boom");
        public void SetRumble(RumbleCommand command) { }
        public void Dispose() { }
    }

    [Fact]
    public void TryRead_例外_で_Faulted_して停止()
    {
        var hub = new SnapshotHub();
        using var loop = new PollingLoop(new ThrowingSource(), hub);
        Exception? caught = null;
        loop.Faulted += e => caught = e;
        loop.Start();
        Thread.Sleep(100);
        Assert.False(loop.IsRunning);
        Assert.IsType<InvalidOperationException>(caught);
    }
}

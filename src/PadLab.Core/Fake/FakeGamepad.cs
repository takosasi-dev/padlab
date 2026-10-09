using PadLab.Core.Model;

namespace PadLab.Core.Fake;

/// <summary>
/// テストとデモ用の仮想コントローラー。Push した内容がそのまま TryRead で出てくる。
/// 実機バックエンドと同じ IGamepadSource 経路を通るのが目的。
/// </summary>
public sealed class FakeGamepad : IGamepadSource
{
    private readonly Queue<GamepadSnapshot> _queue = new();
    private readonly object _gate = new();
    public RumbleCommand LastRumble { get; private set; } = RumbleCommand.Stop;

    public FakeGamepad(DeviceKind kind = DeviceKind.Fake, Capabilities caps = Capabilities.Rumble | Capabilities.ImpulseTriggers)
    {
        Descriptor = new DeviceDescriptor("fake:0", 0x0000, 0x0000, "デモ用 仮想コントローラー", ConnectionType.Unknown, kind, BackendName: "Fake");
        Capabilities = caps;
    }

    public DeviceDescriptor Descriptor { get; }
    public Capabilities Capabilities { get; }

    public void Push(GamepadSnapshot s) { lock (_gate) _queue.Enqueue(s); }
    public void Push(IEnumerable<GamepadSnapshot> seq) { lock (_gate) foreach (var s in seq) _queue.Enqueue(s); }

    public bool TryRead(out GamepadSnapshot snapshot)
    {
        lock (_gate)
        {
            if (_queue.Count == 0) { snapshot = GamepadSnapshot.Neutral; return false; }
            snapshot = _queue.Dequeue();
            return true;
        }
    }

    public void SetRumble(RumbleCommand command) => LastRumble = command;
    public void Dispose() { }
}

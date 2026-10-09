using System.Windows.Input;
using PadLab.Core.Fake;
using PadLab.Core.Model;

namespace PadLab.App.Services;

/// <summary>
/// Fake デバイスに 250Hz で入力を流す。既定は左スティック円回し + 右スティック微小ドリフト。
/// キーボード (WASD=左, 矢印=右, JKLI=A/B/X/Y, Q/E=トリガー) が押されている間はその軸/ボタンを上書きする。
/// </summary>
public sealed class DemoDriver
{
    public const int Hz = 250;
    private const long StepNs = 1_000_000_000L / Hz;

    private readonly FakeGamepad _fake;
    private readonly HashSet<Key> _keys = new();
    private readonly object _gate = new();
    private readonly Random _rng = new(7);
    private Thread? _thread;
    private volatile bool _run;

    /// <summary>MainWindow のキー入力を届ける先。Start 中のインスタンス。</summary>
    public static DemoDriver? Active { get; private set; }

    public DemoDriver(FakeGamepad fake) => _fake = fake;

    public void Start()
    {
        if (_run) return;
        _run = true;
        Active = this;
        _thread = new Thread(Loop) { IsBackground = true, Name = "DemoDriver" };
        _thread.Start();
    }

    public void Stop()
    {
        _run = false;
        if (Active == this) Active = null;
        _thread?.Join(200);
        _thread = null;
    }

    public void KeyDown(Key k) { lock (_gate) _keys.Add(k); }
    public void KeyUp(Key k) { lock (_gate) _keys.Remove(k); }

    private void Loop()
    {
        var start = Clock.NowNs();
        long i = 0;
        while (_run)
        {
            var due = start + i * StepNs;
            var now = Clock.NowNs();
            if (now < due) { Thread.Sleep(1); continue; }
            if (now - due > 100 * Clock.NsPerMs) { i = (now - start) / StepNs; continue; }   // 遅れたら再同期
            _fake.Push(Compose(due, (due - start) / 1e9));
            i++;
        }
    }

    private GamepadSnapshot Compose(long ns, double t)
    {
        Key[] keys;
        lock (_gate) keys = _keys.ToArray();
        bool K(Key k) => Array.IndexOf(keys, k) >= 0;

        var left = keys.Any(k => k is Key.W or Key.A or Key.S or Key.D)
            ? new Vector2F((K(Key.D) ? 1 : 0) - (K(Key.A) ? 1 : 0), (K(Key.W) ? 1 : 0) - (K(Key.S) ? 1 : 0))
            : new Vector2F(MathF.Cos((float)(t * Math.PI)) * 0.85f, MathF.Sin((float)(t * Math.PI)) * 0.85f); // 0.5 周/s
        var right = keys.Any(k => k is Key.Up or Key.Down or Key.Left or Key.Right)
            ? new Vector2F((K(Key.Right) ? 1 : 0) - (K(Key.Left) ? 1 : 0), (K(Key.Up) ? 1 : 0) - (K(Key.Down) ? 1 : 0))
            : new Vector2F(0.02f + Noise(), 0.01f + Noise());
        ulong b = 0;
        if (K(Key.J)) b |= GamepadSnapshot.Bit(ButtonId.A);
        if (K(Key.K)) b |= GamepadSnapshot.Bit(ButtonId.B);
        if (K(Key.L)) b |= GamepadSnapshot.Bit(ButtonId.X);
        if (K(Key.I)) b |= GamepadSnapshot.Bit(ButtonId.Y);
        return new GamepadSnapshot(ns, b, left, right, K(Key.Q) ? 1f : 0f, K(Key.E) ? 1f : 0f);
    }

    private float Noise() => (float)(_rng.NextDouble() * 2 - 1) * 0.006f;
}

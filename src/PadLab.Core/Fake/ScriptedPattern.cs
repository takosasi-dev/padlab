using PadLab.Core.Model;

namespace PadLab.Core.Fake;

/// <summary>
/// テスト・デモ用の入力列生成。すべて TimestampNs 付きで、周期 hz のポーリングを模す。
/// </summary>
public static class ScriptedPattern
{
    public static IEnumerable<GamepadSnapshot> Idle(double seconds, int hz = 250, long startNs = 0)
    {
        var step = 1_000_000_000L / hz;
        var n = (int)(seconds * hz);
        for (var i = 0; i < n; i++)
            yield return GamepadSnapshot.Neutral with { TimestampNs = startNs + i * step };
    }

    /// <summary>ニュートラルが (dx,dy) にずれた静止。ノイズ幅 noise を乱数で加える。</summary>
    public static IEnumerable<GamepadSnapshot> Drift(double seconds, float dx, float dy, float noise = 0.005f, StickId stick = StickId.Left, int hz = 250, long startNs = 0, int seed = 1)
    {
        var rng = new Random(seed);
        var step = 1_000_000_000L / hz;
        var n = (int)(seconds * hz);
        for (var i = 0; i < n; i++)
        {
            var v = new Vector2F(dx + (float)(rng.NextDouble() * 2 - 1) * noise, dy + (float)(rng.NextDouble() * 2 - 1) * noise);
            var s = GamepadSnapshot.Neutral with { TimestampNs = startNs + i * step };
            yield return stick == StickId.Left ? s with { LeftStick = v } : s with { RightStick = v };
        }
    }

    /// <summary>外周を radius で turns 周回す。</summary>
    public static IEnumerable<GamepadSnapshot> Circle(double seconds, float radius = 1f, int turns = 1, StickId stick = StickId.Left, int hz = 250, long startNs = 0)
    {
        var step = 1_000_000_000L / hz;
        var n = (int)(seconds * hz);
        for (var i = 0; i < n; i++)
        {
            var a = 2 * MathF.PI * turns * i / n;
            var v = new Vector2F(MathF.Cos(a) * radius, MathF.Sin(a) * radius);
            var s = GamepadSnapshot.Neutral with { TimestampNs = startNs + i * step };
            yield return stick == StickId.Left ? s with { LeftStick = v } : s with { RightStick = v };
        }
    }

    /// <summary>ボタンを押す→離すを intervalMs 間隔で count 回。holdMs は押している長さ。</summary>
    public static IEnumerable<GamepadSnapshot> Taps(ButtonId id, int count, double intervalMs, double holdMs = 40, int hz = 250, long startNs = 0)
    {
        var step = 1_000_000_000L / hz;
        var total = (long)(count * intervalMs * Clock.NsPerMs) + step;
        for (long t = 0; t < total; t += step)
        {
            var phase = (t / Clock.NsPerMs) % intervalMs;
            var down = phase < holdMs;
            yield return GamepadSnapshot.Neutral with { TimestampNs = startNs + t, Buttons = down ? GamepadSnapshot.Bit(id) : 0 };
        }
    }

    /// <summary>1回の押下の直後に gapMs 空けてもう1回入る (チャタリング) を count 回。</summary>
    public static IEnumerable<GamepadSnapshot> Chatter(ButtonId id, int count, double gapMs = 4, double spacingMs = 500, int hz = 1000, long startNs = 0)
    {
        var step = 1_000_000_000L / hz;
        var bit = GamepadSnapshot.Bit(id);
        long t = 0;
        for (var i = 0; i < count; i++)
        {
            var baseNs = startNs + (long)(i * spacingMs * Clock.NsPerMs);
            // 押下 30ms → 解放 gapMs → 押下 30ms → 解放
            foreach (var (down, ms) in new[] { (true, 30.0), (false, gapMs), (true, 30.0), (false, 100.0) })
            {
                var end = (long)(ms * Clock.NsPerMs);
                for (long u = 0; u < end; u += step)
                    yield return GamepadSnapshot.Neutral with { TimestampNs = baseNs + t + u, Buttons = down ? bit : 0 };
                t += end;
            }
            t = 0;
        }
    }
}

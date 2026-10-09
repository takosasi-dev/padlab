using PadLab.Core.Analysis;
using PadLab.Core.Model;

namespace PadLab.Core.Tests.Analysis;

/// <summary>テスト用: 入力列をアナライザへ流すヘルパ。</summary>
internal static class Seq
{
    public static long Feed(this IAnalyzer a, IEnumerable<GamepadSnapshot> seq)
    {
        long last = 0;
        foreach (var s in seq) { a.Feed(s); last = s.TimestampNs; }
        return last;
    }

    public static GamepadSnapshot At(double ms, ulong buttons = 0, Vector2F? left = null, float lt = 0)
        => GamepadSnapshot.Neutral with { TimestampNs = (long)(ms * Clock.NsPerMs), Buttons = buttons, LeftStick = left ?? Vector2F.Zero, LeftTrigger = lt };

    public static ulong Bits(params ButtonId[] ids) { ulong b = 0; foreach (var i in ids) b |= GamepadSnapshot.Bit(i); return b; }
}

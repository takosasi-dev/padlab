using System.Diagnostics;

namespace PadLab.Core.Model;

/// <summary>Stopwatch 由来の単調ナノ秒時刻。全バックエンド共通で使う。</summary>
public static class Clock
{
    private static readonly double NsPerTick = 1_000_000_000.0 / Stopwatch.Frequency;
    public static long NowNs() => (long)(Stopwatch.GetTimestamp() * NsPerTick);
    public static double ToMs(long ns) => ns / 1_000_000.0;
    public const long NsPerMs = 1_000_000;
}

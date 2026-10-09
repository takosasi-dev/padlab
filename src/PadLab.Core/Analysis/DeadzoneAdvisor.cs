namespace PadLab.Core.Analysis;

public sealed record DeadzoneAdvice(int RecommendedPercent, string Rationale, IReadOnlyList<(string Use, string Hint)> Hints);

/// <summary>ドリフト最大距離・ノイズ・放置監視の最大ズレからデッドゾーン推奨値 (%) と根拠文を出す。</summary>
public static class DeadzoneAdvisor
{
    private const float Margin = 0.02f;
    private static readonly (string Use, string Hint)[] HintTable =
    {
        ("FPS", "小さめ (推奨値どおり) が微調整に有利"),
        ("格闘ゲーム", "推奨値より 2〜3% 広めで暴発を防ぐ"),
        ("レース", "推奨値どおり"),
    };

    public static DeadzoneAdvice Compute(float maxDriftDistance, float noiseLevel, float? idleMaxDeviation)
    {
        var drift = MathF.Max(maxDriftDistance, idleMaxDeviation ?? 0);
        var total = drift + 2 * noiseLevel + Margin;
        // 浮動小数の丸め誤差で 0.09 → 9.0000001 → 10 とならないよう 4 桁で丸めてから切り上げる
        var percent = Math.Clamp((int)Math.Ceiling(Math.Round(total * 100.0, 4)), 3, 30);
        var rationale = $"最大ズレ {drift:F2} + ノイズ {noiseLevel:F3}×2 + 余裕 {Margin:F2} = {total:F2} → {percent}%";
        return new DeadzoneAdvice(percent, rationale, HintTable);
    }
}

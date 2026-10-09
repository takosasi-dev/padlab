namespace PadLab.App.Rendering;

/// <summary>
/// 点灯後の減衰。点灯そのものは呼び出し側が即時に色を切り替え、Glow は解放後 160ms の ease-out 減衰だけを担う。
/// motion=false なら「押下中 1 / 解放 0」で減衰しない。
/// </summary>
public sealed class Glow
{
    public const double DecaySeconds = 0.16;
    private double _t = double.MaxValue;   // Trigger からの経過秒
    private bool _wasDown;

    public bool IsDown { get; private set; }

    /// <summary>減衰を開始する (1→0)。</summary>
    public void Trigger() => _t = 0;

    /// <summary>押下状態を渡す。解放エッジで Trigger する。</summary>
    public void Update(bool down)
    {
        if (_wasDown && !down) Trigger();
        _wasDown = down;
        IsDown = down;
    }

    /// <summary>この時点の強度 0..1。dt 秒だけ進める。</summary>
    public float Value(double dt, bool motion = true)
    {
        if (IsDown) return 1f;
        if (!motion) return 0f;
        if (_t >= DecaySeconds) return 0f;
        var x = (float)(_t / DecaySeconds);
        _t += dt;
        var eased = 1f - x;                // ease-out: (1-x)^2
        return eased * eased;
    }
}

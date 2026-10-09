namespace PadLab.Core.Model;

public readonly record struct Vector2F(float X, float Y)
{
    public float Length => MathF.Sqrt(X * X + Y * Y);
    /// <summary>0..360 度。+X が 0、+Y (上) が 90。</summary>
    public float AngleDeg => (MathF.Atan2(Y, X) * 180f / MathF.PI + 360f) % 360f;
    public static readonly Vector2F Zero = new(0, 0);
    public static Vector2F operator -(Vector2F a, Vector2F b) => new(a.X - b.X, a.Y - b.Y);
}

public readonly record struct Vector3F(float X, float Y, float Z);

/// <summary>タッチパッド上の1点。X/Y は 0..1 に正規化。</summary>
public readonly record struct TouchPoint(int Id, float X, float Y);

/// <summary>
/// バックエンドの生値を正規化した1サンプル。不変。
/// 軸は -1..1 (Y は上が +1)、トリガーは 0..1。取れない項目は null。
/// </summary>
public sealed record GamepadSnapshot(
    long TimestampNs,
    ulong Buttons,
    Vector2F LeftStick,
    Vector2F RightStick,
    float LeftTrigger,
    float RightTrigger,
    Vector3F? Gyro = null,
    Vector3F? Accel = null,
    IReadOnlyList<TouchPoint>? Touches = null,
    float? Battery = null,
    IReadOnlyList<int>? ExtraButtons = null)
{
    public static ulong Bit(ButtonId id) => 1UL << (int)id;
    public bool IsDown(ButtonId id) => (Buttons & Bit(id)) != 0;
    public Vector2F Stick(StickId id) => id == StickId.Left ? LeftStick : RightStick;
    public float Trigger(TriggerId id) => id == TriggerId.Left ? LeftTrigger : RightTrigger;

    public static readonly GamepadSnapshot Neutral = new(0, 0, Vector2F.Zero, Vector2F.Zero, 0, 0);

    public static IEnumerable<ButtonId> DownButtons(ulong bits)
    {
        for (var i = 0; i < 64 && bits != 0; i++, bits >>= 1)
            if ((bits & 1) != 0) yield return (ButtonId)i;
    }
}

namespace PadLab.Core.Model;

/// <summary>正規化後のボタン識別子。ulong ビット列に載せるため最大 64 個。</summary>
public enum ButtonId
{
    // 分類A: 標準セット
    A = 0, B, X, Y,
    LB, RB,
    DUp, DDown, DLeft, DRight,
    L3, R3,
    Start, Select, Guide,
    // 分類B: 独立入力として見える拡張ボタン
    Share = 15, Mute, TouchpadClick, Capture, Fn1, Fn2,
    // 分類C: 物理パドル (生HIDで取れた場合のみ)
    P1 = 21, P2, P3, P4,
    // 汎用パッド用 (DirectInput ボタン番号 0..31 → Generic1..Generic32)
    Generic1 = 32, Generic2, Generic3, Generic4, Generic5, Generic6, Generic7, Generic8,
    Generic9, Generic10, Generic11, Generic12, Generic13, Generic14, Generic15, Generic16,
    Generic17, Generic18, Generic19, Generic20, Generic21, Generic22, Generic23, Generic24,
    Generic25, Generic26, Generic27, Generic28, Generic29, Generic30, Generic31, Generic32,
}

public enum ButtonCategory { Standard, Extended, Paddle }
public enum StickId { Left, Right }
public enum TriggerId { Left, Right }
public enum DeviceKind { Fake, XboxOne, XboxElite2, DualShock4, DualSense, DualSenseEdge, SwitchPro, Generic }
public enum ConnectionType { Unknown, Usb, Bluetooth, Dongle }

[Flags]
public enum Capabilities
{
    None = 0,
    Rumble = 1,
    ImpulseTriggers = 2,
    Gyro = 4,
    Accel = 8,
    Touchpad = 16,
    Battery = 32,
    PhysicalPaddles = 64,
    Led = 128,
}

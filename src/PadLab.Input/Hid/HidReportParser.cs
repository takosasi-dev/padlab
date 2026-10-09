using PadLab.Core.Model;

namespace PadLab.Input.Hid;

/// <summary>
/// 生 HID 入力レポート → GamepadSnapshot の純粋パーサ。実機無しでもバイト列テストできるよう状態を持たない。
/// </summary>
public interface IHidReportParser
{
    bool TryParse(ReadOnlySpan<byte> report, bool bluetooth, long timestampNs, out GamepadSnapshot snapshot);

    /// <summary>振動 (+ LED 既定色) の出力レポート。</summary>
    byte[] BuildOutputReport(RumbleCommand rumble, bool bluetooth);

    /// <summary>接続直後に Write で送るレポート列。不要なら空。</summary>
    byte[][] HandshakeReports(bool bluetooth);

    /// <summary>
    /// 接続直後に GetFeature で読むフィーチャーレポート (先頭がレポート ID)。null なら不要。
    /// DS4/DualSense は BT でこれを読むまで簡易レポート (0x01) しか来ない [要検証]。
    /// </summary>
    byte[]? ConnectFeatureRequest(bool bluetooth) => null;

    Capabilities Capabilities { get; }
}

public static class HidReportParsers
{
    public static IHidReportParser? For(DeviceKind kind) => kind switch
    {
        DeviceKind.DualShock4 => new DualShock4Report(),
        DeviceKind.DualSense => new DualSenseReport(edge: false),
        DeviceKind.DualSenseEdge => new DualSenseReport(edge: true),
        DeviceKind.SwitchPro => new SwitchProReport(),
        _ => null,
    };
}

/// <summary>各パーサ共通の変換。</summary>
internal static class HidMath
{
    /// <summary>8bit 軸 → -1..1。</summary>
    public static float Axis8(byte v) => (v - 127.5f) / 127.5f;

    /// <summary>12bit 軸 (キャリブ無し) → -1..1。</summary>
    public static float Axis12(int v) => Math.Clamp((v - 2048) / 1700f, -1f, 1f);

    public static short I16(ReadOnlySpan<byte> r, int offset) => (short)(r[offset] | (r[offset + 1] << 8));

    public static Vector3F Vec3(ReadOnlySpan<byte> r, int offset) => new(I16(r, offset), I16(r, offset + 2), I16(r, offset + 4));

    /// <summary>ハットスイッチ 0..7 (8 = 中立) → 十字キー。0 = 上、時計回り。</summary>
    public static ulong HatToBits(int hat)
    {
        ulong up = GamepadSnapshot.Bit(ButtonId.DUp), down = GamepadSnapshot.Bit(ButtonId.DDown);
        ulong left = GamepadSnapshot.Bit(ButtonId.DLeft), right = GamepadSnapshot.Bit(ButtonId.DRight);
        return hat switch
        {
            0 => up, 1 => up | right, 2 => right, 3 => down | right,
            4 => down, 5 => down | left, 6 => left, 7 => up | left,
            _ => 0,
        };
    }

    public static byte Byte01(float v) => (byte)Math.Clamp(MathF.Round(v * 255f), 0, 255);

    /// <summary>CRC-32 (IEEE, 反射)。DS4/DualSense の BT 出力レポート末尾用。seed は前置バイト (0xA2) を先に流す。</summary>
    public static uint Crc32(ReadOnlySpan<byte> data, byte prefix)
    {
        var crc = 0xFFFFFFFFu;
        crc = Step(crc, prefix);
        foreach (var b in data) crc = Step(crc, b);
        return ~crc;

        static uint Step(uint crc, byte b)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            return crc;
        }
    }

    public static void PutCrc32(byte[] report, byte prefix)
    {
        var crc = Crc32(report.AsSpan(0, report.Length - 4), prefix);
        report[^4] = (byte)crc; report[^3] = (byte)(crc >> 8); report[^2] = (byte)(crc >> 16); report[^1] = (byte)(crc >> 24);
    }
}

using PadLab.Core.Model;

namespace PadLab.Input.Hid;

/// <summary>
/// DualSense (054C:0CE6) / DualSense Edge (054C:0DF2) の入力・出力レポート。
/// 参照: Linux hid-playstation.c (struct dualsense_input_report / dualsense_output_report_common)、
///       nondebug/dualsense (https://github.com/nondebug/dualsense)、pydualsense、DS4Windows (Ryochan7) DualSenseDevice.cs
/// USB: report 0x01 (64B)。BT: report 0x31 (78B)、USB レイアウトの offset +1。
/// </summary>
public sealed class DualSenseReport : IHidReportParser
{
    private readonly bool _edge;
    public DualSenseReport(bool edge) => _edge = edge;

    public Capabilities Capabilities =>
        Capabilities.Rumble | Capabilities.Gyro | Capabilities.Accel | Capabilities.Touchpad | Capabilities.Battery | Capabilities.Led
        | (_edge ? Capabilities.PhysicalPaddles : Capabilities.None);

    public bool TryParse(ReadOnlySpan<byte> r, bool bluetooth, long ns, out GamepadSnapshot snapshot)
    {
        snapshot = GamepadSnapshot.Neutral;
        if (r.Length < 11) return false;
        int o;
        if (r[0] == 0x01) o = 0;
        else if (r[0] == 0x31 && bluetooth) o = 1;
        else return false;
        if (r.Length < o + 11) return false;

        // [要検証] 以下のオフセットは USB 実機で確認する (offset は USB 基準)
        var left = new Vector2F(HidMath.Axis8(r[o + 1]), -HidMath.Axis8(r[o + 2]));
        var right = new Vector2F(HidMath.Axis8(r[o + 3]), -HidMath.Axis8(r[o + 4]));
        var lt = r[o + 5] / 255f;
        var rt = r[o + 6] / 255f;
        var b8 = r[o + 8]; var b9 = r[o + 9]; var b10 = r[o + 10];

        var bits = HidMath.HatToBits(b8 & 0x0F);
        void Set(bool on, ButtonId id) { if (on) bits |= GamepadSnapshot.Bit(id); }
        Set((b8 & 0x10) != 0, ButtonId.X);      // □
        Set((b8 & 0x20) != 0, ButtonId.A);      // ×
        Set((b8 & 0x40) != 0, ButtonId.B);      // ○
        Set((b8 & 0x80) != 0, ButtonId.Y);      // △
        Set((b9 & 0x01) != 0, ButtonId.LB);     // L1
        Set((b9 & 0x02) != 0, ButtonId.RB);     // R1
        Set((b9 & 0x10) != 0, ButtonId.Share);  // Create
        Set((b9 & 0x20) != 0, ButtonId.Start);  // Options
        Set((b9 & 0x40) != 0, ButtonId.L3);
        Set((b9 & 0x80) != 0, ButtonId.R3);
        Set((b10 & 0x01) != 0, ButtonId.Guide); // PS
        Set((b10 & 0x02) != 0, ButtonId.TouchpadClick);
        Set((b10 & 0x04) != 0, ButtonId.Mute);
        if (_edge)
        {
            // [要検証] hid-playstation.c: LEFT_FN=0x10 RIGHT_FN=0x20 LEFT_PADDLE=0x40 RIGHT_PADDLE=0x80
            Set((b10 & 0x10) != 0, ButtonId.Fn1);
            Set((b10 & 0x20) != 0, ButtonId.Fn2);
            Set((b10 & 0x40) != 0, ButtonId.P1);
            Set((b10 & 0x80) != 0, ButtonId.P2);
        }

        Vector3F? gyro = null, accel = null;
        float? battery = null;
        List<TouchPoint>? touches = null;
        if (r.Length >= o + 54)
        {
            gyro = HidMath.Vec3(r, o + 16);
            accel = HidMath.Vec3(r, o + 22);
            // タッチ: 33: id (bit7 = 非アクティブ), 34-36: 12bit x / 12bit y。2 点目は +4
            for (var i = 0; i < 2; i++)
            {
                var t = o + 33 + i * 4;
                if ((r[t] & 0x80) != 0) continue;
                var x = r[t + 1] | ((r[t + 2] & 0x0F) << 8);
                var y = (r[t + 2] >> 4) | (r[t + 3] << 4);
                (touches ??= new List<TouchPoint>()).Add(new TouchPoint(r[t] & 0x7F, x / 1920f, y / 1080f));
            }
            battery = Math.Min((r[o + 53] & 0x0F) * 10, 100) / 100f;
        }

        snapshot = new GamepadSnapshot(ns, bits, left, right, lt, rt, gyro, accel, touches, battery);
        return true;
    }

    /// <summary>
    /// 共通部 (47B): [0]=flag0 (0x01 互換振動 | 0x02 ハプティクス選択), [1]=flag1 (0x04 ライトバー),
    /// [2]=右(小), [3]=左(大), [44..46]=RGB。USB: 0x02 + 共通部 (48B)。BT: 0x31, [1]=seq<<4, [2]=0x10, [3..]=共通部, 末尾 CRC32 (prefix 0xA2)。
    /// </summary>
    public byte[] BuildOutputReport(RumbleCommand c, bool bluetooth)
    {
        var common = new byte[47];
        common[0] = 0x01 | 0x02;
        common[1] = 0x04;
        common[2] = HidMath.Byte01(c.RightMotor);
        common[3] = HidMath.Byte01(c.LeftMotor);
        common[44] = 0x00; common[45] = 0x20; common[46] = 0xFF; // 既定 LED: 水色
        if (!bluetooth)
        {
            var usb = new byte[48];
            usb[0] = 0x02;
            common.CopyTo(usb, 1);
            return usb;
        }
        var bt = new byte[78];
        bt[0] = 0x31; bt[1] = 0x00; bt[2] = 0x10;
        common.CopyTo(bt, 3);
        HidMath.PutCrc32(bt, 0xA2);
        return bt;
    }

    public byte[][] HandshakeReports(bool bluetooth) => Array.Empty<byte[]>();

    /// <summary>BT ではフィーチャー 0x05 (キャリブレーション, 41B) を読むとフルレポート 0x31 に切り替わる [要検証]。</summary>
    public byte[]? ConnectFeatureRequest(bool bluetooth)
    {
        if (!bluetooth) return null;
        var b = new byte[41]; b[0] = 0x05; return b;
    }
}

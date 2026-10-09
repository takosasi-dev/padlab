using PadLab.Core.Model;

namespace PadLab.Input.Hid;

/// <summary>
/// DualShock 4 (054C:05C4 / 09CC) の入力・出力レポート。
/// 参照: DS4Windows (Ryochan7 フォーク) DS4Library/DS4Device.cs、Linux hid-sony.c、
///       https://www.psdevwiki.com/ps4/DS4-USB  https://www.psdevwiki.com/ps4/DS4-BT
/// USB: report 0x01 (64B)。BT: report 0x11 (78B)、USB レイアウトの offset +2。
/// BT で 0x01 (簡易 10B) が来た場合はスティック・ボタン・トリガーだけ読む。
/// </summary>
public sealed class DualShock4Report : IHidReportParser
{
    public Capabilities Capabilities => Capabilities.Rumble | Capabilities.Gyro | Capabilities.Accel | Capabilities.Touchpad | Capabilities.Battery | Capabilities.Led;

    public bool TryParse(ReadOnlySpan<byte> r, bool bluetooth, long ns, out GamepadSnapshot snapshot)
    {
        snapshot = GamepadSnapshot.Neutral;
        if (r.Length < 10) return false;
        int o;
        if (r[0] == 0x01) o = 0;
        else if (r[0] == 0x11 && bluetooth) o = 2;
        else return false;
        if (r.Length < o + 10) return false;

        // [要検証] 以下のオフセットは USB 実機で確認する (offset は USB 基準)
        var left = new Vector2F(HidMath.Axis8(r[o + 1]), -HidMath.Axis8(r[o + 2]));
        var right = new Vector2F(HidMath.Axis8(r[o + 3]), -HidMath.Axis8(r[o + 4]));
        var b5 = r[o + 5]; var b6 = r[o + 6]; var b7 = r[o + 7];

        var bits = HidMath.HatToBits(b5 & 0x0F);
        void Set(bool on, ButtonId id) { if (on) bits |= GamepadSnapshot.Bit(id); }
        Set((b5 & 0x10) != 0, ButtonId.X);      // □
        Set((b5 & 0x20) != 0, ButtonId.A);      // ×
        Set((b5 & 0x40) != 0, ButtonId.B);      // ○
        Set((b5 & 0x80) != 0, ButtonId.Y);      // △
        Set((b6 & 0x01) != 0, ButtonId.LB);     // L1
        Set((b6 & 0x02) != 0, ButtonId.RB);     // R1
        Set((b6 & 0x10) != 0, ButtonId.Share);
        Set((b6 & 0x20) != 0, ButtonId.Start);  // Options
        Set((b6 & 0x40) != 0, ButtonId.L3);
        Set((b6 & 0x80) != 0, ButtonId.R3);
        Set((b7 & 0x01) != 0, ButtonId.Guide);  // PS
        Set((b7 & 0x02) != 0, ButtonId.TouchpadClick);

        var lt = r[o + 8] / 255f;
        var rt = r[o + 9] / 255f;

        Vector3F? gyro = null, accel = null;
        float? battery = null;
        List<TouchPoint>? touches = null;
        if (r.Length >= o + 43)
        {
            gyro = HidMath.Vec3(r, o + 13);
            accel = HidMath.Vec3(r, o + 19);
            battery = Math.Min(r[o + 30] & 0x0F, 10) / 10f; // 下位 4bit 0..10 (USB 充電中は 11 まで)
            // タッチ: 35: id (bit7 = 非アクティブ), 36-38: 12bit x / 12bit y。2 点目は +4
            for (var i = 0; i < 2; i++)
            {
                var t = o + 35 + i * 4;
                if ((r[t] & 0x80) != 0) continue;
                var x = r[t + 1] | ((r[t + 2] & 0x0F) << 8);
                var y = (r[t + 2] >> 4) | (r[t + 3] << 4);
                (touches ??= new List<TouchPoint>()).Add(new TouchPoint(r[t] & 0x7F, x / 1920f, y / 942f));
            }
        }

        snapshot = new GamepadSnapshot(ns, bits, left, right, lt, rt, gyro, accel, touches, battery);
        return true;
    }

    /// <summary>
    /// USB: 0x05 (32B) [1]=0xFF フラグ, [4]=右(小)モーター, [5]=左(大)モーター, [6..8]=RGB。
    /// BT: 0x11 (78B) [1]=0xC0, [3]=0x07, [6]=右, [7]=左, [8..10]=RGB, 末尾 CRC32 (prefix 0xA2)。
    /// </summary>
    public byte[] BuildOutputReport(RumbleCommand c, bool bluetooth)
    {
        var right = HidMath.Byte01(c.RightMotor);
        var left = HidMath.Byte01(c.LeftMotor);
        if (!bluetooth)
        {
            var usb = new byte[32];
            usb[0] = 0x05; usb[1] = 0xFF;
            usb[4] = right; usb[5] = left;
            usb[6] = 0x00; usb[7] = 0x20; usb[8] = 0xFF; // 既定 LED: 水色
            return usb;
        }
        var bt = new byte[78];
        bt[0] = 0x11; bt[1] = 0xC0; bt[3] = 0x07;
        bt[6] = right; bt[7] = left;
        bt[8] = 0x00; bt[9] = 0x20; bt[10] = 0xFF;
        HidMath.PutCrc32(bt, 0xA2);
        return bt;
    }

    public byte[][] HandshakeReports(bool bluetooth) => Array.Empty<byte[]>();

    /// <summary>BT ではフィーチャー 0x02 (37B) を読むとフルレポート 0x11 に切り替わる [要検証]。</summary>
    public byte[]? ConnectFeatureRequest(bool bluetooth) => bluetooth ? new byte[37] { 0x02, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0 } : null;
}

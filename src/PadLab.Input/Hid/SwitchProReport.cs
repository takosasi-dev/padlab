using PadLab.Core.Model;

namespace PadLab.Input.Hid;

/// <summary>
/// Nintendo Switch Pro Controller (057E:2009)。フルレポート 0x30 (USB/BT 共通レイアウト)。
/// 参照: dekuNukem/Nintendo_Switch_Reverse_Engineering (bluetooth_hid_notes.md, rumble_data_table.md)、
///       BetterJoy (Davidobot) Joycon.cs、Linux hid-nintendo.c
/// ボタン対応 (南=A 規約): B→A, A→B, Y→X, X→Y, −→Select, +→Start, Home→Guide, Capture→Capture, ZL/ZR→トリガー 1.0。
/// </summary>
public sealed class SwitchProReport : IHidReportParser
{
    public Capabilities Capabilities => Capabilities.Rumble | Capabilities.Gyro | Capabilities.Accel | Capabilities.Battery | Capabilities.Led;

    public bool TryParse(ReadOnlySpan<byte> r, bool bluetooth, long ns, out GamepadSnapshot snapshot)
    {
        snapshot = GamepadSnapshot.Neutral;
        // 0x30 = フル (IMU 付き)、0x21 = サブコマンド応答 (同じ位置に入力あり)。0x3F (簡易) は非対応
        if (r.Length < 12 || (r[0] != 0x30 && r[0] != 0x21)) return false;

        // [要検証] オフセットは dekuNukem 資料に基づく
        var b3 = r[3]; var b4 = r[4]; var b5 = r[5];
        ulong bits = 0;
        void Set(bool on, ButtonId id) { if (on) bits |= GamepadSnapshot.Bit(id); }
        Set((b3 & 0x01) != 0, ButtonId.X);      // Y
        Set((b3 & 0x02) != 0, ButtonId.Y);      // X
        Set((b3 & 0x04) != 0, ButtonId.A);      // B
        Set((b3 & 0x08) != 0, ButtonId.B);      // A
        Set((b3 & 0x40) != 0, ButtonId.RB);     // R
        Set((b4 & 0x01) != 0, ButtonId.Select); // −
        Set((b4 & 0x02) != 0, ButtonId.Start);  // +
        Set((b4 & 0x04) != 0, ButtonId.R3);
        Set((b4 & 0x08) != 0, ButtonId.L3);
        Set((b4 & 0x10) != 0, ButtonId.Guide);  // Home
        Set((b4 & 0x20) != 0, ButtonId.Capture);
        Set((b5 & 0x01) != 0, ButtonId.DDown);
        Set((b5 & 0x02) != 0, ButtonId.DUp);
        Set((b5 & 0x04) != 0, ButtonId.DRight);
        Set((b5 & 0x08) != 0, ButtonId.DLeft);
        Set((b5 & 0x40) != 0, ButtonId.LB);     // L
        var rt = (b3 & 0x80) != 0 ? 1f : 0f;    // ZR (デジタル)
        var lt = (b5 & 0x80) != 0 ? 1f : 0f;    // ZL (デジタル)

        // スティック 12bit×2: x = d0 | (d1&0xF)<<8, y = d1>>4 | d2<<4。Switch は y 上が + なので反転しない [要検証]
        var left = Stick12(r, 6);
        var right = Stick12(r, 9);

        Vector3F? accel = null, gyro = null;
        if (r[0] == 0x30 && r.Length >= 25)
        {
            // IMU 3 サンプル × 12B (accel xyz, gyro xyz)。最新の 1 サンプル目 (13..24) のみ使う
            accel = HidMath.Vec3(r, 13);
            gyro = HidMath.Vec3(r, 19);
        }
        // battery: 上位 4bit 8=満, 6, 4, 2=残りわずか, 0
        float? battery = Math.Clamp((r[2] >> 4) / 8f, 0f, 1f);

        snapshot = new GamepadSnapshot(ns, bits, left, right, lt, rt, gyro, accel, null, battery);
        return true;
    }

    private static Vector2F Stick12(ReadOnlySpan<byte> r, int o)
    {
        var x = r[o] | ((r[o + 1] & 0x0F) << 8);
        var y = (r[o + 1] >> 4) | (r[o + 2] << 4);
        return new Vector2F(HidMath.Axis12(x), HidMath.Axis12(y));
    }

    private static byte _counter;
    private static byte NextCounter() => _counter = (byte)((_counter + 1) & 0x0F);

    /// <summary>サブコマンド: [0]=0x01, [1]=カウンタ, [2..9]=振動 (中立), [10]=subcmd, [11..]=引数。</summary>
    internal static byte[] Subcommand(byte id, params byte[] args)
    {
        var b = new byte[Math.Max(12, 11 + args.Length)];
        b[0] = 0x01; b[1] = NextCounter();
        NeutralRumble.CopyTo(b, 2); NeutralRumble.CopyTo(b, 6);
        b[10] = id;
        args.CopyTo(b, 11);
        return b;
    }

    private static readonly byte[] NeutralRumble = { 0x00, 0x01, 0x40, 0x40 };

    /// <summary>
    /// USB: 0x80 0x02 (handshake) → 0x80 0x04 (USB タイムアウト無効) の後にサブコマンド。BT はサブコマンドのみ。
    /// 0x03 0x30 = フルレポートモード、0x40 0x01 = IMU 有効、0x48 0x01 = 振動有効、0x30 0x01 = プレイヤー LED 1。
    /// </summary>
    public byte[][] HandshakeReports(bool bluetooth)
    {
        var list = new List<byte[]>();
        if (!bluetooth)
        {
            list.Add(new byte[] { 0x80, 0x02 });
            list.Add(new byte[] { 0x80, 0x04 });
        }
        list.Add(Subcommand(0x03, 0x30));
        list.Add(Subcommand(0x40, 0x01));
        list.Add(Subcommand(0x48, 0x01));
        list.Add(Subcommand(0x30, 0x01));
        return list.ToArray();
    }

    /// <summary>振動レポート 0x10: [1]=カウンタ, [2..5]=左, [6..9]=右。</summary>
    public byte[] BuildOutputReport(RumbleCommand c, bool bluetooth)
    {
        var b = new byte[10];
        b[0] = 0x10; b[1] = NextCounter();
        EncodeRumble(c.LeftMotor).CopyTo(b, 2);
        EncodeRumble(c.RightMotor).CopyTo(b, 6);
        return b;
    }

    /// <summary>
    /// HD 振動の 4 バイト符号化 (BetterJoy Joycon.cs SetRumble の移植)。周波数は低 160Hz / 高 320Hz 固定。
    /// amp=0 は中立 00 01 40 40。[要検証] 実機での体感強度
    /// </summary>
    internal static byte[] EncodeRumble(float amp)
    {
        var data = new byte[4];
        if (amp <= 0f) { NeutralRumble.CopyTo(data, 0); return data; }
        amp = Math.Clamp(amp, 0f, 1f);
        const double lowHz = 160, highHz = 320;
        var hf = (ushort)((Math.Round(32 * Math.Log2(highHz * 0.1)) - 0x60) * 4);
        var lf = (byte)(Math.Round(32 * Math.Log2(lowHz * 0.1)) - 0x40);
        byte hfAmp;
        var l = Math.Log2(amp * 1000) * 32;
        if (amp < 0.117) hfAmp = (byte)((l - 0x60) / (5 - Math.Pow(amp, 2)) - 1);
        else if (amp < 0.23) hfAmp = (byte)(l - 0x60 - 0x5C);
        else hfAmp = (byte)((l - 0x60) * 2 - 0xF6);
        var lfAmp = (ushort)(hfAmp * 0.5);
        var parity = (byte)(lfAmp % 2);
        if (parity > 0) lfAmp--;
        lfAmp = (ushort)(lfAmp >> 1);
        lfAmp += 0x40;
        if (parity > 0) lfAmp |= 0x8000;
        data[0] = (byte)(hf & 0xFF);
        data[1] = (byte)(((hf >> 8) & 0xFF) + hfAmp);
        data[2] = (byte)(lf + ((lfAmp >> 8) & 0xFF));
        data[3] = (byte)(lfAmp & 0xFF);
        return data;
    }
}

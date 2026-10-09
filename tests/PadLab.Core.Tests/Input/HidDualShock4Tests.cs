using PadLab.Core.Model;
using PadLab.Input.Hid;

namespace PadLab.Core.Tests.Input;

public class HidDualShock4Tests
{
    private static byte[] Usb()
    {
        var r = new byte[64];
        r[0] = 0x01;
        r[1] = 128; r[2] = 128; r[3] = 128; r[4] = 128; // sticks 中立付近
        r[5] = 0x08; // hat 中立
        r[35] = 0x80; r[39] = 0x80; // タッチ 2 点とも非アクティブ
        return r;
    }

    private static GamepadSnapshot Parse(byte[] r, bool bt = false)
    {
        Assert.True(new DualShock4Report().TryParse(r, bt, 123, out var s));
        return s;
    }

    [Fact]
    public void ニュートラル()
    {
        var s = Parse(Usb());
        Assert.Equal(0UL, s.Buttons);
        Assert.InRange(s.LeftStick.Length, 0, 0.01f);
        Assert.Equal(0f, s.LeftTrigger);
        Assert.Equal(new Vector3F(0, 0, 0), s.Gyro);
        Assert.Equal(123, s.TimestampNs);
    }

    [Fact]
    public void バツ押下_は_A()
    {
        var r = Usb(); r[5] |= 0x20;
        var s = Parse(r);
        Assert.True(s.IsDown(ButtonId.A));
        Assert.Equal(GamepadSnapshot.Bit(ButtonId.A), s.Buttons);
    }

    [Fact]
    public void LX最大_と_LY上()
    {
        var r = Usb(); r[1] = 255; r[2] = 0;
        var s = Parse(r);
        Assert.Equal(1f, s.LeftStick.X);
        Assert.Equal(1f, s.LeftStick.Y);
    }

    [Fact]
    public void ジャイロ非ゼロ_と_L2()
    {
        var r = Usb();
        r[13] = 0x10; r[14] = 0x00; // gx = 16
        r[17] = 0xFF; r[18] = 0xFF; // gz = -1
        r[8] = 255;
        var s = Parse(r);
        Assert.Equal(new Vector3F(16, 0, -1), s.Gyro);
        Assert.Equal(1f, s.LeftTrigger);
    }

    [Fact]
    public void BT_0x11_は_offset_2()
    {
        var r = new byte[78];
        r[0] = 0x11; r[1] = 0xC0;
        r[3] = 255; r[4] = 128; r[5] = 128; r[6] = 128; r[7] = 0x28; // LX max, □... hat 8 + ×
        var s = Parse(r, bt: true);
        Assert.Equal(1f, s.LeftStick.X);
        Assert.True(s.IsDown(ButtonId.A));
    }

    [Fact]
    public void 出力レポート_USB_と_BT()
    {
        var p = new DualShock4Report();
        var usb = p.BuildOutputReport(new RumbleCommand(1f, 0.5f), false);
        Assert.Equal(32, usb.Length);
        Assert.Equal(0x05, usb[0]);
        Assert.Equal(255, usb[5]);
        Assert.Equal(128, usb[4]);
        var bt = p.BuildOutputReport(new RumbleCommand(1f, 0.5f), true);
        Assert.Equal(78, bt.Length);
        Assert.Equal(0x11, bt[0]);
        // CRC32 が末尾に入っている (全部 0 でない)
        Assert.NotEqual(0u, BitConverter.ToUInt32(bt, 74));
    }

    [Fact]
    public void Crc32_既知値()
    {
        // "123456789" の CRC-32 = 0xCBF43926 (prefix 無しの検算のため 0x00 を prefix にした値では無く、標準関数と一致させる)
        var data = "123456789"u8.ToArray();
        // prefix 分を含めるため、prefix=data[0] + 残りで検算
        Assert.Equal(0xCBF43926u, HidMath.Crc32(data.AsSpan(1), data[0]));
    }
}

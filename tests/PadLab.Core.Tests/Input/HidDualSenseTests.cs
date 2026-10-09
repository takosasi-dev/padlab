using PadLab.Core.Model;
using PadLab.Input.Hid;

namespace PadLab.Core.Tests.Input;

public class HidDualSenseTests
{
    private static byte[] Usb()
    {
        var r = new byte[64];
        r[0] = 0x01;
        r[1] = 128; r[2] = 128; r[3] = 128; r[4] = 128;
        r[8] = 0x08; // hat 中立
        r[53] = 0x0A; // battery 100%
        r[33] = 0x80; r[37] = 0x80; // タッチ 2 点とも非アクティブ
        return r;
    }

    private static GamepadSnapshot Parse(byte[] r, bool bt = false, bool edge = false)
    {
        Assert.True(new DualSenseReport(edge).TryParse(r, bt, 5, out var s));
        return s;
    }

    [Fact]
    public void ニュートラル()
    {
        var s = Parse(Usb());
        Assert.Equal(0UL, s.Buttons);
        Assert.InRange(s.RightStick.Length, 0, 0.01f);
        Assert.Equal(0f, s.RightTrigger);
        Assert.Equal(1f, s.Battery);
        Assert.Null(s.Touches);
    }

    [Fact]
    public void バツ押下_は_A_と_ミュート()
    {
        var r = Usb(); r[8] |= 0x20; r[10] = 0x04;
        var s = Parse(r);
        Assert.Equal(GamepadSnapshot.Bit(ButtonId.A) | GamepadSnapshot.Bit(ButtonId.Mute), s.Buttons);
    }

    [Fact]
    public void LX最大()
    {
        var r = Usb(); r[1] = 255; r[2] = 255;
        var s = Parse(r);
        Assert.Equal(1f, s.LeftStick.X);
        Assert.Equal(-1f, s.LeftStick.Y);
    }

    [Fact]
    public void ジャイロ非ゼロ_と_タッチ()
    {
        var r = Usb();
        r[18] = 0x34; r[19] = 0x12; // gy = 0x1234
        r[33] = 0x01; r[34] = 0x80; r[35] = 0x07; r[36] = 0x21; // id 1, x = 0x780 = 1920, y = 0x210 = 528
        var s = Parse(r);
        Assert.Equal(new Vector3F(0, 0x1234, 0), s.Gyro);
        var t = Assert.Single(s.Touches!);
        Assert.Equal(1, t.Id);
        Assert.Equal(1f, t.X);
        Assert.InRange(t.Y, 0.48f, 0.49f);
    }

    [Fact]
    public void Edge_のみ_Fn_と_パドル()
    {
        var r = Usb(); r[10] = 0x10 | 0x40;
        Assert.Equal(0UL, Parse(r).Buttons);
        Assert.Equal(GamepadSnapshot.Bit(ButtonId.Fn1) | GamepadSnapshot.Bit(ButtonId.P1), Parse(r, edge: true).Buttons);
    }

    [Fact]
    public void BT_0x31_は_offset_1()
    {
        var r = new byte[78];
        r[0] = 0x31; r[1] = 0x01;
        r[2] = 128; r[3] = 128; r[4] = 128; r[5] = 128; r[7] = 255; r[9] = 0x08 | 0x40;
        var s = Parse(r, bt: true);
        Assert.Equal(1f, s.RightTrigger);
        Assert.True(s.IsDown(ButtonId.B));
    }

    [Fact]
    public void 出力レポート()
    {
        var p = new DualSenseReport(false);
        var usb = p.BuildOutputReport(new RumbleCommand(0.5f, 1f), false);
        Assert.Equal(48, usb.Length);
        Assert.Equal(0x02, usb[0]);
        Assert.Equal(0x03, usb[1]);
        Assert.Equal(255, usb[3]); // 右
        Assert.Equal(128, usb[4]); // 左
        var bt = p.BuildOutputReport(RumbleCommand.Stop, true);
        Assert.Equal(78, bt.Length);
        Assert.Equal(0x31, bt[0]);
        Assert.Equal(0x10, bt[2]);
        Assert.NotEqual(0u, BitConverter.ToUInt32(bt, 74));
    }
}

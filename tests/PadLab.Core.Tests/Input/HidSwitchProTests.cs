using PadLab.Core.Model;
using PadLab.Input.Hid;

namespace PadLab.Core.Tests.Input;

public class HidSwitchProTests
{
    private static byte[] Full()
    {
        var r = new byte[49];
        r[0] = 0x30; r[2] = 0x80; // battery 満
        // 左右スティック中立 0x800/0x800: d0=0x00, d1=0x08, d2=0x80
        r[6] = 0x00; r[7] = 0x08; r[8] = 0x80;
        r[9] = 0x00; r[10] = 0x08; r[11] = 0x80;
        return r;
    }

    private static GamepadSnapshot Parse(byte[] r)
    {
        Assert.True(new SwitchProReport().TryParse(r, true, 9, out var s));
        return s;
    }

    [Fact]
    public void ニュートラル()
    {
        var s = Parse(Full());
        Assert.Equal(0UL, s.Buttons);
        Assert.Equal(Vector2F.Zero, s.LeftStick);
        Assert.Equal(Vector2F.Zero, s.RightStick);
        Assert.Equal(1f, s.Battery);
        Assert.Equal(new Vector3F(0, 0, 0), s.Gyro);
    }

    [Fact]
    public void B押下_は_A_と_ZR()
    {
        var r = Full(); r[3] = 0x04 | 0x80;
        var s = Parse(r);
        Assert.Equal(GamepadSnapshot.Bit(ButtonId.A), s.Buttons);
        Assert.Equal(1f, s.RightTrigger);
    }

    [Fact]
    public void LX最大()
    {
        var r = Full(); r[6] = 0xFF; r[7] = 0x0F; // x = 0xFFF, y = 0x800
        var s = Parse(r);
        Assert.Equal(1f, s.LeftStick.X);
        Assert.Equal(0f, s.LeftStick.Y);
    }

    [Fact]
    public void ジャイロ非ゼロ()
    {
        var r = Full();
        r[13] = 0x01; r[14] = 0x00; // accel x = 1
        r[21] = 0x00; r[22] = 0x80; // gyro y = -32768
        var s = Parse(r);
        Assert.Equal(new Vector3F(1, 0, 0), s.Accel);
        Assert.Equal(new Vector3F(0, -32768, 0), s.Gyro);
    }

    [Fact]
    public void 簡易レポート_0x3F_は_非対応()
    {
        var r = new byte[12]; r[0] = 0x3F;
        Assert.False(new SwitchProReport().TryParse(r, true, 0, out _));
    }

    [Fact]
    public void ハンドシェイク_USB_は_0x80_から()
    {
        var usb = new SwitchProReport().HandshakeReports(false);
        Assert.Equal(new byte[] { 0x80, 0x02 }, usb[0]);
        Assert.Equal(new byte[] { 0x80, 0x04 }, usb[1]);
        Assert.Equal(0x01, usb[2][0]);
        Assert.Equal(0x03, usb[2][10]);
        Assert.Equal(0x30, usb[2][11]);
        var bt = new SwitchProReport().HandshakeReports(true);
        Assert.All(bt, b => Assert.Equal(0x01, b[0]));
    }

    [Fact]
    public void 振動符号化()
    {
        Assert.Equal(new byte[] { 0x00, 0x01, 0x40, 0x40 }, SwitchProReport.EncodeRumble(0f));
        var full = SwitchProReport.EncodeRumble(1f);
        Assert.NotEqual(new byte[] { 0x00, 0x01, 0x40, 0x40 }, full);
        var rep = new SwitchProReport().BuildOutputReport(new RumbleCommand(1f, 0f), true);
        Assert.Equal(10, rep.Length);
        Assert.Equal(0x10, rep[0]);
        Assert.Equal(new byte[] { 0x00, 0x01, 0x40, 0x40 }, rep[6..10]);
    }

    [Fact]
    public void For_で_機種ごとのパーサ()
    {
        Assert.IsType<DualShock4Report>(HidReportParsers.For(DeviceKind.DualShock4));
        Assert.IsType<DualSenseReport>(HidReportParsers.For(DeviceKind.DualSenseEdge));
        Assert.IsType<SwitchProReport>(HidReportParsers.For(DeviceKind.SwitchPro));
        Assert.Null(HidReportParsers.For(DeviceKind.XboxOne));
        Assert.True(HidReportParsers.For(DeviceKind.DualSenseEdge)!.Capabilities.HasFlag(Capabilities.PhysicalPaddles));
    }
}

using System.Runtime.InteropServices;
using PadLab.Input.Native;

namespace PadLab.Core.Tests.Input;

public class XInputStructTests
{
    [Fact]
    public void 構造体サイズ_が_Win32_定義と一致()
    {
        Assert.Equal(12, Marshal.SizeOf<XInput.XINPUT_GAMEPAD>());
        Assert.Equal(16, Marshal.SizeOf<XInput.XINPUT_STATE>());
        Assert.Equal(4, Marshal.SizeOf<XInput.XINPUT_VIBRATION>());
    }
}

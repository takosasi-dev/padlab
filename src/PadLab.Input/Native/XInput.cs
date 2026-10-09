using System.Runtime.InteropServices;

namespace PadLab.Input.Native;

/// <summary>
/// xinput1_4.dll の P/Invoke。用途は Guide ボタン (XInputGetStateEx = ordinal 100 の隠し API) と
/// WGI が使えない場合の振動フォールバックのみ。通常入力は WGI 側で読む。
/// 参照: https://learn.microsoft.com/windows/win32/api/xinput/ns-xinput-xinput_gamepad
/// </summary>
internal static class XInput
{
    internal const uint ErrorSuccess = 0;
    internal const uint ErrorDeviceNotConnected = 1167;

    // wButtons
    internal const ushort DPadUp = 0x0001, DPadDown = 0x0002, DPadLeft = 0x0004, DPadRight = 0x0008;
    internal const ushort Start = 0x0010, Back = 0x0020, LeftThumb = 0x0040, RightThumb = 0x0080;
    internal const ushort LeftShoulder = 0x0100, RightShoulder = 0x0200;
    /// <summary>XInputGetStateEx でのみ立つ。</summary>
    internal const ushort Guide = 0x0400;
    internal const ushort A = 0x1000, B = 0x2000, X = 0x4000, Y = 0x8000;

    [StructLayout(LayoutKind.Sequential)]
    internal struct XINPUT_GAMEPAD
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XINPUT_STATE
    {
        public uint dwPacketNumber;
        public XINPUT_GAMEPAD Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XINPUT_VIBRATION
    {
        public ushort wLeftMotorSpeed;
        public ushort wRightMotorSpeed;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    internal static extern uint XInputGetState(uint dwUserIndex, out XINPUT_STATE pState);

    /// <summary>非公開の XInputGetStateEx (ordinal 100)。Guide ボタン (0x0400) が取れる。</summary>
    [DllImport("xinput1_4.dll", EntryPoint = "#100")]
    internal static extern uint XInputGetStateEx(uint dwUserIndex, out XINPUT_STATE pState);

    [DllImport("xinput1_4.dll", EntryPoint = "XInputSetState")]
    internal static extern uint XInputSetState(uint dwUserIndex, ref XINPUT_VIBRATION pVibration);

    /// <summary>接続中のユーザーインデックス (0..3) を順に返す。DLL が無い等の例外は空扱い。</summary>
    internal static IEnumerable<int> ConnectedUserIndices()
    {
        var list = new List<int>(4);
        try
        {
            for (uint i = 0; i < 4; i++)
                if (XInputGetState(i, out _) == ErrorSuccess) list.Add((int)i);
        }
        catch (Exception) { }
        return list;
    }
}

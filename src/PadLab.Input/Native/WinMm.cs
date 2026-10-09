using System.Runtime.InteropServices;

namespace PadLab.Input.Native;

/// <summary>winmm.dll のタイマー分解能制御。PollingLoop の Sleep(1) を実際に 1ms にするため。</summary>
internal static class WinMm
{
    [DllImport("winmm.dll")]
    internal static extern uint timeBeginPeriod(uint uPeriod);

    [DllImport("winmm.dll")]
    internal static extern uint timeEndPeriod(uint uPeriod);
}

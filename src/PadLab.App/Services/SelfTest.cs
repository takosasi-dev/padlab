using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using PadLab.Core.Model;
using PadLab.Core.Profiles;
using PadLab.Core.Recording;
using PadLab.Input;
using SkiaSharp;
using Vortice.DirectInput;

namespace PadLab.App.Services;

/// <summary>--selftest。各ライブラリ・ネイティブ依存が単体 exe で動くかを順に確認する。</summary>
public static class SelfTest
{
    [DllImport("kernel32.dll")] private static extern bool AttachConsole(int pid);

    // PadLab.Input.Native.XInput は internal のため、ここで最小限だけ再宣言する
    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern uint XInputGetState(uint dwUserIndex, byte[] pState);

    public static int Run()
    {
        AttachConsole(-1);
        var sb = new StringBuilder();
        var allOk = true;

        void Check(string name, Func<string> body)
        {
            string line;
            try { line = $"OK  {name}: {body()}"; }
            catch (Exception ex) { line = $"NG  {name}: {ex.GetType().Name}: {ex.Message}"; allOk = false; }
            sb.AppendLine(line);
            try { Console.WriteLine(line); } catch (Exception) { }
        }

        Check("SkiaSharp", () => { using var b = new SKBitmap(1, 1); return $"SKBitmap {b.Width}x{b.Height}"; });
        Check("HidSharp", () => $"{HidSharp.DeviceList.Local.GetHidDevices().Count()} HID devices");
        Check("XInput", () =>
        {
            var r = XInputGetState(0, new byte[16]);
            return r is 0 or 1167 ? $"XInputGetState(0) = {r}" : throw new InvalidOperationException($"XInputGetState(0) = {r}");
        });
        Check("WGI", () => $"Gamepad.Gamepads.Count = {Windows.Gaming.Input.Gamepad.Gamepads.Count}");
        Check("Vortice.DirectInput", () => { using var di = DInput.DirectInput8Create(); return "DirectInput8Create"; });
        Check("ProfileStore", () =>
        {
            var store = new ProfileStore();
            var kinds = Enum.GetValues<DeviceKind>();
            foreach (var k in kinds) _ = store.For(k);
            return $"{kinds.Length} profiles";
        });
        Check("SessionStore", () =>
        {
            var dir = SessionStore.DefaultDirectory(AppContext.BaseDirectory);
            Directory.CreateDirectory(dir);
            var probe = Path.Combine(dir, ".selftest-write");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return dir;
        });
        Check("WindowsDeviceWatcher", () =>
        {
            using var w = new WindowsDeviceWatcher(new ProfileStore());
            w.Scan();
            return $"{w.Devices.Count} devices (Fake 込み)";
        });

        var summary = allOk ? "selftest: ALL OK" : "selftest: FAILED";
        sb.AppendLine(summary);
        try { Console.WriteLine(summary); } catch (Exception) { }

        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PadLab");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "selftest.txt"), sb.ToString(), new UTF8Encoding(true)); // BOM 付き: PowerShell 5.1 の Get-Content でも文字化けしない
        }
        catch (Exception) { }
        return allOk ? 0 : 1;
    }
}

using PadLab.Core.Recording;

namespace PadLab.Core.Tests.Recording;

internal static class TestSessions
{
    public static DiagnosticSession Minimal(string id = "ab12cd34ef", DateTimeOffset? at = null, string notes = "") => new(
        id,
        at ?? new DateTimeOffset(2026, 9, 3, 17, 15, 0, TimeSpan.FromHours(9)),
        "0.1.0",
        new SessionDevice("DualSense", "Bluetooth", "VID 0x054C PID 0x0CE6", "dualsense/1", null),
        new SessionResults(
            [new("A", "×", "Standard", true), new("B", "○", "Standard", false)],
            null,
            [new("Left", 0.01f, 0.01f, 0.06f, 0.03f, 12f, "Minor", 0.98f, 0.96f, 0.01f, -0.02f, 8.5, 0.05f, 0, 0.001f, 0.004f),
             new("Right", 0f, 0f, 0.01f, 0.005f, 90f, "None", 0.97f, null, null, null, null, null, null, null, null)],
            [new("Left", 1f), new("Right", 0.5f)],
            [new("LeftMotor", true, true), new("RightMotor", false, null)],
            null,
            new(250.3, 0.4, true),
            [new("A", 50, 0, 8.0)],
            new(600, 1, 0, 0, 0, 0),
            9,
            "最大ズレ 0.06 + ノイズ 0.004×2 + 余裕 0.02 = 0.09 → 9%"),
        new Dictionary<string, string> { ["A"] = "決定" },
        notes);
}

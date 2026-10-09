using PadLab.Core.Model;
using PadLab.Core.Recording;

namespace PadLab.Core.Tests.Recording;

public class SessionStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "padlab_sessions_" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

    [Fact]
    public void MakeFileName_strips_invalid_chars()
    {
        var name = SessionStore.MakeFileName(new DateTimeOffset(2026, 9, 3, 17, 15, 0, TimeSpan.FromHours(9)),
            DeviceKind.DualSense, ConnectionType.Bluetooth, "a/b:c*?d<>|1234");
        Assert.Equal("20260903-171500_DualSense_Bluetooth_abcd1234.json", name);
        Assert.DoesNotContain(name, c => Path.GetInvalidFileNameChars().Contains(c));
    }

    [Fact]
    public void Save_then_Load_roundtrips()
    {
        var store = new SessionStore(_dir);
        var s = TestSessions.Minimal(notes: "メモ \"引用\" 日本語");
        var path = store.Save(s);
        Assert.True(File.Exists(path));
        var loaded = store.Load(path);
        Assert.Equal(s.SessionId, loaded.SessionId);
        Assert.Equal(s.MeasuredAt, loaded.MeasuredAt);
        Assert.Equal(s.Notes, loaded.Notes);
        Assert.Equal(s.Device, loaded.Device);
        Assert.Equal(s.Results.Buttons, loaded.Results.Buttons);
        Assert.Equal(s.Results.Sticks, loaded.Results.Sticks);
        Assert.Equal(s.Results.Vibration, loaded.Results.Vibration);
        Assert.Equal(s.Results.LatencyReference, loaded.Results.LatencyReference);
        Assert.Equal(s.Results.Idle, loaded.Results.Idle);
        Assert.Equal(s.Results.DeadzoneRationale, loaded.Results.DeadzoneRationale);
        Assert.Equal("決定", loaded.ButtonAliases["A"]);
        Assert.Null(loaded.Results.Paddles);
        Assert.Contains("\"session_id\"", File.ReadAllText(path));
    }

    [Fact]
    public void Save_ignores_notes_for_path()
    {
        var store = new SessionStore(_dir);
        var path = store.Save(TestSessions.Minimal(id: "../evil", notes: "../evil"));
        Assert.Equal(Path.GetFullPath(_dir), Path.GetDirectoryName(Path.GetFullPath(path)));
        Assert.DoesNotContain("..", Path.GetFileName(path));
    }

    [Fact]
    public void List_is_newest_first()
    {
        var store = new SessionStore(_dir);
        var t0 = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        store.Save(TestSessions.Minimal("old", t0));
        store.Save(TestSessions.Minimal("new", t0.AddDays(2)));
        store.Save(TestSessions.Minimal("mid", t0.AddDays(1)));
        var list = store.List();
        Assert.Equal(["new", "mid", "old"], list.Select(x => x.SessionId).ToArray());
        Assert.Equal("DualSense", list[0].DeviceType);
    }

    [Fact]
    public void ExportText_contains_key_values_and_disclaimer()
    {
        var text = new SessionStore(_dir).ExportText(TestSessions.Minimal());
        Assert.Contains("DualSense", text);
        Assert.Contains("参考値", text);
        Assert.DoesNotContain("故障しています", text);
    }

    [Fact]
    public void DefaultDirectory_prefers_portable()
    {
        Directory.CreateDirectory(_dir);
        Assert.NotEqual(Path.Combine(_dir, "sessions"), SessionStore.DefaultDirectory(_dir));
        File.WriteAllText(Path.Combine(_dir, "portable.txt"), "");
        Assert.Equal(Path.Combine(_dir, "sessions"), SessionStore.DefaultDirectory(_dir));
    }
}

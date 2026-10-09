#if TRACKS_READY
// TRACKS_READY は Track A/B/C の成果物 (PaddleMapper.cs, SessionStore.cs, WindowsDeviceWatcher.cs) が
// 存在するときに PadLab.App.csproj が自動定義する。未着の間はこのファイル全体をコンパイルしない。
using System.Collections.ObjectModel;
using System.Windows;
using PadLab.App.Settings;
using PadLab.Core.Fake;
using PadLab.Core.Model;
using PadLab.Core.Profiles;
using PadLab.Core.Recording;
using PadLab.Input;

namespace PadLab.App.Services;

/// <summary>アプリ全体の状態。DI なしの static Instance。</summary>
public sealed class AppState
{
    public static AppState Instance { get; } = new();

    public AppSettings Settings => App.Settings;
    public ProfileStore Profiles { get; } = new();
    public SessionStore Sessions { get; }
    public IDeviceWatcher Watcher { get; }
    /// <summary>UI スレッドで更新される。</summary>
    public ObservableCollection<DeviceDescriptor> Devices { get; } = new();
    public DeviceSession? Current { get; private set; }
    public DemoDriver Demo { get; }
    public event Action? CurrentChanged;
    public bool MotionEnabled => App.MotionEnabled;

    private readonly Dictionary<string, IGamepadSource> _sources = new();

    private AppState()
    {
        Sessions = new SessionStore(SessionStore.DefaultDirectory(AppContext.BaseDirectory));
        Enum.TryParse<DeviceKind>(Settings.FakeKind, true, out var fakeKind);
        var watcher = new WindowsDeviceWatcher(Profiles, Settings.ShowFakeDevice, fakeKind);
        Watcher = watcher;
        Demo = new DemoDriver(watcher.Fake);
        watcher.Added += src => OnUi(() =>
        {
            _sources[src.Descriptor.DeviceId] = src;
            var i = IndexOf(src.Descriptor.DeviceId);
            if (i >= 0) Devices[i] = src.Descriptor; else Devices.Add(src.Descriptor);
        });
        watcher.Removed += id => OnUi(() =>
        {
            _sources.Remove(id);
            var i = IndexOf(id);
            if (i >= 0) Devices.RemoveAt(i);
            if (Current?.Source.Descriptor.DeviceId == id) Clear();
        });
        watcher.Start();
    }

    private int IndexOf(string id)
    {
        for (var i = 0; i < Devices.Count; i++) if (Devices[i].DeviceId == id) return i;
        return -1;
    }

    private static void OnUi(Action a) => Application.Current.Dispatcher.BeginInvoke(a);

    public void Select(string deviceId)
    {
        if (Current?.Source.Descriptor.DeviceId == deviceId) return;
        if (!_sources.TryGetValue(deviceId, out var src)) return;
        Clear();
        Current = new DeviceSession(src, Profiles.For(src.Descriptor.Kind), Settings);
        if (src is FakeGamepad) Demo.Start();
        CurrentChanged?.Invoke();
    }

    private void Clear()
    {
        Demo.Stop();
        Current?.Dispose();
        Current = null;
        CurrentChanged?.Invoke();
    }
}
#endif

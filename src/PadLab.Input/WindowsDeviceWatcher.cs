using HidSharp;
using PadLab.Core.Fake;
using PadLab.Core.Model;
using PadLab.Core.Profiles;
using PadLab.Input.Backends;
using PadLab.Input.Hid;
using PadLab.Input.Native;
using Vortice.DirectInput;
using Windows.Gaming.Input;

namespace PadLab.Input;

/// <summary>
/// 1 秒周期で WGI / 生 HID / DirectInput を列挙し、差分で Added / Removed を発火する。
/// バックエンド単位で例外を握り、他の列挙を止めない。includeFake なら FakeGamepad を常に 1 台含める。
/// </summary>
public sealed class WindowsDeviceWatcher : IDeviceWatcher
{
    private readonly ProfileStore _profiles;
    private readonly Dictionary<string, IGamepadSource> _sources = new();
    private readonly object _gate = new();
    private readonly bool _includeFake;
    private Timer? _timer;
    private IDirectInput8? _di;
    private int _scanning;

    public WindowsDeviceWatcher(ProfileStore profiles, bool includeFake = true, DeviceKind fakeKind = DeviceKind.Fake)
    {
        _profiles = profiles;
        _includeFake = includeFake;
        Fake = new FakeGamepad(fakeKind);
        if (includeFake) _sources[Fake.Descriptor.DeviceId] = Fake;
    }

    public FakeGamepad Fake { get; }
    public event Action<IGamepadSource>? Added;
    public event Action<string>? Removed;

    public IReadOnlyList<IGamepadSource> Devices
    {
        get { lock (_gate) return _sources.Values.ToArray(); }
    }

    public void Start()
    {
        if (_timer is not null) return;
        if (_includeFake) Added?.Invoke(Fake);
        _timer = new Timer(_ => Scan(), null, 0, 1000);
    }

    /// <summary>1 回分の列挙。テスト・selftest からも呼べる。</summary>
    public void Scan()
    {
        if (Interlocked.Exchange(ref _scanning, 1) == 1) return;
        try
        {
            var seen = new Dictionary<string, Func<IGamepadSource>>();
            if (_includeFake) seen[Fake.Descriptor.DeviceId] = () => Fake;
            try { EnumerateWgi(seen); } catch (Exception) { }
            try { EnumerateHid(seen); } catch (Exception) { }
            try { EnumerateDirectInput(seen); } catch (Exception) { }

            List<IGamepadSource> added = new();
            List<string> removed = new();
            lock (_gate)
            {
                foreach (var id in _sources.Keys.Where(k => !seen.ContainsKey(k)).ToList())
                {
                    var s = _sources[id];
                    _sources.Remove(id);
                    removed.Add(id);
                    try { s.Dispose(); } catch (Exception) { }
                }
                foreach (var (id, factory) in seen)
                {
                    if (_sources.ContainsKey(id)) continue;
                    try
                    {
                        var s = factory();
                        _sources[id] = s;
                        added.Add(s);
                    }
                    catch (Exception) { }
                }
            }
            foreach (var id in removed) Removed?.Invoke(id);
            foreach (var s in added) Added?.Invoke(s);
        }
        finally { Interlocked.Exchange(ref _scanning, 0); }
    }

    private void EnumerateWgi(Dictionary<string, Func<IGamepadSource>> seen)
    {
        var xinput = XInput.ConnectedUserIndices().ToList();
        var gamepads = Gamepad.Gamepads;
        for (var i = 0; i < gamepads.Count; i++)
        {
            var g = gamepads[i];
            RawGameController? raw = null;
            try { raw = RawGameController.FromGameController(g); } catch (Exception) { }
            var id = "xbox:" + (raw?.NonRoamableId ?? $"index{i}");
            var vid = raw?.HardwareVendorId ?? 0x045E;
            var pid = raw?.HardwareProductId ?? 0;
            var kind = ProfileStore.KindFromVidPid(vid, pid);
            if (kind == DeviceKind.Generic) kind = DeviceKind.XboxOne;
            var conn = raw?.IsWireless == true ? ConnectionType.Bluetooth : ConnectionType.Usb;
            // [要検証] WGI の並び順と XInput ユーザーインデックスの対応 (i 番目同士を対応させる)
            var xi = i < xinput.Count ? xinput[i] : -1;
            var name = string.IsNullOrWhiteSpace(raw?.DisplayName) ? _profiles.For(kind).Name : raw!.DisplayName;
            var desc = new DeviceDescriptor(id, vid, pid, $"{name} ({ProfileStore.VendorName(vid)})", conn, kind, BackendName: "WGI");
            seen[id] = () => new XboxSource(g, xi, desc);
        }
    }

    private void EnumerateHid(Dictionary<string, Func<IGamepadSource>> seen)
    {
        foreach (var dev in DeviceList.Local.GetHidDevices())
        {
            var vid = dev.VendorID; var pid = dev.ProductID;
            if (vid != 0x054C && vid != 0x057E) continue;
            var kind = ProfileStore.KindFromVidPid(vid, pid);
            var parser = HidReportParsers.For(kind);
            if (parser is null) continue;
            int maxIn;
            try { maxIn = dev.GetMaxInputReportLength(); } catch (Exception) { continue; }
            if (maxIn < 48) continue; // 同一 VID/PID の別インターフェース (オーディオ等) を除外
            var path = dev.DevicePath;
            var id = "hid:" + path;
            // [要検証] BT 判定: デバイスパスに "bth" (BTHENUM) または HID サービス UUID を含むか
            var bt = path.Contains("bth", StringComparison.OrdinalIgnoreCase)
                     || path.Contains("00001124-0000-1000-8000-00805f9b34fb", StringComparison.OrdinalIgnoreCase);
            string? serial = null;
            try { serial = dev.GetSerialNumber(); } catch (Exception) { }
            var desc = new DeviceDescriptor(id, vid, pid, $"{_profiles.For(kind).Name} ({ProfileStore.VendorName(vid)})",
                bt ? ConnectionType.Bluetooth : ConnectionType.Usb, kind, serial, "HID");
            seen[id] = () => new HidSource(dev, desc, parser);
        }
    }

    private void EnumerateDirectInput(Dictionary<string, Func<IGamepadSource>> seen)
    {
        _di ??= DInput.DirectInput8Create();
        var di = _di;
        foreach (var inst in di.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly))
        {
            // ProductGuid.Data1 = (PID << 16) | VID
            var data1 = BitConverter.ToUInt32(inst.ProductGuid.ToByteArray(), 0);
            var vid = (int)(data1 & 0xFFFF);
            var pid = (int)(data1 >> 16);
            // 計画: VID 045E/054C/057E は除外。ただし KindFromVidPid が Generic の Sony/Nintendo 製品は他バックエンドに乗らないので DI で拾う
            if (vid == 0x045E || ProfileStore.KindFromVidPid(vid, pid) != DeviceKind.Generic) continue;
            var id = "di:" + inst.InstanceGuid.ToString("D");
            var profile = _profiles.For(DeviceKind.Generic);
            var desc = new DeviceDescriptor(id, vid, pid, $"{inst.ProductName} ({ProfileStore.VendorName(vid)})", ConnectionType.Unknown, DeviceKind.Generic, BackendName: "DirectInput");
            seen[id] = () => new DirectInputSource(di, inst, desc, profile);
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
        lock (_gate)
        {
            foreach (var s in _sources.Values) { try { s.Dispose(); } catch (Exception) { } }
            _sources.Clear();
        }
        try { _di?.Dispose(); } catch (Exception) { }
    }
}

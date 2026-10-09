using System.Collections.Concurrent;
using HidSharp;
using PadLab.Core.Model;
using PadLab.Input.Hid;

namespace PadLab.Input.Backends;

/// <summary>
/// 生 HID 経由 (DS4 / DualSense / Switch Pro)。読み取りスレッドが TryParse 成功ごとにキューへ積み、TryRead が 1 件ずつ取り出す。
/// 読み取り例外 → Faulted、Capabilities を None、IsBroken = true。
/// </summary>
public sealed class HidSource : IGamepadSource
{
    private readonly HidDevice _device;
    private readonly IHidReportParser _parser;
    private readonly bool _bluetooth;
    private readonly ConcurrentQueue<GamepadSnapshot> _queue = new();
    private readonly HidStream? _stream;
    private readonly Thread? _reader;
    private volatile bool _running;
    private Capabilities _caps;

    public HidSource(HidDevice device, DeviceDescriptor descriptor, IHidReportParser parser)
    {
        _device = device;
        _parser = parser;
        Descriptor = descriptor;
        _bluetooth = descriptor.Connection == ConnectionType.Bluetooth;
        _caps = parser.Capabilities;
        try
        {
            _stream = device.Open();
            _stream.ReadTimeout = 100;
            _stream.WriteTimeout = 100;
            var feature = parser.ConnectFeatureRequest(_bluetooth);
            if (feature is not null)
            {
                try { _stream.GetFeature(feature); } catch (Exception) { /* [要検証] 失敗しても入力は簡易レポートで続く */ }
            }
            foreach (var rep in parser.HandshakeReports(_bluetooth))
            {
                try { _stream.Write(rep); Thread.Sleep(20); } catch (Exception) { /* Switch Pro の 0x80 系は BT で失敗しうる */ }
            }
            _running = true;
            _reader = new Thread(ReadLoop) { IsBackground = true, Name = "PadLab.HidSource" };
            _reader.Start();
        }
        catch (Exception ex)
        {
            Break(ex);
        }
    }

    public DeviceDescriptor Descriptor { get; }
    public Capabilities Capabilities => _caps;
    public bool IsBroken { get; private set; }
    public event Action<Exception>? Faulted;

    private void ReadLoop()
    {
        var buf = new byte[Math.Max(_device.GetMaxInputReportLength(), 64)];
        while (_running)
        {
            int n;
            try { n = _stream!.Read(buf, 0, buf.Length); }
            catch (TimeoutException) { continue; }
            catch (Exception ex) { Break(ex); return; }
            if (n <= 0) continue;
            if (_parser.TryParse(buf.AsSpan(0, n), _bluetooth, Clock.NowNs(), out var s)) _queue.Enqueue(s);
        }
    }

    private void Break(Exception ex)
    {
        _running = false;
        IsBroken = true;
        _caps = Capabilities.None;
        Faulted?.Invoke(ex);
    }

    public bool TryRead(out GamepadSnapshot snapshot) => _queue.TryDequeue(out snapshot!) || Neutral(out snapshot);

    private static bool Neutral(out GamepadSnapshot s) { s = GamepadSnapshot.Neutral; return false; }

    public void SetRumble(RumbleCommand command)
    {
        if (_stream is null || IsBroken || (_caps & Capabilities.Rumble) == 0) return;
        try { _stream.Write(_parser.BuildOutputReport(command, _bluetooth)); }
        catch (Exception) { _caps &= ~(Capabilities.Rumble | Capabilities.Led); }
    }

    public void Dispose()
    {
        _running = false;
        try { SetRumble(RumbleCommand.Stop); } catch (Exception) { }
        _reader?.Join(300);
        try { _stream?.Dispose(); } catch (Exception) { }
    }
}

using PadLab.Core.Analysis;
using PadLab.Core.Model;
using PadLab.Input.Native;

namespace PadLab.Input;

/// <summary>
/// 1 台の IGamepadSource を専用スレッドで回し、読めたスナップショットをそのまま SnapshotHub へ流す。
/// TryRead が false なら Sleep(1) (timeBeginPeriod(1) で実 1ms)。TryRead が例外を投げたら停止して Faulted。
/// </summary>
public sealed class PollingLoop : IDisposable
{
    private readonly IGamepadSource _source;
    private readonly SnapshotHub _hub;
    private volatile bool _running;
    private Thread? _thread;

    public PollingLoop(IGamepadSource source, SnapshotHub hub)
    {
        _source = source;
        _hub = hub;
    }

    public bool IsRunning => _running;
    public event Action<Exception>? Faulted;

    public void Start()
    {
        if (_running) return;
        _running = true;
        _thread = new Thread(Run) { IsBackground = true, Priority = ThreadPriority.AboveNormal, Name = "PadLab.PollingLoop" };
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        var t = _thread;
        _thread = null;
        if (t is not null && t != Thread.CurrentThread) t.Join(500);
    }

    private void Run()
    {
        WinMm.timeBeginPeriod(1);
        try
        {
            while (_running)
            {
                if (_source.TryRead(out var s)) _hub.Publish(s);
                else Thread.Sleep(1);
            }
        }
        catch (Exception ex)
        {
            _running = false;
            Faulted?.Invoke(ex);
        }
        finally
        {
            WinMm.timeEndPeriod(1);
        }
    }

    public void Dispose() => Stop();
}

using PadLab.Core.Model;

namespace PadLab.Core.Analysis;

public enum IdleEventKind { StickDrift, PhantomButton, TriggerWobble, PollingGap, Disconnected, Reconnected }
public sealed record IdleEvent(long OffsetNs, IdleEventKind Kind, string Detail, float Magnitude);
public sealed record IdleMonitorSettings(double DurationSeconds = 600, float StickThreshold = 0.05f, float TriggerThreshold = 0.05f, double PollingGapMs = 100);
public sealed record IdleSummary(double ElapsedSeconds, bool Running, bool Finished,
    IReadOnlyList<IdleEvent> Events, int DriftEvents, int ButtonEvents, int TriggerEvents, int GapEvents, int DisconnectEvents,
    float MaxLeftDeviation, float MaxRightDeviation);

/// <summary>放置中に起きた入力・切断・ポーリング空きをイベントとして記録する。閾値超過は立ち上がりで1イベント。</summary>
public sealed class IdleMonitor : IAnalyzer
{
    private readonly object _gate = new();
    private readonly List<IdleEvent> _events = new();
    private IdleMonitorSettings _settings;
    private bool _running, _finished;
    private long _startNs = -1, _lastNs;
    private Vector2F _leftCenter, _rightCenter;
    private float _maxLeft, _maxRight;
    private bool _leftOver, _rightOver, _ltOver, _rtOver;
    private ulong _prevBits;

    public IdleMonitor(IdleMonitorSettings? settings = null) => _settings = settings ?? new IdleMonitorSettings();

    public IdleMonitorSettings Settings
    {
        get { lock (_gate) return _settings; }
        set { lock (_gate) _settings = value; }
    }

    public void Start(Vector2F leftCenter, Vector2F rightCenter)
    {
        lock (_gate)
        {
            ResetCore();
            _leftCenter = leftCenter; _rightCenter = rightCenter;
            _running = true;
        }
    }

    public void Stop() { lock (_gate) if (_running) { _running = false; _finished = true; } }

    public void NoteDisconnected(long ns) => Note(ns, IdleEventKind.Disconnected, "切断");
    public void NoteReconnected(long ns) => Note(ns, IdleEventKind.Reconnected, "再接続");

    private void Note(long ns, IdleEventKind kind, string detail)
    {
        lock (_gate)
        {
            if (!_running) return;
            _events.Add(new IdleEvent(_startNs < 0 ? 0 : ns - _startNs, kind, detail, 0));
        }
    }

    public void Feed(GamepadSnapshot s)
    {
        lock (_gate)
        {
            if (!_running) return;
            if (_startNs < 0) { _startNs = s.TimestampNs; _lastNs = s.TimestampNs; _prevBits = s.Buttons; }
            var offset = s.TimestampNs - _startNs;
            if (offset / 1e9 >= _settings.DurationSeconds) { _running = false; _finished = true; return; }

            var gapMs = Clock.ToMs(s.TimestampNs - _lastNs);
            if (gapMs > _settings.PollingGapMs)
                _events.Add(new IdleEvent(offset, IdleEventKind.PollingGap, $"ポーリング空き {gapMs:F0}ms", (float)gapMs));
            _lastNs = s.TimestampNs;

            CheckStick(offset, "左スティック", (s.LeftStick - _leftCenter).Length, ref _maxLeft, ref _leftOver);
            CheckStick(offset, "右スティック", (s.RightStick - _rightCenter).Length, ref _maxRight, ref _rightOver);
            CheckTrigger(offset, "左トリガー", s.LeftTrigger, ref _ltOver);
            CheckTrigger(offset, "右トリガー", s.RightTrigger, ref _rtOver);

            foreach (var id in GamepadSnapshot.DownButtons(s.Buttons & ~_prevBits))
                _events.Add(new IdleEvent(offset, IdleEventKind.PhantomButton, $"ボタン {id}", 1));
            _prevBits = s.Buttons;
        }
    }

    private void CheckStick(long offset, string name, float dev, ref float max, ref bool over)
    {
        if (dev > max) max = dev;
        var now = dev > _settings.StickThreshold;
        if (now && !over) _events.Add(new IdleEvent(offset, IdleEventKind.StickDrift, $"{name} {dev:F2}", dev));
        over = now;
    }

    private void CheckTrigger(long offset, string name, float v, ref bool over)
    {
        var now = v > _settings.TriggerThreshold;
        if (now && !over) _events.Add(new IdleEvent(offset, IdleEventKind.TriggerWobble, $"{name} {v:F2}", v));
        over = now;
    }

    public void Reset() { lock (_gate) ResetCore(); }

    private void ResetCore()
    {
        _events.Clear();
        _running = _finished = false;
        _startNs = -1; _lastNs = 0;
        _maxLeft = _maxRight = 0;
        _leftOver = _rightOver = _ltOver = _rtOver = false;
        _prevBits = 0;
    }

    public IdleSummary Summary
    {
        get
        {
            lock (_gate)
            {
                var ev = _events.ToArray();
                int Count(IdleEventKind k) => ev.Count(e => e.Kind == k);
                return new IdleSummary(_startNs < 0 ? 0 : (_lastNs - _startNs) / 1e9, _running, _finished, ev,
                    Count(IdleEventKind.StickDrift), Count(IdleEventKind.PhantomButton), Count(IdleEventKind.TriggerWobble),
                    Count(IdleEventKind.PollingGap), Count(IdleEventKind.Disconnected) + Count(IdleEventKind.Reconnected),
                    _maxLeft, _maxRight);
            }
        }
    }
}

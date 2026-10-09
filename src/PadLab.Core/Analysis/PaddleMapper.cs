using PadLab.Core.Model;

namespace PadLab.Core.Analysis;

public enum PaddlePhase { Idle, Waiting, Captured, NoResponse, Invalid, Done }
public sealed record PaddleMapping(int PaddleIndex, IReadOnlyList<ButtonId> Mapped, bool PhysicalSeen);
public sealed record PaddleState(PaddlePhase Phase, int CurrentPaddle, IReadOnlyList<PaddleMapping> Mappings, double WaitedSeconds);

/// <summary>
/// 「パドル n を押してください」→ 最初の押下から captureWindowMs 内に立ち上がったボタンを割り当てとして記録する。
/// P1..P4 の物理ビットは Mapped に含めず PhysicalSeen に立てる。Begin/Next/Retry 直後は押しっぱなしも立ち上がりとみなす。
/// </summary>
public sealed class PaddleMapper : IAnalyzer
{
    private readonly object _gate = new();
    private readonly int _count;
    private readonly double _timeoutSeconds;
    private readonly long _windowNs;
    private readonly SortedDictionary<int, PaddleMapping> _mappings = new();
    private readonly List<ButtonId> _captured = new();
    private readonly HashSet<ButtonId> _seen = new();
    private PaddlePhase _phase;
    private int _current;
    private long _waitStartNs = -1, _captureStartNs = -1, _lastNs;
    private bool _physical;
    private ulong _prevBits;

    public PaddleMapper(int paddleCount, double timeoutSeconds = 10, double captureWindowMs = 100)
    {
        _count = paddleCount;
        _timeoutSeconds = timeoutSeconds;
        _windowNs = (long)(captureWindowMs * Clock.NsPerMs);
    }

    public void Begin()
    {
        lock (_gate) { _mappings.Clear(); _current = 1; BeginWait(); }
    }

    public void Next()
    {
        lock (_gate)
        {
            if (_phase is not (PaddlePhase.Captured or PaddlePhase.NoResponse or PaddlePhase.Invalid)) return;
            _mappings.TryAdd(_current, new PaddleMapping(_current, Array.Empty<ButtonId>(), false));
            if (_current >= _count) { _phase = PaddlePhase.Done; return; }
            _current++;
            BeginWait();
        }
    }

    public void Retry()
    {
        lock (_gate)
        {
            if (_phase is PaddlePhase.Idle or PaddlePhase.Done) return;
            _mappings.Remove(_current);
            BeginWait();
        }
    }

    private void BeginWait()
    {
        _phase = PaddlePhase.Waiting;
        _waitStartNs = _captureStartNs = -1;
        _captured.Clear(); _seen.Clear();
        _physical = false;
        _prevBits = 0;
    }

    public void Feed(GamepadSnapshot s)
    {
        lock (_gate)
        {
            if (_phase != PaddlePhase.Waiting) return;
            if (_waitStartNs < 0) _waitStartNs = s.TimestampNs;
            _lastNs = s.TimestampNs;
            var rising = s.Buttons & ~_prevBits;
            _prevBits = s.Buttons;

            if (_captureStartNs < 0)
            {
                if (rising == 0)
                {
                    if ((s.TimestampNs - _waitStartNs) / 1e9 >= _timeoutSeconds) Finish(PaddlePhase.NoResponse);
                    return;
                }
                _captureStartNs = s.TimestampNs;
            }
            else if (s.TimestampNs - _captureStartNs > _windowNs)
            {
                Finish(PaddlePhase.Captured);
                return;
            }

            foreach (var id in GamepadSnapshot.DownButtons(rising))
            {
                if (id is >= ButtonId.P1 and <= ButtonId.P4) { _physical = true; continue; }
                if (!_seen.Add(id)) { Finish(PaddlePhase.Invalid); return; }
                _captured.Add(id);
            }
            if (_captured.Count(id => id < ButtonId.Share) >= 3) Finish(PaddlePhase.Invalid);
        }
    }

    private void Finish(PaddlePhase phase)
    {
        _phase = phase;
        if (phase != PaddlePhase.Invalid)
            _mappings[_current] = new PaddleMapping(_current, _captured.ToArray(), _physical);
    }

    public void Reset()
    {
        lock (_gate) { BeginWait(); _phase = PaddlePhase.Idle; _mappings.Clear(); _current = 0; _lastNs = 0; }
    }

    public PaddleState State
    {
        get
        {
            lock (_gate)
            {
                var waited = _waitStartNs >= 0 ? (_lastNs - _waitStartNs) / 1e9 : 0;
                return new PaddleState(_phase, _current, _mappings.Values.ToArray(), waited);
            }
        }
    }
}

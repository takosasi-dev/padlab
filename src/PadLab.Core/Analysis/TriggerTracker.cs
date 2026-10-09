using PadLab.Core.Model;

namespace PadLab.Core.Analysis;

/// <summary>トリガー値の時系列 (リング) と最大到達値。</summary>
public sealed class TriggerTracker : IAnalyzer
{
    private readonly object _gate = new();
    private readonly TriggerId _id;
    private readonly int _capacity;
    private readonly Queue<(long TimestampNs, float Value)> _history = new();
    private float _max, _current;

    public TriggerTracker(TriggerId id, int capacity = 2000)
    {
        _id = id;
        _capacity = capacity;
    }

    public void Feed(GamepadSnapshot snapshot)
    {
        var v = snapshot.Trigger(_id);
        lock (_gate)
        {
            _current = v;
            if (v > _max) _max = v;
            _history.Enqueue((snapshot.TimestampNs, v));
            while (_history.Count > _capacity) _history.Dequeue();
        }
    }

    public void Reset() { lock (_gate) { _history.Clear(); _max = _current = 0; } }

    public float Max { get { lock (_gate) return _max; } }
    public float Current { get { lock (_gate) return _current; } }
    public IReadOnlyList<(long TimestampNs, float Value)> History { get { lock (_gate) return _history.ToArray(); } }
}

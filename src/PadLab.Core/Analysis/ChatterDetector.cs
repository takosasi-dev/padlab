using PadLab.Core.Model;

namespace PadLab.Core.Analysis;

public sealed record ChatterSettings(double SuspectGapMs = 15, double HumanRatio = 0.34);
public sealed record ButtonChatterStats(ButtonId Id, int Presses, int Suspects, IReadOnlyList<double> SuspectGapsMs, double? ShortestNormalGapMs);
public sealed record ChatterResult(IReadOnlyDictionary<ButtonId, ButtonChatterStats> Buttons, double DetectionLimitMs, int TotalSuspects);

/// <summary>ボタンごとの「解放→次の押下」間隔から、機械的に短すぎる再押下を疑いとして数える。</summary>
public sealed class ChatterDetector : IAnalyzer
{
    private const int IntervalWindow = 64;

    private sealed class Entry
    {
        public int Presses, Suspects;
        public long LastReleaseNs = -1;
        public readonly List<double> SuspectGaps = new();
        public double? ShortestNormal;
    }

    private readonly object _gate = new();
    private readonly Dictionary<ButtonId, Entry> _buttons = new();
    private readonly Queue<double> _intervals = new();
    private ChatterSettings _settings;
    private ulong _prevBits;
    private long _prevNs = -1;

    public ChatterDetector(ChatterSettings? settings = null) => _settings = settings ?? new ChatterSettings();

    public ChatterSettings Settings
    {
        get { lock (_gate) return _settings; }
        set { lock (_gate) _settings = value; }
    }

    public void Feed(GamepadSnapshot snapshot)
    {
        lock (_gate)
        {
            if (_prevNs >= 0)
            {
                _intervals.Enqueue(Clock.ToMs(snapshot.TimestampNs - _prevNs));
                while (_intervals.Count > IntervalWindow) _intervals.Dequeue();
            }
            _prevNs = snapshot.TimestampNs;

            var changed = snapshot.Buttons ^ _prevBits;
            foreach (var id in GamepadSnapshot.DownButtons(changed))
            {
                if (!_buttons.TryGetValue(id, out var e)) _buttons[id] = e = new Entry();
                if (snapshot.IsDown(id))
                {
                    e.Presses++;
                    if (e.LastReleaseNs < 0) continue;
                    var gap = Clock.ToMs(snapshot.TimestampNs - e.LastReleaseNs);
                    var reference = e.ShortestNormal ?? 120;
                    if (gap < _settings.SuspectGapMs && gap < reference * _settings.HumanRatio)
                    {
                        e.Suspects++;
                        e.SuspectGaps.Add(gap);
                    }
                    else if (e.ShortestNormal is null || gap < e.ShortestNormal) e.ShortestNormal = gap;
                }
                else e.LastReleaseNs = snapshot.TimestampNs;
            }
            _prevBits = snapshot.Buttons;
        }
    }

    public void Reset()
    {
        lock (_gate) { _buttons.Clear(); _intervals.Clear(); _prevBits = 0; _prevNs = -1; }
    }

    public ChatterResult Result
    {
        get
        {
            lock (_gate)
            {
                var dict = _buttons.ToDictionary(kv => kv.Key,
                    kv => new ButtonChatterStats(kv.Key, kv.Value.Presses, kv.Value.Suspects, kv.Value.SuspectGaps.ToArray(), kv.Value.ShortestNormal));
                var limit = _intervals.Count == 0 ? 0 : _intervals.Average() * 2;
                return new ChatterResult(dict, limit, dict.Values.Sum(s => s.Suspects));
            }
        }
    }
}

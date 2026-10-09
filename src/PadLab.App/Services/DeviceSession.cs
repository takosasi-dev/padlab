#if TRACKS_READY
// TRACKS_READY については AppState.cs 先頭のコメントを参照。
using PadLab.App.Settings;
using PadLab.Core.Analysis;
using PadLab.Core.Analysis.Precision;
using PadLab.Core.Model;
using PadLab.Core.Profiles;
using PadLab.Core.Recording;
using PadLab.Input;

namespace PadLab.App.Services;

/// <summary>精度テストの結果 (スティック単位)。未実施は null。</summary>
public sealed record PrecisionResults(StickId Stick, CircleResult? Circle, SnapbackResult? Snapback, LinearityResult? Linearity, ResolutionResult? Resolution);

/// <summary>選択中デバイスの入力パイプラインと常駐アナライザ一式。</summary>
public sealed class DeviceSession : IDisposable
{
    public IGamepadSource Source { get; }
    public DeviceProfile Profile { get; }
    public SnapshotHub Hub { get; } = new();
    public AnalysisPump Pump { get; }
    public PollingLoop Loop { get; }
    public PollingRateMeter Polling { get; } = new();
    public ButtonCoverage Coverage { get; }
    public StickRange LeftRange { get; } = new(StickId.Left);
    public StickRange RightRange { get; } = new(StickId.Right);
    public TriggerTracker LeftTrigger { get; } = new(TriggerId.Left);
    public TriggerTracker RightTrigger { get; } = new(TriggerId.Right);
    public DriftAnalyzer LeftDrift { get; }
    public DriftAnalyzer RightDrift { get; }
    public ChatterDetector Chatter { get; }
    public IdleMonitor Idle { get; }
    /// <summary>PaddleCount &gt; 0 の機種のみ。</summary>
    public PaddleMapper? Paddles { get; }
    public List<VibrationResultDto> VibrationLog { get; } = new();

    public DeviceSession(IGamepadSource source, DeviceProfile profile, AppSettings settings)
    {
        Source = source;
        Profile = profile;
        Pump = new AnalysisPump(Hub);
        Loop = new PollingLoop(source, Hub);
        Coverage = new ButtonCoverage(profile);
        var th = new DriftThresholds(settings.DriftMinor, settings.DriftNotable);
        LeftDrift = new DriftAnalyzer(StickId.Left, th);
        RightDrift = new DriftAnalyzer(StickId.Right, th);
        Chatter = new ChatterDetector(new ChatterSettings(settings.ChatterGapMs));
        Idle = new IdleMonitor(new IdleMonitorSettings(settings.IdleMinutes * 60));
        Paddles = profile.PaddleCount > 0 ? new PaddleMapper(profile.PaddleCount) : null;

        foreach (var a in new IAnalyzer?[] { Polling, Coverage, LeftRange, RightRange, LeftTrigger, RightTrigger, LeftDrift, RightDrift, Chatter, Idle, Paddles })
            if (a is not null) Pump.Add(a);
        Pump.Start();
        Loop.Start();
    }

    /// <summary>設定変更を常駐アナライザへ即反映する。</summary>
    public void ApplySettings(AppSettings s)
    {
        var th = new DriftThresholds(s.DriftMinor, s.DriftNotable);
        LeftDrift.Thresholds = th;
        RightDrift.Thresholds = th;
        Chatter.Settings = Chatter.Settings with { SuspectGapMs = s.ChatterGapMs };
        Idle.Settings = Idle.Settings with { DurationSeconds = s.IdleMinutes * 60 };
    }

    /// <summary>ドリフト監視中ならデッドゾーン推奨値。監視前は null。</summary>
    public DeadzoneAdvice? ComputeDeadzone()
    {
        var l = LeftDrift.Result; var r = RightDrift.Result;
        if (l.Phase != DriftPhase.Monitoring || l.Points.Count < 2) return null;
        var idle = Idle.Summary;
        float? idleMax = idle.Finished ? Math.Max(idle.MaxLeftDeviation, idle.MaxRightDeviation) : null;
        var noise = Math.Max(Noise(l.Points), Noise(r.Points));
        return DeadzoneAdvisor.Compute(Math.Max(l.MaxDistance, r.MaxDistance), noise, idleMax);
    }

    /// <summary>直近の点の基準点からの距離の標準偏差。</summary>
    private static float Noise(IReadOnlyList<Vector2F> pts)
    {
        var n = Math.Min(pts.Count, 500);
        if (n < 2) return 0;
        double sum = 0, sq = 0;
        for (var i = pts.Count - n; i < pts.Count; i++) { var d = pts[i].Length; sum += d; sq += d * d; }
        var mean = sum / n;
        return (float)Math.Sqrt(Math.Max(0, sq / n - mean * mean));
    }

    public DiagnosticSession BuildSession(string notes, Dictionary<string, string> aliases,
        IReadOnlyList<PrecisionResults>? precision = null, DeadzoneAdvice? deadzone = null)
    {
        deadzone ??= ComputeDeadzone();
        var cov = Coverage.State;
        var buttons = cov.Expected
            .Select(b => new ButtonResultDto(b.Id.ToString(), b.Label, b.Category.ToString(), cov.Pressed.Contains(b.Id)))
            .ToArray();
        var paddles = Paddles?.State.Mappings
            .Select(m => new PaddleResultDto(m.PaddleIndex, m.Mapped.Select(x => x.ToString()).ToArray(), m.PhysicalSeen))
            .ToArray();
        var sticks = new[]
        {
            StickDto(StickId.Left, LeftDrift.Result, LeftRange, precision),
            StickDto(StickId.Right, RightDrift.Result, RightRange, precision),
        };
        var triggers = new[]
        {
            new TriggerResultDto("Left", LeftTrigger.Max),
            new TriggerResultDto("Right", RightTrigger.Max),
        };
        var ps = Polling.Stats;
        var chatter = Chatter.Result;
        var chatterDto = chatter.Buttons.Values
            .Select(b => new ChatterResultDto(b.Id.ToString(), b.Presses, b.Suspects, chatter.DetectionLimitMs))
            .ToArray();
        var idle = Idle.Summary;
        var idleDto = idle.Finished
            ? new IdleResultDto(idle.ElapsedSeconds, idle.DriftEvents, idle.ButtonEvents, idle.TriggerEvents, idle.GapEvents, idle.DisconnectEvents)
            : null;
        var d = Source.Descriptor;
        var device = new SessionDevice(d.Kind.ToString(), d.Connection.ToString(),
            $"VID 0x{d.Vid:X4} PID 0x{d.Pid:X4} {d.DeviceId}", $"{Profile.Name} v{Profile.Version}", d.Serial);
        var results = new SessionResults(buttons, paddles, sticks, triggers, VibrationLog.ToArray(), null,
            new LatencyReferenceDto(ps.Hz, ps.JitterMs, true), chatterDto, idleDto,
            deadzone?.RecommendedPercent, deadzone?.Rationale);
        return new DiagnosticSession(Guid.NewGuid().ToString("N"), DateTimeOffset.Now, App.Version, device, results, aliases, notes);
    }

    private static StickResultDto StickDto(StickId id, DriftResult dr, StickRange range, IReadOnlyList<PrecisionResults>? precision)
    {
        var p = precision?.FirstOrDefault(x => x.Stick == id);
        return new StickResultDto(id.ToString(), dr.Center.X, dr.Center.Y, dr.MaxDistance, dr.MeanDistance, dr.DominantAngleDeg, dr.Verdict.ToString(),
            range.MaxRadius, p?.Circle?.Roundness, p?.Circle?.AsymmetryX, p?.Circle?.AsymmetryY,
            p?.Snapback?.MeanReturnMs, p?.Snapback?.MaxOvershoot, p?.Linearity?.JumpCount, p?.Resolution?.MinStep, p?.Resolution?.NoiseLevel);
    }

    public void Dispose()
    {
        try { Source.SetRumble(RumbleCommand.Stop); } catch (Exception) { }
        Loop.Dispose();
        Pump.Dispose();
    }
}
#endif

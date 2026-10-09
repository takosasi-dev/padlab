using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PadLab.App.Services;
using PadLab.Core.Analysis;
using PadLab.Core.Model;

namespace PadLab.App.ViewModels;

public sealed record IdleEventRow(string Time, string Kind, string Detail);

public enum IdlePagePhase { Setup, Waiting, Result }

/// <summary>放置監視画面。時間設定 → 暗転待機 → 要約。状態は IdleMonitor.Summary から Tick で導出する。</summary>
public sealed partial class IdleViewModel : ObservableObject
{
    public ObservableCollection<IdleEventRow> Events { get; } = new();

    [ObservableProperty] private double _minutes = Math.Clamp(App.Settings.IdleMinutes, 1, 180);
    [ObservableProperty] private IdlePagePhase _phase = IdlePagePhase.Setup;
    [ObservableProperty] private bool _isSetup = true;
    [ObservableProperty] private bool _isWaiting;
    [ObservableProperty] private bool _isResult;
    [ObservableProperty] private bool _canStart;
    [ObservableProperty] private string _elapsed = "";
    [ObservableProperty] private string _eventCount = "";
    [ObservableProperty] private string _summary = "";
    [ObservableProperty] private string _note = "";

    /// <summary>監視中に捕まえたセッション。切断で AppState.Current が null になっても結果を読めるよう保持する。</summary>
    private DeviceSession? _session;
    private string? _deviceId;

    public void Attach(DeviceSession? s)
    {
        // 監視中/結果表示中は元のセッションを見続ける
        if (_session is not null && (_session.Idle.Summary.Running || Phase == IdlePagePhase.Result)) { Tick(); return; }
        _session = s;
        _deviceId = s?.Source.Descriptor.DeviceId;
        Phase = s is not null && s.Idle.Summary.Finished ? IdlePagePhase.Result : IdlePagePhase.Setup;
        Tick();
    }

    partial void OnPhaseChanged(IdlePagePhase value)
    {
        IsSetup = value == IdlePagePhase.Setup;
        IsWaiting = value == IdlePagePhase.Waiting;
        IsResult = value == IdlePagePhase.Result;
    }

    [RelayCommand]
    private void Start()
    {
        var s = _session;
        if (s is null) return;
        Note = "";
        Events.Clear();
        s.Idle.Settings = s.Idle.Settings with { DurationSeconds = Minutes * 60 };
        var latest = s.Hub.Latest;
        s.Idle.Start(latest.LeftStick, latest.RightStick);
        Phase = IdlePagePhase.Waiting;
        Tick();
    }

    [RelayCommand]
    private void Stop() { _session?.Idle.Stop(); Tick(); }

    [RelayCommand]
    private void Again()
    {
        _session = AppState.Instance.Current;
        _deviceId = _session?.Source.Descriptor.DeviceId;
        Phase = IdlePagePhase.Setup;
        Tick();
    }

    /// <summary>Watcher.Removed (任意スレッド)。監視対象の切断は記録して終了する。</summary>
    public void OnDeviceRemoved(string id)
    {
        var s = _session;
        if (s is null || id != _deviceId) return;
        var sum = s.Idle.Summary;
        if (!sum.Running) return;
        s.Idle.NoteDisconnected(Clock.NowNs());
        s.Idle.Stop();
        App.Current?.Dispatcher.BeginInvoke(() => { Note = Strings.IdleDisconnectedStopped; Tick(); });
    }

    /// <summary>Watcher.Added (任意スレッド)。同じ DeviceId の再接続を記録する (監視中のみ)。</summary>
    public void OnDeviceAdded(string id)
    {
        var s = _session;
        if (s is null || id != _deviceId) return;
        s.Idle.NoteReconnected(Clock.NowNs());
    }

    public void Tick()
    {
        var s = _session;
        CanStart = s is not null && Phase == IdlePagePhase.Setup;
        if (s is null) { Elapsed = ""; EventCount = ""; Summary = Strings.NoDevice; return; }
        var sum = s.Idle.Summary;
        if (Phase == IdlePagePhase.Waiting && !sum.Running) Phase = IdlePagePhase.Result;

        var total = (int)Math.Round(s.Idle.Settings.DurationSeconds / 60);
        var el = TimeSpan.FromSeconds(sum.ElapsedSeconds);
        Elapsed = string.Format(Strings.IdleElapsedFormat, (int)el.TotalMinutes, el.Seconds, total);
        EventCount = string.Format(Strings.IdleEventCountFormat, sum.Events.Count);

        if (Phase != IdlePagePhase.Result) return;
        Summary = string.Format(Strings.IdleSummaryFormat, Math.Max(1, Math.Round(sum.ElapsedSeconds / 60)),
            sum.MaxLeftDeviation, sum.MaxRightDeviation,
            sum.DriftEvents, sum.ButtonEvents, sum.TriggerEvents, sum.GapEvents, sum.DisconnectEvents);
        if (Events.Count != sum.Events.Count)
        {
            Events.Clear();
            foreach (var e in sum.Events)
            {
                var t = TimeSpan.FromSeconds(e.OffsetNs / 1e9);
                Events.Add(new IdleEventRow($"{(int)t.TotalMinutes:00}:{t.Seconds:00}.{t.Milliseconds:000}", KindText(e.Kind), e.Detail));
            }
        }
    }

    private static string KindText(IdleEventKind k) => k switch
    {
        IdleEventKind.StickDrift => Strings.IdleKindStick,
        IdleEventKind.PhantomButton => Strings.IdleKindButton,
        IdleEventKind.TriggerWobble => Strings.IdleKindTrigger,
        IdleEventKind.PollingGap => Strings.IdleKindGap,
        IdleEventKind.Disconnected => Strings.IdleKindDisconnected,
        _ => Strings.IdleKindReconnected,
    };
}

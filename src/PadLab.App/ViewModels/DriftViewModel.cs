using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PadLab.App.Services;
using PadLab.Core.Analysis;
using PadLab.Core.Model;
using PadLab.Core.Recording;

namespace PadLab.App.ViewModels;

public sealed partial class StickDriftViewModel : ObservableObject
{
    public string Title { get; }
    [ObservableProperty] private string _verdict = "";
    [ObservableProperty] private string _max = "";
    [ObservableProperty] private string _mean = "";
    [ObservableProperty] private string _angle = "";
    [ObservableProperty] private string _exceed = "";
    [ObservableProperty] private string _center = "";
    [ObservableProperty] private string _compare = "";

    public StickDriftViewModel(string title) => Title = title;

    public void Update(DriftResult r)
    {
        Verdict = VerdictText(r.Verdict);
        Max = string.Format(Strings.DriftMaxFormat, r.MaxDistance);
        Mean = string.Format(Strings.DriftMeanFormat, r.MeanDistance);
        Angle = string.Format(Strings.DriftAngleFormat, r.DominantAngleDeg);
        Exceed = string.Format(Strings.DriftExceedFormat, r.ExceedCount);
        Center = string.Format(Strings.DriftCenterFormat, r.Center.X, r.Center.Y);
    }

    public static string VerdictText(Verdict v) => v switch
    {
        Core.Analysis.Verdict.Notable => Strings.VerdictNotable,
        Core.Analysis.Verdict.Minor => Strings.VerdictMinor,
        _ => Strings.VerdictNone,
    };
}

/// <summary>ドリフト画面。案内 → 測定開始 (両スティック StartCalibration) → 監視。状態は DriftAnalyzer.Result から毎 Tick 導出する。</summary>
public sealed partial class DriftViewModel : ObservableObject
{
    public StickDriftViewModel Left { get; } = new(Strings.LeftStick);
    public StickDriftViewModel Right { get; } = new(Strings.RightStick);
    public ObservableCollection<SessionSummary> PastSessions { get; } = new();

    [ObservableProperty] private string _status = Strings.DriftGuide;
    [ObservableProperty] private bool _isMonitoring;
    [ObservableProperty] private bool _isIdle = true;
    [ObservableProperty] private string _thresholds = "";
    [ObservableProperty] private string _deadzone = "";
    [ObservableProperty] private string _deadzoneRationale = Strings.DeadzoneNeedMonitoring;
    [ObservableProperty] private string _deadzoneHints = "";
    [ObservableProperty] private SessionSummary? _selectedSession;
    [ObservableProperty] private string _compareStatus = Strings.CompareNone;
    [ObservableProperty] private string _saved = "";

    private readonly Stopwatch _calibration = new();
    private DeviceSession? _session;

    public void Attach(DeviceSession? s)
    {
        _session = s;
        _calibration.Reset();
        ReloadSessions();
        Tick();
    }

    public void ReloadSessions()
    {
        PastSessions.Clear();
        try { foreach (var x in AppState.Instance.Sessions.List()) PastSessions.Add(x); }
        catch (Exception) { /* 読めない記録は一覧に出さない */ }
        CompareStatus = PastSessions.Count == 0 ? Strings.CompareNone : Strings.ComparePick;
    }

    [RelayCommand]
    private void Start()
    {
        if (_session is null) return;
        _session.LeftDrift.StartCalibration();
        _session.RightDrift.StartCalibration();
        _calibration.Restart();
        Tick();
    }

    public void Tick()
    {
        var s = _session;
        if (s is null) { IsIdle = true; IsMonitoring = false; Status = Strings.NoDevice; return; }
        var l = s.LeftDrift.Result;
        var r = s.RightDrift.Result;
        var th = s.LeftDrift.Thresholds;
        Thresholds = string.Format(Strings.DriftThresholdFormat, th.MinorDistance, th.NotableDistance);
        var phase = l.Phase == DriftPhase.Monitoring && r.Phase == DriftPhase.Monitoring ? DriftPhase.Monitoring
            : l.Phase == DriftPhase.Idle && r.Phase == DriftPhase.Idle ? DriftPhase.Idle : DriftPhase.Calibrating;
        IsIdle = phase == DriftPhase.Idle;
        IsMonitoring = phase == DriftPhase.Monitoring;
        Status = phase switch
        {
            DriftPhase.Idle => Strings.DriftGuide,
            DriftPhase.Calibrating => string.Format(Strings.DriftCalibratingFormat,
                Math.Max(0, th.CalibrationSeconds - _calibration.Elapsed.TotalSeconds)),
            _ => string.Format(Strings.DriftMonitoringFormat, Math.Max(l.ElapsedSeconds, r.ElapsedSeconds)),
        };
        Left.Update(l);
        Right.Update(r);
        UpdateDeadzone(s, l, r);
    }

    private void UpdateDeadzone(DeviceSession s, DriftResult l, DriftResult r)
    {
        var advice = s.ComputeDeadzone();
        if (advice is null)
        {
            Deadzone = ""; DeadzoneRationale = Strings.DeadzoneNeedMonitoring; DeadzoneHints = "";
            return;
        }
        Deadzone = string.Format(Strings.DeadzoneFormat, advice.RecommendedPercent);
        DeadzoneRationale = advice.Rationale + (s.Idle.Summary.Finished ? "\n" + Strings.DeadzoneIdleUsed : "");
        DeadzoneHints = string.Join("\n", advice.Hints.Select(h => $"{h.Use}: {h.Hint}"));
    }

    [RelayCommand]
    private void Save()
    {
        if (_session is null) return;
        try
        {
            var path = AppState.Instance.Sessions.Save(_session.BuildSession("", new Dictionary<string, string>()));
            Saved = string.Format(Strings.PrecSavedFormat, path);
            ReloadSessions();
        }
        catch (Exception ex) { Saved = string.Format(Strings.HistFailedFormat, ex.Message); }
    }

    partial void OnSelectedSessionChanged(SessionSummary? value)
    {
        if (value is null) { Left.Compare = Right.Compare = ""; return; }
        try
        {
            var d = AppState.Instance.Sessions.Load(value.Path);
            Left.Compare = CompareLine(d, "Left");
            Right.Compare = CompareLine(d, "Right");
        }
        catch (Exception) { Left.Compare = Right.Compare = Strings.Unconfirmed; }
    }

    private static string CompareLine(DiagnosticSession d, string stick)
    {
        var s = d.Results.Sticks.FirstOrDefault(x => x.Stick == stick);
        if (s is null) return Strings.Unconfirmed;
        Enum.TryParse<Verdict>(s.Verdict, true, out var v);
        return string.Format(Strings.CompareRowFormat, d.MeasuredAt.ToString("yyyy-MM-dd HH:mm"), s.MaxDrift, StickDriftViewModel.VerdictText(v));
    }
}

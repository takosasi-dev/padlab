using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PadLab.App.Rendering;
using PadLab.App.Services;
using PadLab.Core.Model;
using PadLab.Core.Recording;

namespace PadLab.App.ViewModels;

/// <summary>振動テストの 1 行 (モーター / インパルストリガー)。</summary>
public sealed class VibrationRow : ObservableObject
{
    private bool _isOn;
    private double _strength = 50;
    private string _report = Strings.VibReportNone;

    public string Name { get; }
    public bool Supported { get; }
    public string UnsupportedText => Supported ? "" : Strings.VibUnsupported;
    public bool IsOn { get => _isOn; set => SetProperty(ref _isOn, value); }
    public double Strength { get => _strength; set { if (SetProperty(ref _strength, value)) OnPropertyChanged(nameof(StrengthText)); } }
    public string StrengthText => string.Format(Strings.VibStrengthFormat, Strength);
    public string Report { get => _report; set => SetProperty(ref _report, value); }
    public bool Sweeping;
    public double SweepSeconds;
    public IRelayCommand SweepCommand { get; }
    public IRelayCommand FeltCommand { get; }
    public IRelayCommand NotFeltCommand { get; }

    public VibrationRow(string name, bool supported, Action<VibrationRow, bool?> report)
    {
        Name = name;
        Supported = supported;
        SweepCommand = new RelayCommand(() => { Sweeping = true; SweepSeconds = 0; });
        FeltCommand = new RelayCommand(() => report(this, true));
        NotFeltCommand = new RelayCommand(() => report(this, false));
    }

    /// <summary>今送るべき強度 0..1。</summary>
    public float Output => !Supported ? 0 : Sweeping ? (float)Math.Min(1, SweepSeconds / 5) : IsOn ? (float)(Strength / 100) : 0;
}

public sealed class VibrationViewModel : ObservableObject, IDisposable
{
    private readonly DeviceSession _session;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private string _commandText = "";

    public VibrationRow[] Rows { get; }
    public VibrationRenderer Renderer { get; }
    public string CommandText { get => _commandText; private set => SetProperty(ref _commandText, value); }
    public IRelayCommand StopAllCommand { get; }

    public VibrationViewModel(DeviceSession session)
    {
        _session = session;
        var caps = session.Source.Capabilities;
        var rumble = caps.HasFlag(Capabilities.Rumble);
        var impulse = caps.HasFlag(Capabilities.ImpulseTriggers);
        Rows = new[]
        {
            new VibrationRow(Strings.VibRowLeftMotor, rumble, Report),
            new VibrationRow(Strings.VibRowRightMotor, rumble, Report),
            new VibrationRow(Strings.VibRowLeftTrigger, impulse, Report),
            new VibrationRow(Strings.VibRowRightTrigger, impulse, Report),
        };
        Renderer = new VibrationRenderer(session.Source.Descriptor.Kind, session.Profile, () => session.Hub.Latest);
        StopAllCommand = new RelayCommand(() => { foreach (var r in Rows) { r.IsOn = false; r.Sweeping = false; } Tick(); });
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    private void Report(VibrationRow row, bool? felt)
    {
        row.Report = felt == true ? Strings.VibReportFelt : Strings.VibReportNotFelt;
        Log(row.Name, true, felt);
    }

    private void Log(string motor, bool tested, bool? felt)
    {
        var log = _session.VibrationLog;
        var i = log.FindIndex(v => v.Motor == motor);
        var cur = i >= 0 ? log[i] : null;
        var dto = new VibrationResultDto(motor, tested || (cur?.Tested ?? false), felt ?? cur?.UserConfirmedFelt);
        if (i >= 0) log[i] = dto; else log.Add(dto);
    }

    private void Tick()
    {
        foreach (var r in Rows)
        {
            if (!r.Sweeping) continue;
            r.SweepSeconds += 0.1;
            if (r.SweepSeconds >= 5.1) r.Sweeping = false;
        }
        var cmd = new RumbleCommand(Rows[0].Output, Rows[1].Output, Rows[2].Output, Rows[3].Output);
        try { _session.Source.SetRumble(cmd); } catch (Exception) { }
        Renderer.LeftMotor = cmd.LeftMotor; Renderer.RightMotor = cmd.RightMotor;
        Renderer.LeftTrigger = cmd.LeftTrigger; Renderer.RightTrigger = cmd.RightTrigger;
        foreach (var r in Rows)
            if (r.Output > 0 && !_session.VibrationLog.Any(v => v.Motor == r.Name)) Log(r.Name, true, null);
        CommandText = string.Format(Strings.VibCommandFormat, cmd.LeftMotor * 100, cmd.RightMotor * 100, cmd.LeftTrigger * 100, cmd.RightTrigger * 100);
    }

    public void Dispose()
    {
        _timer.Stop();
        try { _session.Source.SetRumble(RumbleCommand.Stop); } catch (Exception) { }
    }
}

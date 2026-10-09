using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PadLab.App.Rendering;
using PadLab.App.Services;
using PadLab.Core.Analysis;
using PadLab.Core.Analysis.Precision;
using PadLab.Core.Model;
using PadLab.Core.Recording;

namespace PadLab.App.ViewModels;

public sealed record ResultRow(string Item, string Now, string Prev);

/// <summary>精度テストのウィザード。各ステップで Core テストを new して Pump に載せ、Complete で外す。</summary>
public sealed class PrecisionViewModel : ObservableObject, IDisposable
{
    public enum Step { Stick, Circle, Snapback, Linearity, Resolution, Chatter, Results }
    private const int ChatterTarget = 50;
    private const ButtonId ChatterButton = ButtonId.A;

    private readonly DeviceSession _session;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private Step _step = Step.Stick;
    private IAnalyzer? _active;
    private bool _running;
    private string _progress = "", _saved = "", _notes = "";
    private bool _isLeft = true;
    private int _chatterBasePresses, _chatterBaseSuspects, _chatterPresses, _chatterSuspects;
    private bool _chatterDone;
    private StickResultDto? _prev;

    public CircleTest? Circle { get; private set; }
    public SnapbackTest? Snapback { get; private set; }
    public LinearityTest? Linearity { get; private set; }
    public ResolutionTest? Resolution { get; private set; }
    public PrecisionRenderer Renderer { get; } = new();
    public ObservableCollection<ResultRow> Rows { get; } = new();

    public bool IsLeft { get => _isLeft; set => SetProperty(ref _isLeft, value); }
    public bool IsStickStep => _step == Step.Stick;
    public bool IsTestStep => _step is >= Step.Circle and <= Step.Chatter;
    public bool IsResultsStep => _step == Step.Results;
    public bool IsRunning { get => _running; private set { if (SetProperty(ref _running, value)) Notify(); } }
    public string Progress { get => _progress; private set => SetProperty(ref _progress, value); }
    public string Saved { get => _saved; private set => SetProperty(ref _saved, value); }
    public string Notes { get => _notes; set => SetProperty(ref _notes, value); }
    public string StepTitle => string.Format(Strings.PrecStepFormat, (int)_step + 1, 7, StepName(_step));
    public string Guide => _step switch
    {
        Step.Stick => Strings.PrecGuideStick,
        Step.Circle => Strings.PrecGuideCircle,
        Step.Snapback => Strings.PrecGuideSnapback,
        Step.Linearity => Strings.PrecGuideLinearity,
        Step.Resolution => Strings.PrecGuideResolution,
        Step.Chatter => string.Format(Strings.PrecGuideChatterFormat, _session.Profile.LabelOf(ChatterButton), ChatterTarget),
        _ => Strings.PrecGuideResults,
    };
    public string ChatterLimit => string.Format(Strings.PrecChatterLimitFormat, _session.Chatter.Result.DetectionLimitMs);

    public IRelayCommand StartCommand { get; }
    public IRelayCommand NextCommand { get; }
    public IRelayCommand BackCommand { get; }
    public IRelayCommand SaveCommand { get; }

    private StickId Stick => IsLeft ? StickId.Left : StickId.Right;

    public PrecisionViewModel(DeviceSession session)
    {
        _session = session;
        StartCommand = new RelayCommand(Start, () => IsTestStep && !IsRunning);
        NextCommand = new RelayCommand(() => Go(_step + 1), () => _step < Step.Results && !IsRunning);
        BackCommand = new RelayCommand(() => Go(_step - 1), () => _step > Step.Stick && !IsRunning);
        SaveCommand = new RelayCommand(Save, () => IsResultsStep);
        Renderer.LiveStick = () => session.Hub.Latest.Stick(Stick);
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    private static string StepName(Step s) => s switch
    {
        Step.Stick => Strings.PrecStepStick,
        Step.Circle => Strings.PrecStepCircle,
        Step.Snapback => Strings.PrecStepSnapback,
        Step.Linearity => Strings.PrecStepLinearity,
        Step.Resolution => Strings.PrecStepResolution,
        Step.Chatter => Strings.PrecStepChatter,
        _ => Strings.PrecStepResults,
    };

    private void Notify()
    {
        OnPropertyChanged(nameof(IsStickStep)); OnPropertyChanged(nameof(IsTestStep)); OnPropertyChanged(nameof(IsResultsStep));
        OnPropertyChanged(nameof(StepTitle)); OnPropertyChanged(nameof(Guide)); OnPropertyChanged(nameof(ChatterLimit));
        StartCommand.NotifyCanExecuteChanged(); NextCommand.NotifyCanExecuteChanged(); BackCommand.NotifyCanExecuteChanged(); SaveCommand.NotifyCanExecuteChanged();
    }

    private void Go(Step s)
    {
        Detach();
        _step = s;
        Progress = IsTestStep ? Strings.PrecIdle : "";
        Renderer.Mode = s switch
        {
            Step.Circle => PrecisionRenderer.View.Circle,
            Step.Snapback => PrecisionRenderer.View.Snapback,
            Step.Linearity => PrecisionRenderer.View.Linearity,
            Step.Resolution => PrecisionRenderer.View.Resolution,
            Step.Results => PrecisionRenderer.View.All,
            _ => PrecisionRenderer.View.None,
        };
        if (s == Step.Results) BuildRows();
        Notify();
    }

    private void Start()
    {
        Detach();
        switch (_step)
        {
            case Step.Circle: Circle = new CircleTest(Stick); Circle.Start(); _active = Renderer.Circle = Circle; break;
            case Step.Snapback: Snapback = new SnapbackTest(Stick); Snapback.Start(); _active = Renderer.Snapback = Snapback; break;
            case Step.Linearity: Linearity = new LinearityTest(Stick); Linearity.Start(); _active = Renderer.Linearity = Linearity; break;
            case Step.Resolution: Resolution = new ResolutionTest(Stick); Resolution.Start(); _active = Renderer.Resolution = Resolution; break;
            case Step.Chatter:
                var b = ChatterStats();
                _chatterBasePresses = b.Presses; _chatterBaseSuspects = b.Suspects;
                _chatterDone = false;
                break;
        }
        if (_active is not null) _session.Pump.Add(_active);
        IsRunning = true;
        Progress = Strings.PrecRunning;
    }

    // ponytail: ガイド付きチャタリングは常駐 ChatterDetector の差分で数える (専用検出器を Pump に足さない)
    private (int Presses, int Suspects) ChatterStats() =>
        _session.Chatter.Result.Buttons.TryGetValue(ChatterButton, out var s) ? (s.Presses, s.Suspects) : (0, 0);

    private void Detach()
    {
        if (_active is not null) { _session.Pump.Remove(_active); _active = null; }
        IsRunning = false;
    }

    private void Tick()
    {
        if (!IsRunning) return;
        bool done;
        switch (_step)
        {
            case Step.Circle: done = Circle!.Result.Phase == TestPhase.Complete; break;
            case Step.Snapback:
                var sr = Snapback!.Result;
                done = sr.Phase == TestPhase.Complete;
                Progress = string.Format(Strings.PrecRemainingFormat, sr.TargetReleases - sr.Releases);
                break;
            case Step.Linearity: done = Linearity!.Result.Phase == TestPhase.Complete; break;
            case Step.Resolution:
                var rr = Resolution!.Result;
                done = rr.Phase == TestPhase.Complete;
                Progress = string.Format(Strings.PrecElapsedFormat, Math.Max(0, 5 - rr.ElapsedSeconds));
                break;
            case Step.Chatter:
                var c = ChatterStats();
                _chatterPresses = c.Presses - _chatterBasePresses;
                _chatterSuspects = c.Suspects - _chatterBaseSuspects;
                done = _chatterPresses >= ChatterTarget;
                Progress = string.Format(Strings.PrecRemainingFormat, Math.Max(0, ChatterTarget - _chatterPresses))
                    + "  " + string.Format(Strings.PrecChatterSuspectsFormat, _chatterSuspects, _chatterPresses);
                if (done) _chatterDone = true;
                break;
            default: done = false; break;
        }
        if (!done) return;
        Detach();
        Progress = Strings.PrecComplete + (_step == Step.Chatter ? "  " + string.Format(Strings.PrecChatterSuspectsFormat, _chatterSuspects, _chatterPresses) : "");
    }

    private void BuildRows()
    {
        _prev = null;
        try
        {
            var kind = _session.Source.Descriptor.Kind.ToString();
            var last = AppState.Instance.Sessions.List().FirstOrDefault(s => s.DeviceType == kind);
            if (last is not null)
                _prev = AppState.Instance.Sessions.Load(last.Path).Results.Sticks.FirstOrDefault(s => s.Stick == Stick.ToString());
        }
        catch (Exception) { }

        var cr = Done(Circle?.Result);
        var sr = Done(Snapback?.Result);
        var lr = Done(Linearity?.Result);
        var rr = Done(Resolution?.Result);
        Rows.Clear();
        Rows.Add(Row(Strings.PrecRoundness, cr?.Roundness, _prev?.Roundness, "0.00"));
        Rows.Add(Row(Strings.PrecMaxRadius, cr?.MaxRadius, null, "0.00"));
        Rows.Add(Row(Strings.PrecAsymX, cr?.AsymmetryX, _prev?.AsymmetryX, "+0.00;-0.00"));
        Rows.Add(Row(Strings.PrecAsymY, cr?.AsymmetryY, _prev?.AsymmetryY, "+0.00;-0.00"));
        Rows.Add(Row(Strings.PrecReturnMs, sr?.MeanReturnMs, _prev?.MeanReturnMs, "0.0", " ms"));
        Rows.Add(Row(Strings.PrecMaxReturnMs, sr?.MaxReturnMs, null, "0.0", " ms"));
        Rows.Add(Row(Strings.PrecOvershoot, sr?.MaxOvershoot, _prev?.MaxOvershoot, "0.00"));
        Rows.Add(Row(Strings.PrecJumps, lr?.JumpCount, _prev?.JumpCount, "0"));
        Rows.Add(Row(Strings.PrecReversals, lr?.ReversalCount, null, "0"));
        Rows.Add(Row(Strings.PrecMinStep, rr?.MinStep, _prev?.MinStep, "0.0000"));
        Rows.Add(Row(Strings.PrecNoise, rr?.NoiseLevel, _prev?.NoiseLevel, "0.0000"));
        Rows.Add(new ResultRow(Strings.PrecChatterSuspects,
            _chatterDone ? string.Format(Strings.PrecChatterSuspectsFormat, _chatterSuspects, _chatterPresses) : Strings.PrecNotDone, "－"));
    }

    private static CircleResult? Done(CircleResult? r) => r?.Phase == TestPhase.Complete ? r : null;
    private static SnapbackResult? Done(SnapbackResult? r) => r?.Phase == TestPhase.Complete ? r : null;
    private static LinearityResult? Done(LinearityResult? r) => r?.Phase == TestPhase.Complete ? r : null;
    private static ResolutionResult? Done(ResolutionResult? r) => r?.Phase == TestPhase.Complete ? r : null;

    private static ResultRow Row<T>(string item, T? now, T? prev, string fmt, string unit = "") where T : struct, IFormattable =>
        new(item, Fmt(now, fmt, unit), Fmt(prev, fmt, unit));

    private static string Fmt<T>(T? v, string fmt, string unit) where T : struct, IFormattable =>
        v is { } x ? x.ToString(fmt, null) + unit : Strings.PrecNotDone;

    private void Save()
    {
        try
        {
            var pr = new PrecisionResults(Stick, Done(Circle?.Result), Done(Snapback?.Result), Done(Linearity?.Result), Done(Resolution?.Result));
            var session = _session.BuildSession(Notes, new Dictionary<string, string>(), new[] { pr });
            var path = AppState.Instance.Sessions.Save(session);
            Saved = string.Format(Strings.PrecSavedFormat, path);
        }
        catch (Exception ex)
        {
            Saved = string.Format(Strings.HistFailedFormat, ex.Message);
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        Detach();
    }
}

using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PadLab.App.Services;
using PadLab.Core.Analysis;
using PadLab.Core.Recording;

namespace PadLab.App.ViewModels;

/// <summary>パドル割り当て確認。PaddleMapper の State を 100ms ごとに読んで表示する。</summary>
public sealed class PaddleViewModel : ObservableObject, IDisposable
{
    private readonly DeviceSession _session;
    private readonly PaddleMapper? _mapper;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private string _guide = Strings.PaddleIntro, _status = "", _diff = "";
    private bool _canBegin = true, _canNext, _canRetry;
    private PaddlePhase _lastPhase = PaddlePhase.Idle;

    public string Guide { get => _guide; private set => SetProperty(ref _guide, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public string Diff { get => _diff; private set => SetProperty(ref _diff, value); }
    public bool CanBegin { get => _canBegin; private set { if (SetProperty(ref _canBegin, value)) BeginCommand.NotifyCanExecuteChanged(); } }
    public bool CanNext { get => _canNext; private set { if (SetProperty(ref _canNext, value)) NextCommand.NotifyCanExecuteChanged(); } }
    public bool CanRetry { get => _canRetry; private set { if (SetProperty(ref _canRetry, value)) RetryCommand.NotifyCanExecuteChanged(); } }
    public ObservableCollection<string> Mappings { get; } = new();
    public IRelayCommand BeginCommand { get; }
    public IRelayCommand NextCommand { get; }
    public IRelayCommand RetryCommand { get; }

    public PaddleViewModel(DeviceSession session)
    {
        _session = session;
        _mapper = session.Paddles;
        BeginCommand = new RelayCommand(() => _mapper?.Begin(), () => CanBegin);
        NextCommand = new RelayCommand(() => _mapper?.Next(), () => CanNext);
        RetryCommand = new RelayCommand(() => _mapper?.Retry(), () => CanRetry);
        if (_mapper is null) { Guide = Strings.PaddleUnavailable; CanBegin = false; return; }
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    private void Tick()
    {
        var st = _mapper!.State;
        var p = st.Phase;
        CanBegin = p is PaddlePhase.Idle or PaddlePhase.Done;
        CanNext = p is PaddlePhase.Captured or PaddlePhase.NoResponse or PaddlePhase.Invalid;
        CanRetry = p is not (PaddlePhase.Idle or PaddlePhase.Done);
        Guide = p switch
        {
            PaddlePhase.Idle => Strings.PaddleIntro,
            PaddlePhase.Done => Strings.PaddleDone,
            _ => string.Format(Strings.PaddleGuideFormat, st.CurrentPaddle),
        };
        var cur = st.Mappings.FirstOrDefault(m => m.PaddleIndex == st.CurrentPaddle);
        Status = p switch
        {
            PaddlePhase.Waiting => string.Format(Strings.PaddleWaitingFormat, st.WaitedSeconds),
            PaddlePhase.Captured => string.Format(Strings.PaddleCapturedFormat, st.CurrentPaddle, Labels(cur)),
            PaddlePhase.NoResponse => Strings.PaddleNoResponse,
            PaddlePhase.Invalid => Strings.PaddleInvalid,
            _ => "",
        };
        var rows = st.Mappings.Select(m => string.Format(Strings.PaddleRowFormat, m.PaddleIndex, Labels(m))).ToList();
        if (!rows.SequenceEqual(Mappings))
        {
            Mappings.Clear();
            foreach (var r in rows) Mappings.Add(r);
        }
        if (p == PaddlePhase.Done && _lastPhase != PaddlePhase.Done) Diff = ComputeDiff(st);
        if (p == PaddlePhase.Idle) Diff = "";
        _lastPhase = p;
    }

    private string Labels(PaddleMapping? m) => m is null || m.Mapped.Count == 0
        ? Strings.PaddleNone
        : string.Join(", ", m.Mapped.Select(_session.Profile.LabelOf)) + (m.PhysicalSeen ? Strings.PaddlePhysical : "");

    private string ComputeDiff(PaddleState st)
    {
        var kind = _session.Source.Descriptor.Kind.ToString();
        PaddleResultDto[]? prev = null;
        try
        {
            var last = AppState.Instance.Sessions.List().FirstOrDefault(s => s.DeviceType == kind);
            if (last is not null) prev = AppState.Instance.Sessions.Load(last.Path).Results.Paddles;
        }
        catch (Exception) { }
        if (prev is null) return Strings.PaddleNoPrevious;
        var lines = new List<string>();
        foreach (var m in st.Mappings)
        {
            var p = prev.FirstOrDefault(x => x.Paddle == m.PaddleIndex);
            if (p is null) continue;
            var now = m.Mapped.Select(x => x.ToString()).OrderBy(x => x);
            var was = p.MappedTo.OrderBy(x => x);
            if (!now.SequenceEqual(was))
                lines.Add(string.Format(Strings.PaddleDiffFormat, m.PaddleIndex,
                    p.MappedTo.Length == 0 ? Strings.PaddleNone : string.Join(", ", p.MappedTo), Labels(m)));
        }
        return lines.Count == 0 ? Strings.PaddleDiffNone : string.Join("\n", lines);
    }

    public void Dispose() => _timer.Stop();
}

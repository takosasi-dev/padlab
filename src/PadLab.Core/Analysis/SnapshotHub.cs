using System.Threading.Channels;
using PadLab.Core.Model;

namespace PadLab.Core.Analysis;

/// <summary>
/// ポーリングスレッドが Publish し、UI は Latest を読み、解析スレッドは Channel から全件受け取る。
/// </summary>
public sealed class SnapshotHub
{
    private readonly Channel<GamepadSnapshot> _channel = Channel.CreateUnbounded<GamepadSnapshot>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private GamepadSnapshot _latest = GamepadSnapshot.Neutral;

    public GamepadSnapshot Latest => Volatile.Read(ref _latest);
    public ChannelReader<GamepadSnapshot> Reader => _channel.Reader;

    public void Publish(GamepadSnapshot s)
    {
        Volatile.Write(ref _latest, s);
        _channel.Writer.TryWrite(s);
    }
}

/// <summary>Channel を読んで登録済みアナライザ全部に Feed する。1スレッド。</summary>
public sealed class AnalysisPump : IDisposable
{
    private readonly SnapshotHub _hub;
    private readonly List<IAnalyzer> _analyzers = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _task;

    public AnalysisPump(SnapshotHub hub) => _hub = hub;

    public void Add(IAnalyzer a) { lock (_analyzers) _analyzers.Add(a); }
    public void Remove(IAnalyzer a) { lock (_analyzers) _analyzers.Remove(a); }

    public void Start() => _task ??= Task.Run(async () =>
    {
        try
        {
            await foreach (var s in _hub.Reader.ReadAllAsync(_cts.Token))
                FeedSync(s);
        }
        catch (OperationCanceledException) { }
    });

    /// <summary>テスト用: Channel を介さず同期的に流す。</summary>
    public void FeedSync(GamepadSnapshot s)
    {
        IAnalyzer[] list;
        lock (_analyzers) list = _analyzers.ToArray();
        foreach (var a in list) a.Feed(s);
    }

    public void Dispose() => _cts.Cancel();
}

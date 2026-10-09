namespace PadLab.Core.Model;

/// <summary>接続中デバイスの識別情報。DeviceId は同一物理機で安定するキー (バックエンド横断で共有)。</summary>
public sealed record DeviceDescriptor(
    string DeviceId,
    int Vid,
    int Pid,
    string DisplayName,
    ConnectionType Connection,
    DeviceKind Kind,
    string? Serial = null,
    string? BackendName = null);

/// <summary>振動指示。各値 0..1。Left/RightTrigger は Xbox 系インパルストリガー。</summary>
public readonly record struct RumbleCommand(float LeftMotor, float RightMotor, float LeftTrigger = 0, float RightTrigger = 0)
{
    public static readonly RumbleCommand Stop = new(0, 0, 0, 0);
}

/// <summary>1台のコントローラーからの入力源。TryRead はポーリングスレッドから呼ばれる。</summary>
public interface IGamepadSource : IDisposable
{
    DeviceDescriptor Descriptor { get; }
    Capabilities Capabilities { get; }
    /// <summary>新しいサンプルがあれば true。値が前回と同じでも到着していれば true (ポーリングレート計測のため)。</summary>
    bool TryRead(out GamepadSnapshot snapshot);
    void SetRumble(RumbleCommand command);
}

/// <summary>抜き差しの監視。イベントは任意スレッドから発火する。</summary>
public interface IDeviceWatcher : IDisposable
{
    event Action<IGamepadSource>? Added;
    /// <summary>引数は DeviceId。</summary>
    event Action<string>? Removed;
    IReadOnlyList<IGamepadSource> Devices { get; }
    void Start();
}

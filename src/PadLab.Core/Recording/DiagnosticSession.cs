namespace PadLab.Core.Recording;

// 結果 JSON (docs/result-format.md) の DTO。すべて System.Text.Json で snake_case にシリアライズされる。

/// <summary>Type / Connection はそれぞれ DeviceKind / ConnectionType の名前。Profile は "name/version"。</summary>
public sealed record SessionDevice(string Type, string Connection, string RawIdentifiers, string Profile, string? Serial);
public sealed record ButtonResultDto(string Id, string Label, string Category, bool Pressed);
public sealed record PaddleResultDto(int Paddle, string[] MappedTo, bool PhysicalSeen);
public sealed record StickResultDto(string Stick, float CenterX, float CenterY, float MaxDrift, float MeanDrift, float DominantAngle, string Verdict,
    float MaxRadius, float? Roundness, float? AsymmetryX, float? AsymmetryY, double? MeanReturnMs, float? MaxOvershoot, int? JumpCount, float? MinStep, float? NoiseLevel);
public sealed record TriggerResultDto(string Trigger, float MaxReached);
public sealed record VibrationResultDto(string Motor, bool Tested, bool? UserConfirmedFelt);
public sealed record ChatterResultDto(string Button, int Presses, int Suspects, double DetectionLimitMs);
public sealed record IdleResultDto(double DurationSeconds, int DriftEvents, int ButtonEvents, int TriggerEvents, int GapEvents, int DisconnectEvents);
public sealed record LatencyReferenceDto(double Hz, double JitterMs, bool IsReferenceOnly /* 常に true */);
public sealed record SessionResults(ButtonResultDto[] Buttons, PaddleResultDto[]? Paddles, StickResultDto[] Sticks, TriggerResultDto[] Triggers,
    VibrationResultDto[] Vibration, object? Sensors /* Phase3 まで null */, LatencyReferenceDto LatencyReference,
    ChatterResultDto[] Chatter, IdleResultDto? Idle, int? DeadzoneRecommendedPercent, string? DeadzoneRationale);
public sealed record DiagnosticSession(string SessionId, DateTimeOffset MeasuredAt, string AppVersion, SessionDevice Device, SessionResults Results,
    Dictionary<string, string> ButtonAliases, string Notes);
public sealed record SessionSummary(string Path, string SessionId, DateTimeOffset MeasuredAt, string DeviceType, string Connection);

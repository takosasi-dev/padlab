using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using PadLab.Core.Model;

namespace PadLab.Core.Recording;

/// <summary>セッション JSON の保存・読込。ファイル名は MakeFileName の1箇所だけが生成する。</summary>
public sealed class SessionStore
{
    public const string Disclaimer = "本レポートの値は参考値であり、故障の確定診断ではありません。";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly string _directory;

    public SessionStore(string directory) => _directory = Path.GetFullPath(directory);

    /// <summary>exe 隣に portable.txt があれば exe隣\sessions、無ければ %LOCALAPPDATA%\PadLab\sessions。</summary>
    public static string DefaultDirectory(string exeDirectory) =>
        File.Exists(Path.Combine(exeDirectory, "portable.txt"))
            ? Path.Combine(exeDirectory, "sessions")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PadLab", "sessions");

    /// <summary>"20260903-171500_DualSense_Bluetooth_ab12cd34.json"。sessionId は英数字以外を除去し先頭8文字。</summary>
    public static string MakeFileName(DateTimeOffset at, DeviceKind kind, ConnectionType connection, string sessionId)
    {
        var id = new string(sessionId.Where(char.IsAsciiLetterOrDigit).Take(8).ToArray());
        if (id.Length == 0) id = "session";
        return $"{at:yyyyMMdd-HHmmss}_{kind}_{connection}_{id}.json";
    }

    public string Save(DiagnosticSession session)
    {
        Directory.CreateDirectory(_directory);
        if (!Enum.TryParse<DeviceKind>(session.Device.Type, true, out var kind)) kind = DeviceKind.Generic;
        if (!Enum.TryParse<ConnectionType>(session.Device.Connection, true, out var conn)) conn = ConnectionType.Unknown;
        var path = Path.Combine(_directory, MakeFileName(session.MeasuredAt, kind, conn, session.SessionId));
        File.WriteAllText(path, JsonSerializer.Serialize(session, JsonOptions), new UTF8Encoding(false));
        return path;
    }

    public DiagnosticSession Load(string path) =>
        JsonSerializer.Deserialize<DiagnosticSession>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException($"セッション JSON を読めませんでした: {path}");

    /// <summary>新しい順。読めないファイルは無視する。</summary>
    public IReadOnlyList<SessionSummary> List()
    {
        if (!Directory.Exists(_directory)) return [];
        var list = new List<SessionSummary>();
        foreach (var path in Directory.EnumerateFiles(_directory, "*.json"))
        {
            try
            {
                var s = Load(path);
                list.Add(new SessionSummary(path, s.SessionId, s.MeasuredAt, s.Device.Type, s.Device.Connection));
            }
            catch (Exception e) when (e is JsonException or IOException or InvalidDataException) { }
        }
        return list.OrderByDescending(x => x.MeasuredAt).ToList();
    }

    /// <summary>人が読めるプレーンテキスト。判定は断定しない言い回しで書く。</summary>
    public string ExportText(DiagnosticSession s)
    {
        var r = s.Results;
        var sb = new StringBuilder();
        sb.AppendLine("PadLab 診断結果");
        sb.AppendLine($"測定日時: {s.MeasuredAt:yyyy-MM-dd HH:mm:ss zzz}");
        sb.AppendLine($"アプリ: PadLab {s.AppVersion}  セッション: {s.SessionId}");
        sb.AppendLine($"機種: {s.Device.Type}  接続: {s.Device.Connection}  プロファイル: {s.Device.Profile}");
        sb.AppendLine($"識別子: {s.Device.RawIdentifiers}");
        sb.AppendLine();
        sb.AppendLine("[ボタン反応]");
        foreach (var b in r.Buttons)
        {
            var alias = s.ButtonAliases.TryGetValue(b.Id, out var a) ? $" ({a})" : "";
            sb.AppendLine($"  {(b.Pressed ? "✓ 反応あり" : "－ 未確認")}  {b.Label}{alias}");
        }
        if (r.Paddles is { Length: > 0 })
        {
            sb.AppendLine("[背面パドル]");
            foreach (var p in r.Paddles)
                sb.AppendLine($"  P{p.Paddle} → {(p.MappedTo.Length == 0 ? "反応なし" : string.Join(", ", p.MappedTo))}{(p.PhysicalSeen ? " (物理ビット検出)" : "")}");
        }
        sb.AppendLine("[スティック]");
        foreach (var st in r.Sticks)
        {
            sb.AppendLine($"  {StickName(st.Stick)}: {VerdictText(st.Verdict)}  最大ズレ {st.MaxDrift:F3}  平均 {st.MeanDrift:F3}  方向 {st.DominantAngle:F0}°  到達半径 {st.MaxRadius:F2}");
            sb.AppendLine($"    真円度 {Opt(st.Roundness, "F2")}  戻り時間 {Opt(st.MeanReturnMs, "F1", " ms")}  オーバーシュート {Opt(st.MaxOvershoot, "F2")}  飛び {Opt(st.JumpCount)}  最小ステップ {Opt(st.MinStep, "F4")}  ノイズ {Opt(st.NoiseLevel, "F4")}");
        }
        sb.AppendLine("[トリガー]");
        foreach (var t in r.Triggers) sb.AppendLine($"  {StickName(t.Trigger)}: 最大到達 {t.MaxReached:F2}");
        sb.AppendLine("[振動]");
        foreach (var v in r.Vibration)
            sb.AppendLine($"  {v.Motor}: {(v.Tested ? "実施" : "未実施")}{(v.UserConfirmedFelt is { } f ? (f ? " / 体感あり" : " / 体感なし") : "")}");
        sb.AppendLine("[チャタリング]");
        if (r.Chatter.Length == 0) sb.AppendLine("  未実施");
        foreach (var c in r.Chatter) sb.AppendLine($"  {c.Button}: 押下 {c.Presses} 回, 疑い {c.Suspects} 回 (検出限界 {c.DetectionLimitMs:F1} ms 未満は検出不可)");
        if (r.Idle is { } idle)
        {
            sb.AppendLine("[放置監視]");
            sb.AppendLine($"  {idle.DurationSeconds / 60:F0} 分間: ドリフト超過 {idle.DriftEvents} 回, ボタン {idle.ButtonEvents} 回, トリガー {idle.TriggerEvents} 回, ポーリング空き {idle.GapEvents} 回, 切断 {idle.DisconnectEvents} 回");
        }
        sb.AppendLine("[参考ポーリング]");
        sb.AppendLine($"  {r.LatencyReference.Hz:F0} Hz  ジッター {r.LatencyReference.JitterMs:F2} ms (参考値)");
        if (r.DeadzoneRecommendedPercent is { } dz)
        {
            sb.AppendLine("[デッドゾーン推奨]");
            sb.AppendLine($"  {dz}%  根拠: {r.DeadzoneRationale}");
        }
        if (!string.IsNullOrWhiteSpace(s.Notes))
        {
            sb.AppendLine("[メモ]");
            sb.AppendLine("  " + s.Notes.Replace("\n", "\n  "));
        }
        sb.AppendLine();
        sb.AppendLine(Disclaimer);
        return sb.ToString();
    }

    internal static string StickName(string id) => id switch { "Left" => "左", "Right" => "右", _ => id };

    internal static string VerdictText(string verdict) => verdict switch
    {
        "None" => "● 基準内",
        "Minor" => "▲ 軽微なズレ",
        "Notable" => "■ 基準より大きなズレ",
        _ => "－ 未確認",
    };

    internal static string Opt<T>(T? v, string fmt = "", string unit = "") where T : struct, IFormattable =>
        v is { } x ? x.ToString(fmt, null) + unit : "未確認";
}

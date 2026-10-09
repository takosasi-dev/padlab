using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using PadLab.Core.Model;

namespace PadLab.Core.Profiles;

/// <summary>
/// 機種プロファイルの読み込み。同梱 JSON (EmbeddedResource: Profiles/data/*.json) を既定とし、
/// overrideDirectory に同名 JSON があればそちらを優先する。
/// </summary>
public sealed class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string? _overrideDirectory;
    private readonly Dictionary<DeviceKind, DeviceProfile> _cache = new();
    private readonly object _gate = new();

    public ProfileStore(string? overrideDirectory = null) => _overrideDirectory = overrideDirectory;

    public DeviceProfile For(DeviceKind kind)
    {
        lock (_gate)
        {
            if (_cache.TryGetValue(kind, out var cached)) return cached;
            var profile = Parse(ReadJson(kind));
            _cache[kind] = profile;
            return profile;
        }
    }

    /// <summary>JSON ファイル名 (拡張子なし)。上書きディレクトリでも同じ名前を使う。</summary>
    public static string FileNameOf(DeviceKind kind) => kind switch
    {
        DeviceKind.XboxOne => "xbox_one",
        DeviceKind.XboxElite2 => "xbox_elite2",
        DeviceKind.DualShock4 => "dualshock4",
        DeviceKind.DualSense => "dualsense",
        DeviceKind.DualSenseEdge => "dualsense_edge",
        DeviceKind.SwitchPro => "switch_pro",
        DeviceKind.Fake => "fake",
        _ => "generic",
    };

    public static DeviceKind KindFromVidPid(int vid, int pid) => (vid, pid) switch
    {
        (0x045E, 0x0B00 or 0x0B05) => DeviceKind.XboxElite2,
        (0x045E, 0x02D1 or 0x02DD or 0x02E3 or 0x02EA or 0x02FD or 0x0B12 or 0x0B13 or 0x0B20 or 0x0B22) => DeviceKind.XboxOne,
        (0x054C, 0x05C4 or 0x09CC) => DeviceKind.DualShock4,
        (0x054C, 0x0CE6) => DeviceKind.DualSense,
        (0x054C, 0x0DF2) => DeviceKind.DualSenseEdge,
        (0x057E, 0x2009) => DeviceKind.SwitchPro,
        _ => DeviceKind.Generic,
    };

    public static string VendorName(int vid) => vid switch
    {
        0x045E => "Microsoft",
        0x054C => "Sony",
        0x057E => "Nintendo",
        0x2DC8 => "8BitDo",
        _ => $"VID 0x{vid:X4}",
    };

    private string ReadJson(DeviceKind kind)
    {
        var name = FileNameOf(kind) + ".json";
        if (_overrideDirectory is not null)
        {
            var path = Path.Combine(_overrideDirectory, name);
            if (File.Exists(path)) return File.ReadAllText(path);
        }
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream($"PadLab.Core.Profiles.data.{name}")
            ?? throw new FileNotFoundException($"同梱プロファイルが見つかりません: {name}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static DeviceProfile Parse(string json)
    {
        var dto = JsonSerializer.Deserialize<ProfileDto>(json, JsonOptions)
            ?? throw new InvalidDataException("プロファイル JSON が空です");
        var caps = Capabilities.None;
        foreach (var c in dto.ExpectedCapabilities ?? []) caps |= Enum.Parse<Capabilities>(c, ignoreCase: true);
        var buttons = (dto.Buttons ?? []).Select(b => new ButtonSpec(b.Id, b.Label, b.Category)).ToList();
        return new DeviceProfile(dto.Kind, dto.Name ?? dto.Kind.ToString(), dto.Version ?? "1", buttons, dto.PaddleCount, caps);
    }

    private sealed record ProfileDto(DeviceKind Kind, string? Name, string? Version, int PaddleCount, string[]? ExpectedCapabilities, ButtonDto[]? Buttons);
    private sealed record ButtonDto(ButtonId Id, string Label, ButtonCategory Category);
}

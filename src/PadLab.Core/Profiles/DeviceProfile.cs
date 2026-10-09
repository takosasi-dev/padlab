using PadLab.Core.Model;

namespace PadLab.Core.Profiles;

public sealed record ButtonSpec(ButtonId Id, string Label, ButtonCategory Category);

/// <summary>機種プロファイル。exe 同梱 JSON (profiles/*.json) から読む。</summary>
public sealed record DeviceProfile(
    DeviceKind Kind,
    string Name,
    string Version,
    IReadOnlyList<ButtonSpec> Buttons,
    int PaddleCount,
    Capabilities ExpectedCapabilities)
{
    public string LabelOf(ButtonId id) =>
        Buttons.FirstOrDefault(b => b.Id == id)?.Label ?? $"ボタン {(int)id}";
}

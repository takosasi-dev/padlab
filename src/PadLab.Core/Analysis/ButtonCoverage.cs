using PadLab.Core.Model;
using PadLab.Core.Profiles;

namespace PadLab.Core.Analysis;

public sealed record CoverageState(
    IReadOnlyList<ButtonSpec> Expected,
    IReadOnlySet<ButtonId> Pressed,
    IReadOnlyList<int> UnregisteredSeen,
    bool AllExpectedPressed);

/// <summary>一度でも押されたボタン集合。プロファイル外のボタンは「未登録ボタン #n」として Expected に自動追加する。</summary>
public sealed class ButtonCoverage : IAnalyzer
{
    private readonly object _gate = new();
    private readonly DeviceProfile _profile;
    private readonly List<ButtonSpec> _expected = new();
    private readonly HashSet<ButtonId> _pressed = new();
    private readonly List<int> _unregistered = new();

    public ButtonCoverage(DeviceProfile profile)
    {
        _profile = profile;
        _expected.AddRange(profile.Buttons);
    }

    public void Feed(GamepadSnapshot snapshot)
    {
        lock (_gate)
        {
            foreach (var id in GamepadSnapshot.DownButtons(snapshot.Buttons))
            {
                if (_pressed.Add(id) && _expected.All(b => b.Id != id))
                    _expected.Add(new ButtonSpec(id, $"未登録ボタン #{(int)id}", ButtonCategory.Extended));
            }
            if (snapshot.ExtraButtons is { } extra)
                foreach (var n in extra)
                {
                    var id = (ButtonId)(32 + n % 32);
                    _pressed.Add(id);
                    if (_unregistered.Contains(n)) continue;
                    _unregistered.Add(n);
                    if (_expected.All(b => b.Id != id))
                        _expected.Add(new ButtonSpec(id, $"未登録ボタン #{n}", ButtonCategory.Extended));
                }
        }
    }

    public void Reset()
    {
        lock (_gate)
        {
            _expected.Clear(); _expected.AddRange(_profile.Buttons);
            _pressed.Clear(); _unregistered.Clear();
        }
    }

    public CoverageState State
    {
        get
        {
            lock (_gate)
            {
                var expected = _expected.ToArray();
                var pressed = new HashSet<ButtonId>(_pressed);
                return new CoverageState(expected, pressed, _unregistered.ToArray(), expected.All(b => pressed.Contains(b.Id)));
            }
        }
    }
}

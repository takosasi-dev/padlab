using CommunityToolkit.Mvvm.ComponentModel;
using PadLab.App.Rendering;
using PadLab.App.Services;
using PadLab.App.Settings;
using Wpf.Ui.Appearance;

namespace PadLab.App.ViewModels;

/// <summary>AppSettings の全項目。変更のたびに保存し、閾値は選択中セッションへ即反映する。</summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _s = App.Settings;

    public string FilePath => string.Format(Strings.SettingsFileFormat, AppSettings.FilePath);

    public bool ReduceMotion { get => _s.ReduceMotion; set { _s.ReduceMotion = value; Apply(); } }
    public bool LightTheme
    {
        get => _s.LightTheme;
        set
        {
            _s.LightTheme = value;
            Theme.Apply(value);
            ApplicationThemeManager.Apply(value ? ApplicationTheme.Light : ApplicationTheme.Dark, Wpf.Ui.Controls.WindowBackdropType.Mica, false);
            Apply();
        }
    }
    public bool ShowPollingRate { get => _s.ShowPollingRate; set { _s.ShowPollingRate = value; Apply(); } }
    public bool ShowFakeDevice { get => _s.ShowFakeDevice; set { _s.ShowFakeDevice = value; Apply(); } }
    public string FakeKind { get => _s.FakeKind; set { _s.FakeKind = value; Apply(); } }
    public string[] FakeKinds { get; } = Enum.GetNames<PadLab.Core.Model.DeviceKind>();
    public bool SoundEnabled { get => _s.SoundEnabled; set { _s.SoundEnabled = value; Apply(); } }
    public bool AllowNetwork { get => _s.AllowNetwork; set { _s.AllowNetwork = value; Apply(); } }
    public double? DriftMinor { get => _s.DriftMinor; set { if (value is { } v) _s.DriftMinor = (float)v; Apply(); } }
    public double? DriftNotable { get => _s.DriftNotable; set { if (value is { } v) _s.DriftNotable = (float)v; Apply(); } }
    public double? ChatterGapMs { get => _s.ChatterGapMs; set { if (value is { } v) _s.ChatterGapMs = v; Apply(); } }
    public double? IdleMinutes { get => _s.IdleMinutes; set { if (value is { } v) _s.IdleMinutes = v; Apply(); } }

    private void Apply()
    {
        if (_s.DriftNotable < _s.DriftMinor) _s.DriftNotable = _s.DriftMinor;
        try { _s.Save(); } catch (Exception) { }
        AppState.Instance.Current?.ApplySettings(_s);
        OnPropertyChanged(string.Empty);
    }
}

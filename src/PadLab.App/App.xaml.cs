using System.Windows;
using PadLab.App.Rendering;
using PadLab.App.Services;
using PadLab.App.Settings;
using Wpf.Ui.Appearance;

namespace PadLab.App;

public partial class App : Application
{
    public static AppSettings Settings { get; private set; } = new();

    /// <summary>設定と Windows の「アニメーション効果」の両方が許すときだけ演出する。</summary>
    public static bool MotionEnabled => !Settings.ReduceMotion && SystemParameters.ClientAreaAnimation;

    public static string Version => typeof(App).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    protected override void OnStartup(StartupEventArgs e)
    {
        var args = e.Args;
        if (args.Length > 0 && args[0] == "--render-smoke")
            Environment.Exit(RenderSmoke.RunSmoke());
        if (args.Length > 0 && args[0] == "--selftest")
            Environment.Exit(SelfTest.Run());
        if (args.Length > 0 && args[0] == "--phase0")
            Environment.Exit(RenderSmoke.RunPhase0(args.Length > 1 ? args[1] : null));

        base.OnStartup(e);
        Settings = AppSettings.Load();
        Theme.Apply(Settings.LightTheme);
        ApplicationThemeManager.Apply(Settings.LightTheme ? ApplicationTheme.Light : ApplicationTheme.Dark, Wpf.Ui.Controls.WindowBackdropType.Mica, false);
        new MainWindow().Show();
    }
}

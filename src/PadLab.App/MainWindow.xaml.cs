using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using PadLab.App.Pages;
using PadLab.App.Services;
using PadLab.Core.Model;

namespace PadLab.App;

public partial class MainWindow
{
    private readonly DispatcherTimer _header = new() { Interval = TimeSpan.FromMilliseconds(250) };

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Nav.Navigate(typeof(DevicePage));
#if TRACKS_READY
            AppState.Instance.CurrentChanged += UpdateHeader;
            _header.Tick += (_, _) => UpdateHeader();
            _header.Start();
#endif
            UpdateHeader();
            ApplyDevArgs();
        };
        Closed += (_, _) => _header.Stop();
        PreviewKeyDown += (_, e) => DemoDriver.Active?.KeyDown(e.Key);
        PreviewKeyUp += (_, e) => DemoDriver.Active?.KeyUp(e.Key);
    }

    /// <summary>開発用: --page 基本入力 [--keys QE] で起動直後に Fake を選び、指定画面へ移動してキーを押しっぱなしにする (スクリーンショット取得用)。</summary>
    private void ApplyDevArgs()
    {
        var a = Environment.GetCommandLineArgs();
        var pi = Array.IndexOf(a, "--page");
        if (pi < 0 || pi + 1 >= a.Length) return;
        var pageName = a[pi + 1];
        var ki = Array.IndexOf(a, "--keys");
        var keys = ki >= 0 && ki + 1 < a.Length ? a[ki + 1] : "";
        var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        t.Tick += (_, _) =>
        {
            t.Stop();
            if (AppState.Instance.Current is null) AppState.Instance.Select("fake:0");
            var item = Nav.MenuItems.OfType<Wpf.Ui.Controls.NavigationViewItem>().Concat(Nav.FooterMenuItems.OfType<Wpf.Ui.Controls.NavigationViewItem>())
                .FirstOrDefault(i => (i.Content as string) == pageName);
            if (item?.TargetPageType is { } pt) Nav.Navigate(pt);
            foreach (var ch in keys)
                if (Enum.TryParse<Key>(ch.ToString(), out var k)) DemoDriver.Active?.KeyDown(k);
        };
        t.Start();
    }

    private void UpdateHeader()
    {
#if TRACKS_READY
        var cur = AppState.Instance.Current;
        if (cur is null)
        {
            DeviceNameText.Text = Strings.NoDevice; ConnectionText.Text = ""; PollingText.Text = "";
            PaddleItem.Visibility = Visibility.Collapsed;
            return;
        }
        var d = cur.Source.Descriptor;
        DeviceNameText.Text = d.DisplayName;
        ConnectionText.Text = ConnectionLabel(d.Connection);
        var hz = cur.Polling.Stats.Hz;
        PollingText.Text = !App.Settings.ShowPollingRate ? "" : hz > 0 ? string.Format(Strings.PollingFormat, hz) : Strings.PollingUnknown;
        PaddleItem.Visibility = cur.Profile.PaddleCount > 0 ? Visibility.Visible : Visibility.Collapsed;
#else
        DeviceNameText.Text = Strings.NoDevice;
        ConnectionText.Text = "";
        PollingText.Text = "";
#endif
    }

    public static string ConnectionLabel(ConnectionType c) => c switch
    {
        ConnectionType.Usb => Strings.ConnectionUsb,
        ConnectionType.Bluetooth => Strings.ConnectionBluetooth,
        ConnectionType.Dongle => Strings.ConnectionDongle,
        _ => Strings.ConnectionUnknown,
    };
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PadLab.App.Rendering;
using PadLab.App.Services;
using PadLab.App.ViewModels;

namespace PadLab.App.Pages;

public partial class BasicInputPage : Page
{
    private static readonly HashSet<string> HeroShown = new();   // 初回接続時だけヒーロー演出
    private readonly BasicInputViewModel _vm = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private DeviceSession? _session;

    public BasicInputPage()
    {
        InitializeComponent();
        DataContext = _vm;
        _timer.Tick += (_, _) => { if (_session is not null) _vm.Tick(_session); };
        Loaded += (_, _) =>
        {
            AppState.Instance.CurrentChanged += Rebind;
            Rebind();
            _timer.Start();
        };
        Unloaded += (_, _) =>
        {
            AppState.Instance.CurrentChanged -= Rebind;
            _timer.Stop();
            Canvas.IsActive = false;
            Canvas.Renderer = null;
        };
    }

    private void Rebind()
    {
        _session = AppState.Instance.Current;
        Canvas.Motion = AppState.Instance.MotionEnabled;
        if (_session is null)
        {
            Canvas.Renderer = null; Canvas.IsActive = false;
            NoDeviceText.Visibility = Visibility.Visible;
            _vm.Clear();
            return;
        }
        var renderer = new BasicInputRenderer(_session);
        if (HeroShown.Add(_session.Source.Descriptor.DeviceId)) renderer.StartHero();
        Canvas.Renderer = renderer;
        Canvas.IsActive = true;
        NoDeviceText.Visibility = Visibility.Collapsed;
        _vm.Tick(_session);
    }
}

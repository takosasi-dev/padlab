using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PadLab.App.Services;
using PadLab.App.ViewModels;

namespace PadLab.App.Pages;

public partial class DevicePage : Page
{
    private readonly DevicePageViewModel _vm = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(250) };

    public DevicePage()
    {
        InitializeComponent();
        DataContext = _vm;
        _timer.Tick += (_, _) => _vm.Tick();
        Loaded += (_, _) => { _vm.Tick(); _timer.Start(); };
        Unloaded += (_, _) => _timer.Stop();
    }

    /// <summary>カードのスライドイン (200ms)。抑制時はなし。</summary>
    private void Card_Loaded(object sender, RoutedEventArgs e)
    {
        if (!AppState.Instance.MotionEnabled || sender is not Border b) return;
        var d = TimeSpan.FromMilliseconds(200);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        b.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, d));
        // DataTemplate 内の TranslateTransform は共有(凍結)されるため、カードごとに新規作成してからアニメーションする
        var tt = new TranslateTransform();
        b.RenderTransform = tt;
        tt.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(40, 0, d) { EasingFunction = ease });
    }
}

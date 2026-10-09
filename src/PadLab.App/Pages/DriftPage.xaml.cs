using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PadLab.App.Rendering;
using PadLab.App.Services;
using PadLab.App.ViewModels;

namespace PadLab.App.Pages;

public partial class DriftPage : Page
{
    private readonly DriftViewModel _vm = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(200) };

    public DriftPage()
    {
        InitializeComponent();
        DataContext = _vm;
        _timer.Tick += (_, _) => _vm.Tick();
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
        var s = AppState.Instance.Current;
        _vm.Attach(s);
        Canvas.Motion = AppState.Instance.MotionEnabled;
        Canvas.Renderer = s is null ? null : new DriftRenderer(s);
        Canvas.IsActive = s is not null;
        NoDeviceText.Visibility = s is null ? Visibility.Visible : Visibility.Collapsed;
    }
}

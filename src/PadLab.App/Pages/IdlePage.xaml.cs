using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PadLab.App.Services;
using PadLab.App.ViewModels;
using PadLab.Core.Model;

namespace PadLab.App.Pages;

public partial class IdlePage : Page
{
    private readonly IdleViewModel _vm = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly Action<IGamepadSource> _onAdded;
    private readonly Action<string> _onRemoved;

    public IdlePage()
    {
        InitializeComponent();
        DataContext = _vm;
        _onAdded = src => _vm.OnDeviceAdded(src.Descriptor.DeviceId);
        _onRemoved = _vm.OnDeviceRemoved;
        _timer.Tick += (_, _) => Tick();
        _vm.Events.CollectionChanged += (_, _) => UpdateNoEvents();
        Loaded += (_, _) =>
        {
            AppState.Instance.CurrentChanged += Rebind;
            // Watcher のイベントは任意スレッドから来る。IdleMonitor 側は lock 済みなのでそのまま渡す
            AppState.Instance.Watcher.Added += _onAdded;
            AppState.Instance.Watcher.Removed += _onRemoved;
            Rebind();
            _timer.Start();
        };
        Unloaded += (_, _) =>
        {
            AppState.Instance.CurrentChanged -= Rebind;
            AppState.Instance.Watcher.Added -= _onAdded;
            AppState.Instance.Watcher.Removed -= _onRemoved;
            _timer.Stop();
        };
    }

    private void Rebind() { _vm.Attach(AppState.Instance.Current); UpdateNoEvents(); }
    private void Tick() { _vm.Tick(); UpdateNoEvents(); }
    private void UpdateNoEvents() => NoEventsText.Visibility = _vm.Events.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
}

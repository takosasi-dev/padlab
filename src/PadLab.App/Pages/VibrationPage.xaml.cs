using System.Windows;
using System.Windows.Controls;
using PadLab.App.Services;
using PadLab.App.ViewModels;

namespace PadLab.App.Pages;

public partial class VibrationPage : Page
{
    private VibrationViewModel? _vm;

    public VibrationPage()
    {
        InitializeComponent();
        Title = Strings.NavVibration;
        Ui.Primary(this);
        Loaded += (_, _) => { Bind(); AppState.Instance.CurrentChanged += Bind; };
        Unloaded += (_, _) => { AppState.Instance.CurrentChanged -= Bind; Teardown(); };
    }

    private void Bind()
    {
        Teardown();
        var cur = AppState.Instance.Current;
        NoDeviceText.Visibility = cur is null ? Visibility.Visible : Visibility.Collapsed;
        Body.Visibility = cur is null ? Visibility.Collapsed : Visibility.Visible;
        if (cur is null) return;
        _vm = new VibrationViewModel(cur);
        DataContext = _vm;
        Canvas.Motion = App.MotionEnabled;
        Canvas.Renderer = _vm.Renderer;
        Canvas.IsActive = true;
    }

    private void Teardown()
    {
        Canvas.IsActive = false;
        Canvas.Renderer = null;
        _vm?.Dispose();
        _vm = null;
        DataContext = null;
    }
}

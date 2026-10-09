using System.Windows;
using System.Windows.Controls;
using PadLab.App.Services;
using PadLab.App.ViewModels;

namespace PadLab.App.Pages;

public partial class PaddlePage : Page
{
    private PaddleViewModel? _vm;

    public PaddlePage()
    {
        InitializeComponent();
        Title = Strings.NavPaddle;
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
        _vm = new PaddleViewModel(cur);
        DataContext = _vm;
    }

    private void Teardown()
    {
        _vm?.Dispose();
        _vm = null;
        DataContext = null;
    }
}

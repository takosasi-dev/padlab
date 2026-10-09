using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PadLab.App.Rendering;

namespace PadLab.App.Pages;

/// <summary>Phase 0 計測: Xbox 図 + トレイル + 散布図 3000 点を描き MeasuredFps を表示する。</summary>
public sealed class DebugPage : Page
{
    private readonly SkiaCanvasView _canvas = new();
    private readonly TextBlock _fps = new() { FontFamily = new System.Windows.Media.FontFamily("Consolas"), FontSize = 14 };
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private DemoScene? _scene;

    public DebugPage()
    {
        Title = Strings.NavDebug;
        Ui.Primary(this);
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var info = new StackPanel { Margin = new Thickness(16, 8, 16, 8) };
        info.Children.Add(_fps);
        info.Children.Add(Ui.Secondary(new TextBlock { Text = Strings.Phase0Hint, TextWrapping = TextWrapping.Wrap }));
        Grid.SetRow(info, 0);
        Grid.SetRow(_canvas, 1);
        grid.Children.Add(info);
        grid.Children.Add(_canvas);
        Content = grid;

        _timer.Tick += (_, _) => _fps.Text = string.Format(Strings.FpsFormat, _canvas.MeasuredFps);
        Loaded += (_, _) =>
        {
            _scene = new DemoScene();
            _scene.Start();
            _canvas.Motion = App.MotionEnabled;
            _canvas.Renderer = _scene;
            _canvas.IsActive = true;
            _timer.Start();
        };
        Unloaded += (_, _) =>
        {
            _timer.Stop();
            _canvas.IsActive = false;
            _canvas.Renderer = null;
            _scene?.Dispose();
            _scene = null;
        };
    }
}

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PadLab.App.Services;
using PadLab.App.ViewModels;
using PadLab.Core.Model;
using PadLab.Core.Recording;

namespace PadLab.App.Pages;

public partial class HistoryPage : Page
{
    private readonly HistoryViewModel _vm = new();

    public HistoryPage()
    {
        InitializeComponent();
        Title = Strings.NavHistory;
        Ui.Primary(this);
        DataContext = _vm;
    }

    /// <summary>ウィンドウ全体を RenderTargetBitmap で PNG 保存。既定名は MakeFileName の拡張子違い。</summary>
    private void Screenshot_Click(object sender, RoutedEventArgs e)
    {
        var win = Window.GetWindow(this);
        if (win is null) return;
        var d = AppState.Instance.Current?.Source.Descriptor;
        var name = Path.GetFileNameWithoutExtension(SessionStore.MakeFileName(
            DateTimeOffset.Now, d?.Kind ?? DeviceKind.Generic, d?.Connection ?? ConnectionType.Unknown, Guid.NewGuid().ToString("N")));
        var path = HistoryViewModel.Ask(name + ".png", "PNG|*.png");
        if (path is null) return;
        try
        {
            var w = (int)Math.Ceiling(win.ActualWidth); var h = (int)Math.Ceiling(win.ActualHeight);
            var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(win);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(rtb));
            using var fs = File.Create(path);
            enc.Save(fs);
            _vm.Message = string.Format(Strings.HistSavedFormat, path);
        }
        catch (Exception ex) { _vm.Message = string.Format(Strings.HistFailedFormat, ex.Message); }
    }
}

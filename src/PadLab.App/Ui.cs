using System.Windows;
using System.Windows.Controls;

namespace PadLab.App;

/// <summary>WPF 側の文字色を WPF-UI のテーマリソースに一元化する。Opacity や固定グレーで薄くしない。</summary>
public static class Ui
{
    public const string PrimaryBrush = "TextFillColorPrimaryBrush";
    public const string SecondaryBrush = "TextFillColorSecondaryBrush";

    /// <summary>要素 (とその子) の前景を主文字色にする。Page のルートに掛けると配下すべてに継承される。</summary>
    public static T Primary<T>(T element) where T : FrameworkElement
    {
        element.SetResourceReference(Control.ForegroundProperty, PrimaryBrush);
        return element;
    }

    /// <summary>補足文用の副文字色。</summary>
    public static TextBlock Secondary(TextBlock text)
    {
        text.SetResourceReference(TextBlock.ForegroundProperty, SecondaryBrush);
        return text;
    }
}

using System.Windows;
using System.Windows.Controls;

namespace PadLab.App.Pages;

/// <summary>波 2 (D2/D3) で XAML ページに置き換える。それまでの空ページ。</summary>
public abstract class PlaceholderPage : Page
{
    protected PlaceholderPage(string title)
    {
        Title = title;
        Ui.Primary(this);
        Content = new StackPanel
        {
            Margin = new Thickness(24),
            Children =
            {
                new TextBlock { Text = title, FontSize = 24, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) },
                Ui.Secondary(new TextBlock { Text = Strings.PlaceholderPage }),
            },
        };
    }
}

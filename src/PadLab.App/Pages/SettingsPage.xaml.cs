using System.Windows.Controls;
using PadLab.App.ViewModels;

namespace PadLab.App.Pages;

public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        Title = Strings.NavSettings;
        Ui.Primary(this);
        DataContext = new SettingsViewModel();
    }
}

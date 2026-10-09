using System.Windows.Controls;
using PadLab.App.ViewModels;

namespace PadLab.App.Pages;

public partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        Title = Strings.NavAbout;
        Ui.Primary(this);
        DataContext = new AboutViewModel();
    }
}

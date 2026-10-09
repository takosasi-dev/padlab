using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PadLab.Core.Recording;

namespace PadLab.App.ViewModels;

public sealed class AboutViewModel : ObservableObject
{
    private string _message = "";

    public string Version => string.Format(Strings.AboutVersionFormat, App.Version);
    public string SessionsDir => string.Format(Strings.AboutSessionsDirFormat, SessionStore.DefaultDirectory(AppContext.BaseDirectory));
    public string Message { get => _message; private set => SetProperty(ref _message, value); }
    public IRelayCommand OpenNoticesCommand { get; }
    public IRelayCommand OpenFolderCommand { get; }

    public AboutViewModel()
    {
        OpenNoticesCommand = new RelayCommand(() =>
        {
            var path = Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.md");
            if (!File.Exists(path)) { Message = Strings.AboutThirdPartyMissing; return; }
            Open(path);
        });
        OpenFolderCommand = new RelayCommand(() =>
        {
            var dir = SessionStore.DefaultDirectory(AppContext.BaseDirectory);
            Directory.CreateDirectory(dir);
            Open(dir);
        });
    }

    private void Open(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { Message = ex.Message; }
    }
}

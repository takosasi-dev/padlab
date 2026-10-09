using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PadLab.App.Services;
using PadLab.Core.Model;
using PadLab.Core.Recording;

namespace PadLab.App.ViewModels;

public sealed record HistoryItem(SessionSummary Summary)
{
    public string Display => string.Format(Strings.HistRowFormat, Summary.MeasuredAt, Summary.DeviceType, Summary.Connection);
}

/// <summary>保存済みセッションの一覧・詳細・エクスポート。既定ファイル名は SessionStore.MakeFileName 由来のみ。</summary>
public sealed class HistoryViewModel : ObservableObject
{
    private HistoryItem? _selected;
    private DiagnosticSession? _session;
    private string _summary = Strings.HistSelectHint, _json = "", _message = "";

    public ObservableCollection<HistoryItem> Items { get; } = new();
    public string Folder => string.Format(Strings.HistFolderFormat, SessionStore.DefaultDirectory(AppContext.BaseDirectory));
    public string Summary { get => _summary; private set => SetProperty(ref _summary, value); }
    public string Json { get => _json; private set => SetProperty(ref _json, value); }
    public string Message { get => _message; set => SetProperty(ref _message, value); }
    public bool IsEmpty => Items.Count == 0;
    public bool HasSelection => _session is not null;
    public HistoryItem? Selected
    {
        get => _selected;
        set { if (SetProperty(ref _selected, value)) LoadSelected(); }
    }
    public IRelayCommand RefreshCommand { get; }
    public IRelayCommand ExportJsonCommand { get; }
    public IRelayCommand ExportTextCommand { get; }
    public IRelayCommand ReportPngCommand { get; }

    public HistoryViewModel()
    {
        RefreshCommand = new RelayCommand(Refresh);
        ExportJsonCommand = new RelayCommand(() => Export(".json", "JSON|*.json", () => new UTF8Encoding(false).GetBytes(Json)), () => HasSelection);
        ExportTextCommand = new RelayCommand(() => Export(".txt", "テキスト|*.txt", () => new UTF8Encoding(false).GetBytes(AppState.Instance.Sessions.ExportText(_session!))), () => HasSelection);
        ReportPngCommand = new RelayCommand(() => Export(".png", "PNG|*.png", () => ReportCard.RenderPng(_session!)), () => HasSelection);
        Refresh();
    }

    public void Refresh()
    {
        Items.Clear();
        foreach (var s in AppState.Instance.Sessions.List()) Items.Add(new HistoryItem(s));
        OnPropertyChanged(nameof(IsEmpty));
        Selected = null;
        LoadSelected();
    }

    private void LoadSelected()
    {
        _session = null; Json = ""; Summary = Strings.HistSelectHint; Message = "";
        if (_selected is not null)
        {
            try
            {
                _session = AppState.Instance.Sessions.Load(_selected.Summary.Path);
                Json = File.ReadAllText(_selected.Summary.Path);
                Summary = AppState.Instance.Sessions.ExportText(_session);
            }
            catch (Exception ex) { Message = string.Format(Strings.HistFailedFormat, ex.Message); }
        }
        OnPropertyChanged(nameof(HasSelection));
        ExportJsonCommand.NotifyCanExecuteChanged(); ExportTextCommand.NotifyCanExecuteChanged(); ReportPngCommand.NotifyCanExecuteChanged();
    }

    /// <summary>既定ファイル名 = MakeFileName の拡張子違い。</summary>
    public static string BaseName(DiagnosticSession s)
    {
        if (!Enum.TryParse<DeviceKind>(s.Device.Type, true, out var kind)) kind = DeviceKind.Generic;
        if (!Enum.TryParse<ConnectionType>(s.Device.Connection, true, out var conn)) conn = ConnectionType.Unknown;
        return Path.GetFileNameWithoutExtension(SessionStore.MakeFileName(s.MeasuredAt, kind, conn, s.SessionId));
    }

    public static string? Ask(string defaultName, string filter)
    {
        var d = new SaveFileDialog { FileName = defaultName, Filter = filter };
        return d.ShowDialog() == true ? d.FileName : null;
    }

    private void Export(string ext, string filter, Func<byte[]> bytes)
    {
        if (_session is null) return;
        var path = Ask(BaseName(_session) + ext, filter);
        if (path is null) return;
        try { File.WriteAllBytes(path, bytes()); Message = string.Format(Strings.HistSavedFormat, path); }
        catch (Exception ex) { Message = string.Format(Strings.HistFailedFormat, ex.Message); }
    }
}

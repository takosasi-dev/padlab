using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PadLab.App.Services;
using PadLab.Core.Model;
using PadLab.Core.Profiles;

namespace PadLab.App.ViewModels;

public sealed partial class DeviceCardViewModel : ObservableObject
{
    public DeviceDescriptor Descriptor { get; }
    public string Name => Descriptor.DisplayName;
    public string Vendor => Descriptor.Kind == DeviceKind.Fake ? Strings.AppTitle : ProfileStore.VendorName(Descriptor.Vid);
    public string Connection => MainWindow.ConnectionLabel(Descriptor.Connection);
    [ObservableProperty] private string _polling = Strings.PollingUnknown;
    [ObservableProperty] private bool _isSelected;
    public bool IsNotSelected => !IsSelected;

    partial void OnIsSelectedChanged(bool value) => OnPropertyChanged(nameof(IsNotSelected));

    public DeviceCardViewModel(DeviceDescriptor d) => Descriptor = d;

    [RelayCommand]
    private void Select() => AppState.Instance.Select(Descriptor.DeviceId);
}

/// <summary>AppState.Devices をカードに写す。Hz と選択状態は Tick で更新。</summary>
public sealed partial class DevicePageViewModel : ObservableObject
{
    public ObservableCollection<DeviceCardViewModel> Cards { get; } = new();
    [ObservableProperty] private bool _isEmpty = true;

    public DevicePageViewModel()
    {
        Rebuild();
        AppState.Instance.Devices.CollectionChanged += OnDevicesChanged;
    }

    private void OnDevicesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var devices = AppState.Instance.Devices;
        // 追加は末尾に足す (スライドイン用)。それ以外は作り直す
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
            foreach (DeviceDescriptor d in e.NewItems) Cards.Add(new DeviceCardViewModel(d));
        else Rebuild();
        IsEmpty = devices.Count == 0;
    }

    private void Rebuild()
    {
        Cards.Clear();
        foreach (var d in AppState.Instance.Devices) Cards.Add(new DeviceCardViewModel(d));
        IsEmpty = Cards.Count == 0;
    }

    public void Tick()
    {
        var cur = AppState.Instance.Current;
        var curId = cur?.Source.Descriptor.DeviceId;
        foreach (var card in Cards)
        {
            card.IsSelected = card.Descriptor.DeviceId == curId;
            var hz = card.IsSelected ? cur!.Polling.Stats.Hz : 0;
            card.Polling = hz > 0 ? string.Format(Strings.PollingShortFormat, hz) : Strings.PollingUnknown;
        }
    }

    public void Detach() => AppState.Instance.Devices.CollectionChanged -= OnDevicesChanged;
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PadLab.App.Services;
using PadLab.Core.Model;

namespace PadLab.App.ViewModels;

public sealed partial class ChecklistItem : ObservableObject
{
    public ButtonId Id { get; }
    public string Label { get; }
    public string Category { get; }
    [ObservableProperty] private bool _pressed;
    public string Mark => Pressed ? Strings.ChecklistDone : Strings.ChecklistPending;

    public ChecklistItem(ButtonId id, string label, ButtonCategory category)
    {
        Id = id; Label = label;
        Category = category switch
        {
            ButtonCategory.Standard => Strings.CategoryStandard,
            ButtonCategory.Paddle => Strings.CategoryPaddle,
            _ => Strings.CategoryExtended,
        };
    }

    partial void OnPressedChanged(bool value) => OnPropertyChanged(nameof(Mark));
}

/// <summary>基本入力画面の右側: チェックリスト・Hz・最大到達値。値は Tick で DeviceSession から読む。</summary>
public sealed partial class BasicInputViewModel : ObservableObject
{
    public ObservableCollection<ChecklistItem> Items { get; } = new();
    [ObservableProperty] private string _polling = Strings.PollingUnknown;
    [ObservableProperty] private string _progress = "";
    [ObservableProperty] private bool _allDone;
    [ObservableProperty] private string _leftMax = "";
    [ObservableProperty] private string _rightMax = "";
    [ObservableProperty] private string _leftTriggerMax = "";
    [ObservableProperty] private string _rightTriggerMax = "";

    public void Tick(DeviceSession s)
    {
        var st = s.Coverage.State;
        if (Items.Count != st.Expected.Count)
        {
            Items.Clear();
            foreach (var b in st.Expected) Items.Add(new ChecklistItem(b.Id, b.Label, b.Category));
        }
        foreach (var item in Items) item.Pressed = st.Pressed.Contains(item.Id);
        Progress = string.Format(Strings.ChecklistProgressFormat, Items.Count(i => i.Pressed), Items.Count);
        AllDone = st.AllExpectedPressed;

        var hz = s.Polling.Stats.Hz;
        Polling = hz > 0 ? string.Format(Strings.PollingFormat, hz) : Strings.PollingUnknown;
        LeftMax = string.Format(Strings.StickMaxFormat, Strings.LeftStick, s.LeftRange.MaxRadius);
        RightMax = string.Format(Strings.StickMaxFormat, Strings.RightStick, s.RightRange.MaxRadius);
        LeftTriggerMax = string.Format(Strings.TriggerMaxFormat, Strings.LeftTrigger, s.LeftTrigger.Max);
        RightTriggerMax = string.Format(Strings.TriggerMaxFormat, Strings.RightTrigger, s.RightTrigger.Max);
    }

    public void Clear()
    {
        Items.Clear();
        Polling = Strings.PollingUnknown;
        Progress = LeftMax = RightMax = LeftTriggerMax = RightTriggerMax = "";
        AllDone = false;
    }
}

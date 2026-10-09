using PadLab.Core.Model;
using PadLab.Core.Profiles;

namespace PadLab.Core.Tests.Profiles;

public class ProfileStoreTests
{
    [Fact]
    public void DualSense_uses_ps_labels()
    {
        var p = new ProfileStore().For(DeviceKind.DualSense);
        Assert.Equal("×", p.LabelOf(ButtonId.A));
        Assert.Equal("ミュート", p.LabelOf(ButtonId.Mute));
        Assert.True(p.ExpectedCapabilities.HasFlag(Capabilities.Gyro));
    }

    [Fact]
    public void All_kinds_load()
    {
        var store = new ProfileStore();
        foreach (var kind in Enum.GetValues<DeviceKind>())
        {
            var p = store.For(kind);
            Assert.Equal(kind, p.Kind);
            Assert.NotEmpty(p.Buttons);
        }
        Assert.Equal(4, store.For(DeviceKind.XboxElite2).PaddleCount);
        Assert.Equal(2, store.For(DeviceKind.DualSenseEdge).PaddleCount);
        Assert.Equal("ボタン 1", store.For(DeviceKind.Generic).LabelOf(ButtonId.Generic1));
    }

    [Theory]
    [InlineData(0x054C, 0x0CE6, DeviceKind.DualSense)]
    [InlineData(0x054C, 0x0DF2, DeviceKind.DualSenseEdge)]
    [InlineData(0x054C, 0x09CC, DeviceKind.DualShock4)]
    [InlineData(0x045E, 0x0B12, DeviceKind.XboxOne)]
    [InlineData(0x045E, 0x0B00, DeviceKind.XboxElite2)]
    [InlineData(0x057E, 0x2009, DeviceKind.SwitchPro)]
    [InlineData(0x1234, 0x0001, DeviceKind.Generic)]
    public void KindFromVidPid(int vid, int pid, DeviceKind expected)
        => Assert.Equal(expected, ProfileStore.KindFromVidPid(vid, pid));

    [Fact]
    public void VendorName()
    {
        Assert.Equal("Sony", ProfileStore.VendorName(0x054C));
        Assert.Equal("8BitDo", ProfileStore.VendorName(0x2DC8));
        Assert.Equal("VID 0x1234", ProfileStore.VendorName(0x1234));
    }

    [Fact]
    public void Override_directory_wins()
    {
        var dir = Path.Combine(Path.GetTempPath(), "padlab_profiles_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "dualsense.json"),
                """{ "kind": "DualSense", "name": "上書き", "version": "9", "paddleCount": 0, "expectedCapabilities": [], "buttons": [ { "id": "A", "label": "バツ", "category": "Standard" } ] }""");
            var store = new ProfileStore(dir);
            Assert.Equal("上書き", store.For(DeviceKind.DualSense).Name);
            Assert.Equal("バツ", store.For(DeviceKind.DualSense).LabelOf(ButtonId.A));
            Assert.Equal("DUALSHOCK 4", store.For(DeviceKind.DualShock4).Name); // 上書きされない機種は同梱版
        }
        finally { Directory.Delete(dir, true); }
    }
}

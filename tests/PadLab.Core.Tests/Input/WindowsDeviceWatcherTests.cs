using PadLab.Core.Profiles;
using PadLab.Input;

namespace PadLab.Core.Tests.Input;

public class WindowsDeviceWatcherTests
{
    [Fact(Skip = "実機: コントローラー未接続の環境で Devices に Fake だけ出ることを手動確認する")]
    public void 未接続なら_Fake_だけ()
    {
        using var w = new WindowsDeviceWatcher(new ProfileStore());
        w.Scan();
        var d = Assert.Single(w.Devices);
        Assert.Equal("fake:0", d.Descriptor.DeviceId);
    }

    [Fact]
    public void Scan_は_実機なしでも例外を出さない()
    {
        using var w = new WindowsDeviceWatcher(new ProfileStore());
        w.Scan();
        Assert.Contains(w.Devices, d => d.Descriptor.DeviceId == "fake:0");
    }
}

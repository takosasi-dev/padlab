using PadLab.Core.Model;
using PadLab.Input.Backends;

namespace PadLab.Core.Tests.Input;

public class DirectInputAxisTests
{
    [Fact]
    public void AxisToStick_範囲と_Y反転()
    {
        Assert.Equal(new Vector2F(-1, 1), DirectInputSource.AxisToStick(0, 0));
        Assert.Equal(new Vector2F(0, 0), DirectInputSource.AxisToStick(32767, 32767));
        var max = DirectInputSource.AxisToStick(65535, 65535);
        Assert.InRange(max.X, 0.99f, 1f);
        Assert.InRange(max.Y, -1f, -0.99f);
    }

    [Fact]
    public void AxisToTrigger_0_1()
    {
        Assert.Equal(0f, DirectInputSource.AxisToTrigger(0));
        Assert.Equal(1f, DirectInputSource.AxisToTrigger(65535));
        Assert.InRange(DirectInputSource.AxisToTrigger(32767), 0.49f, 0.51f);
    }

    [Fact]
    public void 合成Z軸_を左右に振り分ける()
    {
        Assert.Equal((0f, 0f), DirectInputSource.SplitCombinedTrigger(32767));
        Assert.Equal((1f, 0f), DirectInputSource.SplitCombinedTrigger(0));
        Assert.InRange(DirectInputSource.SplitCombinedTrigger(65535).Right, 0.99f, 1f);
    }

    [Fact]
    public void POV_十字()
    {
        Assert.Equal(0UL, DirectInputSource.PovToBits(-1));
        Assert.Equal(GamepadSnapshot.Bit(ButtonId.DUp), DirectInputSource.PovToBits(0));
        Assert.Equal(GamepadSnapshot.Bit(ButtonId.DUp) | GamepadSnapshot.Bit(ButtonId.DRight), DirectInputSource.PovToBits(4500));
        Assert.Equal(GamepadSnapshot.Bit(ButtonId.DDown), DirectInputSource.PovToBits(18000));
        Assert.Equal(GamepadSnapshot.Bit(ButtonId.DLeft), DirectInputSource.PovToBits(27000));
    }
}

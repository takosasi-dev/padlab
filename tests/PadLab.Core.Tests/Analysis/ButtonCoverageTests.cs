using PadLab.Core.Analysis;
using PadLab.Core.Model;
using PadLab.Core.Profiles;

namespace PadLab.Core.Tests.Analysis;

public class ButtonCoverageTests
{
    private static readonly DeviceProfile Profile = new(DeviceKind.Fake, "test", "1",
        new[] { new ButtonSpec(ButtonId.A, "A", ButtonCategory.Standard), new ButtonSpec(ButtonId.B, "B", ButtonCategory.Standard) },
        0, Capabilities.None);

    [Fact]
    public void PressingAllExpected_SetsFlag()
    {
        var c = new ButtonCoverage(Profile);
        c.Feed(Seq.At(0, Seq.Bits(ButtonId.A)));
        Assert.Equal(new HashSet<ButtonId> { ButtonId.A }, c.State.Pressed);
        Assert.False(c.State.AllExpectedPressed);
        c.Feed(Seq.At(4, Seq.Bits(ButtonId.B)));
        Assert.True(c.State.AllExpectedPressed);
        Assert.Equal(2, c.State.Expected.Count);
    }

    [Fact]
    public void ExtraButton_AddsUnregisteredSpecOnce()
    {
        var c = new ButtonCoverage(Profile);
        var s = Seq.At(0) with { ExtraButtons = new[] { 7 } };
        c.Feed(s);
        c.Feed(s with { TimestampNs = 4 });
        var st = c.State;
        Assert.Equal(new[] { 7 }, st.UnregisteredSeen);
        var extra = Assert.Single(st.Expected, b => b.Label == "未登録ボタン #7");
        Assert.Equal(ButtonCategory.Extended, extra.Category);
        Assert.Equal((ButtonId)(32 + 7), extra.Id);
        Assert.Equal(3, st.Expected.Count);
    }

    [Fact]
    public void UnknownButtonIdBit_AddsToExpected()
    {
        var c = new ButtonCoverage(Profile);
        c.Feed(Seq.At(0, Seq.Bits(ButtonId.X)));
        Assert.Contains(c.State.Expected, b => b.Id == ButtonId.X);
        Assert.Contains(ButtonId.X, c.State.Pressed);
        c.Reset();
        Assert.Equal(2, c.State.Expected.Count);
        Assert.Empty(c.State.Pressed);
    }
}

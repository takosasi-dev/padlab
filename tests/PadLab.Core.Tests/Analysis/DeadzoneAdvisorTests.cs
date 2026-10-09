using PadLab.Core.Analysis;

namespace PadLab.Core.Tests.Analysis;

public class DeadzoneAdvisorTests
{
    [Theory]
    [InlineData(0.06f, 0.005f, null, 9)]
    [InlineData(0.0f, 0.0f, null, 3)]
    [InlineData(0.5f, 0.0f, null, 30)]
    [InlineData(0.02f, 0.0f, 0.05f, 7)]
    public void Compute_Percent(float maxDrift, float noise, float? idle, int expected)
    {
        var a = DeadzoneAdvisor.Compute(maxDrift, noise, idle);
        Assert.Equal(expected, a.RecommendedPercent);
        Assert.Contains($"{expected}%", a.Rationale);
        Assert.Equal(3, a.Hints.Count);
        Assert.Contains(a.Hints, h => h.Use == "FPS");
    }
}

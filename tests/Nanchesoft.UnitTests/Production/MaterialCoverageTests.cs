namespace Nanchesoft.UnitTests.Production;

public class MaterialCoverageTests
{
    // Mirrors the coverage logic from ProductionOrderEndpoints /explode
    private static string DetermineCoverage(decimal required, decimal onHand)
    {
        if (required <= onHand) return "covered";
        if (onHand > 0) return "partial";
        return "shortage";
    }

    [Theory]
    [InlineData(100, 100, "covered")]
    [InlineData(50,  200, "covered")]
    [InlineData(1,   1,   "covered")]
    public void WhenOnHandMeetsOrExceedsRequired_CoverageIsCovered(
        decimal required, decimal onHand, string expected)
    {
        DetermineCoverage(required, onHand).Should().Be(expected);
    }

    [Theory]
    [InlineData(100, 50,  "partial")]
    [InlineData(200, 1,   "partial")]
    [InlineData(500, 499, "partial")]
    public void WhenOnHandIsPositiveButInsufficient_CoverageIsPartial(
        decimal required, decimal onHand, string expected)
    {
        DetermineCoverage(required, onHand).Should().Be(expected);
    }

    [Theory]
    [InlineData(100, 0,   "shortage")]
    [InlineData(1,   0,   "shortage")]
    [InlineData(999, 0,   "shortage")]
    public void WhenOnHandIsZero_CoverageIsShortage(
        decimal required, decimal onHand, string expected)
    {
        DetermineCoverage(required, onHand).Should().Be(expected);
    }

    [Fact]
    public void Coverage_IsNeverNull()
    {
        var cases = new[] { (100m, 0m), (100m, 50m), (100m, 100m), (100m, 200m) };
        foreach (var (req, stock) in cases)
            DetermineCoverage(req, stock).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CoverageSet_ContainsExactlyThreeValues()
    {
        var possible = new HashSet<string>();
        possible.Add(DetermineCoverage(100, 0));
        possible.Add(DetermineCoverage(100, 50));
        possible.Add(DetermineCoverage(100, 100));

        possible.Should().BeEquivalentTo(new[] { "covered", "partial", "shortage" });
    }

    // Shortage count and fullyCovered count tracking (matches MaterialRequirement.TotalLines logic)
    [Fact]
    public void RequirementStatsCounting_IsAccurate()
    {
        var lines = new[]
        {
            DetermineCoverage(100, 100),  // covered
            DetermineCoverage(100, 50),   // partial
            DetermineCoverage(100, 0),    // shortage
            DetermineCoverage(200, 200),  // covered
            DetermineCoverage(300, 0),    // shortage
        };

        var shortages = lines.Count(c => c == "shortage");
        var fulyCovered = lines.Count(c => c == "covered");

        shortages.Should().Be(2);
        fulyCovered.Should().Be(2);

        var reqStatus = shortages > 0 ? "with_shortages" : "confirmed";
        reqStatus.Should().Be("with_shortages");
    }
}

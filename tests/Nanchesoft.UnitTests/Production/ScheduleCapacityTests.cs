namespace Nanchesoft.UnitTests.Production;

public class ScheduleCapacityTests
{
    private static decimal CalcLoadPercent(int scheduled, int capacity)
        => capacity > 0 ? Math.Round((decimal)scheduled / capacity * 100, 2) : 0m;

    [Theory]
    [InlineData(0,    1000, 0.00)]
    [InlineData(500,  1000, 50.00)]
    [InlineData(1000, 1000, 100.00)]
    [InlineData(1200, 1000, 120.00)]   // overloaded
    [InlineData(1,    3,    33.33)]
    public void LoadPercent_CalculatesCorrectly(int scheduled, int capacity, decimal expected)
    {
        CalcLoadPercent(scheduled, capacity).Should().Be(expected);
    }

    [Fact]
    public void LoadPercent_ZeroCapacity_ReturnsZero()
    {
        CalcLoadPercent(100, 0).Should().Be(0m);
    }

    [Fact]
    public void OverloadedSchedule_LoadExceeds100()
    {
        CalcLoadPercent(1500, 1000).Should().BeGreaterThan(100m);
    }

    [Fact]
    public void AvailableUnits_CannotBeNegativeAfterClamp()
    {
        int capacity = 1000;
        int scheduled = 1200;
        var available = Math.Max(0, capacity - scheduled);
        available.Should().Be(0);
    }

    [Theory]
    [InlineData(200, 1000, 800)]
    [InlineData(1000, 1000, 0)]
    [InlineData(0, 1000, 1000)]
    public void AvailableUnits_IsCapacityMinusScheduled(int scheduled, int capacity, int expected)
    {
        var available = Math.Max(0, capacity - scheduled);
        available.Should().Be(expected);
    }

    // Surplus detection
    [Fact]
    public void SurplusUnits_IsProducedMinusPlanned()
    {
        const int planned = 100;
        const int produced = 115;
        var surplus = produced - planned;
        surplus.Should().Be(15);
    }

    [Theory]
    [InlineData(100, 100, 0)]
    [InlineData(100, 115, 15)]
    [InlineData(100, 90,  -10)]  // shortfall (not surplus)
    public void SurplusCalculation_IncludesNegativeForShortfall(int planned, int produced, int expected)
    {
        (produced - planned).Should().Be(expected);
    }

    // Progress percent
    [Theory]
    [InlineData(0,   100, 0.0)]
    [InlineData(50,  100, 50.0)]
    [InlineData(100, 100, 100.0)]
    [InlineData(1,   3,   33.3)]
    public void ProgressPercent_RoundsToOneDecimal(int produced, int planned, decimal expected)
    {
        var pct = planned > 0 ? Math.Round((decimal)produced / planned * 100, 1) : 0m;
        pct.Should().Be(expected);
    }
}

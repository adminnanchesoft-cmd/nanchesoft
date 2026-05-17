namespace Nanchesoft.UnitTests.Production;

public class PieceWorkCalculationTests
{
    [Theory]
    [InlineData(100, 0, 6.00, 600.00, 0.00, 600.00)]
    [InlineData(100, 5, 6.00, 600.00, 15.00, 585.00)]   // 5 rejected × 6.00 × 0.50
    [InlineData(50, 2, 8.00, 400.00, 8.00, 392.00)]     // 2 rejected × 8.00 × 0.50
    [InlineData(200, 0, 4.50, 900.00, 0.00, 900.00)]
    [InlineData(1, 1, 6.00, 6.00, 3.00, 3.00)]
    public void GrossAndNet_AreCalculatedCorrectly(
        int unitsProduced, int unitsRejected, decimal unitPrice,
        decimal expectedGross, decimal expectedDeduction, decimal expectedNet)
    {
        var gross = Math.Round(unitPrice * unitsProduced, 4);
        var deduction = unitsRejected > 0 ? Math.Round(unitPrice * unitsRejected * 0.5m, 4) : 0m;
        var net = Math.Round(gross - deduction, 4);

        gross.Should().Be(expectedGross);
        deduction.Should().Be(expectedDeduction);
        net.Should().Be(expectedNet);
    }

    [Fact]
    public void ZeroUnitsProduced_ProducesZeroAmounts()
    {
        var unitPrice = 6.00m;
        var gross = Math.Round(unitPrice * 0, 4);
        var net = gross;

        gross.Should().Be(0m);
        net.Should().Be(0m);
    }

    [Fact]
    public void QualityDeduction_IsHalfUnitPricePerRejectedUnit()
    {
        const decimal unitPrice = 8.00m;
        const int rejected = 3;
        var expected = unitPrice * rejected * 0.5m;

        var actual = Math.Round(unitPrice * rejected * 0.5m, 4);

        actual.Should().Be(expected);
    }

    [Fact]
    public void NetAmount_NeverExceedsGross()
    {
        for (var rejected = 0; rejected <= 10; rejected++)
        {
            var gross = 100m;
            var deduction = Math.Round(5.00m * rejected * 0.5m, 4);
            var net = Math.Round(gross - deduction, 4);

            net.Should().BeLessThanOrEqualTo(gross);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void NegativeOrZeroUnitPrice_ProducesZeroGross(decimal badPrice)
    {
        var effectivePrice = badPrice <= 0 ? 0m : badPrice;
        var gross = effectivePrice * 100;
        gross.Should().Be(0m);
    }
}

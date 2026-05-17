namespace Nanchesoft.UnitTests.Payroll;

public class PayrollSplitTaxableTests
{
    private static (decimal Taxable, decimal Exempt) SplitTaxable(string taxableType, decimal amount) =>
        taxableType?.ToLowerInvariant() switch
        {
            "exempt"  => (0m, amount),
            "mixed"   => (Math.Round(amount * 0.5m, 2), Math.Round(amount * 0.5m, 2)),
            _         => (amount, 0m)   // "taxable" y cualquier otro valor
        };

    [Fact]
    public void Taxable_AllAmountIsTaxable()
    {
        var (taxable, exempt) = SplitTaxable("taxable", 1_000m);
        taxable.Should().Be(1_000m);
        exempt.Should().Be(0m);
    }

    [Fact]
    public void Exempt_AllAmountIsExempt()
    {
        var (taxable, exempt) = SplitTaxable("exempt", 1_000m);
        taxable.Should().Be(0m);
        exempt.Should().Be(1_000m);
    }

    [Fact]
    public void Mixed_SplitsFiftyFifty()
    {
        var (taxable, exempt) = SplitTaxable("mixed", 1_000m);
        taxable.Should().Be(500m);
        exempt.Should().Be(500m);
    }

    [Fact]
    public void Mixed_OddAmount_RoundsCorrectly()
    {
        var (taxable, exempt) = SplitTaxable("mixed", 101m);
        (taxable + exempt).Should().BeApproximately(101m, 0.01m);
    }

    [Fact]
    public void NullType_DefaultsToTaxable()
    {
        var (taxable, exempt) = SplitTaxable(null!, 500m);
        taxable.Should().Be(500m);
        exempt.Should().Be(0m);
    }

    [Fact]
    public void UnknownType_DefaultsToTaxable()
    {
        var (taxable, exempt) = SplitTaxable("unknown", 500m);
        taxable.Should().Be(500m);
        exempt.Should().Be(0m);
    }

    [Theory]
    [InlineData("taxable", 2000.0, 2000.0, 0.0)]
    [InlineData("exempt",  2000.0,    0.0, 2000.0)]
    [InlineData("mixed",   2000.0, 1000.0, 1000.0)]
    [InlineData("TAXABLE", 2000.0, 2000.0, 0.0)]
    [InlineData("EXEMPT",  2000.0,    0.0, 2000.0)]
    public void SplitTaxable_AllCases(string type, double amount, double expectedTaxable, double expectedExempt)
    {
        var (taxable, exempt) = SplitTaxable(type, (decimal)amount);
        taxable.Should().Be((decimal)expectedTaxable);
        exempt.Should().Be((decimal)expectedExempt);
    }
}

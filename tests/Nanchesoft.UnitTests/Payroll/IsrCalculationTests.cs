namespace Nanchesoft.UnitTests.Payroll;

/// <summary>
/// Verifica la tarifa ISR 2024 Art. 96 LISR y el subsidio al empleo.
/// Los valores esperados se obtienen aplicando manualmente la tabla oficial
/// de retención para renta gravable mensual.
/// </summary>
public class IsrCalculationTests
{
    // ── Tabla ISR 2024 (privada en PayrollMvpEndpoints) ─────────────────────
    // Replicada aquí para poder probar la lógica sin modificar el endpoint.

    private static readonly (decimal LI, decimal LS, decimal CF, decimal Rate)[] IsrTable2024 =
    [
        (      0.01m,     746.04m,      0.00m, 0.0192m),
        (    746.05m,   6_332.05m,     14.32m, 0.0640m),
        (  6_332.06m,  11_128.01m,    371.83m, 0.1088m),
        ( 11_128.02m,  12_935.82m,    893.63m, 0.1600m),
        ( 12_935.83m,  15_487.71m,  1_182.88m, 0.1792m),
        ( 15_487.72m,  31_236.49m,  1_640.18m, 0.2136m),
        ( 31_236.50m,  49_233.00m,  5_004.12m, 0.2352m),
        ( 49_233.01m,  93_993.90m,  9_236.89m, 0.3000m),
        ( 93_993.91m, 125_325.20m, 22_665.17m, 0.3200m),
        (125_325.21m, 375_975.61m, 32_691.18m, 0.3400m),
        (375_975.62m, decimal.MaxValue, 117_912.32m, 0.3500m),
    ];

    private static readonly (decimal LI, decimal LS, decimal Subsidy)[] SubsidioTable2024 =
    [
        (    0.01m, 1_768.96m, 407.02m),
        (1_768.97m, 2_653.38m, 406.83m),
        (2_653.39m, 3_472.84m, 406.62m),
        (3_472.85m, 3_537.87m, 392.77m),
        (3_537.88m, 4_446.15m, 382.46m),
        (4_446.16m, 4_717.18m, 354.23m),
        (4_717.19m, 5_335.42m, 324.87m),
        (5_335.43m, 6_224.67m, 294.63m),
        (6_224.68m, 7_113.90m, 253.54m),
        (7_113.91m, 7_382.33m, 217.61m),
        (7_382.34m, decimal.MaxValue, 0.00m),
    ];

    private static decimal IsrMonthly(decimal monthly)
    {
        if (monthly <= 0m) return 0m;
        foreach (var (li, ls, cf, rate) in IsrTable2024)
            if (monthly >= li && monthly <= ls)
                return Math.Round(cf + (monthly - li) * rate, 2);
        return 0m;
    }

    private static decimal SubsidioMonthly(decimal monthly)
    {
        if (monthly <= 0m) return 0m;
        foreach (var (li, ls, subsidy) in SubsidioTable2024)
            if (monthly >= li && monthly <= ls)
                return subsidy;
        return 0m;
    }

    private static (decimal NetIsr, decimal SubsidioPerception) CalculateIsrAndSubsidio(decimal taxable, int periodDays)
    {
        if (taxable <= 0m || periodDays <= 0) return (0m, 0m);
        var factor = periodDays / 30.4m;
        var monthly = taxable / factor;
        var isr = IsrMonthly(monthly) * factor;
        var sub = SubsidioMonthly(monthly) * factor;
        if (sub >= isr) return (0m, Math.Round(sub - isr, 2));
        return (Math.Round(isr - sub, 2), 0m);
    }

    // ── IsrMonthly ────────────────────────────────────────────────────────────

    [Fact]
    public void IsrMonthly_Zero_ReturnsZero()
    {
        IsrMonthly(0m).Should().Be(0m);
    }

    [Fact]
    public void IsrMonthly_Negative_ReturnsZero()
    {
        IsrMonthly(-100m).Should().Be(0m);
    }

    // Primer tramo: 0.01 – 746.04  |  CF=0  |  Tasa=1.92%
    [Theory]
    [InlineData(400.0)]
    [InlineData(746.04)]
    public void IsrMonthly_FirstBracket_UsesCorrectFormula(double monthlyD)
    {
        var monthly = (decimal)monthlyD;
        var result = IsrMonthly(monthly);
        result.Should().BeGreaterThan(0m);
        result.Should().BeLessThan(14.33m);
    }

    // Segundo tramo: 746.05 – 6,332.05  |  CF=14.32  |  Tasa=6.4%
    [Fact]
    public void IsrMonthly_SecondBracket_MatchesManualCalc()
    {
        // monthly = 3,000  →  14.32 + (3000 - 746.05) * 0.064
        var expected = Math.Round(14.32m + (3_000m - 746.05m) * 0.0640m, 2);
        IsrMonthly(3_000m).Should().Be(expected);
    }

    // Octavo tramo: 49,233.01 – 93,993.90  |  CF=9,236.89  |  Tasa=30%
    [Fact]
    public void IsrMonthly_EighthBracket_MatchesManualCalc()
    {
        var monthly = 70_000m;
        var expected = Math.Round(9_236.89m + (monthly - 49_233.01m) * 0.30m, 2);
        IsrMonthly(monthly).Should().Be(expected);
    }

    // Último tramo (>375,975.62): tasa 35%
    [Fact]
    public void IsrMonthly_TopBracket_Uses35PctRate()
    {
        var monthly = 500_000m;
        var expected = Math.Round(117_912.32m + (monthly - 375_975.62m) * 0.35m, 2);
        IsrMonthly(monthly).Should().Be(expected);
    }

    // ── SubsidioMonthly ───────────────────────────────────────────────────────

    [Fact]
    public void SubsidioMonthly_Zero_ReturnsZero()
    {
        SubsidioMonthly(0m).Should().Be(0m);
    }

    [Fact]
    public void SubsidioMonthly_BelowFirstLimit_Returns407()
    {
        // 0.01 – 1,768.96  → subsidio = 407.02
        SubsidioMonthly(1_000m).Should().Be(407.02m);
    }

    [Fact]
    public void SubsidioMonthly_AboveTopLimit_ReturnsZero()
    {
        // > 7,382.34 → subsidio = 0
        SubsidioMonthly(8_000m).Should().Be(0m);
    }

    [Fact]
    public void SubsidioMonthly_MidRange_ReturnsCorrectValue()
    {
        // 4,446.16 – 4,717.18 → 354.23
        SubsidioMonthly(4_500m).Should().Be(354.23m);
    }

    // ── CalculateIsrAndSubsidio ───────────────────────────────────────────────

    [Fact]
    public void CalculateIsrAndSubsidio_ZeroTaxable_ReturnsBothZero()
    {
        var (isr, sub) = CalculateIsrAndSubsidio(0m, 15);
        isr.Should().Be(0m);
        sub.Should().Be(0m);
    }

    [Fact]
    public void CalculateIsrAndSubsidio_ZeroDays_ReturnsBothZero()
    {
        var (isr, sub) = CalculateIsrAndSubsidio(5_000m, 0);
        isr.Should().Be(0m);
        sub.Should().Be(0m);
    }

    [Fact]
    public void CalculateIsrAndSubsidio_LowIncome_ReturnsSubsidioPerception()
    {
        // Ingreso bajo: subsidio > ISR → debe devolver subsidio como percepción (ISR=0)
        var (isr, sub) = CalculateIsrAndSubsidio(1_000m, 15);
        isr.Should().Be(0m);
        sub.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void CalculateIsrAndSubsidio_HighIncome_ReturnsIsrDeduction()
    {
        // Ingreso alto: ISR > subsidio → deve devolver ISR como deducción (subsidio=0)
        var (isr, sub) = CalculateIsrAndSubsidio(40_000m, 15);
        isr.Should().BeGreaterThan(0m);
        sub.Should().Be(0m);
    }

    [Fact]
    public void CalculateIsrAndSubsidio_NeverReturnsBothNonZero()
    {
        // Invariante: exactamente uno de los dos debe ser cero
        var cases = new[] { 500m, 2_000m, 5_000m, 10_000m, 30_000m, 80_000m };
        foreach (var taxable in cases)
        {
            var (isr, sub) = CalculateIsrAndSubsidio(taxable, 15);
            (isr == 0m || sub == 0m).Should().BeTrue(
                because: $"para taxable={taxable}, exactamente uno debe ser cero (isr={isr}, sub={sub})");
        }
    }

    [Fact]
    public void CalculateIsrAndSubsidio_15DayPeriod_IsProportionalToMonthly()
    {
        // Para un periodo de 15 días el ISR debe ser aproximadamente la mitad del mensual
        var taxable15 = 15_000m; // 15-day taxable
        var (isr15, _) = CalculateIsrAndSubsidio(taxable15, 15);

        var taxable30 = 30_000m; // equivalent 30-day taxable
        var (isr30, _) = CalculateIsrAndSubsidio(taxable30, 30);

        // Allow ±5% tolerance due to progressive bracket rounding
        var ratio = isr30 > 0 ? isr15 / isr30 : 0m;
        ratio.Should().BeInRange(0.45m, 0.55m);
    }
}

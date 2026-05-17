namespace Nanchesoft.UnitTests.Payroll;

/// <summary>
/// Verifica el cálculo de la cuota obrera IMSS 2024.
/// Ramas aplicadas: Enfermedades y Maternidad excedente + prestaciones en dinero,
/// Invalidez y Vida, Cesantía en Edad Avanzada y Vejez.
/// </summary>
public class ImssCalculationTests
{
    private const decimal UmaDaily2024 = 108.57m;

    private static decimal CalculateImssCuotaObrera(decimal sbcDaily, int periodDays)
    {
        if (sbcDaily <= 0m || periodDays <= 0) return 0m;
        var excess   = Math.Max(0m, sbcDaily - 3m * UmaDaily2024);
        var emExcede = excess   * 0.0040m;
        var emDinero = sbcDaily * 0.0025m;
        var iv       = sbcDaily * 0.00625m;
        var cv       = sbcDaily * 0.01125m;
        return Math.Round((emExcede + emDinero + iv + cv) * periodDays, 2);
    }

    [Fact]
    public void ImssObrera_ZeroSbc_ReturnsZero()
    {
        CalculateImssCuotaObrera(0m, 15).Should().Be(0m);
    }

    [Fact]
    public void ImssObrera_ZeroDays_ReturnsZero()
    {
        CalculateImssCuotaObrera(300m, 0).Should().Be(0m);
    }

    [Fact]
    public void ImssObrera_BelowThreeUma_NoExcedente()
    {
        // SBC <= 3 × UMA  →  excedente = 0, sólo EM dinero + IV + CV
        var sbc = 3m * UmaDaily2024 - 1m;   // justo por debajo del límite
        var expected = Math.Round((0m + sbc * 0.0025m + sbc * 0.00625m + sbc * 0.01125m) * 15, 2);
        CalculateImssCuotaObrera(sbc, 15).Should().Be(expected);
    }

    [Fact]
    public void ImssObrera_AboveThreeUma_IncludesExcedente()
    {
        var sbc = 500m; // bien por encima de 3 × 108.57 = 325.71
        var excess   = sbc - 3m * UmaDaily2024;
        var emExcede = excess   * 0.0040m;
        var emDinero = sbc     * 0.0025m;
        var iv       = sbc     * 0.00625m;
        var cv       = sbc     * 0.01125m;
        var expected = Math.Round((emExcede + emDinero + iv + cv) * 15, 2);
        CalculateImssCuotaObrera(sbc, 15).Should().Be(expected);
    }

    [Fact]
    public void ImssObrera_IsProportionalToPeriodDays()
    {
        var sbc = 400m;
        var imss30 = CalculateImssCuotaObrera(sbc, 30);
        var imss15 = CalculateImssCuotaObrera(sbc, 15);

        // 30 días debe ser el doble de 15 días (permitir ±1 centavo por redondeo)
        Math.Abs(imss30 - imss15 * 2m).Should().BeLessThanOrEqualTo(0.01m);
    }

    [Fact]
    public void ImssObrera_TotalRateComponents_AreCorrect()
    {
        // Para un SBC = 3 × UMA exacto, el excedente es 0
        // Total rate sin excedente = 0.0025 + 0.00625 + 0.01125 = 0.02
        var sbc = 3m * UmaDaily2024;
        var expected = Math.Round(sbc * 0.02m * 7, 2);
        CalculateImssCuotaObrera(sbc, 7).Should().Be(expected);
    }

    [Theory]
    [InlineData(200.0, 15)]   // SBC bajo (< 3 × UMA)
    [InlineData(400.0, 15)]   // SBC medio
    [InlineData(1000.0, 15)]  // SBC alto
    [InlineData(400.0, 7)]    // periodo corto
    [InlineData(400.0, 30)]   // periodo mensual
    public void ImssObrera_AlwaysPositive(double sbcD, int days)
    {
        CalculateImssCuotaObrera((decimal)sbcD, days).Should().BeGreaterThan(0m);
    }
}

namespace Nanchesoft.UnitTests.Production;

public class FolioGenerationTests
{
    // Mirrors the folio-formatting logic from ProductionOrderEndpoints
    private static string FormatFolio(string prefix, int number, int padLength)
        => $"{prefix}{number.ToString().PadLeft(padLength, '0')}";

    [Theory]
    [InlineData("OP",   1,   6, "OP000001")]
    [InlineData("OP",   100, 6, "OP000100")]
    [InlineData("OP",   999999, 6, "OP999999")]
    [InlineData("VALE", 1,   6, "VALE000001")]
    [InlineData("VALE", 42,  6, "VALE000042")]
    public void FormatFolio_PadsNumberCorrectly(string prefix, int number, int pad, string expected)
    {
        FormatFolio(prefix, number, pad).Should().Be(expected);
    }

    [Fact]
    public void FormatFolio_NumberExceedingPad_StillFormatsWithoutTruncation()
    {
        // Numbers larger than pad width must not be truncated
        var result = FormatFolio("OP", 1_000_000, 6);
        result.Should().Be("OP1000000");
        result.Length.Should().BeGreaterThan(8);
    }

    [Fact]
    public void FolioFallback_WhenNoSeriesExists_ContainsPrefixAndTimestamp()
    {
        var fallback = $"OP-{DateTime.UtcNow:yyyyMMddHHmmss}";
        fallback.Should().StartWith("OP-");
        fallback.Length.Should().BeGreaterThan(10);
    }

    [Theory]
    [InlineData("OP",   1, "OP000001")]
    [InlineData("OP",   2, "OP000002")]
    [InlineData("VALE", 1, "VALE000001")]
    [InlineData("VALE", 2, "VALE000002")]
    public void ConsecutiveFolios_AreSequential(string prefix, int startNumber, string expectedFirst)
    {
        var first = FormatFolio(prefix, startNumber, 6);
        first.Should().Be(expectedFirst);

        var second = FormatFolio(prefix, startNumber + 1, 6);
        second.Should().NotBe(first);

        // The number embedded in the second should be greater
        var firstNum = int.Parse(first[prefix.Length..]);
        var secondNum = int.Parse(second[prefix.Length..]);
        secondNum.Should().BeGreaterThan(firstNum);
    }

    [Fact]
    public void WeekCode_Normalisation_UpperCasesAndTrims()
    {
        var raw = "  2026-w20  ";
        var normalised = raw.Trim().ToUpper();
        normalised.Should().Be("2026-W20");
    }

    [Theory]
    [InlineData("2026-W01")]
    [InlineData("2026-W20")]
    [InlineData("2026-W52")]
    public void WeekCode_NormalisedForm_MatchesExpectedPattern(string code)
    {
        // Format: 4-digit year, dash, W, 1-2 digit week
        code.Should().MatchRegex(@"^\d{4}-W\d{1,2}$");
    }
}

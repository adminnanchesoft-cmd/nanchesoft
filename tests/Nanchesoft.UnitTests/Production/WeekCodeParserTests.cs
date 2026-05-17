namespace Nanchesoft.UnitTests.Production;

public class WeekCodeParserTests
{
    // Mirrors the ParseWeekCode logic from ProductionScheduleEndpoints
    private static (DateOnly start, DateOnly end) ParseWeekCode(string weekCode)
    {
        try
        {
            var parts = weekCode.Split('-');
            if (parts.Length != 2) return (default, default);
            if (!int.TryParse(parts[0], out var year)) return (default, default);
            var weekPart = parts[1];
            if (!weekPart.StartsWith('W')) return (default, default);
            if (!int.TryParse(weekPart[1..], out var week)) return (default, default);

            // Jan 4 is always in ISO week 1; treat Sunday as 7 for correct Monday anchor
            var jan4 = new DateOnly(year, 1, 4);
            var dow = jan4.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)jan4.DayOfWeek;
            var startOfYear = jan4.AddDays(1 - dow);
            var start = startOfYear.AddDays((week - 1) * 7);
            var end = start.AddDays(6);
            return (start, end);
        }
        catch
        {
            return (default, default);
        }
    }

    [Theory]
    [InlineData("2026-W01", 2025, 12, 29, 2026, 1, 4)]   // ISO W01 2026 starts Dec 29 2025
    [InlineData("2026-W20", 2026, 5, 11, 2026, 5, 17)]
    [InlineData("2026-W52", 2026, 12, 21, 2026, 12, 27)]
    [InlineData("2025-W01", 2024, 12, 30, 2025, 1, 5)]
    public void ParseWeekCode_ReturnsCorrectMondayToSunday(
        string code,
        int startYear, int startMonth, int startDay,
        int endYear, int endMonth, int endDay)
    {
        var (start, end) = ParseWeekCode(code);

        start.Should().Be(new DateOnly(startYear, startMonth, startDay),
            because: $"{code} should start on Monday {startYear}-{startMonth:D2}-{startDay:D2}");
        end.Should().Be(new DateOnly(endYear, endMonth, endDay),
            because: $"{code} should end on Sunday");
    }

    [Fact]
    public void ParseWeekCode_StartIsAlwaysMonday()
    {
        var codes = new[] { "2026-W01", "2026-W10", "2026-W20", "2026-W40", "2026-W52" };
        foreach (var code in codes)
        {
            var (start, _) = ParseWeekCode(code);
            start.DayOfWeek.Should().Be(DayOfWeek.Monday, because: $"{code} must start on Monday");
        }
    }

    [Fact]
    public void ParseWeekCode_EndIsAlwaysSunday()
    {
        var codes = new[] { "2026-W01", "2026-W10", "2026-W20", "2026-W40", "2026-W52" };
        foreach (var code in codes)
        {
            var (_, end) = ParseWeekCode(code);
            end.DayOfWeek.Should().Be(DayOfWeek.Sunday, because: $"{code} must end on Sunday");
        }
    }

    [Fact]
    public void ParseWeekCode_SpanIsAlways7Days()
    {
        var (start, end) = ParseWeekCode("2026-W20");
        (end.DayNumber - start.DayNumber).Should().Be(6);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026")]
    [InlineData("2026-20")]
    [InlineData("W20")]
    [InlineData("INVALID")]
    [InlineData("2026-W")]
    [InlineData("2026-Wxx")]
    public void ParseWeekCode_InvalidFormats_ReturnDefault(string bad)
    {
        var (start, end) = ParseWeekCode(bad);
        start.Should().Be(default);
        end.Should().Be(default);
    }

    [Fact]
    public void WeekCode_RoundTrip_ProducesConsistentResults()
    {
        // The same week code always produces the same date range
        var (s1, e1) = ParseWeekCode("2026-W20");
        var (s2, e2) = ParseWeekCode("2026-W20");

        s1.Should().Be(s2);
        e1.Should().Be(e2);
    }
}

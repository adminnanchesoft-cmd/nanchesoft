namespace Nanchesoft.UnitTests.Production;

public class ProductionOrderStatusTests
{
    // Valid forward transitions per the state machine
    private static readonly Dictionary<string, string[]> ValidTransitions = new()
    {
        ["draft"]       = ["planned", "cancelled"],
        ["planned"]     = ["exploded", "in_progress", "cancelled"],
        ["exploded"]    = ["reserved", "in_progress", "cancelled"],
        ["reserved"]    = ["in_progress", "planned", "cancelled"],
        ["in_progress"] = ["completed", "planned", "cancelled"],
        ["completed"]   = ["closed"],
        ["closed"]      = [],
        ["cancelled"]   = []
    };

    [Theory]
    [InlineData("draft",       "planned")]
    [InlineData("draft",       "cancelled")]
    [InlineData("planned",     "exploded")]
    [InlineData("planned",     "in_progress")]
    [InlineData("planned",     "cancelled")]
    [InlineData("exploded",    "reserved")]
    [InlineData("exploded",    "in_progress")]
    [InlineData("reserved",    "in_progress")]
    [InlineData("reserved",    "planned")]
    [InlineData("in_progress", "completed")]
    [InlineData("in_progress", "planned")]
    [InlineData("in_progress", "cancelled")]
    [InlineData("completed",   "closed")]
    public void ValidTransition_IsAllowed(string from, string to)
    {
        CanTransition(from, to).Should().BeTrue(
            because: $"'{from}' → '{to}' is a valid production order transition");
    }

    [Theory]
    [InlineData("draft",     "completed")]
    [InlineData("draft",     "closed")]
    [InlineData("planned",   "closed")]
    [InlineData("completed", "in_progress")]
    [InlineData("completed", "planned")]
    [InlineData("closed",    "draft")]
    [InlineData("closed",    "cancelled")]
    [InlineData("cancelled", "draft")]
    [InlineData("cancelled", "planned")]
    [InlineData("cancelled", "in_progress")]
    public void InvalidTransition_IsRejected(string from, string to)
    {
        CanTransition(from, to).Should().BeFalse(
            because: $"'{from}' → '{to}' is not a valid production order transition");
    }

    [Fact]
    public void TerminalStatuses_HaveNoValidForwardTransitions()
    {
        CanTransition("closed",    "planned").Should().BeFalse();
        CanTransition("cancelled", "draft").Should().BeFalse();
    }

    [Fact]
    public void AllKnownStatuses_AreHandled()
    {
        var statuses = new[] { "draft", "planned", "exploded", "reserved", "in_progress", "completed", "closed", "cancelled" };
        foreach (var s in statuses)
            ValidTransitions.ContainsKey(s).Should().BeTrue(because: $"status '{s}' must be in the state machine");
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("planned")]
    [InlineData("exploded")]
    [InlineData("reserved")]
    [InlineData("in_progress")]
    public void ActiveStatuses_CanBeCancelled(string status)
    {
        CanTransition(status, "cancelled").Should().BeTrue();
    }

    [Theory]
    [InlineData("completed")]
    [InlineData("closed")]
    [InlineData("cancelled")]
    public void FinalStatuses_CannotBeCancelled(string status)
    {
        CanTransition(status, "cancelled").Should().BeFalse();
    }

    private static bool CanTransition(string from, string to)
        => ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}

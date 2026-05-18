using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Accounting;

[Collection("NanchesoftApi")]
public class AccountingEndpointTests
{
    private readonly HttpClient _client;

    public AccountingEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Lookups_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounting/lookups");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Dashboard_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounting/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChartOfAccounts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounting/chart-of-accounts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FiscalPeriods_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounting/fiscal-periods");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task JournalEntries_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounting/journal-entries");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetJournalEntry_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/accounting/journal-entries/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TrialBalance_ReturnsOk()
    {
        var now = DateTime.UtcNow;
        var response = await _client.GetAsync($"/api/accounting/trial-balance?year={now.Year}&month={now.Month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BalanceSheet_ReturnsOk()
    {
        var now = DateTime.UtcNow;
        var response = await _client.GetAsync($"/api/accounting/balance-sheet?year={now.Year}&month={now.Month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task IncomeStatement_ReturnsOk()
    {
        var now = DateTime.UtcNow;
        var response = await _client.GetAsync($"/api/accounting/income-statement?year={now.Year}&month={now.Month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AutoPoliciesSummary_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounting/auto-policies/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ledger_WithUnknownAccount_ReturnsOkOrNotFound()
    {
        var response = await _client.GetAsync($"/api/accounting/ledger?accountId={Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}

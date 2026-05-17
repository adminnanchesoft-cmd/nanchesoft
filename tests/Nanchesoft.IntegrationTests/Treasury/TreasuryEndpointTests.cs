using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Treasury;

public class TreasuryEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public TreasuryEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Dashboard ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DashboardSummary_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/treasury/dashboard/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DashboardBalances_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/treasury/dashboard/balances");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DashboardRecent_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/treasury/dashboard/recent");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Lookups_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/treasury/lookups");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Cash Accounts ────────────────────────────────────────────────────────

    [Fact]
    public async Task ListCashAccounts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/treasury/cash-accounts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCashAccount_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/treasury/cash-accounts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateCashAccount_WithMinimalData_ReturnsOkWithId()
    {
        var payload = new
        {
            code = "INT-CX-01",
            name = "Caja integración test",
            initialBalance = 0m,
            isActive = true
        };

        var response = await _client.PostAsJsonAsync("/api/treasury/cash-accounts", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);
    }

    // ── Bank Accounts ────────────────────────────────────────────────────────

    [Fact]
    public async Task ListBankAccounts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/treasury/bank-accounts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBankAccount_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/treasury/bank-accounts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Incomes ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListIncomes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/treasury/incomes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetIncome_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/treasury/incomes/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Expenses ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListExpenses_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/treasury/expenses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetExpense_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/treasury/expenses/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Receipts ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListReceipts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/treasury/receipts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReceipt_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/treasury/receipts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Payments ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListPayments_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/treasury/payments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPayment_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/treasury/payments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Reconciliations ──────────────────────────────────────────────────────

    [Fact]
    public async Task ListReconciliations_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/treasury/reconciliations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReconciliation_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/treasury/reconciliations/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record CreatedResponse(Guid Id);
}

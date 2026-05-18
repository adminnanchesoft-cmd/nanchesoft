using System.Net;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Finance;

[Collection("NanchesoftApi")]
public class AccountsReceivableEndpointTests
{
    private readonly HttpClient _client;

    public AccountsReceivableEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Lookups_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounts-receivable/lookups");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Balances_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounts-receivable/balances");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Aging_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounts-receivable/aging");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Applications_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounts-receivable/applications");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Statements_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounts-receivable/statements");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DashboardSummary_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounts-receivable/dashboard/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DashboardRecent_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounts-receivable/dashboard/recent");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Aging_WithUnknownCustomer_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/accounts-receivable/aging?customerId={Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Statements_WithUnknownCustomer_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/accounts-receivable/statements?customerId={Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

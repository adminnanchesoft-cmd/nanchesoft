using System.Net;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Finance;

public class AccountsPayableEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public AccountsPayableEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Lookups_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounts-payable/lookups");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Balances_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounts-payable/balances");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Aging_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounts-payable/aging");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Applications_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounts-payable/applications");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Statements_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounts-payable/statements");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DashboardSummary_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounts-payable/dashboard/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DashboardRecent_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/accounts-payable/dashboard/recent");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Statements_WithUnknownSupplier_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/accounts-payable/statements?supplierId={Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

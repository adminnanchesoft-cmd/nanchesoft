using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Admin;

[Collection("NanchesoftApi")]
public class ReportsAndSubscriptionEndpointTests
{
    private readonly HttpClient _client;

    public ReportsAndSubscriptionEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Reports ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOperationalSummary_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/reports/operational/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOperationalPurchases_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/reports/operational/purchases");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOperationalSales_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/reports/operational/sales");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOperationalInventory_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/reports/operational/inventory");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetExecutiveSummary_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/reports/executive/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Subscription control ───────────────────────────────────────────────────

    [Fact]
    public async Task GetSubscriptionDashboard_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/subscription/control/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateSubscriptionCharge_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/subscription/control/charges/{Guid.NewGuid()}", new { status = "paid" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

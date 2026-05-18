using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Admin;

[Collection("NanchesoftApi")]
public class CoreAdminEndpointTests
{
    private readonly HttpClient _client;

    public CoreAdminEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Plans ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListPlans_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/core/plans");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreatePlan_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/core/plans", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePlan_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/core/plans/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePlan_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/core/plans/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Tenants ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListTenants_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/core/tenants");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateTenant_MissingRequired_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/core/tenants", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTenant_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/core/tenants/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTenant_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/core/tenants/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Access logs ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAccessLogs_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/administration/access-logs");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Audit ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAuditEntries_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/audit");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

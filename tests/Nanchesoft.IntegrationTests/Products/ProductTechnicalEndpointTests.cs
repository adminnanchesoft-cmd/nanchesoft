using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Products;

public class ProductTechnicalEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public ProductTechnicalEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Product size runs enterprise ───────────────────────────────────────────

    [Fact]
    public async Task ListSizeRunsEnterprise_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/size-runs-enterprise");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateSizeRunEnterprise_MissingRequired_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/products/size-runs-enterprise", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSizeRunEnterprise_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/size-runs-enterprise/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSizeRunEnterprise_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/size-runs-enterprise/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateVariants_UnknownId_Returns404()
    {
        var response = await _client.PostAsJsonAsync($"/api/products/size-runs-enterprise/{Guid.NewGuid()}/generate-variants", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Technical center overview ──────────────────────────────────────────────

    [Fact]
    public async Task GetTechnicalCenterOverview_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/technical-center/overview");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTechnicalCenterOverviewByProduct_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/products/technical-center/overview/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Product governance overview ────────────────────────────────────────────

    [Fact]
    public async Task GetProductGovernanceOverview_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/technical-center/overview");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Engineering readiness ──────────────────────────────────────────────────

    [Fact]
    public async Task GetEngineeringReadiness_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/engineering-readiness");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

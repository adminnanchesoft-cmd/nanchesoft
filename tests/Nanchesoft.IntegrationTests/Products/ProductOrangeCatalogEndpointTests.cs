using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Products;

[Collection("NanchesoftApi")]
public class ProductOrangeCatalogEndpointTests
{
    private readonly HttpClient _client;

    public ProductOrangeCatalogEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Leather types ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ListLeatherTypes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/leather-types");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateLeatherType_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/leather-types/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteLeatherType_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/leather-types/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Soles ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListSoles_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/soles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateSole_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/soles/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSole_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/soles/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Colors ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListColors_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/colors");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateColor_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/colors/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Sole colors ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListSoleColors_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/sole-colors");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateSoleColor_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/sole-colors/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Quality control dies ───────────────────────────────────────────────────

    [Fact]
    public async Task ListQualityControlDies_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/quality-control-dies");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateQualityControlDie_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/quality-control-dies/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Folio patterns ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ListFolioPatterns_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/folio-patterns");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateFolioPattern_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/folio-patterns/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Manufacturing types ────────────────────────────────────────────────────

    [Fact]
    public async Task ListManufacturingTypes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/manufacturing-types");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateManufacturingType_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/manufacturing-types/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Products;

public class ProductEngineeringEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public ProductEngineeringEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Unit conversions ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListUnitConversions_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/unit-conversions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateUnitConversion_MissingRequired_Returns400()
    {
        var payload = new { fromUnit = "", toUnit = "", factor = 0 };
        var response = await _client.PostAsJsonAsync("/api/products/unit-conversions", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUnitConversion_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/unit-conversions/{Guid.NewGuid()}", new { factor = 1 });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUnitConversion_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/unit-conversions/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Size runs ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListSizeRuns_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/size-runs");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateSizeRun_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/products/size-runs", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSizeRun_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/size-runs/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSizeRun_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/size-runs/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Product families ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListProductFamilies_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/families");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProductFamily_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/products/families", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProductFamily_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/families/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProductFamily_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/families/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Product lasts ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ListProductLasts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/lasts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProductLast_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/products/lasts", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProductLast_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/lasts/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProductLast_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/lasts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Product lines ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ListProductLines_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/lines");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProductLine_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/products/lines", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProductLine_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/lines/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProductLine_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/lines/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

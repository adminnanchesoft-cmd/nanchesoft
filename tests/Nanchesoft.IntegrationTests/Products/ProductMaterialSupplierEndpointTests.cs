using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Products;

[Collection("NanchesoftApi")]
public class ProductMaterialSupplierEndpointTests
{
    private readonly HttpClient _client;

    public ProductMaterialSupplierEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Material suppliers ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListMaterialSuppliers_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/material-suppliers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateMaterialSupplier_MissingRequired_Returns400()
    {
        var payload = new { materialId = (Guid?)null, supplierId = (Guid?)null };
        var response = await _client.PostAsJsonAsync("/api/products/material-suppliers", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateMaterialSupplier_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/material-suppliers/{Guid.NewGuid()}", new { leadTimeDays = 5 });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMaterialSupplier_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/material-suppliers/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Material supplier cost history ─────────────────────────────────────────

    [Fact]
    public async Task ListMaterialSupplierCostHistory_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/material-supplier-cost-history");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateMaterialSupplierCostHistory_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/material-supplier-cost-history/{Guid.NewGuid()}", new { unitCost = 10 });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMaterialSupplierCostHistory_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/material-supplier-cost-history/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

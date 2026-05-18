using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.ThirdParties;

public class ProductCatalogEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public ProductCatalogEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Item categories ────────────────────────────────────────────────────────

    [Fact]
    public async Task ListCategories_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateCategory_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/products/categories", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCategory_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/categories/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/categories/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Item brands ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ListBrands_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/brands");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateBrand_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/products/brands", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateBrand_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/brands/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteBrand_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/brands/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Item models ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ListModels_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/models");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateModel_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/products/models", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateModel_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/models/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteModel_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/models/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Items ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListItems_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/items");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateItem_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/products/items", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateItem_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/items/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteItem_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/items/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Price lists ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListPriceLists_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/price-lists");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdatePriceList_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/price-lists/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePriceList_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/price-lists/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Barcodes ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListBarcodes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/barcodes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateBarcode_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/barcodes/{Guid.NewGuid()}", new { barcode = "123456" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteBarcode_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/barcodes/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

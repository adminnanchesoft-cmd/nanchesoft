using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Products;

[Collection("NanchesoftApi")]
public class ProductCatalogOpsEndpointTests
{
    private readonly HttpClient _client;

    public ProductCatalogOpsEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Material characteristics ───────────────────────────────────────────────

    [Fact]
    public async Task ListMaterialCharacteristics_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/material-characteristics");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListMaterialCharacteristicsOptions_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/material-characteristics/options");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateMaterialCharacteristic_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/products/material-characteristics", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateMaterialCharacteristic_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/material-characteristics/{Guid.NewGuid()}", new { code = "ZZ-NOTFOUND", name = "Test", isActive = true });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMaterialCharacteristic_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/material-characteristics/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Material sizes ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ListMaterialSizes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/material-sizes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListMaterialSizesOptions_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/material-sizes/options");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateMaterialSize_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/products/material-sizes", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateMaterialSize_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/material-sizes/{Guid.NewGuid()}", new { code = "ZZ-NOTFOUND", name = "Test", isActive = true });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMaterialSize_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/material-sizes/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Material families ──────────────────────────────────────────────────────

    [Fact]
    public async Task ListMaterialFamilies_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/material-families");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListMaterialFamiliesOptions_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/material-families/options");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateMaterialFamily_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/products/material-families", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateMaterialFamily_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/material-families/{Guid.NewGuid()}", new { code = "ZZ-NOTFOUND", name = "Test", isActive = true });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMaterialFamily_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/material-families/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Material subfamilies ───────────────────────────────────────────────────

    [Fact]
    public async Task ListMaterialSubfamilies_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/material-subfamilies");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListMaterialSubfamiliesOptions_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/material-subfamilies/options");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateMaterialSubfamily_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/products/material-subfamilies", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateMaterialSubfamily_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/material-subfamilies/{Guid.NewGuid()}", new { code = "ZZ-NOTFOUND", name = "Test", materialFamilyId = Guid.NewGuid(), isActive = true });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMaterialSubfamily_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/material-subfamilies/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Material items ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ListMaterialItems_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/material-items");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListMaterialItemsOptions_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/material-items/options");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateMaterialItem_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/products/material-items", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateMaterialItem_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/material-items/{Guid.NewGuid()}", new { code = "ZZ-NOTFOUND", name = "Test", materialSubfamilyId = Guid.NewGuid(), isActive = true });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMaterialItem_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/material-items/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Finished products ──────────────────────────────────────────────────────

    [Fact]
    public async Task ListFinishedProducts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/finished-products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListFinishedProductsOptions_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/finished-products/options");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateFinishedProduct_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/products/finished-products", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateFinishedProduct_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/finished-products/{Guid.NewGuid()}", new { code = "ZZ-NOTFOUND-FP", isActive = true });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteFinishedProduct_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/finished-products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Product components ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListProductComponents_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/product-components");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListProductComponentsOptions_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/product-components/options");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateProductComponent_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/product-components/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProductComponent_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/product-components/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Product consumption profiles ───────────────────────────────────────────

    [Fact]
    public async Task ListProductConsumptionProfiles_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products/product-consumption-profiles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateProductConsumptionProfile_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/product-consumption-profiles/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProductConsumptionProfile_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/product-consumption-profiles/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Services;

public class ServiceBillingEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public ServiceBillingEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Service notes ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ListServiceNotes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/services/service-notes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateServiceNote_MissingRequired_Returns400()
    {
        var payload = new { code = "", customerId = (Guid?)null };
        var response = await _client.PostAsJsonAsync("/api/services/service-notes", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateServiceNote_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/services/service-notes/{Guid.NewGuid()}", new { status = "open" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteServiceNote_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/services/service-notes/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Service catalog ────────────────────────────────────────────────────────

    [Fact]
    public async Task ListServiceCatalog_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/services/catalog");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateServiceCatalogItem_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/services/catalog", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateServiceCatalogItem_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/services/catalog/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteServiceCatalogItem_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/services/catalog/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Customer service rates ─────────────────────────────────────────────────

    [Fact]
    public async Task ListCustomerServiceRates_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/services/customer-rates");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateCustomerServiceRate_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/services/customer-rates/{Guid.NewGuid()}", new { rate = 100 });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCustomerServiceRate_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/services/customer-rates/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

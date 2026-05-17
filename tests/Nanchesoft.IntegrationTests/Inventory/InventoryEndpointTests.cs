using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Inventory;

public class InventoryEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public InventoryEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Read-only views ──────────────────────────────────────────────────────

    [Fact]
    public async Task StockBalances_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/inventory/stock-balances");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Kardex_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/inventory/kardex");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Lots_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/inventory/lots");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Serials_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/inventory/serials");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DashboardSummary_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/inventory/dashboard/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Lookups_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/inventory/lookups");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Entries ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListEntries_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/inventory/entries");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetEntry_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/inventory/entries/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateEntry_WithMinimalData_ReturnsOkWithId()
    {
        var payload = new
        {
            folio = "INT-ENT-001",
            documentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            documentType = "entry",
            notes = "Integration test entry",
            lines = Array.Empty<object>()
        };

        var response = await _client.PostAsJsonAsync("/api/inventory/entries", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);

        // Cleanup
        await _client.DeleteAsync($"/api/inventory/entries/{created.Id}");
    }

    [Fact]
    public async Task DeleteEntry_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/inventory/entries/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Exits ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListExits_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/inventory/exits");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetExit_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/inventory/exits/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Transfers ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListTransfers_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/inventory/transfers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTransfer_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/inventory/transfers/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Adjustments ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAdjustments_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/inventory/adjustments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAdjustment_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/inventory/adjustments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record CreatedResponse(Guid Id);
}

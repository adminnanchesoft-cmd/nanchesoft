using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Admin;

public class MasterCatalogEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public MasterCatalogEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Currencies ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListCurrencies_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/catalogs/currencies");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateCurrency_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/catalogs/currencies", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCurrency_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/catalogs/currencies/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCurrency_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/catalogs/currencies/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Exchange rates ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ListExchangeRates_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/catalogs/exchange-rates");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateExchangeRate_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/catalogs/exchange-rates/{Guid.NewGuid()}", new { rate = 1.0 });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteExchangeRate_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/catalogs/exchange-rates/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Taxes ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListTaxes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/catalogs/taxes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateTax_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/catalogs/taxes/{Guid.NewGuid()}", new { rate = 0.16 });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTax_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/catalogs/taxes/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Units ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListUnits_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/catalogs/units");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateUnit_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/catalogs/units/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUnit_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/catalogs/units/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Banks ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListBanks_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/catalogs/banks");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateBank_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/catalogs/banks/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteBank_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/catalogs/banks/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Monitoring ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task MonitoringHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/monitoring/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MonitoringSchemaCatalog_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/monitoring/schema-catalog");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

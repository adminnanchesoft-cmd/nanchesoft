using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Admin;

[Collection("NanchesoftApi")]
public class AdminOrganizationEndpointTests
{
    private readonly HttpClient _client;

    public AdminOrganizationEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Branches ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListBranches_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/organization/branches");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateBranch_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/organization/branches", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateBranch_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/organization/branches/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteBranch_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/organization/branches/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Companies ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListCompanies_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/organization/companies");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateCompany_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/organization/companies", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCompany_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/organization/companies/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCompany_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/organization/companies/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Warehouses ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListWarehouses_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/organization/warehouses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateWarehouse_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/organization/warehouses", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateWarehouse_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/organization/warehouses/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteWarehouse_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/organization/warehouses/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

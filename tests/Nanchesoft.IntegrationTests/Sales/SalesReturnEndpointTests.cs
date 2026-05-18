using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Sales;

[Collection("NanchesoftApi")]
public class SalesReturnEndpointTests
{
    private readonly HttpClient _client;

    public SalesReturnEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListReturns_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/sales/returns");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReturn_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/sales/returns/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateReturn_MinimalData_ReturnsOkWithId()
    {
        var payload = new
        {
            folio = "INT-DEV-001",
            returnDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            lines = Array.Empty<object>()
        };
        var response = await _client.PostAsJsonAsync("/api/sales/returns", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);

        await _client.DeleteAsync($"/api/sales/returns/{created.Id}");
    }

    [Fact]
    public async Task UpdateReturn_UnknownId_Returns404()
    {
        var payload = new { notes = "Updated" };
        var response = await _client.PutAsJsonAsync($"/api/sales/returns/{Guid.NewGuid()}", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteReturn_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/sales/returns/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record CreatedResponse(Guid Id);
}

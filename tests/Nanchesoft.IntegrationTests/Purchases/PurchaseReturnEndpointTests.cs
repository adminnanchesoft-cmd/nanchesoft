using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Purchases;

[Collection("NanchesoftApi")]
public class PurchaseReturnEndpointTests
{
    private readonly HttpClient _client;

    public PurchaseReturnEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListReturns_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/purchases/returns");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReturn_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/purchases/returns/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateReturn_MinimalData_ReturnsOkWithId()
    {
        var payload = new
        {
            folio = "INT-DCOM-001",
            returnDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            lines = Array.Empty<object>()
        };
        var response = await _client.PostAsJsonAsync("/api/purchases/returns", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);

        await _client.DeleteAsync($"/api/purchases/returns/{created.Id}");
    }

    [Fact]
    public async Task UpdateReturn_UnknownId_Returns404()
    {
        var payload = new { notes = "Updated" };
        var response = await _client.PutAsJsonAsync($"/api/purchases/returns/{Guid.NewGuid()}", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteReturn_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/purchases/returns/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record CreatedResponse(Guid Id);
}

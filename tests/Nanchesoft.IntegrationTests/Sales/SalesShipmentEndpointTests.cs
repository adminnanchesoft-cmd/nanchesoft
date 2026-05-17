using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Sales;

public class SalesShipmentEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public SalesShipmentEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListShipments_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/sales/shipments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetShipment_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/sales/shipments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateShipment_MinimalData_ReturnsOkWithId()
    {
        var payload = new { folio = "INT-REM-001", lines = Array.Empty<object>() };
        var response = await _client.PostAsJsonAsync("/api/sales/shipments", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);

        await _client.DeleteAsync($"/api/sales/shipments/{created.Id}");
    }

    [Fact]
    public async Task UpdateShipment_UnknownId_Returns404()
    {
        var payload = new { notes = "Updated" };
        var response = await _client.PutAsJsonAsync($"/api/sales/shipments/{Guid.NewGuid()}", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteShipment_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/sales/shipments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record CreatedResponse(Guid Id);
}

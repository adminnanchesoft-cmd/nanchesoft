using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Purchases;

[Collection("NanchesoftApi")]
public class PurchaseOrderEndpointTests
{
    private readonly HttpClient _client;

    public PurchaseOrderEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListOrders_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/purchases/orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOrder_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/purchases/orders/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateOrder_WithMinimalData_ReturnsOkWithId()
    {
        var payload = new
        {
            folio = "INT-OC-001",
            orderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            status = "draft",
            exchangeRate = 1.0m,
            paymentTermDays = 30,
            lines = Array.Empty<object>()
        };

        var response = await _client.PostAsJsonAsync("/api/purchases/orders", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);

        // Cleanup
        await _client.DeleteAsync($"/api/purchases/orders/{created.Id}");
    }

    [Fact]
    public async Task UpdateOrder_UnknownId_Returns404()
    {
        var payload = new { notes = "test" };
        var response = await _client.PutAsJsonAsync($"/api/purchases/orders/{Guid.NewGuid()}", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteOrder_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/purchases/orders/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAndUpdateOrder_UpdatesSuccessfully()
    {
        var createPayload = new
        {
            folio = "INT-OC-UPD",
            orderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            status = "draft",
            exchangeRate = 1.0m,
            notes = "Original",
            lines = Array.Empty<object>()
        };

        var createResponse = await _client.PostAsJsonAsync("/api/purchases/orders", createPayload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();

        var updatePayload = new { notes = "Updated by integration test" };
        var updateResponse = await _client.PutAsJsonAsync($"/api/purchases/orders/{created!.Id}", updatePayload);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Cleanup
        await _client.DeleteAsync($"/api/purchases/orders/{created.Id}");
    }

    private sealed record CreatedResponse(Guid Id);
}

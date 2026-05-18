using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Sales;

[Collection("NanchesoftApi")]
public class SalesOrderEndpointTests
{
    private readonly HttpClient _client;

    public SalesOrderEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListOrders_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/sales/orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOrder_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/sales/orders/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateOrder_WithMinimalData_ReturnsOkWithId()
    {
        var payload = new
        {
            folio = "INT-PED-001",
            orderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            status = "draft",
            exchangeRate = 1.0m,
            paymentTermDays = 30,
            lines = Array.Empty<object>()
        };

        var response = await _client.PostAsJsonAsync("/api/sales/orders", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);

        // Cleanup
        await _client.DeleteAsync($"/api/sales/orders/{created.Id}");
    }

    [Fact]
    public async Task UpdateOrder_UnknownId_Returns404()
    {
        var payload = new { notes = "test" };
        var response = await _client.PutAsJsonAsync($"/api/sales/orders/{Guid.NewGuid()}", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteOrder_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/sales/orders/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAndDeleteOrder_CompleteCycle()
    {
        var payload = new
        {
            folio = "INT-PED-DEL",
            orderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            status = "draft",
            exchangeRate = 1.0m,
            lines = Array.Empty<object>()
        };

        var createResponse = await _client.PostAsJsonAsync("/api/sales/orders", payload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();

        var deleteResponse = await _client.DeleteAsync($"/api/sales/orders/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record CreatedResponse(Guid Id);
}

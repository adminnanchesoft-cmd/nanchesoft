using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Purchases;

public class PurchaseRequisitionEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public PurchaseRequisitionEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListRequisitions_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/purchases/requisitions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRequisition_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/purchases/requisitions/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateRequisition_WithMinimalData_ReturnsOkWithId()
    {
        var payload = new
        {
            folio = "INT-REQ-001",
            requestDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            status = "draft",
            notes = "Integration test requisition",
            lines = Array.Empty<object>()
        };

        var response = await _client.PostAsJsonAsync("/api/purchases/requisitions", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);

        // Cleanup
        await _client.DeleteAsync($"/api/purchases/requisitions/{created.Id}");
    }

    [Fact]
    public async Task UpdateRequisition_UnknownId_Returns404()
    {
        var payload = new { notes = "test" };
        var response = await _client.PutAsJsonAsync($"/api/purchases/requisitions/{Guid.NewGuid()}", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRequisition_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/purchases/requisitions/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAndDeleteRequisition_CompleteCycle()
    {
        var payload = new
        {
            folio = "INT-REQ-DEL",
            requestDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            status = "draft",
            lines = Array.Empty<object>()
        };

        var createResponse = await _client.PostAsJsonAsync("/api/purchases/requisitions", payload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();

        var deleteResponse = await _client.DeleteAsync($"/api/purchases/requisitions/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record CreatedResponse(Guid Id);
}

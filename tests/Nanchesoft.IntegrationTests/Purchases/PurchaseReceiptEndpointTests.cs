using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Purchases;

public class PurchaseReceiptEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public PurchaseReceiptEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListReceipts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/purchases/receipts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReceipt_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/purchases/receipts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateReceipt_MinimalData_ReturnsOkWithId()
    {
        var payload = new { folio = "INT-REC-001", lines = Array.Empty<object>() };
        var response = await _client.PostAsJsonAsync("/api/purchases/receipts", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);

        await _client.DeleteAsync($"/api/purchases/receipts/{created.Id}");
    }

    [Fact]
    public async Task UpdateReceipt_UnknownId_Returns404()
    {
        var payload = new { notes = "Updated" };
        var response = await _client.PutAsJsonAsync($"/api/purchases/receipts/{Guid.NewGuid()}", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteReceipt_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/purchases/receipts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record CreatedResponse(Guid Id);
}

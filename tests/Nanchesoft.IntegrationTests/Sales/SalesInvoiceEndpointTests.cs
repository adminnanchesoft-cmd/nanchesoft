using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Sales;

[Collection("NanchesoftApi")]
public class SalesInvoiceEndpointTests
{
    private readonly HttpClient _client;

    public SalesInvoiceEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListInvoices_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/sales/invoices");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListInvoices_WithFilter_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/sales/invoices?status=draft");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetInvoice_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/sales/invoices/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateInvoice_MinimalData_ReturnsOkWithId()
    {
        var payload = new
        {
            folio = "INT-FAC-001",
            invoiceDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            exchangeRate = 1.0m,
            lines = Array.Empty<object>()
        };
        var response = await _client.PostAsJsonAsync("/api/sales/invoices", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);

        await _client.DeleteAsync($"/api/sales/invoices/{created.Id}");
    }

    [Fact]
    public async Task UpdateInvoice_UnknownId_Returns404()
    {
        var payload = new { notes = "Updated" };
        var response = await _client.PutAsJsonAsync($"/api/sales/invoices/{Guid.NewGuid()}", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteInvoice_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/sales/invoices/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record CreatedResponse(Guid Id);
}

using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Sales;

[Collection("NanchesoftApi")]
public class SalesDashboardEndpointTests
{
    private readonly HttpClient _client;

    public SalesDashboardEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DashboardSummary_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/sales/dashboard/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DashboardSummary_ReturnsExpectedShape()
    {
        var response = await _client.GetAsync("/api/sales/dashboard/summary");
        var json = await response.Content.ReadFromJsonAsync<SalesDashboardShape>();
        json.Should().NotBeNull();
        json!.OpenOrders.Should().BeGreaterThanOrEqualTo(0);
        json.OpenQuotes.Should().BeGreaterThanOrEqualTo(0);
        json.NetSales.Should().Be(json.PeriodSales - json.ReturnsAmount - json.CreditNotesAmount);
        json.AverageInvoiceTicket.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task SalesLookups_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/sales/lookups");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListShipments_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/sales/shipments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListInvoices_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/sales/invoices");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListReturns_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/sales/returns");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListCreditNotes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/sales/credit-notes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetShipment_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/sales/shipments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetInvoice_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/sales/invoices/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCreditNote_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/sales/credit-notes/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record SalesDashboardShape(
        int OpenOrders,
        int OpenQuotes,
        decimal PeriodSales,
        decimal ReturnsAmount,
        decimal CreditNotesAmount,
        decimal NetSales,
        decimal AverageInvoiceTicket);
}

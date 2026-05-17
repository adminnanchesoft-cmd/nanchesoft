using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Purchases;

public class PurchaseDashboardEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public PurchaseDashboardEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DashboardSummary_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/purchases/dashboard/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DashboardSummary_ReturnsExpectedShape()
    {
        var response = await _client.GetAsync("/api/purchases/dashboard/summary");
        var json = await response.Content.ReadFromJsonAsync<PurchaseDashboardShape>();
        json.Should().NotBeNull();
        json!.OpenOrders.Should().BeGreaterThanOrEqualTo(0);
        json.PendingRequisitions.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task PurchasesLookups_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/purchases/lookups");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListReceipts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/purchases/receipts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListInvoices_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/purchases/invoices");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListReturns_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/purchases/returns");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReceipt_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/purchases/receipts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetInvoice_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/purchases/invoices/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetReturn_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/purchases/returns/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record PurchaseDashboardShape(int OpenOrders, int PendingRequisitions, decimal PeriodPurchased);
}

using System.Net;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Finance;

[Collection("NanchesoftApi")]
public class CfdiEndpointTests
{
    private readonly HttpClient _client;

    public CfdiEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Configuration_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/cfdi/configuration");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Dashboard_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/cfdi/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Documents_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/cfdi/documents");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StampQueue_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/cfdi/stamp-queue");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Cancellation_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/cfdi/cancellation");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SourcesSalesInvoices_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/cfdi/sources/sales-invoices");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SourcesCreditNotes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/cfdi/sources/credit-notes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SourcesPayrollRuns_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/cfdi/sources/payroll-runs");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SourcesShipments_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/cfdi/sources/shipments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PayrollRunStatus_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/cfdi/payroll-runs/{Guid.NewGuid()}/status");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PayrollRunReceipts_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/cfdi/payroll-runs/{Guid.NewGuid()}/receipts");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SalesInvoiceStatus_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/cfdi/sales-invoices/{Guid.NewGuid()}/status");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

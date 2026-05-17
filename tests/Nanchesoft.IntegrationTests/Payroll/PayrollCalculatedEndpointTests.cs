using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Payroll;

public class PayrollCalculatedEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public PayrollCalculatedEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Source applications ────────────────────────────────────────────────────

    [Fact]
    public async Task ListSourceApplications_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/source-applications");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateSourceApplication_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/payroll/source-applications/{Guid.NewGuid()}", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSourceApplication_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/payroll/source-applications/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Receipt control ────────────────────────────────────────────────────────

    [Fact]
    public async Task ListReceiptControls_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/receipt-control");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GenerateReceiptControls_UnknownRun_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/payroll/receipt-control/runs/{Guid.NewGuid()}/generate",
            new { userId = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateReceiptControl_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/payroll/receipt-control/{Guid.NewGuid()}", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Run closings ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ListRunClosings_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/run-closings");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GenerateRunClosing_UnknownRun_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/payroll/run-closings/runs/{Guid.NewGuid()}/generate",
            new { userId = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateRunClosing_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/payroll/run-closings/{Guid.NewGuid()}", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRunClosing_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/payroll/run-closings/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

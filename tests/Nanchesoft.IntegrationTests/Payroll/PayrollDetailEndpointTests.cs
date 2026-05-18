using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Payroll;

[Collection("NanchesoftApi")]
public class PayrollDetailEndpointTests
{
    private readonly HttpClient _client;

    public PayrollDetailEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Run line details ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListRunLineDetails_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/run-line-details");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateRunLineDetail_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/payroll/run-line-details/{Guid.NewGuid()}", new { amount = 0 });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRunLineDetail_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/payroll/run-line-details/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Run breakdown ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateRunDetails_UnknownRunId_Returns404()
    {
        var response = await _client.PostAsJsonAsync($"/api/payroll/runs/{Guid.NewGuid()}/generate-details", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRunBreakdown_UnknownRunId_Returns404()
    {
        var response = await _client.GetAsync($"/api/payroll/runs/{Guid.NewGuid()}/breakdown");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

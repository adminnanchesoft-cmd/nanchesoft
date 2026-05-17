using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Payroll;

public class PayrollPrePayrollEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public PayrollPrePayrollEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Attendance daily summaries ─────────────────────────────────────────────

    [Fact]
    public async Task ListSummaries_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/attendance-daily-summaries");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateSummary_MissingEmployee_Returns400()
    {
        var payload = new { workDate = DateTime.UtcNow.ToString("yyyy-MM-dd") };
        var response = await _client.PostAsJsonAsync("/api/payroll/attendance-daily-summaries", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSummary_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/payroll/attendance-daily-summaries/{Guid.NewGuid()}", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSummary_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/payroll/attendance-daily-summaries/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Pre-payroll adjustments ────────────────────────────────────────────────

    [Fact]
    public async Task ListAdjustments_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/prepayroll-adjustments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateAdjustment_MissingEmployee_Returns400()
    {
        var payload = new { adjustmentCode = "ADJ-001" }; // no employeeId
        var response = await _client.PostAsJsonAsync("/api/payroll/prepayroll-adjustments", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateAdjustment_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/payroll/prepayroll-adjustments/{Guid.NewGuid()}", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAdjustment_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/payroll/prepayroll-adjustments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Pre-payroll cutoffs ────────────────────────────────────────────────────

    [Fact]
    public async Task ListCutoffs_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/prepayroll-cutoffs");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateCutoff_MissingPeriod_Returns400()
    {
        var payload = new { cutoffCode = "CUT-001" }; // no periodId
        var response = await _client.PostAsJsonAsync("/api/payroll/prepayroll-cutoffs", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCutoff_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/payroll/prepayroll-cutoffs/{Guid.NewGuid()}", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCutoff_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/payroll/prepayroll-cutoffs/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

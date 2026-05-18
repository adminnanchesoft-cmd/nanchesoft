using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Payroll;

public class PayrollDisbursementEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public PayrollDisbursementEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Dispersion batches ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListDispersionBatches_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/dispersion-batches");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateDispersionBatch_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/payroll/dispersion-batches/{Guid.NewGuid()}", new { status = "pending" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteDispersionBatch_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/payroll/dispersion-batches/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateDispersionBatch_UnknownRunId_Returns404()
    {
        var response = await _client.PostAsJsonAsync($"/api/payroll/dispersion-batches/runs/{Guid.NewGuid()}/generate", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Dispersion lines ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListDispersionLines_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/dispersion-lines");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateDispersionLine_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/payroll/dispersion-lines/{Guid.NewGuid()}", new { amount = 0 });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteDispersionLine_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/payroll/dispersion-lines/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateDispersionLines_UnknownBatchId_Returns404()
    {
        var response = await _client.PostAsJsonAsync($"/api/payroll/dispersion-lines/batches/{Guid.NewGuid()}/generate", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Accounting postings ────────────────────────────────────────────────────

    [Fact]
    public async Task ListAccountingPostings_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/accounting-postings");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateAccountingPosting_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/payroll/accounting-postings/{Guid.NewGuid()}", new { status = "pending" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAccountingPosting_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/payroll/accounting-postings/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateAccountingPosting_UnknownRunId_Returns404()
    {
        var response = await _client.PostAsJsonAsync($"/api/payroll/accounting-postings/runs/{Guid.NewGuid()}/generate", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

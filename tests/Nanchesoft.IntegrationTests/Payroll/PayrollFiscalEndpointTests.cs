using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Payroll;

public class PayrollFiscalEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public PayrollFiscalEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Tax accumulators ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListTaxAccumulators_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/tax-accumulators");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateTaxAccumulator_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/payroll/tax-accumulators/{Guid.NewGuid()}", new { amount = 0 });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTaxAccumulator_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/payroll/tax-accumulators/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateTaxAccumulators_UnknownRunId_Returns404()
    {
        var response = await _client.PostAsJsonAsync($"/api/payroll/tax-accumulators/runs/{Guid.NewGuid()}/generate", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Employer obligations ───────────────────────────────────────────────────

    [Fact]
    public async Task ListEmployerObligations_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/employer-obligations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateEmployerObligation_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/payroll/employer-obligations/{Guid.NewGuid()}", new { amount = 0 });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEmployerObligation_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/payroll/employer-obligations/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateEmployerObligations_UnknownRunId_Returns404()
    {
        var response = await _client.PostAsJsonAsync($"/api/payroll/employer-obligations/runs/{Guid.NewGuid()}/generate", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Fiscal reconciliations ─────────────────────────────────────────────────

    [Fact]
    public async Task ListFiscalReconciliations_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/fiscal-reconciliations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateFiscalReconciliation_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/payroll/fiscal-reconciliations/{Guid.NewGuid()}", new { status = "pending" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteFiscalReconciliation_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/payroll/fiscal-reconciliations/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReconcileFiscal_UnknownRunId_Returns404()
    {
        var response = await _client.PostAsJsonAsync($"/api/payroll/fiscal-reconciliations/runs/{Guid.NewGuid()}/reconcile", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

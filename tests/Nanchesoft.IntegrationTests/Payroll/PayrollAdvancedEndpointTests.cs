using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Payroll;

public class PayrollAdvancedEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public PayrollAdvancedEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Time clock ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListPunches_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/time-clock");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreatePunch_MissingEmployee_Returns400()
    {
        var payload = new { punchType = "entry" }; // no employeeId
        var response = await _client.PostAsJsonAsync("/api/hr/time-clock", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePunch_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/hr/time-clock/{Guid.NewGuid()}", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePunch_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/hr/time-clock/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Recurring movements ────────────────────────────────────────────────────

    [Fact]
    public async Task ListRecurringMovements_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/recurring-movements");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateRecurringMovement_MissingEmployee_Returns400()
    {
        var payload = new { movementType = "perception" }; // no employeeId or conceptId
        var response = await _client.PostAsJsonAsync("/api/payroll/recurring-movements", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateRecurringMovement_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/payroll/recurring-movements/{Guid.NewGuid()}", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRecurringMovement_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/payroll/recurring-movements/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Loans ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListLoans_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/loans");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateLoan_MissingEmployee_Returns400()
    {
        var payload = new { loanAmount = 1000m }; // no employeeId or conceptId
        var response = await _client.PostAsJsonAsync("/api/payroll/loans", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateLoan_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/payroll/loans/{Guid.NewGuid()}", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteLoan_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/payroll/loans/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Loan deductions ────────────────────────────────────────────────────────

    [Fact]
    public async Task ListLoanDeductions_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/loan-deductions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateLoanDeduction_MissingLoan_Returns400()
    {
        var payload = new { amount = 100m }; // no loanId or employeeId
        var response = await _client.PostAsJsonAsync("/api/payroll/loan-deductions", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateLoanDeduction_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/payroll/loan-deductions/{Guid.NewGuid()}", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteLoanDeduction_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/payroll/loan-deductions/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Receipt lines ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetReceiptLines_UnknownRun_Returns404()
    {
        var response = await _client.GetAsync($"/api/payroll/runs/{Guid.NewGuid()}/receipt-lines");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRunPrintHtml_UnknownRun_Returns404()
    {
        var response = await _client.GetAsync($"/api/payroll/runs/{Guid.NewGuid()}/print-html");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

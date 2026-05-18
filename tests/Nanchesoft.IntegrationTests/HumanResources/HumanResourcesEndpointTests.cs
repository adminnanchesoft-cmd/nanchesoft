using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.HumanResources;

[Collection("NanchesoftApi")]
public class HumanResourcesEndpointTests
{
    private readonly HttpClient _client;

    public HumanResourcesEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Departments ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ListDepartments_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/departments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDepartment_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/hr/departments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Positions ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListPositions_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/positions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPosition_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/hr/positions/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Employees ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListEmployees_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/employees");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetEmployee_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/hr/employees/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Payroll Periods ──────────────────────────────────────────────────────

    [Fact]
    public async Task ListPayrollPeriods_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/periods");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPayrollPeriod_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/payroll/periods/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Payroll Concepts ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListPayrollConcepts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/concepts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Payroll Runs ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ListPayrollRuns_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/runs");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPayrollRun_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/payroll/runs/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Incidents ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListIncidents_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/incidents");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Employee Loans ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListLoans_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/payroll/loans");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLoan_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/payroll/loans/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

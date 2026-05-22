using System.Net;
using System.Net.Http.Json;
using Nanchesoft.Domain.Entities;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Payroll;

[Collection("NanchesoftApi")]
public class PayrollMvpEndpointTests
{
    private readonly HttpClient _client;
    private readonly NanchesoftWebFactory _factory;

    public PayrollMvpEndpointTests(NanchesoftWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SeedDefaultConcepts_ReturnsOkOrBadRequest()
    {
        // Returns 200 on success or 400 if a company is missing — either is valid
        var response = await _client.PostAsJsonAsync("/api/payroll/seed-default-concepts", new { });
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateAttendanceSummaries_UnknownPeriod_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/payroll/periods/{Guid.NewGuid()}/generate-summaries",
            new { userId = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateIncidents_UnknownPeriod_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/payroll/periods/{Guid.NewGuid()}/generate-incidents",
            new { userId = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CalculatePayrollRun_UnknownRun_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/payroll/runs/{Guid.NewGuid()}/calculate",
            new { userId = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CalculatePayrollRun_UsesPeriodSalaryForSueldoSemanalReceipt()
    {
        var periodId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var expectedSalary = 2345.67m;
        var suffix = Guid.NewGuid().ToString("N")[..8];

        await using (var db = _factory.CreateDbContext())
        {
            var company = await db.Companies
                .OrderBy(x => x.CreatedAt)
                .Select(x => new { x.Id, x.TenantId })
                .FirstAsync();
            var branchId = await db.Branches
                .Where(x => x.CompanyId == company.Id)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync();

            db.PayrollPeriods.Add(new PayrollPeriod
            {
                Id = periodId,
                TenantId = company.TenantId,
                CompanyId = company.Id,
                Code = $"PER-{suffix}",
                Name = "Periodo sueldo semanal",
                PeriodType = "semanal",
                StartDate = new DateTime(2026, 5, 18, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
                PaymentDate = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
                Status = "draft",
                CreatedBy = "test"
            });

            db.Employees.Add(new Employee
            {
                Id = employeeId,
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branchId,
                Code = $"EMP-{suffix}",
                EmployeeNumber = $"EMP-{suffix}",
                FirstName = "Empleado",
                LastName = "Semanal",
                HireDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                PeriodSalary = expectedSalary,
                DailySalary = 100m,
                IntegratedDailySalary = 100m,
                Status = "active",
                IsActive = true,
                CreatedBy = "test"
            });

            db.PayrollRuns.Add(new PayrollRun
            {
                Id = runId,
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branchId,
                PayrollPeriodId = periodId,
                Folio = $"NOM-{suffix}",
                RunDate = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
                Status = "draft",
                CreatedBy = "test"
            });

            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsync($"/api/payroll/runs/{runId}/calculate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var verifyDb = _factory.CreateDbContext();
        var line = await verifyDb.PayrollRunLines.SingleAsync(x => x.PayrollRunId == runId && x.EmployeeId == employeeId);
        line.GrossAmount.Should().BeGreaterThanOrEqualTo(expectedSalary);

        var salaryDetail = await verifyDb.PayrollRunLineDetails.SingleAsync(x =>
            x.PayrollRunLineId == line.Id &&
            x.ConceptName == "SUELDO SEMANAL" &&
            x.ConceptType == "perception");
        salaryDetail.Amount.Should().Be(expectedSalary);

        var isrDetailExists = await verifyDb.PayrollRunLineDetails.AnyAsync(x =>
            x.PayrollRunLineId == line.Id &&
            x.ConceptCode == "ISR");
        isrDetailExists.Should().BeFalse();

        var imssDetailExists = await verifyDb.PayrollRunLineDetails.AnyAsync(x =>
            x.PayrollRunLineId == line.Id &&
            x.ConceptCode == "IMSS");
        imssDetailExists.Should().BeFalse();
    }

    [Fact]
    public async Task GetPayrollReceipt_UnknownRun_Returns404()
    {
        var response = await _client.GetAsync(
            $"/api/payroll/runs/{Guid.NewGuid()}/receipts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ImportEmployees_EmptyFile_Returns400()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Array.Empty<byte>()), "file", "empty.xlsx");
        var response = await _client.PostAsync("/api/hr/employees/import", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

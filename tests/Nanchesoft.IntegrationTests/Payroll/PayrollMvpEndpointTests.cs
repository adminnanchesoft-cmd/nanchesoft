using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Payroll;

public class PayrollMvpEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public PayrollMvpEndpointTests(NanchesoftWebFactory factory)
    {
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

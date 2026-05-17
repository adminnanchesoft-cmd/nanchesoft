using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Production;

public class ProductionPieceWorkEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public ProductionPieceWorkEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Rates ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListRates_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/production/piecework/rates");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRate_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/production/piecework/rates/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateRate_MissingCompany_Returns400()
    {
        var payload = new { companyId = Guid.Empty, phaseId = Guid.NewGuid(), unitPrice = 10.0m };
        var response = await _client.PostAsJsonAsync("/api/production/piecework/rates", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateRate_UnknownId_Returns404()
    {
        var payload = new { unitPrice = 12.5m };
        var response = await _client.PutAsJsonAsync($"/api/production/piecework/rates/{Guid.NewGuid()}", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRate_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/production/piecework/rates/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Records ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListRecords_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/production/piecework/records");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRecord_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/production/piecework/records/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateRecord_MissingCompany_Returns400()
    {
        var payload = new
        {
            companyId = Guid.Empty,
            productionOrderId = Guid.NewGuid(),
            phaseId = Guid.NewGuid(),
            employeeId = Guid.NewGuid(),
            workDate = DateTime.Today.ToString("yyyy-MM-dd"),
            unitsProduced = 50
        };
        var response = await _client.PostAsJsonAsync("/api/production/piecework/records", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ApproveRecord_UnknownId_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/production/piecework/records/{Guid.NewGuid()}/approve",
            new { userId = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ProcessToPayroll_EmptyBatch_Returns400()
    {
        var payload = new { productionOrderId = Guid.NewGuid(), recordIds = Array.Empty<Guid>(), userId = "test" };
        var response = await _client.PostAsJsonAsync("/api/production/piecework/process-to-payroll", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Summary ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task PieceWorkSummary_ReturnsOk()
    {
        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month;
        var response = await _client.GetAsync($"/api/production/piecework/summary?year={year}&month={month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

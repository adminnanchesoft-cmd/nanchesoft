using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Production;

public class ProductionDashboardEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;
    private readonly NanchesoftWebFactory _factory;

    public ProductionDashboardEndpointTests(NanchesoftWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Kpis_MissingCompanyId_Returns400()
    {
        var response = await _client.GetAsync("/api/production/dashboard/kpis?companyId=00000000-0000-0000-0000-000000000000");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Kpis_ValidCompanyId_ReturnsOk()
    {
        using var db = _factory.CreateDbContext();
        var company = await db.Companies.FirstOrDefaultAsync();
        if (company is null) return;

        var response = await _client.GetAsync($"/api/production/dashboard/kpis?companyId={company.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Kpis_ResponseHasExpectedShape()
    {
        using var db = _factory.CreateDbContext();
        var company = await db.Companies.FirstOrDefaultAsync();
        if (company is null) return;

        var response = await _client.GetAsync($"/api/production/dashboard/kpis?companyId={company.Id}");
        var json = await response.Content.ReadFromJsonAsync<KpiResponse>();

        json.Should().NotBeNull();
        json!.CompanyId.Should().Be(company.Id);
        json.Orders.Should().NotBeNull();
        json.Production.Should().NotBeNull();
        json.Alerts.Should().NotBeNull();
    }

    [Fact]
    public async Task OrdersBoard_ValidCompanyId_ReturnsOk()
    {
        using var db = _factory.CreateDbContext();
        var company = await db.Companies.FirstOrDefaultAsync();
        if (company is null) return;

        var response = await _client.GetAsync($"/api/production/dashboard/orders-board?companyId={company.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OrdersBoard_HasAllStatusColumns()
    {
        using var db = _factory.CreateDbContext();
        var company = await db.Companies.FirstOrDefaultAsync();
        if (company is null) return;

        var response = await _client.GetAsync($"/api/production/dashboard/orders-board?companyId={company.Id}");
        var body = await response.Content.ReadAsStringAsync();

        // All kanban columns must be present in the JSON
        body.Should().Contain("\"draft\"");
        body.Should().Contain("\"planned\"");
        body.Should().Contain("\"in_progress\"");
        body.Should().Contain("\"completed\"");
        body.Should().Contain("\"cancelled\"");
    }

    [Fact]
    public async Task PhaseThroughput_Today_ReturnsOk()
    {
        using var db = _factory.CreateDbContext();
        var company = await db.Companies.FirstOrDefaultAsync();
        if (company is null) return;

        var response = await _client.GetAsync($"/api/production/dashboard/phase-throughput?companyId={company.Id}&period=today");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PhaseThroughput_Week_ReturnsOk()
    {
        using var db = _factory.CreateDbContext();
        var company = await db.Companies.FirstOrDefaultAsync();
        if (company is null) return;

        var response = await _client.GetAsync($"/api/production/dashboard/phase-throughput?companyId={company.Id}&period=week");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task InProcess_Snapshot_ReturnsOk()
    {
        using var db = _factory.CreateDbContext();
        var company = await db.Companies.FirstOrDefaultAsync();
        if (company is null) return;

        var response = await _client.GetAsync($"/api/production/in-process?companyId={company.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SurplusRecords_ReturnsOk()
    {
        using var db = _factory.CreateDbContext();
        var company = await db.Companies.FirstOrDefaultAsync();
        if (company is null) return;

        var response = await _client.GetAsync($"/api/production/surplus?companyId={company.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ScheduleList_ReturnsOk()
    {
        using var db = _factory.CreateDbContext();
        var company = await db.Companies.FirstOrDefaultAsync();
        if (company is null) return;

        var response = await _client.GetAsync($"/api/production/schedules?companyId={company.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PieceWorkRates_ReturnsOk()
    {
        using var db = _factory.CreateDbContext();
        var company = await db.Companies.FirstOrDefaultAsync();
        if (company is null) return;

        var response = await _client.GetAsync($"/api/production/piecework/rates?companyId={company.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record KpiOrders(int Total, int InProgress, int Completed, int Planned);
    private sealed record KpiProduction(int ProducedThisWeek, int VouchersIssuedToday, decimal PieceWorkNetThisWeek);
    private sealed record KpiAlerts(int MaterialShortages);
    private sealed record KpiResponse(Guid CompanyId, string WeekCode, KpiOrders Orders, KpiProduction Production, KpiAlerts Alerts);
}

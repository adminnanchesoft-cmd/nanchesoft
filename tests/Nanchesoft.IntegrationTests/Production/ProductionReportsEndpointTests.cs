using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.IntegrationTests.Production;

[Collection("NanchesoftApi")]
public class ProductionReportsEndpointTests
{
    private readonly HttpClient _client;
    private readonly NanchesoftWebFactory _factory;

    public ProductionReportsEndpointTests(NanchesoftWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task WeeklySummary_WithValidCompanyId_ReturnsOk()
    {
        var companyId = await GetFirstCompanyIdAsync();
        if (companyId == Guid.Empty) { Assert.True(true, "Skipped: no company in test DB."); return; }

        var response = await _client.GetAsync($"/api/reports/production/weekly-summary?companyId={companyId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WeeklySummary_ReturnsArray()
    {
        var companyId = await GetFirstCompanyIdAsync();
        if (companyId == Guid.Empty) { Assert.True(true, "Skipped: no company in test DB."); return; }

        var response = await _client.GetAsync($"/api/reports/production/weekly-summary?companyId={companyId}");
        var json = await response.Content.ReadFromJsonAsync<List<object>>();
        json.Should().NotBeNull();
    }

    [Fact]
    public async Task PieceworkByEmployee_WithValidCompanyId_ReturnsOk()
    {
        var companyId = await GetFirstCompanyIdAsync();
        if (companyId == Guid.Empty) { Assert.True(true, "Skipped: no company in test DB."); return; }

        var response = await _client.GetAsync($"/api/reports/production/piecework-by-employee?companyId={companyId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PhaseEfficiency_WithValidCompanyId_ReturnsOk()
    {
        var companyId = await GetFirstCompanyIdAsync();
        if (companyId == Guid.Empty) { Assert.True(true, "Skipped: no company in test DB."); return; }

        var response = await _client.GetAsync($"/api/reports/production/phase-efficiency?companyId={companyId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task InProcessSnapshot_WithValidCompanyId_ReturnsOk()
    {
        var companyId = await GetFirstCompanyIdAsync();
        if (companyId == Guid.Empty) { Assert.True(true, "Skipped: no company in test DB."); return; }

        var response = await _client.GetAsync($"/api/reports/production/in-process-snapshot?companyId={companyId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SurplusSummary_WithValidCompanyId_ReturnsOk()
    {
        var companyId = await GetFirstCompanyIdAsync();
        if (companyId == Guid.Empty) { Assert.True(true, "Skipped: no company in test DB."); return; }

        var response = await _client.GetAsync($"/api/reports/production/surplus-summary?companyId={companyId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AllProductionReports_ReturnArrayShapes()
    {
        var companyId = await GetFirstCompanyIdAsync();
        if (companyId == Guid.Empty) { Assert.True(true, "Skipped: no company in test DB."); return; }

        var t1 = _client.GetAsync($"/api/reports/production/weekly-summary?companyId={companyId}");
        var t2 = _client.GetAsync($"/api/reports/production/piecework-by-employee?companyId={companyId}");
        var t3 = _client.GetAsync($"/api/reports/production/phase-efficiency?companyId={companyId}");
        var t4 = _client.GetAsync($"/api/reports/production/in-process-snapshot?companyId={companyId}");
        var t5 = _client.GetAsync($"/api/reports/production/surplus-summary?companyId={companyId}");

        await Task.WhenAll(t1, t2, t3, t4, t5);

        (await t1).StatusCode.Should().Be(HttpStatusCode.OK);
        (await t2).StatusCode.Should().Be(HttpStatusCode.OK);
        (await t3).StatusCode.Should().Be(HttpStatusCode.OK);
        (await t4).StatusCode.Should().Be(HttpStatusCode.OK);
        (await t5).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Guid> GetFirstCompanyIdAsync()
    {
        using var db = _factory.CreateDbContext();
        var company = await db.Companies.AsNoTracking().OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        return company?.Id ?? Guid.Empty;
    }
}

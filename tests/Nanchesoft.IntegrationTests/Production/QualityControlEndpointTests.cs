using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Production;

public class QualityControlEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;
    private readonly int _year = DateTime.UtcNow.Year;

    public QualityControlEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Dashboard_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/production/quality-control/dashboard?year={_year}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListRecords_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/production/quality-control?year={_year}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListRecords_WithStatusFilter_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/production/quality-control?year={_year}&status=pending");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRecord_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/production/quality-control/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateRecord_MissingCompany_Returns400()
    {
        var payload = new
        {
            companyId = Guid.Empty,
            productionOrderId = Guid.NewGuid(),
            inspectionDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"),
            inspectorName = "Inspector Test",
            totalUnitsInspected = 100
        };
        var response = await _client.PostAsJsonAsync("/api/production/quality-control", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRecord_MissingInspector_Returns400()
    {
        var payload = new
        {
            companyId = Guid.NewGuid(),
            productionOrderId = Guid.NewGuid(),
            inspectionDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"),
            inspectorName = "",
            totalUnitsInspected = 100
        };
        var response = await _client.PostAsJsonAsync("/api/production/quality-control", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ApproveRecord_UnknownId_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/production/quality-control/{Guid.NewGuid()}/approve",
            new { userId = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RejectRecord_UnknownId_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/production/quality-control/{Guid.NewGuid()}/reject",
            new { userId = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HoldRecord_UnknownId_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/production/quality-control/{Guid.NewGuid()}/hold",
            new { userId = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddDefect_UnknownRecord_Returns404()
    {
        var payload = new { defectCode = "DEF-001", severity = "low", quantityAffected = 5 };
        var response = await _client.PostAsJsonAsync(
            $"/api/production/quality-control/{Guid.NewGuid()}/defects", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResolveDefect_UnknownIds_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/production/quality-control/{Guid.NewGuid()}/defects/{Guid.NewGuid()}/resolve",
            new { notes = "Fixed", userId = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MaterialRequirement_UnknownOrder_Returns404()
    {
        var response = await _client.GetAsync(
            $"/api/production/orders/{Guid.NewGuid()}/material-requirement");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

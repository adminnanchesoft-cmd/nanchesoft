using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Production;

public class ProductionScheduleEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public ProductionScheduleEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListSchedules_ReturnsOk()
    {
        var year = DateTime.UtcNow.Year;
        var response = await _client.GetAsync($"/api/production/schedules?year={year}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSchedule_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/production/schedules/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateWeekSchedule_MissingCompany_Returns400()
    {
        var payload = new { companyId = Guid.Empty, branchId = Guid.NewGuid(), weekCode = "2026-W22" };
        var response = await _client.PostAsJsonAsync("/api/production/schedules/week", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateWeekSchedule_InvalidWeekCode_Returns400()
    {
        var payload = new { companyId = Guid.NewGuid(), branchId = Guid.NewGuid(), weekCode = "INVALID" };
        var response = await _client.PostAsJsonAsync("/api/production/schedules/week", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LockSchedule_UnknownId_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/production/schedules/{Guid.NewGuid()}/lock",
            new { userId = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CloseSchedule_UnknownId_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/production/schedules/{Guid.NewGuid()}/close",
            new { userId = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddLine_UnknownSchedule_Returns404()
    {
        var payload = new
        {
            productionOrderId = Guid.NewGuid(),
            productionOrderLineId = Guid.NewGuid(),
            productionPhaseId = Guid.NewGuid(),
            scheduledDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"),
            shift = "morning",
            unitsScheduled = 10
        };
        var response = await _client.PostAsJsonAsync(
            $"/api/production/schedules/{Guid.NewGuid()}/lines", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveLine_UnknownSchedule_Returns404()
    {
        var response = await _client.DeleteAsync(
            $"/api/production/schedules/{Guid.NewGuid()}/lines/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CapacityBoard_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/production/capacity/2026-W22");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OrdersToSchedule_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/production/orders-to-schedule");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

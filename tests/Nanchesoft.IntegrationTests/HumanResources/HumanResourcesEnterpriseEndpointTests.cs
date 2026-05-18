using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.HumanResources;

[Collection("NanchesoftApi")]
public class HumanResourcesEnterpriseEndpointTests
{
    private readonly HttpClient _client;

    public HumanResourcesEnterpriseEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Shifts ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListShifts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/shifts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateShift_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/hr/shifts", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateShift_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/hr/shifts/{Guid.NewGuid()}", new { code = "S1", name = "Shift 1" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteShift_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/hr/shifts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Work schedules ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ListWorkSchedules_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/work-schedules");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateWorkSchedule_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/hr/work-schedules", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateWorkSchedule_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/hr/work-schedules/{Guid.NewGuid()}", new { code = "WS1", name = "Schedule 1" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteWorkSchedule_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/hr/work-schedules/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Time-clock devices ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListDevices_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/time-clock-devices");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateDevice_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/hr/time-clock-devices", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateDevice_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/hr/time-clock-devices/{Guid.NewGuid()}", new { code = "D1", name = "Device 1" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteDevice_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/hr/time-clock-devices/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Leave types ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListLeaveTypes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/leave-types");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateLeaveType_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/hr/leave-types/{Guid.NewGuid()}", new { name = "Vacaciones" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteLeaveType_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/hr/leave-types/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Vacation requests ──────────────────────────────────────────────────────

    [Fact]
    public async Task ListVacationRequests_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/vacation-requests");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateVacationRequest_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/hr/vacation-requests/{Guid.NewGuid()}", new { status = "approved" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteVacationRequest_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/hr/vacation-requests/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

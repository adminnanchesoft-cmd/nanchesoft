using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Admin;

public class AdminSecurityEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public AdminSecurityEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Users ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListUsers_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/security/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateUser_MissingEmail_Returns400()
    {
        var payload = new { email = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/security/users", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUser_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/security/users/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUser_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/security/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Roles ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListRoles_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/security/roles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateRole_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/security/roles", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateRole_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/security/roles/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRole_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/security/roles/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Permissions ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListPermissions_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/security/permissions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdatePermission_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/security/permissions/{Guid.NewGuid()}", new { allowed = true });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePermission_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/security/permissions/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

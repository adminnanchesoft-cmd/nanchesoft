using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Production;

[Collection("NanchesoftApi")]
public class ConsumptionTemplateEndpointTests
{
    private readonly HttpClient _client;

    public ConsumptionTemplateEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListTemplates_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/consumption-templates");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListTemplates_WithStyleFilter_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/consumption-templates?productStyleId={Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTemplate_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/consumption-templates/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InitializeTemplate_EmptyGuids_Returns400()
    {
        var payload = new
        {
            productStyleId = Guid.Empty,
            productSizeRunId = Guid.Empty
        };
        var response = await _client.PostAsJsonAsync("/api/consumption-templates/initialize", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InitializeTemplate_UnknownSizeRun_Returns400()
    {
        // sizeRun not found → 400 ("Corrida no encontrada")
        var payload = new
        {
            productStyleId = Guid.NewGuid(),
            productSizeRunId = Guid.NewGuid()
        };
        var response = await _client.PostAsJsonAsync("/api/consumption-templates/initialize", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTemplate_UnknownId_Returns404()
    {
        var payload = new { notes = "Updated", details = Array.Empty<object>() };
        var response = await _client.PutAsJsonAsync($"/api/consumption-templates/{Guid.NewGuid()}", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AuthorizeTemplate_UnknownId_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/consumption-templates/{Guid.NewGuid()}/authorize",
            new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CopyFrom_UnknownTargetId_Returns404()
    {
        var payload = new { sourceTemplateId = Guid.NewGuid() };
        var response = await _client.PostAsJsonAsync(
            $"/api/consumption-templates/{Guid.NewGuid()}/copy-from", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

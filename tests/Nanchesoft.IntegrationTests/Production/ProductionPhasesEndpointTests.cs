using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Production;

[Collection("NanchesoftApi")]
public class ProductionPhasesEndpointTests
{
    private readonly HttpClient _client;

    public ProductionPhasesEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPhases_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/production/phases");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPhases_ReturnsJsonArray()
    {
        var response = await _client.GetAsync("/api/production/phases");
        var json = await response.Content.ReadFromJsonAsync<List<PhaseItem>>();
        json.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPhases_ResultsAreOrderedBySequence()
    {
        var response = await _client.GetAsync("/api/production/phases");
        var json = await response.Content.ReadFromJsonAsync<List<PhaseItem>>();

        if (json is null || json.Count < 2) return; // skip if DB is empty

        var sequences = json.Select(x => x.Sequence).ToList();
        sequences.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetCells_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/production/cells");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record PhaseItem(Guid Id, string Code, string Name, string Description, int Sequence);
}

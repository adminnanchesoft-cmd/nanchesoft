using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Production;

[Collection("NanchesoftApi")]
public class MaterialExplosionEndpointTests
{
    private readonly HttpClient _client;

    public MaterialExplosionEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSizes_UnknownProduct_Returns404()
    {
        var response = await _client.GetAsync($"/api/products/material-explosion/{Guid.NewGuid()}/sizes");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Calculate_EmptyProductId_Returns400()
    {
        var payload = new
        {
            finishedProductId = Guid.Empty,
            quantitiesPerSize = new Dictionary<string, int> { ["size-1"] = 10 }
        };
        var response = await _client.PostAsJsonAsync("/api/products/material-explosion/calculate", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Calculate_NoQuantities_Returns400()
    {
        var payload = new
        {
            finishedProductId = Guid.NewGuid(),
            quantitiesPerSize = new Dictionary<string, int>()
        };
        var response = await _client.PostAsJsonAsync("/api/products/material-explosion/calculate", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Calculate_UnknownProduct_Returns404()
    {
        var payload = new
        {
            finishedProductId = Guid.NewGuid(),
            quantitiesPerSize = new Dictionary<string, int> { ["size-1"] = 10 }
        };
        var response = await _client.PostAsJsonAsync("/api/products/material-explosion/calculate", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Report_NoResult_Returns400()
    {
        var payload = new { result = (object?)null };
        var response = await _client.PostAsJsonAsync("/api/products/material-explosion/report", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

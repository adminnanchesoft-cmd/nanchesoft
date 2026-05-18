using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.HumanResources;

[Collection("NanchesoftApi")]
public class HumanResourcesPerformanceEndpointTests
{
    private readonly HttpClient _client;

    public HumanResourcesPerformanceEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Performance reviews ────────────────────────────────────────────────────

    [Fact]
    public async Task ListPerformanceReviews_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/performance-reviews");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreatePerformanceReview_MissingEmployee_Returns400()
    {
        var payload = new { employeeId = (Guid?)null, period = "" };
        var response = await _client.PostAsJsonAsync("/api/hr/performance-reviews", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePerformanceReview_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/hr/performance-reviews/{Guid.NewGuid()}", new { score = 0 });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePerformanceReview_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/hr/performance-reviews/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Competency assessments ─────────────────────────────────────────────────

    [Fact]
    public async Task ListCompetencyAssessments_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/competency-assessments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateCompetencyAssessment_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/hr/competency-assessments/{Guid.NewGuid()}", new { score = 0 });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCompetencyAssessment_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/hr/competency-assessments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Succession plans ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListSuccessionPlans_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/succession-plans");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateSuccessionPlan_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/hr/succession-plans/{Guid.NewGuid()}", new { status = "active" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSuccessionPlan_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/hr/succession-plans/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

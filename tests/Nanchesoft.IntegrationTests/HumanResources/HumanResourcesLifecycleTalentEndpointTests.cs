using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.HumanResources;

public class HumanResourcesLifecycleTalentEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public HumanResourcesLifecycleTalentEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Employee documents ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListEmployeeDocuments_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/employee-documents");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateEmployeeDocument_MissingEmployee_Returns400()
    {
        var payload = new { code = "", name = "" }; // no employeeId
        var response = await _client.PostAsJsonAsync("/api/hr/employee-documents", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateEmployeeDocument_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/hr/employee-documents/{Guid.NewGuid()}", new { name = "Contrato" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEmployeeDocument_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/hr/employee-documents/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Employee movements ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListEmployeeMovements_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/employee-movements");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateEmployeeMovement_MissingEmployee_Returns400()
    {
        var payload = new { code = "", movementType = "" }; // no employeeId
        var response = await _client.PostAsJsonAsync("/api/hr/employee-movements", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateEmployeeMovement_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/hr/employee-movements/{Guid.NewGuid()}", new { movementType = "alta" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEmployeeMovement_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/hr/employee-movements/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Recruitment vacancies ──────────────────────────────────────────────────

    [Fact]
    public async Task ListVacancies_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/recruitment-vacancies");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateVacancy_MissingCode_Returns400()
    {
        var payload = new { code = "", title = "" }; // no company context
        var response = await _client.PostAsJsonAsync("/api/hr/recruitment-vacancies", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateVacancy_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/hr/recruitment-vacancies/{Guid.NewGuid()}", new { title = "Actualizado" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteVacancy_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/hr/recruitment-vacancies/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Candidate applications ─────────────────────────────────────────────────

    [Fact]
    public async Task ListCandidates_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/candidate-applications");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateCandidate_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/hr/candidate-applications/{Guid.NewGuid()}", new { status = "interviewed" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCandidate_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/hr/candidate-applications/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Onboarding checklists ──────────────────────────────────────────────────

    [Fact]
    public async Task ListOnboardingChecklists_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/hr/onboarding-checklists");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateOnboardingChecklist_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/hr/onboarding-checklists/{Guid.NewGuid()}", new { status = "completed" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteOnboardingChecklist_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/hr/onboarding-checklists/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

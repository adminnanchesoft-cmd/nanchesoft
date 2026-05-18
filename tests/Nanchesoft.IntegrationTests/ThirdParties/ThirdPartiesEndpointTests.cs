using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.ThirdParties;

public class ThirdPartiesEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public ThirdPartiesEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Customers ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListCustomers_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/third-parties/customers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateCustomer_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/third-parties/customers", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCustomer_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/third-parties/customers/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCustomer_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/third-parties/customers/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Suppliers ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListSuppliers_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/third-parties/suppliers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateSupplier_MissingCode_Returns400()
    {
        var payload = new { code = "", name = "" };
        var response = await _client.PostAsJsonAsync("/api/third-parties/suppliers", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSupplier_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/third-parties/suppliers/{Guid.NewGuid()}", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSupplier_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/third-parties/suppliers/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Contacts ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListContacts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/third-parties/contacts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateContact_MissingName_Returns400()
    {
        var payload = new { firstName = "", lastName = "" };
        var response = await _client.PostAsJsonAsync("/api/third-parties/contacts", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateContact_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/third-parties/contacts/{Guid.NewGuid()}", new { firstName = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteContact_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/third-parties/contacts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Addresses ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAddresses_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/third-parties/addresses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateAddress_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/third-parties/addresses/{Guid.NewGuid()}", new { street = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAddress_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/third-parties/addresses/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Third-party bank accounts ──────────────────────────────────────────────

    [Fact]
    public async Task ListThirdPartyBankAccounts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/third-parties/bank-accounts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateThirdPartyBankAccount_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/third-parties/bank-accounts/{Guid.NewGuid()}", new { accountNumber = "123" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteThirdPartyBankAccount_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/third-parties/bank-accounts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

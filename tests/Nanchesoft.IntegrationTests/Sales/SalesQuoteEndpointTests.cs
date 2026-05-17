using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.IntegrationTests.Sales;

public class SalesQuoteEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;
    private readonly NanchesoftWebFactory _factory;

    public SalesQuoteEndpointTests(NanchesoftWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListQuotes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/sales/quotes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListQuotes_ReturnsArray()
    {
        var response = await _client.GetAsync("/api/sales/quotes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<List<object>>();
        json.Should().NotBeNull();
    }

    [Fact]
    public async Task GetQuote_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/sales/quotes/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateQuote_WithMinimalData_ReturnsOkWithId()
    {
        var payload = new
        {
            folio = "INT-COT-001",
            quoteDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            status = "draft",
            exchangeRate = 1.0m,
            lines = Array.Empty<object>()
        };

        var response = await _client.PostAsJsonAsync("/api/sales/quotes", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);

        // Cleanup
        await _client.DeleteAsync($"/api/sales/quotes/{created.Id}");
    }

    [Fact]
    public async Task CreateAndUpdateQuote_UpdatesSuccessfully()
    {
        var createPayload = new
        {
            folio = "INT-COT-UPD",
            quoteDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            status = "draft",
            exchangeRate = 1.0m,
            notes = "Original",
            lines = Array.Empty<object>()
        };

        var createResponse = await _client.PostAsJsonAsync("/api/sales/quotes", createPayload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();

        var updatePayload = new { notes = "Updated by integration test" };
        var updateResponse = await _client.PutAsJsonAsync($"/api/sales/quotes/{created!.Id}", updatePayload);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Cleanup
        await _client.DeleteAsync($"/api/sales/quotes/{created.Id}");
    }

    [Fact]
    public async Task DeleteQuote_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/sales/quotes/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetQuoteById_AfterCreate_ReturnsQuoteData()
    {
        var payload = new
        {
            folio = "INT-COT-GET",
            quoteDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            status = "draft",
            exchangeRate = 1.0m,
            lines = Array.Empty<object>()
        };

        var createResponse = await _client.PostAsJsonAsync("/api/sales/quotes", payload);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var getResponse = await _client.GetAsync($"/api/sales/quotes/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Cleanup
        await _client.DeleteAsync($"/api/sales/quotes/{created.Id}");
    }

    private static async Task<T?> GetFirstAsync<T>(NanchesoftDbContext db, Func<NanchesoftDbContext, Microsoft.EntityFrameworkCore.DbSet<T>> selector)
        where T : class
        => await selector(db).FirstOrDefaultAsync();

    private sealed record CreatedResponse(Guid Id);
}

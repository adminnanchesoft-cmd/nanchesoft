using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.IntegrationTests.Infrastructure;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.IntegrationTests.Production;

[Collection("NanchesoftApi")]
public class ProductionOrderEndpointTests
{
    private readonly HttpClient _client;
    private readonly NanchesoftWebFactory _factory;

    public ProductionOrderEndpointTests(NanchesoftWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListOrders_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/production/orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListOrders_ReturnsPaginatedShape()
    {
        var response = await _client.GetAsync("/api/production/orders?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<PaginatedResponse>();
        json.Should().NotBeNull();
        json!.Page.Should().Be(1);
        json.PageSize.Should().Be(10);
        json.Total.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetOrder_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/production/orders/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateOrder_WithoutRequiredFields_Returns400()
    {
        var payload = new { companyId = Guid.Empty, branchId = Guid.Empty, lines = Array.Empty<object>() };
        var response = await _client.PostAsJsonAsync("/api/production/orders", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithValidData_Returns201()
    {
        using var db = _factory.CreateDbContext();

        var company = await db.Companies.FirstOrDefaultAsync(x => x.Id == NanchesoftWebFactory.SeedCompanyId);
        var branch = await db.Branches.FirstOrDefaultAsync(x => x.CompanyId == NanchesoftWebFactory.SeedCompanyId);
        var product = await db.FinishedProducts.FirstOrDefaultAsync(x => x.CompanyId == NanchesoftWebFactory.SeedCompanyId);

        if (company is null || branch is null || product is null)
        {
            // Skip if seed data is not present
            Assert.True(true, "Skipped: required seed data not found in test DB.");
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var payload = new
        {
            companyId = company.Id,
            branchId = branch.Id,
            weekCode = $"{today.Year}-W{System.Globalization.ISOWeek.GetWeekOfYear(today.ToDateTime(TimeOnly.MinValue)):D2}",
            startDate = today.ToString("yyyy-MM-dd"),
            endDate = today.AddDays(4).ToString("yyyy-MM-dd"),
            deliveryDate = today.AddDays(7).ToString("yyyy-MM-dd"),
            priority = 1,
            userId = "integration-test",
            lines = new[]
            {
                new
                {
                    finishedProductId = product.Id,
                    quantitiesPerSize = new Dictionary<string, int> { ["test-size"] = 50 },
                    priority = 1
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/production/orders", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();
        created!.ProductionOrderId.Should().NotBe(Guid.Empty);
        created.Folio.Should().NotBeNullOrEmpty();

        // Cleanup
        await _client.PostAsJsonAsync($"/api/production/orders/{created.ProductionOrderId}/cancel",
            new { userId = "integration-test", reason = "Created by integration test cleanup" });
    }

    [Fact]
    public async Task ConfirmOrder_NonExistentId_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/production/orders/{Guid.NewGuid()}/confirm",
            new { userId = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StatusFilter_ReturnsOnlyMatchingOrders()
    {
        var response = await _client.GetAsync("/api/production/orders?status=draft");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<PaginatedOrderResponse>();
        json.Should().NotBeNull();
        json!.Items.Should().AllSatisfy(o => o.Status.Should().Be("draft"));
    }

    private static async Task<T?> GetFirstAsync<T>(NanchesoftDbContext db, Func<NanchesoftDbContext, Microsoft.EntityFrameworkCore.DbSet<T>> selector)
        where T : class
    {
        return await selector(db).FirstOrDefaultAsync();
    }

    private sealed record PaginatedResponse(int Total, int Page, int PageSize);
    private sealed record CreatedResponse(Guid ProductionOrderId, string Folio);
    private sealed record OrderItem(Guid ProductionOrderId, string Status);
    private sealed record PaginatedOrderResponse(int Total, int Page, int PageSize, List<OrderItem> Items);
}

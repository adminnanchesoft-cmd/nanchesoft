using System.Net;
using System.Net.Http.Json;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Production;

public class ProductionVoucherEndpointTests : IClassFixture<NanchesoftWebFactory>
{
    private readonly HttpClient _client;

    public ProductionVoucherEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListVouchers_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/production/vouchers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListVouchers_WithFilters_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/production/vouchers?status=open&page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetVoucher_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/production/vouchers/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task IssueVoucher_MissingCompany_Returns400()
    {
        var payload = new
        {
            companyId = Guid.Empty,
            productionOrderId = Guid.NewGuid(),
            productionOrderLineId = Guid.NewGuid(),
            phaseId = Guid.NewGuid()
        };
        var response = await _client.PostAsJsonAsync("/api/production/vouchers", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompleteVoucher_UnknownId_Returns404()
    {
        var payload = new { unitsProduced = 10, userId = "test" };
        var response = await _client.PostAsJsonAsync(
            $"/api/production/vouchers/{Guid.NewGuid()}/complete", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelVoucher_UnknownId_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/production/vouchers/{Guid.NewGuid()}/cancel",
            new { userId = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

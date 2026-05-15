using System.Net.Http.Json;

namespace Nanchesoft.Web.Services.Products;

public sealed class ProductTechnicalCenterApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProductTechnicalCenterApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<ProductTechnicalCenterOverviewItemDto>> GetOverviewAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<ProductTechnicalCenterOverviewItemDto>>("/api/products/technical-center/overview") ?? new();
    }

    public async Task<ProductTechnicalCenterOverviewItemDto?> GetByProductAsync(Guid finishedProductId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<ProductTechnicalCenterOverviewItemDto>($"/api/products/technical-center/overview/{finishedProductId}");
    }

    public async Task<ProductTechnicalCenterOverviewItemDto?> GenerateTechnicalSheetAsync(Guid finishedProductId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PostAsync($"/api/products/technical-center/generate-sheet/{finishedProductId}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductTechnicalCenterOverviewItemDto>();
    }

    public async Task<ProductTechnicalCenterOverviewItemDto?> GenerateCostSheetAsync(Guid finishedProductId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PostAsync($"/api/products/technical-center/generate-cost/{finishedProductId}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductTechnicalCenterOverviewItemDto>();
    }

    public async Task<ProductTechnicalCenterOverviewItemDto?> SyncAuthorizationAsync(Guid finishedProductId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PostAsync($"/api/products/technical-center/sync-authorization/{finishedProductId}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductTechnicalCenterOverviewItemDto>();
    }
}

public sealed record ProductTechnicalCenterOverviewItemDto(
    Guid FinishedProductId,
    string ProductCode,
    string ProductName,
    string StyleName,
    string SizeRunName,
    string MainMaterialName,
    bool HasPhoto,
    bool HasConsumptionDefinition,
    bool HasMaterialAssignments,
    string TechnicalSheetCode,
    string TechnicalSheetStatus,
    bool TechnicalSheetApproved,
    int TechnicalSheetMaterialCount,
    int TechnicalSheetProcessCount,
    string CostSheetCode,
    string CostSheetStatus,
    bool CostSheetApproved,
    decimal TotalCost,
    decimal SuggestedPrice,
    string AuthorizationCode,
    string AuthorizationStatus,
    bool IsAuthorizedForExplosion,
    int ReadinessPercent,
    string MissingRequirements);

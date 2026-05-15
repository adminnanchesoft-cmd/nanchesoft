using System.Net.Http.Json;

namespace Nanchesoft.Web.Services.Products;

public sealed class ProductGovernanceApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProductGovernanceApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ProductTechnicalCenterOverviewDto> GetOverviewAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<ProductTechnicalCenterOverviewDto>("/api/products/technical-center/overview")
            ?? new ProductTechnicalCenterOverviewDto(0, 0, 0, 0, 0, 0, 0m);
    }

    public async Task<List<ProductTechnicalCenterSummaryDto>> GetSummaryAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<ProductTechnicalCenterSummaryDto>>("/api/products/technical-center/summary")
            ?? new List<ProductTechnicalCenterSummaryDto>();
    }
}

public sealed record ProductTechnicalCenterOverviewDto(
    int TotalProducts,
    int AuthorizedProducts,
    int ReadyForLaunchProducts,
    int MissingTechnicalSheetProducts,
    int MissingCostSheetProducts,
    int MissingPhotoProducts,
    decimal AverageReadinessPercent);

public sealed record ProductTechnicalCenterSummaryDto(
    Guid FinishedProductId,
    string ProductCode,
    string ProductName,
    string BillingName,
    bool HasPhoto,
    bool HasConsumptionDefinition,
    bool HasMaterialAssignments,
    bool HasTechnicalSheet,
    bool HasCostSheet,
    bool TechnicalSheetApproved,
    bool CostSheetApproved,
    bool IsAuthorizedForExplosion,
    decimal ReadinessPercent,
    int AssignedMaterialCount,
    int ConsumptionLineCount,
    int TechnicalSheetMaterialCount,
    int TechnicalSheetProcessCount,
    string TechnicalSheetCode,
    string CostSheetCode,
    string AuthorizationCode,
    string AuthorizationStatus,
    decimal TotalCost,
    decimal SuggestedSalePrice,
    decimal MarginPercent,
    List<string> MissingRequirements,
    string PrimaryBlocker,
    DateTime LastUpdatedAt);

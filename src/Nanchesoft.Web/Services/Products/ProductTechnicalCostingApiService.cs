
using System.Net.Http.Json;

namespace Nanchesoft.Web.Services.Products;

public sealed class ProductTechnicalCostingApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProductTechnicalCostingApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient CreateClient() => _httpClientFactory.CreateClient("Nanchesoft.Api");

    public async Task<List<ProductTechnicalCenterRowDto>> GetTechnicalCenterOverviewAsync()
        => await CreateClient().GetFromJsonAsync<List<ProductTechnicalCenterRowDto>>("/api/products/technical-center/overview") ?? [];

    public async Task<ProductTechnicalActionResponse?> GenerateTechnicalSheetAsync(Guid finishedProductId)
        => await PostActionAsync($"/api/products/technical-sheets/generate-from-product/{finishedProductId}");

    public async Task<ProductTechnicalActionResponse?> GenerateCostSheetAsync(Guid finishedProductId)
        => await PostActionAsync($"/api/products/cost-sheets/generate-from-product/{finishedProductId}");

    public async Task<ProductTechnicalActionResponse?> SyncAuthorizationAsync(Guid finishedProductId)
        => await PostActionAsync($"/api/products/authorizations/sync/{finishedProductId}");

    public async Task<ProductTechnicalActionResponse?> AutoAuthorizeAsync(Guid finishedProductId)
        => await PostActionAsync($"/api/products/authorizations/auto-authorize/{finishedProductId}");

    public async Task<List<ProductTechnicalSheetDto>> GetTechnicalSheetsAsync()
        => await CreateClient().GetFromJsonAsync<List<ProductTechnicalSheetDto>>("/api/products/technical-sheets") ?? [];

    public async Task<ProductTechnicalSheetDto?> SaveTechnicalSheetAsync(ProductTechnicalSheetRequest request, Guid? id = null)
    {
        var client = CreateClient();
        HttpResponseMessage response = id.HasValue
            ? await client.PutAsJsonAsync($"/api/products/technical-sheets/{id}", request)
            : await client.PostAsJsonAsync("/api/products/technical-sheets", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductTechnicalSheetDto>();
    }

    public async Task DeleteTechnicalSheetAsync(Guid id)
    {
        var response = await CreateClient().DeleteAsync($"/api/products/technical-sheets/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<ProductCostSheetDto>> GetCostSheetsAsync()
        => await CreateClient().GetFromJsonAsync<List<ProductCostSheetDto>>("/api/products/cost-sheets") ?? [];

    public async Task<ProductCostSheetDto?> SaveCostSheetAsync(ProductCostSheetRequest request, Guid? id = null)
    {
        var client = CreateClient();
        HttpResponseMessage response = id.HasValue
            ? await client.PutAsJsonAsync($"/api/products/cost-sheets/{id}", request)
            : await client.PostAsJsonAsync("/api/products/cost-sheets", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductCostSheetDto>();
    }

    public async Task DeleteCostSheetAsync(Guid id)
    {
        var response = await CreateClient().DeleteAsync($"/api/products/cost-sheets/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<ProductAuthorizationRecordDto>> GetAuthorizationsAsync()
        => await CreateClient().GetFromJsonAsync<List<ProductAuthorizationRecordDto>>("/api/products/authorizations") ?? [];

    public async Task<ProductAuthorizationRecordDto?> SaveAuthorizationAsync(ProductAuthorizationRecordRequest request, Guid? id = null)
    {
        var client = CreateClient();
        HttpResponseMessage response = id.HasValue
            ? await client.PutAsJsonAsync($"/api/products/authorizations/{id}", request)
            : await client.PostAsJsonAsync("/api/products/authorizations", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductAuthorizationRecordDto>();
    }

    public async Task DeleteAuthorizationAsync(Guid id)
    {
        var response = await CreateClient().DeleteAsync($"/api/products/authorizations/{id}");
        response.EnsureSuccessStatusCode();
    }

    private async Task<ProductTechnicalActionResponse?> PostActionAsync(string endpoint)
    {
        var response = await CreateClient().PostAsync(endpoint, content: null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductTechnicalActionResponse>();
    }
}

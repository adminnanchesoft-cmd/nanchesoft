using System.Net.Http.Json;

namespace Nanchesoft.Web.Services.Monitoring;

public sealed class MonitoringApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MonitoringApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<MonitoringErrorRowDto>> GetErrorsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<MonitoringErrorRowDto>>("/api/monitoring/errors") ?? new();
    }

    public async Task<MonitoringSecurityReviewDto> GetSecurityReviewAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<MonitoringSecurityReviewDto>("/api/monitoring/security-review") ?? new MonitoringSecurityReviewDto();
    }

    public async Task<MonitoringHealthDto> GetHealthAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<MonitoringHealthDto>("/api/monitoring/health") ?? new MonitoringHealthDto();
    }

    public async Task<List<MonitoringSchemaCatalogRowDto>> GetSchemaCatalogAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<MonitoringSchemaCatalogRowDto>>("/api/monitoring/schema-catalog") ?? new();
    }

    public async Task<List<MonitoringSchemaSummaryDto>> GetSchemaSummaryAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<MonitoringSchemaSummaryDto>>("/api/monitoring/schema-summary") ?? new();
    }

    public async Task<List<MonitoringSchemaGapDto>> GetSchemaGapsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<MonitoringSchemaGapDto>>("/api/monitoring/schema-gaps") ?? new();
    }
}

public sealed class MonitoringErrorRowDto
{
    public Guid ErrorLogId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RequestPath { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? UserId { get; set; }
    public string? StackTrace { get; set; }
}

public sealed class MonitoringSecurityReviewDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int LockedUsers { get; set; }
    public int ActiveSessions { get; set; }
    public int TotalRoles { get; set; }
    public int TotalPermissions { get; set; }
    public int RecentAccessFailures { get; set; }
}

public sealed class MonitoringHealthDto
{
    public int Companies { get; set; }
    public int Branches { get; set; }
    public int Warehouses { get; set; }
    public int Customers { get; set; }
    public int Suppliers { get; set; }
    public int Items { get; set; }
    public int PurchaseInvoices { get; set; }
    public int SalesInvoices { get; set; }
    public int InventoryMovements { get; set; }
    public int CashAccounts { get; set; }
    public int BankAccounts { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
    public DateTime? LastSalesDate { get; set; }
    public DateTime? LastInventoryMovementDate { get; set; }
    public DateTime? LastTreasuryMovementDate { get; set; }
    public DateTime? LastErrorDate { get; set; }
}

public sealed class MonitoringSchemaCatalogRowDto
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ClrType { get; set; } = string.Empty;
    public string PrimaryKey { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public bool ExistsInDatabase { get; set; }
    public long EstimatedRows { get; set; }
    public string? DatabaseComment { get; set; }
    public bool IsOwnedEntity { get; set; }
}

public sealed class MonitoringSchemaSummaryDto
{
    public string SchemaName { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public int ModelTableCount { get; set; }
    public int PhysicalTableCount { get; set; }
    public int MissingTableCount { get; set; }
    public long EstimatedRows { get; set; }
}

public sealed class MonitoringSchemaGapDto
{
    public string GapType { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

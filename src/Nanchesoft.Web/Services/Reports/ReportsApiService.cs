using System.Net.Http.Json;

namespace Nanchesoft.Web.Services.Reports;

public sealed class ReportsApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ReportsApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ReportsOperationalSummaryDto> GetOperationalSummaryAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<ReportsOperationalSummaryDto>("/api/reports/operational/summary") ?? new ReportsOperationalSummaryDto();
    }

    public async Task<List<ReportsPurchaseRowDto>> GetPurchasesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<ReportsPurchaseRowDto>>("/api/reports/operational/purchases") ?? new();
    }

    public async Task<List<ReportsSalesRowDto>> GetSalesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<ReportsSalesRowDto>>("/api/reports/operational/sales") ?? new();
    }

    public async Task<List<ReportsInventoryRowDto>> GetInventoryAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<ReportsInventoryRowDto>>("/api/reports/operational/inventory") ?? new();
    }

    public async Task<List<ReportsTreasuryRowDto>> GetTreasuryAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<ReportsTreasuryRowDto>>("/api/reports/operational/treasury") ?? new();
    }

    public async Task<ExecutiveSummaryDto> GetExecutiveSummaryAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<ExecutiveSummaryDto>("/api/reports/executive/summary") ?? new ExecutiveSummaryDto();
    }

    public string GetExportUrl(string reportKey)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return new Uri(client.BaseAddress!, $"/api/reports/export/{reportKey}").ToString();
    }
}

public class ReportsPurchaseRowDto
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string Reference { get; set; } = string.Empty;
}

public sealed class ReportsSalesRowDto : ReportsPurchaseRowDto
{
}

public sealed class ReportsOperationalSummaryDto
{
    public int PurchaseOrdersOpen { get; set; }
    public decimal PurchaseInvoicesPeriod { get; set; }
    public int SalesOrdersOpen { get; set; }
    public decimal SalesInvoicesPeriod { get; set; }
    public int StockRows { get; set; }
    public decimal StockOnHand { get; set; }
    public decimal StockValue { get; set; }
    public decimal CashBalance { get; set; }
    public decimal BankBalance { get; set; }
    public decimal CombinedTreasuryBalance { get; set; }
}

public sealed class ReportsInventoryRowDto
{
    public Guid Id { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityAvailable { get; set; }
    public decimal AverageCost { get; set; }
    public decimal LastCost { get; set; }
    public decimal ExtendedValue { get; set; }
}

public sealed class ReportsTreasuryRowDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Extra { get; set; } = string.Empty;
}

public sealed class ExecutiveSummaryDto
{
    public int ActiveCustomers { get; set; }
    public int ActiveSuppliers { get; set; }
    public int ActiveItems { get; set; }
    public decimal PurchaseTotal30Days { get; set; }
    public decimal SalesTotal30Days { get; set; }
    public decimal GrossMargin30Days { get; set; }
    public decimal InventoryValue { get; set; }
    public decimal TreasuryAvailable { get; set; }
    public int PendingReconciliations { get; set; }
    public List<ExecutiveTopRowDto> RecentSales { get; set; } = new();
    public List<ExecutiveTopRowDto> RecentPurchases { get; set; } = new();
}

public sealed class ExecutiveTopRowDto
{
    public string Label { get; set; } = string.Empty;
    public string Secondary { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

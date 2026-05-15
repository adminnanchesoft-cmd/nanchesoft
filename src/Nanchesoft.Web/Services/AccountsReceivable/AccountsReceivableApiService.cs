using System.Net.Http.Json;

namespace Nanchesoft.Web.Services.AccountsReceivable;

public sealed class AccountsReceivableApiService
{
    private readonly HttpClient _httpClient;

    public AccountsReceivableApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Nanchesoft.Api");
    }

    public async Task<List<ArBalanceRowDto>> GetBalancesAsync()
        => await _httpClient.GetFromJsonAsync<List<ArBalanceRowDto>>("/api/accounts-receivable/balances") ?? [];

    public async Task<List<ArStatementRowDto>> GetStatementsAsync(Guid? customerId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = new List<string>();
        if (customerId.HasValue)
            query.Add($"customerId={customerId.Value}");
        if (fromDate.HasValue)
            query.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue)
            query.Add($"toDate={toDate.Value:yyyy-MM-dd}");

        var url = "/api/accounts-receivable/statements";
        if (query.Count > 0)
            url += "?" + string.Join("&", query);

        return await _httpClient.GetFromJsonAsync<List<ArStatementRowDto>>(url) ?? [];
    }

    public async Task<List<ArAgingRowDto>> GetAgingAsync(Guid? customerId = null)
    {
        var url = customerId.HasValue
            ? $"/api/accounts-receivable/aging?customerId={customerId.Value}"
            : "/api/accounts-receivable/aging";
        return await _httpClient.GetFromJsonAsync<List<ArAgingRowDto>>(url) ?? [];
    }

    public async Task<List<ArReceiptApplicationRowDto>> GetApplicationsAsync(Guid? customerId = null)
    {
        var url = customerId.HasValue
            ? $"/api/accounts-receivable/applications?customerId={customerId.Value}"
            : "/api/accounts-receivable/applications";
        return await _httpClient.GetFromJsonAsync<List<ArReceiptApplicationRowDto>>(url) ?? [];
    }

    public async Task<ArDashboardSummaryDto> GetDashboardSummaryAsync()
        => await _httpClient.GetFromJsonAsync<ArDashboardSummaryDto>("/api/accounts-receivable/dashboard/summary") ?? new ArDashboardSummaryDto();

    public async Task<List<ArDashboardRecentRowDto>> GetDashboardRecentAsync()
        => await _httpClient.GetFromJsonAsync<List<ArDashboardRecentRowDto>>("/api/accounts-receivable/dashboard/recent") ?? [];

    public async Task<ArLookupsDto> GetLookupsAsync(Guid? customerId = null)
    {
        var url = customerId.HasValue
            ? $"/api/accounts-receivable/lookups?customerId={customerId.Value}"
            : "/api/accounts-receivable/lookups";
        return await _httpClient.GetFromJsonAsync<ArLookupsDto>(url) ?? new ArLookupsDto();
    }

    public async Task ApplyReceiptAsync(ArApplyReceiptRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/accounts-receivable/apply-receipt", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task CancelApplicationAsync(Guid receiptApplicationId)
    {
        var response = await _httpClient.PostAsync($"/api/accounts-receivable/applications/{receiptApplicationId}/cancel", null);
        response.EnsureSuccessStatusCode();
    }
}

public sealed class ArLookupsDto
{
    public List<ArLookupItem> Customers { get; set; } = new();
    public List<ArLookupItem> Receipts { get; set; } = new();
    public List<ArLookupItem> SalesInvoices { get; set; } = new();
}

public sealed class ArLookupItem
{
    public Guid Id { get; set; }
    public Guid? CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal OpenAmount { get; set; }
    public DateTime? MovementDate { get; set; }
}

public sealed class ArBalanceRowDto
{
    public Guid CustomerId { get; set; }
    public Guid CompanyId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal TotalCharges { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime? LastMovementAt { get; set; }
}

public sealed class ArStatementRowDto
{
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime MovementDate { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal ChargeAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal BalanceAfter { get; set; }
}

public sealed class ArAgingRowDto
{
    public Guid SalesInvoiceId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid CompanyId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal OpenAmount { get; set; }
    public decimal BucketCurrent { get; set; }
    public decimal Bucket31To60 { get; set; }
    public decimal Bucket61To90 { get; set; }
    public decimal BucketOver90 { get; set; }
}

public sealed class ArReceiptApplicationRowDto
{
    public Guid ReceiptApplicationId { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid ReceiptId { get; set; }
    public string ReceiptFolio { get; set; } = string.Empty;
    public Guid SalesInvoiceId { get; set; }
    public string SalesInvoiceFolio { get; set; } = string.Empty;
    public DateTime ApplicationDate { get; set; }
    public decimal AppliedAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
}

public sealed class ArApplyReceiptRequest
{
    public Guid? CustomerId { get; set; }
    public Guid? ReceiptId { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    public DateTime? ApplicationDate { get; set; }
    public decimal AppliedAmount { get; set; }
    public string? Reference { get; set; }
}

public sealed class ArDashboardSummaryDto
{
    public int ActiveCustomersWithBalance { get; set; }
    public decimal TotalOpenBalance { get; set; }
    public decimal OverdueBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public int AppliedReceiptsCount { get; set; }
}

public sealed class ArDashboardRecentRowDto
{
    public DateTime MovementDate { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal ChargeAmount { get; set; }
    public decimal CreditAmount { get; set; }
}

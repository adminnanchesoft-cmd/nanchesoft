using System.Net.Http.Json;

namespace Nanchesoft.Web.Services.AccountsPayable;

public sealed class AccountsPayableApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AccountsPayableApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient CreateClient() => _httpClientFactory.CreateClient("Nanchesoft.Api");

    public async Task<List<ApBalanceRowDto>> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        return await client.GetFromJsonAsync<List<ApBalanceRowDto>>("/api/accounts-payable/balances", cancellationToken) ?? [];
    }

    public async Task<List<ApStatementRowDto>> GetStatementsAsync(Guid? supplierId = null, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var url = supplierId.HasValue
            ? $"/api/accounts-payable/statements?supplierId={supplierId.Value}"
            : "/api/accounts-payable/statements";

        return await client.GetFromJsonAsync<List<ApStatementRowDto>>(url, cancellationToken) ?? [];
    }

    public async Task<List<ApAgingRowDto>> GetAgingAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        return await client.GetFromJsonAsync<List<ApAgingRowDto>>("/api/accounts-payable/aging", cancellationToken) ?? [];
    }

    public async Task<List<ApPaymentApplicationRowDto>> GetApplicationsAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        return await client.GetFromJsonAsync<List<ApPaymentApplicationRowDto>>("/api/accounts-payable/applications", cancellationToken) ?? [];
    }

    public async Task<ApDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        return await client.GetFromJsonAsync<ApDashboardSummaryDto>("/api/accounts-payable/dashboard/summary", cancellationToken)
               ?? new ApDashboardSummaryDto();
    }

    public async Task<List<ApDashboardRecentRowDto>> GetDashboardRecentAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        return await client.GetFromJsonAsync<List<ApDashboardRecentRowDto>>("/api/accounts-payable/dashboard/recent", cancellationToken) ?? [];
    }

    public async Task<ApLookupsDto> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        return await client.GetFromJsonAsync<ApLookupsDto>("/api/accounts-payable/lookups", cancellationToken) ?? new ApLookupsDto();
    }

    public async Task ApplyPaymentAsync(ApApplyPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/accounts-payable/apply-payment", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // Compatibilidad con la implementación financiera que simplificó este servicio.
    public async Task<AccountsPayableBalancesResponse> GetBalancesResponseAsync(CancellationToken cancellationToken = default)
        => new()
        {
            Items = (await GetBalancesAsync(cancellationToken))
                .Select(x => new AccountsPayableBalanceRow
                {
                    SupplierId = x.SupplierId.ToString(),
                    SupplierName = x.SupplierName,
                    Balance = x.CurrentBalance,
                    OverdueBalance = 0m
                })
                .ToList()
        };

    public async Task<AccountsPayableDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var summary = await GetDashboardSummaryAsync(cancellationToken);
        return new AccountsPayableDashboardResponse
        {
            TotalBalance = summary.TotalOpenBalance,
            OverdueBalance = summary.OverdueBalance,
            SuppliersWithBalance = summary.ActiveSuppliersWithBalance,
            PendingApplications = summary.AppliedPaymentsCount
        };
    }

    public async Task<HttpResponseMessage> ApplyPaymentAsync(ApplyPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        return await client.PostAsJsonAsync("/api/accounts-payable/apply-payment", new ApApplyPaymentRequest
        {
            SupplierId = request.SupplierId,
            PaymentId = request.PaymentId,
            PurchaseInvoiceId = request.PurchaseInvoiceId,
            ApplicationDate = request.ApplicationDate,
            AppliedAmount = request.AppliedAmount,
            Notes = request.Notes
        }, cancellationToken);
    }
}

public sealed class ApLookupsDto
{
    public List<ApLookupItem> Suppliers { get; set; } = new();
    public List<ApLookupItem> Payments { get; set; } = new();
    public List<ApLookupItem> PurchaseInvoices { get; set; } = new();
}

public sealed class ApLookupItem
{
    public Guid Id { get; set; }
    public Guid? SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
}

public sealed class ApBalanceRowDto
{
    public Guid SupplierId { get; set; }
    public Guid CompanyId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal TotalCharges { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime? LastMovementAt { get; set; }
}

public sealed class ApStatementRowDto
{
    public Guid? SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateTime MovementDate { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal ChargeAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal BalanceAfter { get; set; }
}

public sealed class ApAgingRowDto
{
    public Guid PurchaseInvoiceId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid CompanyId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal OpenAmount { get; set; }
    public decimal BucketCurrent { get; set; }
    public decimal Bucket31To60 { get; set; }
    public decimal Bucket61To90 { get; set; }
    public decimal BucketOver90 { get; set; }
}

public sealed class ApPaymentApplicationRowDto
{
    public Guid PaymentLineId { get; set; }
    public Guid? SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public Guid PaymentId { get; set; }
    public string PaymentFolio { get; set; } = string.Empty;
    public Guid PurchaseInvoiceId { get; set; }
    public string PurchaseInvoiceFolio { get; set; } = string.Empty;
    public DateTime ApplicationDate { get; set; }
    public decimal AppliedAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class ApApplyPaymentRequest
{
    public Guid? SupplierId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? PurchaseInvoiceId { get; set; }
    public DateTime? ApplicationDate { get; set; }
    public decimal AppliedAmount { get; set; }
    public string? Notes { get; set; }
}

public sealed class ApDashboardSummaryDto
{
    public int ActiveSuppliersWithBalance { get; set; }
    public decimal TotalOpenBalance { get; set; }
    public decimal OverdueBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public int AppliedPaymentsCount { get; set; }
}

public sealed class ApDashboardRecentRowDto
{
    public DateTime MovementDate { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal ChargeAmount { get; set; }
    public decimal CreditAmount { get; set; }
}

// DTOs de compatibilidad para el módulo financiero.
public sealed class AccountsPayableBalancesResponse
{
    public List<AccountsPayableBalanceRow> Items { get; set; } = new();
}

public sealed class AccountsPayableBalanceRow
{
    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal OverdueBalance { get; set; }
}

public sealed class AccountsPayableDashboardResponse
{
    public decimal TotalBalance { get; set; }
    public decimal OverdueBalance { get; set; }
    public int SuppliersWithBalance { get; set; }
    public int PendingApplications { get; set; }
}

public sealed class ApplyPaymentRequest
{
    public Guid SupplierId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid PurchaseInvoiceId { get; set; }
    public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;
    public decimal AppliedAmount { get; set; }
    public string? Notes { get; set; }
}

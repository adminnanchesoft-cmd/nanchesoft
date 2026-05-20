using System.Net.Http.Json;

namespace Nanchesoft.Web.Services.Finance;

public sealed class PaymentCentralApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    public PaymentCentralApiService(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;
    private HttpClient CreateClient() => _httpClientFactory.CreateClient("Nanchesoft.Api");

    public async Task<List<PendingPaymentRow>> GetPendingAsync(Guid? companyId = null, Guid? supplierId = null, Guid? currencyId = null, int? overdueDays = null, string? priority = null)
    {
        var url = "/api/payment-central/pending";
        var query = new List<string>();
        if (companyId.HasValue) query.Add($"companyId={companyId.Value}");
        if (supplierId.HasValue) query.Add($"supplierId={supplierId.Value}");
        if (currencyId.HasValue) query.Add($"currencyId={currencyId.Value}");
        if (overdueDays.HasValue) query.Add($"overdueDays={overdueDays.Value}");
        if (!string.IsNullOrWhiteSpace(priority)) query.Add($"priority={Uri.EscapeDataString(priority)}");
        if (query.Count > 0) url += "?" + string.Join("&", query);
        return await CreateClient().GetFromJsonAsync<List<PendingPaymentRow>>(url) ?? new();
    }

    public async Task<PaymentCentralLookups?> GetLookupsAsync()
        => await CreateClient().GetFromJsonAsync<PaymentCentralLookups>("/api/payment-central/lookups");

    public async Task<List<PaymentBatchListRow>> GetBatchesAsync(string? status = null, DateTime? from = null, DateTime? to = null)
    {
        var url = "/api/payment-central/batches";
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={Uri.EscapeDataString(status)}");
        if (from.HasValue) query.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue) query.Add($"to={to.Value:yyyy-MM-dd}");
        if (query.Count > 0) url += "?" + string.Join("&", query);
        return await CreateClient().GetFromJsonAsync<List<PaymentBatchListRow>>(url) ?? new();
    }

    public async Task<PaymentBatchDetail?> GetBatchAsync(Guid id)
        => await CreateClient().GetFromJsonAsync<PaymentBatchDetail>($"/api/payment-central/batches/{id}");

    public Task<HttpResponseMessage> CreateBatchAsync(PaymentBatchSaveRequest request)
        => CreateClient().PostAsJsonAsync("/api/payment-central/batches", request);

    public Task<HttpResponseMessage> AuthorizeAsync(Guid id, PaymentBatchAuthorizeRequest request)
        => CreateClient().PostAsJsonAsync($"/api/payment-central/batches/{id}/authorize", request);

    public Task<HttpResponseMessage> RejectAsync(Guid id, PaymentBatchRejectRequest request)
        => CreateClient().PostAsJsonAsync($"/api/payment-central/batches/{id}/reject", request);

    public Task<HttpResponseMessage> CancelAsync(Guid id)
        => CreateClient().PostAsync($"/api/payment-central/batches/{id}/cancel", null);

    public Task<HttpResponseMessage> ExecuteAsync(Guid id, PaymentBatchExecuteRequest request)
        => CreateClient().PostAsJsonAsync($"/api/payment-central/batches/{id}/execute", request);

    public async Task<List<PaymentBatchAuditRow>> GetAuditAsync(Guid id)
        => await CreateClient().GetFromJsonAsync<List<PaymentBatchAuditRow>>($"/api/payment-central/batches/{id}/audit") ?? new();

    public async Task<PaymentCentralExecutive?> GetExecutiveAsync()
        => await CreateClient().GetFromJsonAsync<PaymentCentralExecutive>("/api/payment-central/executive");
}

public sealed class PendingPaymentRow
{
    public Guid PurchaseInvoiceId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public string SupplierInvoiceFolio { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public int PaymentTermDays { get; set; }
    public Guid? CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal AvailableToPay { get; set; }
    public string Priority { get; set; } = "normal";

    // UI helpers (no se envían al backend)
    public bool Selected { get; set; }
    public decimal AmountToPay { get; set; }
    public string PaymentType { get; set; } = "transfer";
    public Guid? BankAccountId { get; set; }
    public Guid? CashAccountId { get; set; }
    public Guid? CheckBookId { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class PaymentCentralLookups
{
    public List<IdName> Companies { get; set; } = new();
    public List<BankAccountLookup> BankAccounts { get; set; } = new();
    public List<BankAccountLookup> CashAccounts { get; set; } = new();
    public List<IdName> Suppliers { get; set; } = new();
    public List<IdName> Currencies { get; set; } = new();
    public List<PaymentTypeOption> PaymentTypes { get; set; } = new();
}

public sealed class IdName
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class BankAccountLookup
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? BankId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public Guid? CurrencyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
}

public sealed class PaymentTypeOption
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FolioPrefix { get; set; } = string.Empty;
}

public sealed class PaymentBatchListRow
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime BatchDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public int CompanyCount { get; set; }
    public int SupplierCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AuthorizedAmount { get; set; }
    public decimal ExecutedAmount { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public string AuthorizedByName { get; set; } = string.Empty;
    public DateTime? AuthorizedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
}

public sealed class PaymentBatchDetail
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime BatchDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public int CompanyCount { get; set; }
    public int SupplierCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AuthorizedAmount { get; set; }
    public decimal ExecutedAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string RequestedByName { get; set; } = string.Empty;
    public string AuthorizedByName { get; set; } = string.Empty;
    public DateTime? AuthorizedAt { get; set; }
    public string RejectedReason { get; set; } = string.Empty;
    public DateTime? RejectedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public List<PaymentBatchLineDetail> Lines { get; set; } = new();
}

public sealed class PaymentBatchLineDetail
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public Guid? PurchaseInvoiceId { get; set; }
    public string InvoiceFolio { get; set; } = string.Empty;
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public Guid? CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountToPay { get; set; }
    public string Priority { get; set; } = "normal";
    public string PaymentType { get; set; } = "transfer";
    public Guid? BankAccountId { get; set; }
    public Guid? CashAccountId { get; set; }
    public Guid? CheckBookId { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string LineStatus { get; set; } = "pending";
    public Guid? PaymentId { get; set; }
    public Guid? CheckId { get; set; }
    public Guid? BankMovementId { get; set; }
    public string ExecutedFolio { get; set; } = string.Empty;
    public DateTime? ExecutedAt { get; set; }
    public string RejectedReason { get; set; } = string.Empty;
}

public sealed class PaymentBatchSaveRequest
{
    public DateTime? BatchDate { get; set; } = DateTime.Today;
    public DateTime? ScheduledDate { get; set; }
    public string Priority { get; set; } = "normal";
    public string? Notes { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public string? RequestedByName { get; set; }
    public List<PaymentBatchLineSave> Lines { get; set; } = new();
}

public sealed class PaymentBatchLineSave
{
    public Guid PurchaseInvoiceId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime? DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountToPay { get; set; }
    public string Priority { get; set; } = "normal";
    public string PaymentType { get; set; } = "transfer";
    public Guid? BankAccountId { get; set; }
    public Guid? CashAccountId { get; set; }
    public Guid? CheckBookId { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public sealed class PaymentBatchAuthorizeRequest
{
    public Guid? AuthorizedByUserId { get; set; }
    public string? AuthorizedByName { get; set; }
    public string? Notes { get; set; }
    public List<PaymentBatchLineOverride>? LineOverrides { get; set; }
}

public sealed class PaymentBatchLineOverride
{
    public Guid LineId { get; set; }
    public decimal? AmountToPay { get; set; }
    public Guid? BankAccountId { get; set; }
    public Guid? CashAccountId { get; set; }
    public string? PaymentType { get; set; }
    public string? Priority { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public bool Reject { get; set; }
    public string? RejectedReason { get; set; }
}

public sealed class PaymentBatchRejectRequest
{
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Reason { get; set; }
}

public sealed class PaymentBatchExecuteRequest
{
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
}

public sealed class PaymentBatchAuditRow
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string PreviousValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class BankAccountBalance
{
    public Guid BankAccountId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal ReconciledBalance { get; set; }
}

public sealed class UpcomingDue
{
    public string InvoiceFolio { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int DaysToDue { get; set; }
    public decimal PendingAmount { get; set; }
}

public sealed class PaymentCentralExecutive
{
    public int PendingBatches { get; set; }
    public decimal PendingAmount { get; set; }
    public int AuthorizedBatches { get; set; }
    public decimal AuthorizedAmount { get; set; }
    public int ExecutedBatches30d { get; set; }
    public decimal ExecutedAmount30d { get; set; }
    public decimal CommittedFlow { get; set; }
    public decimal AvailableFlow { get; set; }
    public decimal RiskRatio { get; set; }
    public List<BankAccountBalance> LowBalanceAccounts { get; set; } = new();
    public List<UpcomingDue> UpcomingDue { get; set; } = new();
}

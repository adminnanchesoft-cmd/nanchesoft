using System.Net.Http.Json;

namespace Nanchesoft.Web.Services.Finance;

public sealed class FinancePhase2ApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public FinancePhase2ApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient CreateClient() => _httpClientFactory.CreateClient("Nanchesoft.Api");

    // ---- Movement Types ----
    public async Task<List<FinanceMovementTypeRow>> GetMovementTypesAsync()
        => await CreateClient().GetFromJsonAsync<List<FinanceMovementTypeRow>>("/api/finance/movement-types") ?? new();

    public Task<HttpResponseMessage> SaveMovementTypeAsync(FinanceMovementTypeRow row)
        => row.Id == Guid.Empty
            ? CreateClient().PostAsJsonAsync("/api/finance/movement-types", row)
            : CreateClient().PutAsJsonAsync($"/api/finance/movement-types/{row.Id}", row);

    public Task<HttpResponseMessage> DeleteMovementTypeAsync(Guid id)
        => CreateClient().DeleteAsync($"/api/finance/movement-types/{id}");

    // ---- Concepts ----
    public async Task<List<FinanceConceptRow>> GetConceptsAsync()
        => await CreateClient().GetFromJsonAsync<List<FinanceConceptRow>>("/api/finance/concepts") ?? new();

    public Task<HttpResponseMessage> SaveConceptAsync(FinanceConceptRow row)
        => row.Id == Guid.Empty
            ? CreateClient().PostAsJsonAsync("/api/finance/concepts", row)
            : CreateClient().PutAsJsonAsync($"/api/finance/concepts/{row.Id}", row);

    public Task<HttpResponseMessage> DeleteConceptAsync(Guid id)
        => CreateClient().DeleteAsync($"/api/finance/concepts/{id}");

    // ---- Check Books ----
    public async Task<List<CheckBookRow>> GetCheckBooksAsync(Guid? bankAccountId = null)
    {
        var url = "/api/finance/check-books";
        if (bankAccountId.HasValue) url += $"?bankAccountId={bankAccountId.Value}";
        return await CreateClient().GetFromJsonAsync<List<CheckBookRow>>(url) ?? new();
    }

    public Task<HttpResponseMessage> SaveCheckBookAsync(CheckBookRow row)
        => row.Id == Guid.Empty
            ? CreateClient().PostAsJsonAsync("/api/finance/check-books", row)
            : CreateClient().PutAsJsonAsync($"/api/finance/check-books/{row.Id}", row);

    public Task<HttpResponseMessage> DeleteCheckBookAsync(Guid id)
        => CreateClient().DeleteAsync($"/api/finance/check-books/{id}");

    // ---- Checks ----
    public async Task<List<CheckListRow>> GetChecksAsync(Guid? bankAccountId = null, string? status = null, DateTime? from = null, DateTime? to = null)
    {
        var url = "/api/finance/checks";
        var query = new List<string>();
        if (bankAccountId.HasValue) query.Add($"bankAccountId={bankAccountId.Value}");
        if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={Uri.EscapeDataString(status)}");
        if (from.HasValue) query.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue) query.Add($"to={to.Value:yyyy-MM-dd}");
        if (query.Count > 0) url += "?" + string.Join("&", query);
        return await CreateClient().GetFromJsonAsync<List<CheckListRow>>(url) ?? new();
    }

    public async Task<CheckSummary?> GetCheckSummaryAsync(Guid? bankAccountId = null)
    {
        var url = "/api/finance/checks/summary";
        if (bankAccountId.HasValue) url += $"?bankAccountId={bankAccountId.Value}";
        return await CreateClient().GetFromJsonAsync<CheckSummary>(url);
    }

    public Task<HttpResponseMessage> SaveCheckAsync(CheckSaveRequest request)
        => request.Id.HasValue
            ? CreateClient().PutAsJsonAsync($"/api/finance/checks/{request.Id.Value}", request)
            : CreateClient().PostAsJsonAsync("/api/finance/checks", request);

    public Task<HttpResponseMessage> IssueCheckAsync(Guid id)
        => CreateClient().PostAsync($"/api/finance/checks/{id}/issue", null);

    public Task<HttpResponseMessage> CashCheckAsync(Guid id)
        => CreateClient().PostAsync($"/api/finance/checks/{id}/cash", null);

    public Task<HttpResponseMessage> CancelCheckAsync(Guid id, string? reason = null)
        => CreateClient().PostAsJsonAsync($"/api/finance/checks/{id}/cancel", new { reason });

    public Task<HttpResponseMessage> PrintCheckAsync(Guid id)
        => CreateClient().PostAsync($"/api/finance/checks/{id}/print", null);

    // ---- Indicators / Projection ----
    public async Task<FinancialIndicators?> GetIndicatorsAsync()
        => await CreateClient().GetFromJsonAsync<FinancialIndicators>("/api/finance/indicators");

    public async Task<FinancialProjection?> GetProjectionAsync(int weeks = 8)
        => await CreateClient().GetFromJsonAsync<FinancialProjection>($"/api/finance/projection?weeks={weeks}");
}

public sealed class FinanceMovementTypeRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Direction { get; set; } = "neutral";
    public string Nature { get; set; } = string.Empty;
    public bool AffectsBalance { get; set; } = true;
    public bool IsSystem { get; set; }
    public Guid? AccountingAccountId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class FinanceConceptRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "other";
    public string Direction { get; set; } = "neutral";
    public Guid? AccountingAccountId { get; set; }
    public bool IsSystem { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class CheckBookRow
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BankAccountId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Series { get; set; } = string.Empty;
    public int FolioStart { get; set; }
    public int FolioEnd { get; set; }
    public int NextFolio { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class CheckListRow
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BankAccountId { get; set; }
    public Guid? CheckBookId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime? PostingDate { get; set; }
    public DateTime? CashedDate { get; set; }
    public string BeneficiaryType { get; set; } = string.Empty;
    public string BeneficiaryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Concept { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsPrinted { get; set; }
    public Guid? BankMovementId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CheckSaveRequest
{
    public Guid? Id { get; set; }
    public Guid BankAccountId { get; set; }
    public Guid? CheckBookId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? Folio { get; set; }
    public DateTime? IssueDate { get; set; } = DateTime.Today;
    public string BeneficiaryType { get; set; } = "other";
    public string BeneficiaryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Concept { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class CheckSummary
{
    public int PendingCount { get; set; }
    public decimal PendingAmount { get; set; }
    public int IssuedCount { get; set; }
    public decimal IssuedAmount { get; set; }
    public int CashedCount { get; set; }
    public decimal CashedAmount { get; set; }
    public int CancelledCount { get; set; }
}

public sealed class FinancialIndicators
{
    public decimal CashBalance { get; set; }
    public decimal BankBalance { get; set; }
    public decimal ReceivableBalance { get; set; }
    public decimal PayableBalance { get; set; }
    public decimal InventoryValue { get; set; }
    public decimal CurrentAssets { get; set; }
    public decimal CurrentLiabilities { get; set; }
    public decimal WorkingCapital { get; set; }
    public decimal CurrentRatio { get; set; }
    public decimal QuickRatio { get; set; }
    public decimal CashRatio { get; set; }
    public decimal DebtIndex { get; set; }
    public decimal OperationalInflow30d { get; set; }
    public decimal OperationalOutflow30d { get; set; }
    public decimal NetOperationalCashFlow30d { get; set; }
}

public sealed class FinancialProjection
{
    public decimal OpeningBalance { get; set; }
    public int HorizonWeeks { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal PendingReceivable { get; set; }
    public decimal PendingPayable { get; set; }
    public decimal PendingChecksAmount { get; set; }
    public List<FinancialProjectionWeek> Weeks { get; set; } = new();
}

public sealed class FinancialProjectionWeek
{
    public int WeekIndex { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal ExpectedInflow { get; set; }
    public decimal ExpectedOutflow { get; set; }
    public decimal NetFlow { get; set; }
    public decimal CumulativeBalance { get; set; }
}

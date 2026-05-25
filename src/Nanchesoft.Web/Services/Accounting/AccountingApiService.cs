using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Nanchesoft.Web.Services.Accounting;

public sealed class AccountingApiService
{
    private readonly HttpClient _httpClient;

    public AccountingApiService(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("Nanchesoft.Api");
    }

    public async Task<AccountingDashboardDto> GetDashboardAsync()
        => await _httpClient.GetFromJsonAsync<AccountingDashboardDto>("/api/accounting/dashboard") ?? new();

    public async Task<AccountingLookupsDto> GetLookupsAsync()
        => await _httpClient.GetFromJsonAsync<AccountingLookupsDto>("/api/accounting/lookups") ?? new();

    public async Task<List<AccountingAccountRowDto>> GetChartOfAccountsAsync()
        => await _httpClient.GetFromJsonAsync<List<AccountingAccountRowDto>>("/api/accounting/chart-of-accounts") ?? [];

    public async Task<Guid> CreateAccountAsync(SaveAccountingAccountRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/accounting/chart-of-accounts", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Guid?>()) ?? Guid.Empty;
    }

    public async Task UpdateAccountAsync(Guid accountId, SaveAccountingAccountRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/accounting/chart-of-accounts/{accountId}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task ActivateAccountAsync(Guid accountId)
    {
        var response = await _httpClient.PostAsync($"/api/accounting/chart-of-accounts/{accountId}/activate", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeactivateAccountAsync(Guid accountId)
    {
        var response = await _httpClient.PostAsync($"/api/accounting/chart-of-accounts/{accountId}/deactivate", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<FiscalPeriodRowDto>> GetFiscalPeriodsAsync(int year)
        => await _httpClient.GetFromJsonAsync<List<FiscalPeriodRowDto>>($"/api/accounting/fiscal-periods?year={year}") ?? [];

    public async Task CloseFiscalPeriodAsync(Guid periodId)
    {
        var response = await _httpClient.PostAsync($"/api/accounting/fiscal-periods/{periodId}/close", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task ReopenFiscalPeriodAsync(Guid periodId)
    {
        var response = await _httpClient.PostAsync($"/api/accounting/fiscal-periods/{periodId}/reopen", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<AccountingJournalEntryRowDto>> GetJournalEntriesAsync()
        => await _httpClient.GetFromJsonAsync<List<AccountingJournalEntryRowDto>>("/api/accounting/journal-entries") ?? [];

    public async Task<AccountingJournalEntryDetailDto?> GetJournalEntryAsync(Guid entryId)
        => await _httpClient.GetFromJsonAsync<AccountingJournalEntryDetailDto>($"/api/accounting/journal-entries/{entryId}");

    public async Task<Guid> CreateJournalEntryAsync(SaveJournalEntryRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/accounting/journal-entries", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Guid?>()) ?? Guid.Empty;
    }

    public async Task UpdateJournalEntryAsync(Guid entryId, SaveJournalEntryRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/accounting/journal-entries/{entryId}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task ApproveJournalEntryAsync(Guid entryId)
    {
        var response = await _httpClient.PostAsync($"/api/accounting/journal-entries/{entryId}/approve", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task PostJournalEntryAsync(Guid entryId)
    {
        var response = await _httpClient.PostAsync($"/api/accounting/journal-entries/{entryId}/post", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task CancelJournalEntryAsync(Guid entryId)
    {
        var response = await _httpClient.PostAsync($"/api/accounting/journal-entries/{entryId}/cancel", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<TrialBalanceRowDto>> GetTrialBalanceAsync(int year, int month)
        => await _httpClient.GetFromJsonAsync<List<TrialBalanceRowDto>>($"/api/accounting/trial-balance?year={year}&month={month}") ?? [];

    public async Task<List<LedgerRowDto>> GetLedgerAsync(Guid accountId, int year, int month)
        => await _httpClient.GetFromJsonAsync<List<LedgerRowDto>>($"/api/accounting/ledger?accountId={accountId}&year={year}&month={month}") ?? [];

    public async Task<BalanceSheetDto> GetBalanceSheetAsync(int year, int month)
        => await _httpClient.GetFromJsonAsync<BalanceSheetDto>($"/api/accounting/balance-sheet?year={year}&month={month}") ?? new();

    public async Task<IncomeStatementDto> GetIncomeStatementAsync(int year, int month)
        => await _httpClient.GetFromJsonAsync<IncomeStatementDto>($"/api/accounting/income-statement?year={year}&month={month}") ?? new();

    public async Task<AccountingAutoPolicySummaryDto> GetAutoPolicySummaryAsync()
        => await _httpClient.GetFromJsonAsync<AccountingAutoPolicySummaryDto>("/api/accounting/auto-policies/summary") ?? new();

    public async Task<List<AccountingAutoPolicySourceDto>> GetAutoPolicySourcesAsync(string sourceType = "all", bool includeGenerated = false)
        => await _httpClient.GetFromJsonAsync<List<AccountingAutoPolicySourceDto>>($"/api/accounting/auto-policies/sources?sourceType={Uri.EscapeDataString(sourceType)}&includeGenerated={includeGenerated.ToString().ToLowerInvariant()}") ?? [];

    public async Task<AccountingAutoPolicyPreviewDto?> GetAutoPolicyPreviewAsync(string sourceType, Guid sourceId)
        => await _httpClient.GetFromJsonAsync<AccountingAutoPolicyPreviewDto>($"/api/accounting/auto-policies/preview?sourceType={Uri.EscapeDataString(sourceType)}&sourceId={sourceId}");

    public async Task<AccountingAutoPolicyGenerateResultDto> GenerateAutoPolicyAsync(AccountingAutoPolicyGenerateRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/accounting/auto-policies/generate", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AccountingAutoPolicyGenerateResultDto>() ?? new(false, 0, 0, 1, "No se recibió respuesta del servidor.", new());
    }

    public async Task<AccountingAutoPolicyGenerateResultDto> GeneratePendingAutoPoliciesAsync(string sourceType = "all")
    {
        var response = await _httpClient.PostAsync($"/api/accounting/auto-policies/generate-pending?sourceType={Uri.EscapeDataString(sourceType)}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AccountingAutoPolicyGenerateResultDto>() ?? new(false, 0, 0, 1, "No se recibió respuesta del servidor.", new());
    }


    public async Task<AccountingMonthlyClosePreviewDto> GetMonthlyClosePreviewAsync(int year, int month)
        => await _httpClient.GetFromJsonAsync<AccountingMonthlyClosePreviewDto>($"/api/accounting/monthly-close/preview?year={year}&month={month}") ?? new();

    public async Task<AccountingMonthlyCloseGenerateResultDto> GenerateMonthlyCloseAsync(AccountingMonthlyCloseGenerateRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/accounting/monthly-close/generate", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AccountingMonthlyCloseGenerateResultDto>() ?? new(false, Guid.Empty, null, "No se recibió respuesta del servidor.");
    }

    // ── Importación de catálogo de cuentas ────────────────────────────────────

    public async Task<List<CatalogGroupCompanyDto>> GetGroupCompaniesAsync()
        => await _httpClient.GetFromJsonAsync<List<CatalogGroupCompanyDto>>("/api/accounting/group-companies") ?? [];

    public async Task<(bool Ok, CatalogImportPreviewDto? Preview, string? Error)> ParseCatalogExcelAsync(
        Stream fileStream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", fileName);

        var response = await _httpClient.PostAsync("/api/accounting/catalog-import/parse", content);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return (false, null, err);
        }
        var preview = await response.Content.ReadFromJsonAsync<CatalogImportPreviewDto>();
        return (true, preview, null);
    }

    public async Task<(bool Ok, CatalogImportResultDto? Result, string? Error)> ConfirmCatalogImportAsync(
        CatalogImportConfirmRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/accounting/catalog-import/confirm", request);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return (false, null, err);
        }
        var result = await response.Content.ReadFromJsonAsync<CatalogImportResultDto>();
        return (true, result, null);
    }

    public async Task<List<CatalogImportHistoryRowDto>> GetCatalogImportHistoryAsync()
        => await _httpClient.GetFromJsonAsync<List<CatalogImportHistoryRowDto>>("/api/accounting/catalog-import/imports") ?? [];
}

public sealed class AccountingLookupsDto
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public int CurrentYear { get; set; }
    public int CurrentMonth { get; set; }
    public List<AccountingLookupItemDto> Accounts { get; set; } = new();
    public List<AccountingLookupItemDto> Periods { get; set; } = new();
}

public sealed class AccountingLookupItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Extra { get; set; }
}

public sealed class AccountingDashboardDto
{
    public int ActiveAccounts { get; set; }
    public int JournalEntries { get; set; }
    public int DraftEntries { get; set; }
    public int ApprovedEntries { get; set; }
    public int PostedEntries { get; set; }
    public int OpenPeriods { get; set; }
    public decimal AssetsTotal { get; set; }
    public decimal LiabilitiesAndEquityTotal { get; set; }
    public decimal ResultOfPeriod { get; set; }
}

public sealed class AccountingAccountRowDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string Nature { get; set; } = string.Empty;
    public Guid? ParentAccountId { get; set; }
    public string? ParentAccountCode { get; set; }
    public string? ParentAccountName { get; set; }
    public bool AllowsPosting { get; set; }
    public bool IsActive { get; set; }
}

public sealed class SaveAccountingAccountRequest
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccountType { get; set; } = "Asset";
    public string Nature { get; set; } = "Debit";
    public Guid? ParentAccountId { get; set; }
    public bool AllowsPosting { get; set; } = true;
}

public sealed class FiscalPeriodRowDto
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class AccountingJournalEntryRowDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Concept { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced { get; set; }
}

public sealed class AccountingJournalEntryDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Concept { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public List<AccountingJournalEntryLineDto> Lines { get; set; } = new();
}

public sealed class AccountingJournalEntryLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public Guid? CostCenterId { get; set; }
}

public sealed class SaveJournalEntryRequest
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; } = DateTime.Today;
    public string EntryType { get; set; } = "manual";
    public string Reference { get; set; } = string.Empty;
    public string Concept { get; set; } = string.Empty;
    public List<SaveJournalEntryLineRequest> Lines { get; set; } = new();
}

public sealed class SaveJournalEntryLineRequest
{
    public Guid AccountId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public Guid? CostCenterId { get; set; }
}

public sealed class TrialBalanceRowDto
{
    public Guid AccountId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}

public sealed class LedgerRowDto
{
    public Guid JournalEntryId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Concept { get; set; } = string.Empty;
    public string LineDescription { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}

public sealed class BalanceSheetDto
{
    public List<FinancialStatementRowDto> Assets { get; set; } = new();
    public List<FinancialStatementRowDto> Liabilities { get; set; } = new();
    public List<FinancialStatementRowDto> Equity { get; set; } = new();
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
}

public sealed class IncomeStatementDto
{
    public List<FinancialStatementRowDto> Income { get; set; } = new();
    public List<FinancialStatementRowDto> Expenses { get; set; } = new();
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetIncome { get; set; }
}

public sealed class FinancialStatementRowDto
{
    public Guid AccountId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}


public sealed class AccountingAutoPolicySummaryDto
{
    public int PendingSalesInvoices { get; set; }
    public int PendingCreditNotes { get; set; }
    public int PendingPurchaseInvoices { get; set; }
    public int PendingReceipts { get; set; }
    public int PendingPayments { get; set; }
    public int DraftAutomaticEntries { get; set; }
    public int ApprovedAutomaticEntries { get; set; }
    public int PostedAutomaticEntries { get; set; }
    public int PendingTotal { get; set; }
}

public sealed class AccountingAutoPolicySourceDto
{
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public DateTime DocumentDate { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string ThirdPartyName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string DebitAccountCode { get; set; } = string.Empty;
    public string DebitAccountName { get; set; } = string.Empty;
    public string CreditAccountCode { get; set; } = string.Empty;
    public string CreditAccountName { get; set; } = string.Empty;
    public string Concept { get; set; } = string.Empty;
    public Guid? ExistingJournalEntryId { get; set; }
    public string? ExistingJournalFolio { get; set; }
    public string? ExistingJournalStatus { get; set; }
    public bool IsReady { get; set; }
    public string? Message { get; set; }
}

public sealed class AccountingAutoPolicyPreviewDto
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public DateTime EntryDate { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string ThirdPartyName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Concept { get; set; } = string.Empty;
    public string ReferenceKey { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public Guid? ExistingJournalEntryId { get; set; }
    public string? ExistingJournalFolio { get; set; }
    public string? ExistingJournalStatus { get; set; }
    public string? Message { get; set; }
    public List<AccountingAutoPolicyPreviewLineDto> Lines { get; set; } = new();
}

public sealed class AccountingAutoPolicyPreviewLineDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public sealed class AccountingAutoPolicyGenerateRequest
{
    public string SourceType { get; set; } = "all";
    public Guid SourceId { get; set; }
}

public sealed record AccountingAutoPolicyGenerateResultDto(
    bool Success,
    int CreatedCount,
    int SkippedCount,
    int FailedCount,
    string Message,
    List<string> Details);


public sealed class AccountingMonthlyClosePreviewDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime ClosingDate { get; set; }
    public Guid? FiscalPeriodId { get; set; }
    public string PeriodStatus { get; set; } = "missing";
    public Guid? ClosingAccountId { get; set; }
    public string? ClosingAccountCode { get; set; }
    public string? ClosingAccountName { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetResult { get; set; }
    public Guid? ExistingJournalEntryId { get; set; }
    public string? ExistingJournalFolio { get; set; }
    public string? ExistingJournalStatus { get; set; }
    public bool IsReady { get; set; }
    public List<string> Messages { get; set; } = new();
    public List<AccountingMonthlyCloseLineDto> SourceLines { get; set; } = new();
    public List<AccountingMonthlyCloseEntryLineDto> CloseLines { get; set; } = new();
}

public sealed class AccountingMonthlyCloseLineDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal IncomeAmount { get; set; }
    public decimal ExpenseAmount { get; set; }
}

public sealed class AccountingMonthlyCloseEntryLineDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public sealed record AccountingMonthlyCloseGenerateRequest(int Year, int Month);
public sealed record AccountingMonthlyCloseGenerateResultDto(bool Success, Guid JournalEntryId, string? JournalFolio, string Message);

// ── DTOs: Importación de catálogo de cuentas ─────────────────────────────────

public sealed class CatalogGroupCompanyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Members { get; set; } = new();
}

public sealed class CatalogImportPreviewDto
{
    public string FileName { get; set; } = string.Empty;
    public List<string> DetectedCompanies { get; set; } = new();
    public List<CatalogImportAccountRowDto> Accounts { get; set; } = new();
    public int TotalAccounts { get; set; }
    public int ValidAccounts { get; set; }
    public int ExistingAccounts { get; set; }
    public int DuplicateAccounts { get; set; }
    public int ErrorAccounts { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public sealed class CatalogImportAccountRowDto
{
    public int ExcelRow { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public string? ParentCode { get; set; }
    public string Status { get; set; } = "valid";
    public string? Message { get; set; }
    public Dictionary<string, bool?> CompanyApplies { get; set; } = new();
}

public sealed class CatalogImportConfirmRequest
{
    public string FileName { get; set; } = string.Empty;
    public string GroupCompanyName { get; set; } = string.Empty;
    public List<CatalogImportAccountRowDto> Accounts { get; set; } = new();
}

public sealed class CatalogImportResultDto
{
    public bool Success { get; set; }
    public Guid ImportId { get; set; }
    public Guid GroupCompanyId { get; set; }
    public string GroupCompanyName { get; set; } = string.Empty;
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Errors { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class CatalogImportHistoryRowDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int ErrorRows { get; set; }
    public DateTime CreatedAt { get; set; }
}

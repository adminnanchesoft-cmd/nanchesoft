using System.Net.Http.Json;

namespace Nanchesoft.Web.Services.Finance;

public sealed class FinancePhase1ApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public FinancePhase1ApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient CreateClient() => _httpClientFactory.CreateClient("Nanchesoft.Api");

    // ---- Bank movements ----
    public async Task<List<BankMovementListRow>> GetMovementsAsync(Guid? bankAccountId = null, DateTime? from = null, DateTime? to = null)
    {
        var url = "/api/finance/bank-movements";
        var query = new List<string>();
        if (bankAccountId.HasValue) query.Add($"bankAccountId={bankAccountId.Value}");
        if (from.HasValue) query.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue) query.Add($"to={to.Value:yyyy-MM-dd}");
        if (query.Count > 0) url += "?" + string.Join("&", query);
        return await CreateClient().GetFromJsonAsync<List<BankMovementListRow>>(url) ?? new();
    }

    public async Task<BankMovementRequest?> GetMovementAsync(Guid id)
        => await CreateClient().GetFromJsonAsync<BankMovementRequest>($"/api/finance/bank-movements/{id}");

    public async Task<HttpResponseMessage> SaveMovementAsync(BankMovementRequest request)
    {
        if (request.BankMovementId.HasValue)
            return await CreateClient().PutAsJsonAsync($"/api/finance/bank-movements/{request.BankMovementId.Value}", request);
        return await CreateClient().PostAsJsonAsync("/api/finance/bank-movements", request);
    }

    public Task<HttpResponseMessage> DeleteMovementAsync(Guid id)
        => CreateClient().DeleteAsync($"/api/finance/bank-movements/{id}");

    public async Task<BankAccountStatement?> GetAccountStatementAsync(Guid bankAccountId, DateTime? from = null, DateTime? to = null)
    {
        var url = $"/api/finance/bank-movements/account/{bankAccountId}/statement";
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue) query.Add($"to={to.Value:yyyy-MM-dd}");
        if (query.Count > 0) url += "?" + string.Join("&", query);
        return await CreateClient().GetFromJsonAsync<BankAccountStatement>(url);
    }

    // ---- Internal transfers ----
    public async Task<List<InternalTransferRow>> GetTransfersAsync(DateTime? from = null, DateTime? to = null)
    {
        var url = "/api/finance/internal-transfers";
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue) query.Add($"to={to.Value:yyyy-MM-dd}");
        if (query.Count > 0) url += "?" + string.Join("&", query);
        return await CreateClient().GetFromJsonAsync<List<InternalTransferRow>>(url) ?? new();
    }

    public Task<HttpResponseMessage> CreateTransferAsync(InternalTransferRequest request)
        => CreateClient().PostAsJsonAsync("/api/finance/internal-transfers", request);

    // ---- Bank statements ----
    public async Task<List<BankStatementListRow>> GetStatementsAsync(Guid? bankAccountId = null)
    {
        var url = "/api/finance/bank-statements";
        if (bankAccountId.HasValue) url += $"?bankAccountId={bankAccountId.Value}";
        return await CreateClient().GetFromJsonAsync<List<BankStatementListRow>>(url) ?? new();
    }

    public async Task<BankStatementDetail?> GetStatementAsync(Guid id)
        => await CreateClient().GetFromJsonAsync<BankStatementDetail>($"/api/finance/bank-statements/{id}");

    public Task<HttpResponseMessage> CreateStatementAsync(BankStatementSaveRequest request)
        => CreateClient().PostAsJsonAsync("/api/finance/bank-statements", request);

    public Task<HttpResponseMessage> ImportCsvAsync(BankStatementCsvImportRequest request)
        => CreateClient().PostAsJsonAsync("/api/finance/bank-statements/import-csv", request);

    public Task<HttpResponseMessage> DeleteStatementAsync(Guid id)
        => CreateClient().DeleteAsync($"/api/finance/bank-statements/{id}");

    // ---- Reconciliation suggestions ----
    public async Task<List<ReconciliationSuggestion>> GetSuggestionsAsync(Guid bankAccountId, Guid? statementId = null, int toleranceDays = 3)
    {
        var url = $"/api/finance/reconciliation/suggestions?bankAccountId={bankAccountId}&toleranceDays={toleranceDays}";
        if (statementId.HasValue) url += $"&bankStatementId={statementId.Value}";
        return await CreateClient().GetFromJsonAsync<List<ReconciliationSuggestion>>(url) ?? new();
    }

    public Task<HttpResponseMessage> ApplyMatchesAsync(ApplyMatchesRequest request)
        => CreateClient().PostAsJsonAsync("/api/finance/reconciliation/apply-matches", request);
}

// ===== DTOs =====
public sealed class BankMovementListRow
{
    public Guid BankMovementId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BankAccountId { get; set; }
    public DateTime MovementDate { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public decimal AmountIn { get; set; }
    public decimal AmountOut { get; set; }
    public decimal BalanceAfter { get; set; }
    public bool IsReconciled { get; set; }
    public bool IsActive { get; set; }
}

public sealed class BankMovementRequest
{
    public Guid? BankMovementId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BankAccountId { get; set; }
    public DateTime? MovementDate { get; set; } = DateTime.Today;
    public string MovementType { get; set; } = "deposit";
    public string DocumentType { get; set; } = "manual";
    public string Reference { get; set; } = string.Empty;
    public decimal AmountIn { get; set; }
    public decimal AmountOut { get; set; }
    public bool IsReconciled { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class BankAccountStatement
{
    public Guid BankAccountId { get; set; }
    public string BankAccountCode { get; set; } = string.Empty;
    public string BankAccountName { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal ReconciledBalance { get; set; }
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public List<BankMovementListRow> Movements { get; set; } = new();
}

public sealed class InternalTransferRow
{
    public Guid InternalTransferId { get; set; }
    public Guid CompanyId { get; set; }
    public DateTime TransferDate { get; set; }
    public string SourceAccountType { get; set; } = string.Empty;
    public Guid SourceAccountId { get; set; }
    public string DestinationAccountType { get; set; } = string.Empty;
    public Guid DestinationAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class InternalTransferRequest
{
    public DateTime? TransferDate { get; set; } = DateTime.Today;
    public string SourceAccountType { get; set; } = "bank";
    public Guid SourceAccountId { get; set; }
    public string DestinationAccountType { get; set; } = "bank";
    public Guid DestinationAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class BankStatementListRow
{
    public Guid BankStatementId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BankAccountId { get; set; }
    public DateTime StatementDate { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public int EntryCount { get; set; }
    public bool IsActive { get; set; }
}

public sealed class BankStatementDetail
{
    public Guid BankStatementId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BankAccountId { get; set; }
    public DateTime StatementDate { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<BankStatementEntryRow> Entries { get; set; } = new();
}

public sealed class BankStatementEntryRow
{
    public Guid Id { get; set; }
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal AmountIn { get; set; }
    public decimal AmountOut { get; set; }
    public decimal? BalanceAfter { get; set; }
    public Guid? MatchedMovementId { get; set; }
    public bool IsMatched { get; set; }
}

public sealed class BankStatementSaveRequest
{
    public Guid BankAccountId { get; set; }
    public DateTime? StatementDate { get; set; } = DateTime.Today;
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public string Source { get; set; } = "manual";
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<BankStatementEntryRow> Entries { get; set; } = new();
}

public sealed class BankStatementCsvImportRequest
{
    public Guid BankAccountId { get; set; }
    public DateTime? StatementDate { get; set; } = DateTime.Today;
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Delimiter { get; set; } = ",";
    public bool HasHeader { get; set; } = true;
    public string CsvText { get; set; } = string.Empty;
}

public sealed class ReconciliationSuggestion
{
    public Guid StatementEntryId { get; set; }
    public Guid BankMovementId { get; set; }
    public DateTime EntryDate { get; set; }
    public DateTime MovementDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int DaysDifference { get; set; }
    public decimal Confidence { get; set; }
    public bool Selected { get; set; } = true;
}

public sealed class ApplyMatchesRequest
{
    public List<ReconciliationMatchPair> Matches { get; set; } = new();
}

public sealed class ReconciliationMatchPair
{
    public Guid StatementEntryId { get; set; }
    public Guid BankMovementId { get; set; }
}

using System.Net.Http.Json;

namespace Nanchesoft.Web.Services.Finance;

public sealed class FinanceApiService
{
    private readonly HttpClient _httpClient;

    public FinanceApiService(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("Nanchesoft.Api");
    }

    public async Task<FinanceDashboardDto> GetDashboardAsync()
        => await _httpClient.GetFromJsonAsync<FinanceDashboardDto>("/api/finance/dashboard") ?? new FinanceDashboardDto();

    public async Task<List<FinanceCashFlowRowDto>> GetCashFlowAsync(int weeks = 12)
        => await _httpClient.GetFromJsonAsync<List<FinanceCashFlowRowDto>>($"/api/finance/cash-flow?weeks={weeks}") ?? new List<FinanceCashFlowRowDto>();

    public async Task<List<FinanceDocumentControlRowDto>> GetDocumentControlAsync()
        => await _httpClient.GetFromJsonAsync<List<FinanceDocumentControlRowDto>>("/api/finance/document-control") ?? new List<FinanceDocumentControlRowDto>();

    public async Task<List<FinanceExceptionRowDto>> GetExceptionsAsync()
        => await _httpClient.GetFromJsonAsync<List<FinanceExceptionRowDto>>("/api/finance/exceptions") ?? new List<FinanceExceptionRowDto>();

    public async Task<List<FinanceBudgetLookupItemDto>> GetBudgetLookupsAsync()
        => await _httpClient.GetFromJsonAsync<List<FinanceBudgetLookupItemDto>>("/api/finance/budgets/lookups") ?? new List<FinanceBudgetLookupItemDto>();

    public async Task<List<FinanceBudgetRowDto>> GetBudgetsAsync(int year)
        => await _httpClient.GetFromJsonAsync<List<FinanceBudgetRowDto>>($"/api/finance/budgets?year={year}") ?? new List<FinanceBudgetRowDto>();

    public async Task SaveBudgetsAsync(int year, List<FinanceBudgetRowDto> rows)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/finance/budgets/save", new FinanceBudgetSaveRequestDto
        {
            Year = year,
            Rows = rows
        });

        response.EnsureSuccessStatusCode();
    }

    public async Task<List<FinanceBudgetVsActualRowDto>> GetBudgetVsActualAsync(int year)
        => await _httpClient.GetFromJsonAsync<List<FinanceBudgetVsActualRowDto>>($"/api/finance/budgets/vs-actual?year={year}") ?? new List<FinanceBudgetVsActualRowDto>();

    public async Task<List<FinanceGoalMetricItemDto>> GetGoalMetricsAsync()
        => await _httpClient.GetFromJsonAsync<List<FinanceGoalMetricItemDto>>("/api/finance/goals/metrics") ?? new List<FinanceGoalMetricItemDto>();

    public async Task<List<FinanceGoalRowDto>> GetGoalsAsync(int year)
        => await _httpClient.GetFromJsonAsync<List<FinanceGoalRowDto>>($"/api/finance/goals?year={year}") ?? new List<FinanceGoalRowDto>();

    public async Task SaveGoalsAsync(int year, List<FinanceGoalRowDto> rows)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/finance/goals/save", new FinanceGoalSaveRequestDto
        {
            Year = year,
            Rows = rows
        });

        response.EnsureSuccessStatusCode();
    }

    public async Task<List<FinanceGoalProgressRowDto>> GetGoalProgressAsync(int year)
        => await _httpClient.GetFromJsonAsync<List<FinanceGoalProgressRowDto>>($"/api/finance/goals/progress?year={year}") ?? new List<FinanceGoalProgressRowDto>();

    public async Task<FinanceApprovalSummaryDto> GetApprovalSummaryAsync()
        => await _httpClient.GetFromJsonAsync<FinanceApprovalSummaryDto>("/api/finance/approvals/summary") ?? new FinanceApprovalSummaryDto();

    public async Task<List<FinanceApprovalRowDto>> GetApprovalsAsync(string status = "pending")
        => await _httpClient.GetFromJsonAsync<List<FinanceApprovalRowDto>>($"/api/finance/approvals/pending?status={Uri.EscapeDataString(status)}") ?? new List<FinanceApprovalRowDto>();

    public async Task ApproveAsync(string moduleKey, Guid documentId, string? comments = null)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/finance/approvals/{moduleKey}/{documentId}/authorize", new FinanceApprovalDecisionRequestDto { Comments = comments });
        response.EnsureSuccessStatusCode();
    }

    public async Task RejectAsync(string moduleKey, Guid documentId, string? comments = null)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/finance/approvals/{moduleKey}/{documentId}/reject", new FinanceApprovalDecisionRequestDto { Comments = comments });
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<FinanceAlertRowDto>> GetAlertsAsync()
        => await _httpClient.GetFromJsonAsync<List<FinanceAlertRowDto>>("/api/finance/alerts") ?? new List<FinanceAlertRowDto>();

    public async Task<List<FinanceSemaphoreRowDto>> GetSemaphoresAsync()
        => await _httpClient.GetFromJsonAsync<List<FinanceSemaphoreRowDto>>("/api/finance/semaphores") ?? new List<FinanceSemaphoreRowDto>();

    public async Task<List<FinanceCollectionCalendarRowDto>> GetCollectionsCalendarAsync(DateTime from, DateTime to)
        => await _httpClient.GetFromJsonAsync<List<FinanceCollectionCalendarRowDto>>($"/api/finance/collections-calendar?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}") ?? new List<FinanceCollectionCalendarRowDto>();

    public async Task<List<FinancePaymentCalendarRowDto>> GetPaymentsCalendarAsync(DateTime from, DateTime to)
        => await _httpClient.GetFromJsonAsync<List<FinancePaymentCalendarRowDto>>($"/api/finance/payments-calendar?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}") ?? new List<FinancePaymentCalendarRowDto>();

    public async Task<List<FinanceScenarioRowDto>> GetScenariosAsync()
        => await _httpClient.GetFromJsonAsync<List<FinanceScenarioRowDto>>("/api/finance/scenarios") ?? new List<FinanceScenarioRowDto>();

    public async Task<List<FinanceCollectionCommitmentRowDto>> GetCollectionCommitmentsAsync(DateTime from, DateTime to)
        => await _httpClient.GetFromJsonAsync<List<FinanceCollectionCommitmentRowDto>>($"/api/finance/collection-commitments?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}") ?? new List<FinanceCollectionCommitmentRowDto>();

    public async Task SaveCollectionCommitmentsAsync(List<FinanceCollectionCommitmentRowDto> rows)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/finance/collection-commitments/save", new FinanceCollectionCommitmentSaveRequestDto { Rows = rows });
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<FinancePaymentScheduleRowDto>> GetPaymentScheduleAsync(DateTime from, DateTime to)
        => await _httpClient.GetFromJsonAsync<List<FinancePaymentScheduleRowDto>>($"/api/finance/payment-schedule?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}") ?? new List<FinancePaymentScheduleRowDto>();

    public async Task SavePaymentScheduleAsync(List<FinancePaymentScheduleRowDto> rows)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/finance/payment-schedule/save", new FinancePaymentScheduleSaveRequestDto { Rows = rows });
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<FinanceTreasuryWeeklyPlanRowDto>> GetWeeklyTreasuryPlanAsync(int weeks = 8)
        => await _httpClient.GetFromJsonAsync<List<FinanceTreasuryWeeklyPlanRowDto>>($"/api/finance/weekly-treasury-plan?weeks={weeks}") ?? new List<FinanceTreasuryWeeklyPlanRowDto>();

    public async Task<List<FinanceCommitmentFollowUpRowDto>> GetCommitmentFollowUpAsync()
        => await _httpClient.GetFromJsonAsync<List<FinanceCommitmentFollowUpRowDto>>("/api/finance/commitment-follow-up") ?? new List<FinanceCommitmentFollowUpRowDto>();

    public async Task<List<FinanceMonthlyProfitabilityRowDto>> GetMonthlyProfitabilityAsync(int year)
        => await _httpClient.GetFromJsonAsync<List<FinanceMonthlyProfitabilityRowDto>>($"/api/finance/monthly-profitability?year={year}") ?? new List<FinanceMonthlyProfitabilityRowDto>();

    public async Task<List<FinanceCollectionPerformanceRowDto>> GetCollectionsPerformanceAsync(int year, int month)
        => await _httpClient.GetFromJsonAsync<List<FinanceCollectionPerformanceRowDto>>($"/api/finance/collections-performance?year={year}&month={month}") ?? new List<FinanceCollectionPerformanceRowDto>();

    public async Task<List<FinancePaymentPerformanceRowDto>> GetPaymentsPerformanceAsync(int year, int month)
        => await _httpClient.GetFromJsonAsync<List<FinancePaymentPerformanceRowDto>>($"/api/finance/payments-performance?year={year}&month={month}") ?? new List<FinancePaymentPerformanceRowDto>();

    public async Task<FinanceConcentrationAnalysisDto> GetConcentrationAnalysisAsync(int year, int top = 10)
        => await _httpClient.GetFromJsonAsync<FinanceConcentrationAnalysisDto>($"/api/finance/concentration-analysis?year={year}&top={top}") ?? new FinanceConcentrationAnalysisDto { Year = year, Top = top };

    public async Task<List<FinanceYearOverYearRowDto>> GetYearOverYearAsync(int year)
        => await _httpClient.GetFromJsonAsync<List<FinanceYearOverYearRowDto>>($"/api/finance/year-over-year?year={year}") ?? new List<FinanceYearOverYearRowDto>();

    public async Task<List<FinanceKpiScorecardRowDto>> GetKpiScorecardAsync(int year, int month)
        => await _httpClient.GetFromJsonAsync<List<FinanceKpiScorecardRowDto>>($"/api/finance/kpi-scorecard?year={year}&month={month}") ?? new List<FinanceKpiScorecardRowDto>();

    public async Task<List<FinanceWorkingCapitalBridgeRowDto>> GetWorkingCapitalBridgeAsync()
        => await _httpClient.GetFromJsonAsync<List<FinanceWorkingCapitalBridgeRowDto>>("/api/finance/working-capital-bridge") ?? new List<FinanceWorkingCapitalBridgeRowDto>();

    public async Task<FinanceVariationRankingDto> GetVariationRankingsAsync(int year, int month)
        => await _httpClient.GetFromJsonAsync<FinanceVariationRankingDto>($"/api/finance/variation-rankings?year={year}&month={month}") ?? new FinanceVariationRankingDto { Year = year, Month = month };


    public async Task<FinanceBoardPackDto> GetBoardPackAsync(int year, int month)
        => await _httpClient.GetFromJsonAsync<FinanceBoardPackDto>($"/api/finance/board-pack?year={year}&month={month}") ?? new FinanceBoardPackDto { Year = year, Month = month };

    public async Task<List<FinanceLiquidityRadarRowDto>> GetLiquidityRadarAsync(int year, int month)
        => await _httpClient.GetFromJsonAsync<List<FinanceLiquidityRadarRowDto>>($"/api/finance/liquidity-radar?year={year}&month={month}") ?? new List<FinanceLiquidityRadarRowDto>();

    public async Task<List<FinanceCashConversionCycleRowDto>> GetCashConversionCycleAsync(int year)
        => await _httpClient.GetFromJsonAsync<List<FinanceCashConversionCycleRowDto>>($"/api/finance/cash-conversion-cycle?year={year}") ?? new List<FinanceCashConversionCycleRowDto>();

    public async Task<FinanceStressTestDto> GetStressTestsAsync(int year, int month)
        => await _httpClient.GetFromJsonAsync<FinanceStressTestDto>($"/api/finance/stress-tests?year={year}&month={month}") ?? new FinanceStressTestDto { Year = year, Month = month };



    public async Task<List<FinanceActionCenterRowDto>> GetActionCenterAsync()
        => await _httpClient.GetFromJsonAsync<List<FinanceActionCenterRowDto>>("/api/finance/action-center") ?? new List<FinanceActionCenterRowDto>();

    public async Task<List<FinanceClosingCockpitRowDto>> GetClosingCockpitAsync(int year, int month)
        => await _httpClient.GetFromJsonAsync<List<FinanceClosingCockpitRowDto>>($"/api/finance/closing-cockpit?year={year}&month={month}") ?? new List<FinanceClosingCockpitRowDto>();

    public async Task<List<FinanceCovenantRowDto>> GetCovenantsAsync(int year, int month)
        => await _httpClient.GetFromJsonAsync<List<FinanceCovenantRowDto>>($"/api/finance/covenants?year={year}&month={month}") ?? new List<FinanceCovenantRowDto>();

    public async Task<List<FinanceExecutiveAgreementRowDto>> GetExecutiveAgreementsAsync(int year, int month)
        => await _httpClient.GetFromJsonAsync<List<FinanceExecutiveAgreementRowDto>>($"/api/finance/executive-agreements?year={year}&month={month}") ?? new List<FinanceExecutiveAgreementRowDto>();

    public async Task SaveExecutiveAgreementsAsync(List<FinanceExecutiveAgreementRowDto> rows)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/finance/executive-agreements/save", new FinanceExecutiveAgreementSaveRequestDto { Rows = rows });
        response.EnsureSuccessStatusCode();
    }

    public async Task SaveScenariosAsync(List<FinanceScenarioRowDto> rows)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/finance/scenarios/save", new FinanceScenarioSaveRequestDto { Rows = rows });
        response.EnsureSuccessStatusCode();
    }


    public async Task<List<FinanceRollingForecastRowDto>> GetRollingForecastAsync(int months = 12)
        => await _httpClient.GetFromJsonAsync<List<FinanceRollingForecastRowDto>>($"/api/finance/rolling-forecast?months={months}") ?? new List<FinanceRollingForecastRowDto>();

    public async Task<List<FinanceCreditPolicyRowDto>> GetCreditPolicyAsync()
        => await _httpClient.GetFromJsonAsync<List<FinanceCreditPolicyRowDto>>("/api/finance/credit-policy") ?? new List<FinanceCreditPolicyRowDto>();

    public async Task<List<FinanceSupplierRiskRowDto>> GetSupplierRiskAsync()
        => await _httpClient.GetFromJsonAsync<List<FinanceSupplierRiskRowDto>>("/api/finance/supplier-risk") ?? new List<FinanceSupplierRiskRowDto>();

    public async Task<List<FinanceRecoveryMatrixRowDto>> GetRecoveryMatrixAsync()
        => await _httpClient.GetFromJsonAsync<List<FinanceRecoveryMatrixRowDto>>("/api/finance/recovery-matrix") ?? new List<FinanceRecoveryMatrixRowDto>();

    public async Task<List<FinanceCustomerRadarRowDto>> GetCustomerRadarAsync(int year, int month)
        => await _httpClient.GetFromJsonAsync<List<FinanceCustomerRadarRowDto>>($"/api/finance/customer-radar?year={year}&month={month}") ?? new List<FinanceCustomerRadarRowDto>();

    public async Task<List<FinanceSupplierRadarRowDto>> GetSupplierRadarAsync(int year, int month)
        => await _httpClient.GetFromJsonAsync<List<FinanceSupplierRadarRowDto>>($"/api/finance/supplier-radar?year={year}&month={month}") ?? new List<FinanceSupplierRadarRowDto>();

    public async Task<List<FinanceBranchPulseRowDto>> GetBranchPulseAsync(int year, int month)
        => await _httpClient.GetFromJsonAsync<List<FinanceBranchPulseRowDto>>($"/api/finance/branch-pulse?year={year}&month={month}") ?? new List<FinanceBranchPulseRowDto>();

    public async Task<List<FinanceMonthlyLiquidityBridgeRowDto>> GetMonthlyLiquidityBridgeAsync(int year)
        => await _httpClient.GetFromJsonAsync<List<FinanceMonthlyLiquidityBridgeRowDto>>($"/api/finance/monthly-liquidity-bridge?year={year}") ?? new List<FinanceMonthlyLiquidityBridgeRowDto>();

    public async Task<FinanceScenarioEvaluationDto> EvaluateScenarioAsync(FinanceScenarioRowDto scenario)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/finance/scenarios/evaluate", new FinanceScenarioEvaluationRequestDto { Scenario = scenario });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FinanceScenarioEvaluationDto>() ?? new FinanceScenarioEvaluationDto();
    }

}


public sealed class FinanceYearOverYearRowDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal SalesCurrent { get; set; }
    public decimal SalesPrevious { get; set; }
    public decimal SalesGrowthPercent { get; set; }
    public decimal PurchasesCurrent { get; set; }
    public decimal PurchasesPrevious { get; set; }
    public decimal PurchasesGrowthPercent { get; set; }
    public decimal ReceiptsCurrent { get; set; }
    public decimal ReceiptsPrevious { get; set; }
    public decimal ReceiptsGrowthPercent { get; set; }
    public decimal PaymentsCurrent { get; set; }
    public decimal PaymentsPrevious { get; set; }
    public decimal PaymentsGrowthPercent { get; set; }
}

public sealed class FinanceKpiScorecardRowDto
{
    public string MetricCode { get; set; } = string.Empty;
    public string MetricLabel { get; set; } = string.Empty;
    public decimal ActualAmount { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal CompliancePercent { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Commentary { get; set; } = string.Empty;
}

public sealed class FinanceWorkingCapitalBridgeRowDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal WeightPercent { get; set; }
    public string Impact { get; set; } = string.Empty;
    public string Commentary { get; set; } = string.Empty;
}

public sealed class FinanceVariationRankingRowDto
{
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public decimal CurrentAmount { get; set; }
    public decimal PreviousAmount { get; set; }
    public decimal VariationAmount { get; set; }
    public decimal VariationPercent { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
}

public sealed class FinanceVariationRankingDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<FinanceVariationRankingRowDto> CustomerGrowth { get; set; } = new();
    public List<FinanceVariationRankingRowDto> CustomerDecline { get; set; } = new();
    public List<FinanceVariationRankingRowDto> SupplierGrowth { get; set; } = new();
    public List<FinanceVariationRankingRowDto> SupplierDecline { get; set; } = new();
}


public sealed class FinanceMonthlyProfitabilityRowDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal GrossSales { get; set; }
    public decimal CreditNotes { get; set; }
    public decimal NetSales { get; set; }
    public decimal Purchases { get; set; }
    public decimal GrossMargin { get; set; }
    public decimal GrossMarginPercent { get; set; }
    public decimal Receipts { get; set; }
    public decimal Payments { get; set; }
    public decimal CashMargin { get; set; }
    public decimal CumulativeGrossMargin { get; set; }
    public decimal CumulativeCashMargin { get; set; }
}

public sealed class FinanceCollectionPerformanceRowDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int OpenInvoices { get; set; }
    public decimal OpenAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal UpcomingWeekAmount { get; set; }
    public decimal PromisedAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal EffectivenessPercent { get; set; }
    public decimal AveragePastDueDays { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
}

public sealed class FinancePaymentPerformanceRowDto
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int PendingInvoices { get; set; }
    public decimal OpenAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal ScheduledAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal CompliancePercent { get; set; }
    public int RequiresAuthorizationCount { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
}

public sealed class FinanceConcentrationPartyRowDto
{
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal OpenAmount { get; set; }
    public int Documents { get; set; }
    public decimal SharePercent { get; set; }
    public string Route { get; set; } = string.Empty;
}

public sealed class FinanceConcentrationAnalysisDto
{
    public int Year { get; set; }
    public int Top { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalPurchases { get; set; }
    public double CustomerHhi { get; set; }
    public double SupplierHhi { get; set; }
    public decimal Top3CustomerSharePercent { get; set; }
    public decimal Top3SupplierSharePercent { get; set; }
    public List<FinanceConcentrationPartyRowDto> Customers { get; set; } = new();
    public List<FinanceConcentrationPartyRowDto> Suppliers { get; set; } = new();
}

public sealed class FinanceDashboardDto
{
    public DateTime Today { get; set; }
    public decimal CashBalance { get; set; }
    public decimal BankBalance { get; set; }
    public decimal TotalLiquidity { get; set; }
    public decimal OpenReceivables { get; set; }
    public decimal OpenPayables { get; set; }
    public decimal WorkingCapital { get; set; }
    public decimal SalesThisMonth { get; set; }
    public decimal PurchasesThisMonth { get; set; }
    public decimal ReceiptsThisMonth { get; set; }
    public decimal PaymentsThisMonth { get; set; }
    public int DraftJournalEntries { get; set; }
    public int PostedJournalEntries { get; set; }
    public int OpenPeriods { get; set; }
    public decimal BudgetThisMonth { get; set; }
    public decimal ActualThisMonth { get; set; }
    public decimal BudgetVarianceThisMonth { get; set; }
    public decimal BudgetCompliancePercent { get; set; }
    public int BudgetRowsThisYear { get; set; }
    public int GoalRowsThisYear { get; set; }
    public int PendingApprovals { get; set; }
    public int ActiveAlerts { get; set; }
    public int RedSemaphores { get; set; }
    public int AmberSemaphores { get; set; }
}

public sealed class FinanceCashFlowRowDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ExpectedInflows { get; set; }
    public decimal ExpectedOutflows { get; set; }
    public decimal NetFlow { get; set; }
    public decimal ProjectedClosing { get; set; }
}

public sealed class FinanceDocumentControlRowDto
{
    public string Module { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public Guid DocumentId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal OpenAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public int AgeDays { get; set; }
    public bool IsAccounted { get; set; }
    public string AccountingStatus { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
}

public sealed class FinanceExceptionRowDto
{
    public string Severity { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
    public DateTime? DocumentDate { get; set; }
    public decimal? Amount { get; set; }
    public string Message { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
}

public sealed class FinanceBudgetLookupItemDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string Nature { get; set; } = string.Empty;
}

public sealed class FinanceBudgetRowDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Year { get; set; }
    public int Month { get; set; }
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string Nature { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class FinanceBudgetSaveRequestDto
{
    public int Year { get; set; }
    public List<FinanceBudgetRowDto> Rows { get; set; } = new();
}

public sealed class FinanceBudgetVsActualRowDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string Nature { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal CompliancePercent { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class FinanceGoalMetricItemDto
{
    public string MetricCode { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
}

public sealed class FinanceGoalRowDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Year { get; set; }
    public int Month { get; set; }
    public string MetricCode { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class FinanceGoalSaveRequestDto
{
    public int Year { get; set; }
    public List<FinanceGoalRowDto> Rows { get; set; } = new();
}

public sealed class FinanceGoalProgressRowDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MetricCode { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal CompliancePercent { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class FinanceApprovalSummaryDto
{
    public int PendingCount { get; set; }
    public int AuthorizedCount { get; set; }
    public int RejectedCount { get; set; }
    public int HighPriorityCount { get; set; }
    public int OverdueCount { get; set; }
}

public sealed class FinanceApprovalRowDto
{
    public string Module { get; set; } = string.Empty;
    public string ModuleKey { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public Guid DocumentId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal OpenAmount { get; set; }
    public DateTime DocumentDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int AgeDays { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
}

public sealed class FinanceApprovalDecisionRequestDto
{
    public string? Comments { get; set; }
}

public sealed class FinanceAlertRowDto
{
    public string Severity { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? DueDate { get; set; }
    public int? DaysToDue { get; set; }
    public string Route { get; set; } = string.Empty;
}

public sealed class FinanceSemaphoreRowDto
{
    public string Category { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal TargetValue { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal VariancePercent { get; set; }
    public string Semaphore { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
}


public sealed class FinanceCollectionCalendarRowDto
{
    public Guid CustomerId { get; set; }
    public Guid SalesInvoiceId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Total { get; set; }
    public decimal AppliedAmount { get; set; }
    public decimal OpenAmount { get; set; }
    public int DaysToDue { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
}

public sealed class FinancePaymentCalendarRowDto
{
    public Guid SupplierId { get; set; }
    public Guid PurchaseInvoiceId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Total { get; set; }
    public decimal AppliedAmount { get; set; }
    public decimal OpenAmount { get; set; }
    public int DaysToDue { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
}

public sealed class FinanceCollectionCommitmentRowDto
{
    public Guid CustomerId { get; set; }
    public Guid SalesInvoiceId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal OpenAmount { get; set; }
    public int DaysToDue { get; set; }
    public DateTime? PlannedCollectionDate { get; set; }
    public string CommitmentStatus { get; set; } = string.Empty;
    public string Responsible { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public string Route { get; set; } = string.Empty;
}

public sealed class FinanceCollectionCommitmentSaveRequestDto
{
    public List<FinanceCollectionCommitmentRowDto> Rows { get; set; } = new();
}

public sealed class FinancePaymentScheduleRowDto
{
    public Guid SupplierId { get; set; }
    public Guid PurchaseInvoiceId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal OpenAmount { get; set; }
    public int DaysToDue { get; set; }
    public DateTime? PlannedPaymentDate { get; set; }
    public string ScheduleStatus { get; set; } = string.Empty;
    public string Responsible { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool NeedsAuthorization { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public string Route { get; set; } = string.Empty;
}

public sealed class FinancePaymentScheduleSaveRequestDto
{
    public List<FinancePaymentScheduleRowDto> Rows { get; set; } = new();
}

public sealed class FinanceTreasuryWeeklyPlanRowDto
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal PlannedCollections { get; set; }
    public decimal PlannedPayments { get; set; }
    public decimal NetFlow { get; set; }
    public decimal ProjectedClosing { get; set; }
    public int CollectionCount { get; set; }
    public int PaymentCount { get; set; }
    public decimal OverdueCollectionsCarryover { get; set; }
    public decimal OverduePaymentsCarryover { get; set; }
}

public sealed class FinanceCommitmentFollowUpRowDto
{
    public string FlowType { get; set; } = string.Empty;
    public Guid DocumentId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DateTime? PlannedDate { get; set; }
    public decimal OpenAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Responsible { get; set; } = string.Empty;
    public int DaysToDue { get; set; }
    public string ActionRequired { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
}

public sealed class FinanceScenarioRowDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int CollectionShiftDays { get; set; }
    public int PaymentShiftDays { get; set; }
    public decimal SalesGrowthPercent { get; set; }
    public decimal PurchaseGrowthPercent { get; set; }
    public decimal ExpenseReductionPercent { get; set; }
    public decimal ExtraInflow { get; set; }
    public decimal ExtraOutflow { get; set; }
    public int HorizonWeeks { get; set; } = 8;
    public bool IsDefault { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class FinanceScenarioSaveRequestDto
{
    public List<FinanceScenarioRowDto> Rows { get; set; } = new();
}

public sealed class FinanceScenarioEvaluationRequestDto
{
    public FinanceScenarioRowDto? Scenario { get; set; }
}

public sealed class FinanceScenarioEvaluationDto
{
    public string ScenarioName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal BaselineInflows { get; set; }
    public decimal BaselineOutflows { get; set; }
    public decimal BaselineClosingBalance { get; set; }
    public decimal ScenarioInflows { get; set; }
    public decimal ScenarioOutflows { get; set; }
    public decimal ScenarioClosingBalance { get; set; }
    public decimal ImpactAmount { get; set; }
    public List<FinanceScenarioEvaluationRowDto> Rows { get; set; } = new();
}

public sealed class FinanceScenarioEvaluationRowDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal BaselineOpening { get; set; }
    public decimal BaselineInflows { get; set; }
    public decimal BaselineOutflows { get; set; }
    public decimal BaselineClosing { get; set; }
    public decimal ScenarioOpening { get; set; }
    public decimal ScenarioInflows { get; set; }
    public decimal ScenarioOutflows { get; set; }
    public decimal ScenarioClosing { get; set; }
    public decimal ImpactAmount { get; set; }
}


public sealed class FinanceBoardPackDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthLabel { get; set; } = string.Empty;
    public decimal SalesCurrentMonth { get; set; }
    public decimal PurchasesCurrentMonth { get; set; }
    public decimal ReceiptsCurrentMonth { get; set; }
    public decimal PaymentsCurrentMonth { get; set; }
    public decimal NetCashCurrentMonth { get; set; }
    public decimal OpenReceivables { get; set; }
    public decimal OpenPayables { get; set; }
    public decimal InventoryValue { get; set; }
    public decimal WorkingCapital { get; set; }
    public decimal BudgetCurrentMonth { get; set; }
    public decimal ActualCurrentMonth { get; set; }
    public decimal GoalCurrentMonth { get; set; }
    public decimal GoalActualCurrentMonth { get; set; }
    public decimal BudgetCompliancePercent { get; set; }
    public decimal GoalCompliancePercent { get; set; }
    public List<FinanceBoardPackMonthlyRowDto> MonthlyRows { get; set; } = new();
}

public sealed class FinanceBoardPackMonthlyRowDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public decimal Purchases { get; set; }
    public decimal Receipts { get; set; }
    public decimal Payments { get; set; }
    public decimal NetCash { get; set; }
    public decimal Budget { get; set; }
    public decimal Actual { get; set; }
}

public sealed class FinanceLiquidityRadarRowDto
{
    public string HorizonLabel { get; set; } = string.Empty;
    public int Days { get; set; }
    public decimal OpeningLiquidity { get; set; }
    public decimal ExpectedInflows { get; set; }
    public decimal ExpectedOutflows { get; set; }
    public decimal NetPosition { get; set; }
    public decimal CoverageRatio { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
}

public sealed class FinanceCashConversionCycleRowDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public decimal Purchases { get; set; }
    public decimal InventoryValue { get; set; }
    public decimal OpenReceivables { get; set; }
    public decimal OpenPayables { get; set; }
    public decimal DsoDays { get; set; }
    public decimal DioDays { get; set; }
    public decimal DpoDays { get; set; }
    public decimal CashConversionCycleDays { get; set; }
}

public sealed class FinanceStressTestDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal BaseLiquidity { get; set; }
    public decimal BaseInflows { get; set; }
    public decimal BaseOutflows { get; set; }
    public decimal BaseClosingCash { get; set; }
    public List<FinanceStressTestScenarioRowDto> Rows { get; set; } = new();
}

public sealed class FinanceStressTestScenarioRowDto
{
    public string Scenario { get; set; } = string.Empty;
    public decimal InflowDelta { get; set; }
    public decimal OutflowDelta { get; set; }
    public decimal ClosingCash { get; set; }
    public decimal VarianceVsBase { get; set; }
    public string ImpactLevel { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}


public sealed class FinanceRollingForecastRowDto
{
    public string Label { get; set; } = string.Empty;
    public decimal OpeningLiquidity { get; set; }
    public decimal CollectionsScheduled { get; set; }
    public decimal CollectionsDue { get; set; }
    public decimal PaymentsScheduled { get; set; }
    public decimal PaymentsDue { get; set; }
    public decimal ProjectedInflows { get; set; }
    public decimal ProjectedOutflows { get; set; }
    public decimal NetFlow { get; set; }
    public decimal ClosingLiquidity { get; set; }
    public decimal SalesTarget { get; set; }
    public decimal PurchasesTarget { get; set; }
    public decimal BudgetAmount { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
}

public sealed class FinanceCreditPolicyRowDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal OpenAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal AvailableCredit { get; set; }
    public decimal UtilizationPercent { get; set; }
    public decimal PromisedAmount { get; set; }
    public decimal SalesThisMonth { get; set; }
    public decimal AveragePastDueDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
}

public sealed class FinanceSupplierRiskRowDto
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal OpenAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal ScheduledAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal CompliancePercent { get; set; }
    public decimal ConcentrationPercent { get; set; }
    public int RequiresAuthorizationCount { get; set; }
    public int PaymentTermDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
}

public sealed class FinanceRecoveryMatrixRowDto
{
    public string PortfolioType { get; set; } = string.Empty;
    public string Band { get; set; } = string.Empty;
    public int Documents { get; set; }
    public int Parties { get; set; }
    public decimal Amount { get; set; }
    public decimal SharePercent { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
}

public sealed class FinanceCustomerRadarRowDto
{
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal InvoicedAmount { get; set; }
    public decimal CreditNoteAmount { get; set; }
    public decimal NetSales { get; set; }
    public decimal ReceiptsAmount { get; set; }
    public decimal OpenAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal ParticipationPercent { get; set; }
    public decimal CollectionEffectivenessPercent { get; set; }
    public decimal AveragePastDueDays { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
}

public sealed class FinanceSupplierRadarRowDto
{
    public Guid SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal PurchasesAmount { get; set; }
    public decimal ReturnAmount { get; set; }
    public decimal NetPurchases { get; set; }
    public decimal PaymentsAmount { get; set; }
    public decimal OpenAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal ParticipationPercent { get; set; }
    public decimal PaymentCoveragePercent { get; set; }
    public int PaymentTermDays { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
}

public sealed class FinanceBranchPulseRowDto
{
    public Guid BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public decimal SalesAmount { get; set; }
    public decimal PurchasesAmount { get; set; }
    public decimal ReceiptsAmount { get; set; }
    public decimal PaymentsAmount { get; set; }
    public decimal NetCashMovement { get; set; }
    public decimal CashBalance { get; set; }
    public decimal OpenReceivables { get; set; }
    public decimal OpenPayables { get; set; }
    public decimal WorkingCapital { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class FinanceMonthlyLiquidityBridgeRowDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal OpeningLiquidity { get; set; }
    public decimal Receipts { get; set; }
    public decimal Payments { get; set; }
    public decimal NetMovement { get; set; }
    public decimal ClosingLiquidity { get; set; }
    public decimal OpenReceivables { get; set; }
    public decimal OpenPayables { get; set; }
    public decimal WorkingCapital { get; set; }
    public decimal CoveragePercent { get; set; }
    public string Status { get; set; } = string.Empty;
}


public sealed class FinanceActionCenterRowDto
{
    public string Source { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? DueDate { get; set; }
    public string RecommendedAction { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
}

public sealed class FinanceClosingCockpitRowDto
{
    public string Area { get; set; } = string.Empty;
    public string CheckName { get; set; } = string.Empty;
    public int PendingCount { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
}

public sealed class FinanceCovenantRowDto
{
    public string CovenantCode { get; set; } = string.Empty;
    public string CovenantName { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal ThresholdValue { get; set; }
    public string ThresholdOperator { get; set; } = string.Empty;
    public decimal Headroom { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class FinanceExecutiveAgreementRowDto
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Agreement { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class FinanceExecutiveAgreementSaveRequestDto
{
    public List<FinanceExecutiveAgreementRowDto> Rows { get; set; } = new();
}


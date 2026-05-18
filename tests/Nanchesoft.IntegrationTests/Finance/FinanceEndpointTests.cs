using System.Net;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Finance;

[Collection("NanchesoftApi")]
public class FinanceEndpointTests
{
    private readonly HttpClient _client;
    private readonly int _year = DateTime.UtcNow.Year;
    private readonly int _month = DateTime.UtcNow.Month;

    public FinanceEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Dashboard y analíticos generales ────────────────────────────────────

    [Fact]
    public async Task Dashboard_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CashFlow_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/cash-flow");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DocumentControl_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/document-control");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Exceptions_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/exceptions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Alerts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/alerts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Semaphores_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/semaphores");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ActionCenter_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/action-center");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Presupuestos ─────────────────────────────────────────────────────────

    [Fact]
    public async Task BudgetsLookups_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/budgets/lookups");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Budgets_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/budgets?year={_year}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BudgetsVsActual_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/budgets/vs-actual?year={_year}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Metas ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GoalsMetrics_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/goals/metrics");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Goals_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/goals?year={_year}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GoalsProgress_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/goals/progress?year={_year}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Autorizaciones ───────────────────────────────────────────────────────

    [Fact]
    public async Task ApprovalsSummary_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/approvals/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApprovalsPending_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/approvals/pending");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Calendarios ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CollectionsCalendar_ReturnsOk()
    {
        var from = new DateTime(_year, _month, 1).ToString("yyyy-MM-dd");
        var to = new DateTime(_year, _month, DateTime.DaysInMonth(_year, _month)).ToString("yyyy-MM-dd");
        var response = await _client.GetAsync($"/api/finance/collections-calendar?from={from}&to={to}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PaymentsCalendar_ReturnsOk()
    {
        var from = new DateTime(_year, _month, 1).ToString("yyyy-MM-dd");
        var to = new DateTime(_year, _month, DateTime.DaysInMonth(_year, _month)).ToString("yyyy-MM-dd");
        var response = await _client.GetAsync($"/api/finance/payments-calendar?from={from}&to={to}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Compromisos y programación ───────────────────────────────────────────

    [Fact]
    public async Task CollectionCommitments_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/collection-commitments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PaymentSchedule_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/payment-schedule");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WeeklyTreasuryPlan_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/weekly-treasury-plan?weeks=4");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CommitmentFollowUp_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/commitment-follow-up");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Escenarios ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Scenarios_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/scenarios");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RollingForecast_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/rolling-forecast?months=6");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Análisis de rentabilidad y variación ─────────────────────────────────

    [Fact]
    public async Task MonthlyProfitability_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/monthly-profitability?year={_year}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CollectionsPerformance_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/collections-performance?year={_year}&month={_month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PaymentsPerformance_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/payments-performance?year={_year}&month={_month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConcentrationAnalysis_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/concentration-analysis?year={_year}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task YearOverYear_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/year-over-year?year={_year}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task KpiScorecard_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/kpi-scorecard?year={_year}&month={_month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VariationRankings_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/variation-rankings?year={_year}&month={_month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Liquidez y capital de trabajo ────────────────────────────────────────

    [Fact]
    public async Task WorkingCapitalBridge_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/working-capital-bridge");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LiquidityRadar_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/liquidity-radar?year={_year}&month={_month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CashConversionCycle_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/cash-conversion-cycle?year={_year}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MonthlyLiquidityBridge_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/monthly-liquidity-bridge?year={_year}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StressTests_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/stress-tests?year={_year}&month={_month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Reportes ejecutivos ──────────────────────────────────────────────────

    [Fact]
    public async Task BoardPack_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/board-pack?year={_year}&month={_month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ClosingCockpit_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/closing-cockpit?year={_year}&month={_month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Covenants_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/covenants?year={_year}&month={_month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ExecutiveAgreements_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/executive-agreements?year={_year}&month={_month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Análisis de riesgo y política ────────────────────────────────────────

    [Fact]
    public async Task CreditPolicy_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/credit-policy");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SupplierRisk_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/supplier-risk");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RecoveryMatrix_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/finance/recovery-matrix");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Torres de control ────────────────────────────────────────────────────

    [Fact]
    public async Task CustomerRadar_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/customer-radar?year={_year}&month={_month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SupplierRadar_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/supplier-radar?year={_year}&month={_month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BranchPulse_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/finance/branch-pulse?year={_year}&month={_month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

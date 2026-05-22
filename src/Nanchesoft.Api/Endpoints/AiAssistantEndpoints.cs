using Microsoft.EntityFrameworkCore;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

/// <summary>
/// Legacy read-only AI endpoints kept for backward compatibility with Fase 1.
/// New chat orchestrator lives in <see cref="Nanchesoft.Api.Ai.AiEndpoints"/>.
/// </summary>
public static class AiAssistantEndpoints
{
    public static IEndpointRouteBuilder MapAiAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ia").WithTags("AI Assistant (legacy)");

        group.MapGet("/resumen-hoy", async (HttpContext http, NanchesoftDbContext db) =>
            Results.Ok(await BuildTodaySummaryAsync(http, db)));

        group.MapGet("/ventas-hoy", async (HttpContext http, NanchesoftDbContext db) =>
            Results.Ok(await BuildTodaySalesAsync(http, db)));

        group.MapGet("/clientes/saldo", async (HttpContext http, NanchesoftDbContext db, string? query) =>
            Results.Ok(await BuildCustomerBalanceAsync(http, db, query)));

        group.MapGet("/nomina/resumen-periodo", async (HttpContext http, NanchesoftDbContext db) =>
            Results.Ok(await BuildPayrollOpenPeriodAsync(http, db)));

        group.MapGet("/clientes/saldos-vencidos", async (HttpContext http, NanchesoftDbContext db) =>
            Results.Ok(await BuildOverdueCustomersAsync(http, db)));

        return app;
    }

    private static (Guid? TenantId, Guid? CompanyId, Guid? BranchId, bool IsPlatformOwner) ResolveScope(HttpContext http)
    {
        return (
            ApiTenantScope.ResolveTenantId(http),
            ApiTenantScope.ResolveCompanyId(http),
            ApiTenantScope.ResolveBranchId(http),
            ApiTenantScope.IsPlatformOwner(http)
        );
    }

    private static async Task<AiTodaySummaryDto> BuildTodaySummaryAsync(HttpContext http, NanchesoftDbContext db)
    {
        var (tenantId, companyId, branchId, isPlatformOwner) = ResolveScope(http);
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var ordersQuery = db.SalesOrders.AsNoTracking()
            .Where(x => x.OrderDate >= today && x.OrderDate < tomorrow);
        var invoicesQuery = db.SalesInvoices.AsNoTracking()
            .Where(x => x.InvoiceDate >= today && x.InvoiceDate < tomorrow);

        if (!isPlatformOwner)
        {
            if (tenantId.HasValue)
            {
                ordersQuery = ordersQuery.Where(x => x.TenantId == tenantId.Value);
                invoicesQuery = invoicesQuery.Where(x => x.TenantId == tenantId.Value);
            }
            if (companyId.HasValue)
            {
                ordersQuery = ordersQuery.Where(x => x.CompanyId == companyId.Value);
                invoicesQuery = invoicesQuery.Where(x => x.CompanyId == companyId.Value);
            }
            if (branchId.HasValue)
            {
                ordersQuery = ordersQuery.Where(x => x.BranchId == branchId.Value);
                invoicesQuery = invoicesQuery.Where(x => x.BranchId == branchId.Value);
            }
        }

        var orderCount = await ordersQuery.CountAsync();
        var orderTotal = await ordersQuery.SumAsync(x => (decimal?)x.Total) ?? 0m;
        var invoiceCount = await invoicesQuery.CountAsync();
        var invoiceTotal = await invoicesQuery.SumAsync(x => (decimal?)x.Total) ?? 0m;

        return new AiTodaySummaryDto
        {
            ReferenceDate = today,
            OrderCount = orderCount,
            OrderTotal = orderTotal,
            InvoiceCount = invoiceCount,
            InvoiceTotal = invoiceTotal
        };
    }

    private static async Task<AiTodaySalesDto> BuildTodaySalesAsync(HttpContext http, NanchesoftDbContext db)
    {
        var (tenantId, companyId, branchId, isPlatformOwner) = ResolveScope(http);
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var invoicesQuery = db.SalesInvoices.AsNoTracking()
            .Where(x => x.InvoiceDate >= today && x.InvoiceDate < tomorrow);

        if (!isPlatformOwner)
        {
            if (tenantId.HasValue) invoicesQuery = invoicesQuery.Where(x => x.TenantId == tenantId.Value);
            if (companyId.HasValue) invoicesQuery = invoicesQuery.Where(x => x.CompanyId == companyId.Value);
            if (branchId.HasValue) invoicesQuery = invoicesQuery.Where(x => x.BranchId == branchId.Value);
        }

        var aggregated = await invoicesQuery
            .GroupBy(x => 1)
            .Select(g => new AiTodaySalesDto
            {
                ReferenceDate = today,
                InvoiceCount = g.Count(),
                Subtotal = g.Sum(x => x.Subtotal),
                Tax = g.Sum(x => x.TaxAmount),
                Total = g.Sum(x => x.Total)
            })
            .FirstOrDefaultAsync();

        return aggregated ?? new AiTodaySalesDto { ReferenceDate = today };
    }

    private static async Task<AiCustomerBalanceDto> BuildCustomerBalanceAsync(HttpContext http, NanchesoftDbContext db, string? query)
    {
        var (tenantId, companyId, _, isPlatformOwner) = ResolveScope(http);

        var accountsQuery = db.AccountsReceivableAccounts.AsNoTracking().AsQueryable();
        var customersQuery = db.Customers.AsNoTracking().AsQueryable();

        if (!isPlatformOwner)
        {
            if (tenantId.HasValue)
            {
                accountsQuery = accountsQuery.Where(x => x.TenantId == tenantId.Value);
                customersQuery = customersQuery.Where(x => x.TenantId == tenantId.Value);
            }
            if (companyId.HasValue)
            {
                accountsQuery = accountsQuery.Where(x => x.CompanyId == companyId.Value);
                customersQuery = customersQuery.Where(x => x.CompanyId == companyId.Value);
            }
        }

        Guid? matchedCustomerId = null;
        string? matchedCustomerName = null;
        string? matchedCustomerCode = null;

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            var pattern = $"%{term}%";
            var customer = await customersQuery
                .Where(x => x.Code.ToLower() == term.ToLower()
                            || EF.Functions.ILike(x.Name, pattern)
                            || EF.Functions.ILike(x.LegalName, pattern)
                            || EF.Functions.ILike(x.TaxId, pattern))
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Code, x.Name })
                .FirstOrDefaultAsync();

            if (customer is null)
            {
                return new AiCustomerBalanceDto
                {
                    NotFound = true,
                    Query = term
                };
            }

            matchedCustomerId = customer.Id;
            matchedCustomerCode = customer.Code;
            matchedCustomerName = customer.Name;
            accountsQuery = accountsQuery.Where(x => x.CustomerId == customer.Id);
        }

        var rows = await accountsQuery
            .Join(db.Customers.AsNoTracking(),
                a => a.CustomerId,
                c => c.Id,
                (a, c) => new AiCustomerBalanceRowDto
                {
                    CustomerId = c.Id,
                    CustomerCode = c.Code,
                    CustomerName = c.Name,
                    TotalCharges = a.TotalCharges,
                    TotalCredits = a.TotalCredits,
                    CurrentBalance = a.CurrentBalance,
                    LastMovementAt = a.LastMovementAt
                })
            .OrderByDescending(x => x.CurrentBalance)
            .Take(50)
            .ToListAsync();

        return new AiCustomerBalanceDto
        {
            Query = query ?? string.Empty,
            CustomerId = matchedCustomerId,
            CustomerName = matchedCustomerName,
            CustomerCode = matchedCustomerCode,
            TotalBalance = rows.Sum(x => x.CurrentBalance),
            Rows = rows
        };
    }

    private static async Task<AiPayrollPeriodSummaryDto> BuildPayrollOpenPeriodAsync(HttpContext http, NanchesoftDbContext db)
    {
        var (tenantId, companyId, _, isPlatformOwner) = ResolveScope(http);

        var periodsQuery = db.PayrollPeriods.AsNoTracking().Where(x => !x.IsClosed);
        if (!isPlatformOwner)
        {
            if (tenantId.HasValue) periodsQuery = periodsQuery.Where(x => x.TenantId == tenantId.Value);
            if (companyId.HasValue) periodsQuery = periodsQuery.Where(x => x.CompanyId == companyId.Value);
        }

        var period = await periodsQuery
            .OrderByDescending(x => x.EndDate)
            .Select(x => new { x.Id, x.Code, x.Name, x.StartDate, x.EndDate, x.PaymentDate, x.Status })
            .FirstOrDefaultAsync();

        if (period is null) return new AiPayrollPeriodSummaryDto { NotFound = true };

        var adjustments = await db.PrePayrollAdjustments.AsNoTracking()
            .Where(x => x.PayrollPeriodId == period.Id)
            .GroupBy(x => 1)
            .Select(g => new
            {
                Perceptions = g.Where(x => x.AdjustmentType == "perception").Sum(x => x.Amount),
                Deductions = g.Where(x => x.AdjustmentType == "deduction").Sum(x => x.Amount),
                EmployeeCount = g.Select(x => x.EmployeeId).Distinct().Count()
            })
            .FirstOrDefaultAsync();

        var runs = await db.PayrollRuns.AsNoTracking()
            .Where(x => x.PayrollPeriodId == period.Id)
            .GroupBy(x => 1)
            .Select(g => new
            {
                Runs = g.Count(),
                Gross = g.Sum(x => x.GrossAmount),
                Deductions = g.Sum(x => x.DeductionsAmount),
                Net = g.Sum(x => x.NetAmount),
                Employees = g.Sum(x => x.EmployeeCount)
            })
            .FirstOrDefaultAsync();

        var perceptions = adjustments?.Perceptions ?? 0m;
        var deductions = adjustments?.Deductions ?? 0m;
        var employeeCount = runs is not null && runs.Employees > 0 ? runs.Employees : (adjustments?.EmployeeCount ?? 0);
        var grossAmount = runs?.Gross ?? perceptions;
        var deductionsAmount = runs?.Deductions ?? deductions;
        var netAmount = runs?.Net ?? (perceptions - deductions);

        return new AiPayrollPeriodSummaryDto
        {
            PeriodId = period.Id,
            PeriodCode = period.Code,
            PeriodName = period.Name,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            PaymentDate = period.PaymentDate,
            Status = period.Status,
            EmployeeCount = employeeCount,
            GrossAmount = grossAmount,
            DeductionsAmount = deductionsAmount,
            NetAmount = netAmount,
            HasRuns = runs is not null && runs.Runs > 0
        };
    }

    private static async Task<AiOverdueBalancesDto> BuildOverdueCustomersAsync(HttpContext http, NanchesoftDbContext db)
    {
        var (tenantId, companyId, _, isPlatformOwner) = ResolveScope(http);
        var today = DateTime.UtcNow.Date;

        var movementsQuery = db.AccountsReceivableMovements.AsNoTracking()
            .Where(x => x.DueDate.HasValue && x.DueDate.Value.Date < today
                        && x.ChargeAmount > 0m
                        && x.Status != "cancelled");

        if (!isPlatformOwner)
        {
            if (tenantId.HasValue) movementsQuery = movementsQuery.Where(x => x.TenantId == tenantId.Value);
            if (companyId.HasValue) movementsQuery = movementsQuery.Where(x => x.CompanyId == companyId.Value);
        }

        var aggregated = await movementsQuery
            .Join(db.Customers.AsNoTracking(),
                m => m.CustomerId,
                c => c.Id,
                (m, c) => new
                {
                    CustomerId = c.Id,
                    CustomerCode = c.Code,
                    CustomerName = c.Name,
                    m.DueDate,
                    Amount = m.ChargeAmount - m.CreditAmount
                })
            .GroupBy(x => new { x.CustomerId, x.CustomerCode, x.CustomerName })
            .Select(g => new AiOverdueRowDto
            {
                CustomerId = g.Key.CustomerId,
                CustomerCode = g.Key.CustomerCode,
                CustomerName = g.Key.CustomerName,
                OverdueAmount = g.Sum(x => x.Amount),
                OldestDueDate = g.Min(x => x.DueDate),
                OverdueDocuments = g.Count()
            })
            .Where(x => x.OverdueAmount > 0m)
            .OrderByDescending(x => x.OverdueAmount)
            .Take(50)
            .ToListAsync();

        return new AiOverdueBalancesDto
        {
            ReferenceDate = today,
            TotalOverdue = aggregated.Sum(x => x.OverdueAmount),
            Rows = aggregated
        };
    }
}

public sealed class AiTodaySummaryDto
{
    public DateTime ReferenceDate { get; set; }
    public int OrderCount { get; set; }
    public decimal OrderTotal { get; set; }
    public int InvoiceCount { get; set; }
    public decimal InvoiceTotal { get; set; }
}

public sealed class AiTodaySalesDto
{
    public DateTime ReferenceDate { get; set; }
    public int InvoiceCount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
}

public sealed class AiCustomerBalanceDto
{
    public string Query { get; set; } = string.Empty;
    public bool NotFound { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public decimal TotalBalance { get; set; }
    public List<AiCustomerBalanceRowDto> Rows { get; set; } = new();
}

public sealed class AiCustomerBalanceRowDto
{
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalCharges { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime? LastMovementAt { get; set; }
}

public sealed class AiPayrollPeriodSummaryDto
{
    public bool NotFound { get; set; }
    public Guid PeriodId { get; set; }
    public string PeriodCode { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public bool HasRuns { get; set; }
}

public sealed class AiOverdueBalancesDto
{
    public DateTime ReferenceDate { get; set; }
    public decimal TotalOverdue { get; set; }
    public List<AiOverdueRowDto> Rows { get; set; } = new();
}

public sealed class AiOverdueRowDto
{
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal OverdueAmount { get; set; }
    public DateTime? OldestDueDate { get; set; }
    public int OverdueDocuments { get; set; }
}

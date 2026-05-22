using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class AiAssistantEndpoints
{
    public static IEndpointRouteBuilder MapAiAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ia").WithTags("AI Assistant");

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

        group.MapPost("/ask", AskAsync);

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

        if (period is null)
        {
            return new AiPayrollPeriodSummaryDto { NotFound = true };
        }

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

    private static async Task<IResult> AskAsync(HttpContext http, NanchesoftDbContext db, AiAskRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Question))
        {
            return Results.Ok(new AiAskResponse
            {
                Intent = AiIntent.Unknown,
                Answer = "No encontré esa información, intenta preguntar de otra forma.",
                Echo = string.Empty,
                Suggestions = AiIntentInterpreter.DefaultSuggestions
            });
        }

        var question = request.Question.Trim();
        var (intent, argument) = AiIntentInterpreter.Interpret(question);

        switch (intent)
        {
            case AiIntent.TodaySummary:
                {
                    var data = await BuildTodaySummaryAsync(http, db);
                    var answer = $"Hoy llevas {data.OrderCount} pedido(s) por {FormatCurrency(data.OrderTotal)} y {data.InvoiceCount} factura(s) por {FormatCurrency(data.InvoiceTotal)}.";
                    return Results.Ok(new AiAskResponse
                    {
                        Intent = intent,
                        Answer = answer,
                        Echo = question,
                        Endpoint = "GET /api/ia/resumen-hoy",
                        Data = data
                    });
                }
            case AiIntent.TodaySales:
                {
                    var data = await BuildTodaySalesAsync(http, db);
                    var answer = data.InvoiceCount == 0
                        ? "Aún no se han registrado ventas facturadas hoy."
                        : $"Hoy se han vendido {FormatCurrency(data.Total)} en {data.InvoiceCount} factura(s) (subtotal {FormatCurrency(data.Subtotal)}, impuestos {FormatCurrency(data.Tax)}).";
                    return Results.Ok(new AiAskResponse
                    {
                        Intent = intent,
                        Answer = answer,
                        Echo = question,
                        Endpoint = "GET /api/ia/ventas-hoy",
                        Data = data
                    });
                }
            case AiIntent.CustomerBalance:
                {
                    var data = await BuildCustomerBalanceAsync(http, db, argument);
                    string answer;
                    if (data.NotFound)
                    {
                        answer = string.IsNullOrWhiteSpace(argument)
                            ? "No encontré clientes registrados con saldo."
                            : $"No encontré un cliente que coincida con \"{argument}\". Intenta con el nombre, código o RFC.";
                    }
                    else if (data.Rows.Count == 0)
                    {
                        answer = "No encontré saldos registrados para los clientes en este contexto.";
                    }
                    else if (!string.IsNullOrWhiteSpace(data.CustomerName))
                    {
                        var firstRow = data.Rows.FirstOrDefault();
                        answer = firstRow is null
                            ? $"El cliente {data.CustomerName} no tiene saldo registrado."
                            : $"El saldo del cliente {data.CustomerName} es {FormatCurrency(firstRow.CurrentBalance)} (cargos {FormatCurrency(firstRow.TotalCharges)}, créditos {FormatCurrency(firstRow.TotalCredits)}).";
                    }
                    else
                    {
                        answer = $"Hay {data.Rows.Count} cliente(s) con saldo abierto por un total de {FormatCurrency(data.TotalBalance)}. Te muestro los principales.";
                    }
                    return Results.Ok(new AiAskResponse
                    {
                        Intent = intent,
                        Answer = answer,
                        Echo = question,
                        Endpoint = "GET /api/ia/clientes/saldo",
                        Data = data
                    });
                }
            case AiIntent.PayrollOpenPeriod:
                {
                    var data = await BuildPayrollOpenPeriodAsync(http, db);
                    var answer = data.NotFound
                        ? "No hay periodos de nómina abiertos en este momento."
                        : data.HasRuns
                            ? $"En el periodo abierto {data.PeriodName} se pagará {FormatCurrency(data.NetAmount)} neto a {data.EmployeeCount} empleado(s) (percepciones {FormatCurrency(data.GrossAmount)}, deducciones {FormatCurrency(data.DeductionsAmount)})."
                            : $"El periodo abierto es {data.PeriodName} ({data.StartDate:dd/MM/yyyy} - {data.EndDate:dd/MM/yyyy}). Aún no hay nómina procesada; con los ajustes capturados se proyecta un neto de {FormatCurrency(data.NetAmount)}.";
                    return Results.Ok(new AiAskResponse
                    {
                        Intent = intent,
                        Answer = answer,
                        Echo = question,
                        Endpoint = "GET /api/ia/nomina/resumen-periodo",
                        Data = data
                    });
                }
            case AiIntent.OverdueCustomers:
                {
                    var data = await BuildOverdueCustomersAsync(http, db);
                    var answer = data.Rows.Count == 0
                        ? "No hay clientes con saldo vencido."
                        : $"Hay {data.Rows.Count} cliente(s) con saldo vencido por un total de {FormatCurrency(data.TotalOverdue)}. Te muestro los principales.";
                    return Results.Ok(new AiAskResponse
                    {
                        Intent = intent,
                        Answer = answer,
                        Echo = question,
                        Endpoint = "GET /api/ia/clientes/saldos-vencidos",
                        Data = data
                    });
                }
            default:
                return Results.Ok(new AiAskResponse
                {
                    Intent = AiIntent.Unknown,
                    Answer = "No encontré esa información, intenta preguntar de otra forma.",
                    Echo = question,
                    Suggestions = AiIntentInterpreter.DefaultSuggestions
                });
        }
    }

    private static string FormatCurrency(decimal value)
    {
        var culture = CultureInfo.GetCultureInfo("es-MX");
        return value.ToString("C2", culture);
    }
}

public enum AiIntent
{
    Unknown = 0,
    TodaySummary = 1,
    TodaySales = 2,
    CustomerBalance = 3,
    PayrollOpenPeriod = 4,
    OverdueCustomers = 5
}

internal static class AiIntentInterpreter
{
    public static readonly string[] DefaultSuggestions =
    {
        "¿Cuántos pedidos tengo hoy?",
        "¿Cuánto vendí hoy?",
        "¿Cuál es el saldo del cliente <nombre o código>?",
        "¿Cuánto se pagará de nómina en el periodo abierto?",
        "¿Qué clientes tienen saldo vencido?"
    };

    public static (AiIntent Intent, string? Argument) Interpret(string question)
    {
        var normalized = Normalize(question);

        if (ContainsAll(normalized, "saldo", "vencid") ||
            ContainsAll(normalized, "cartera", "vencid") ||
            ContainsAll(normalized, "cliente", "vencid") ||
            normalized.Contains("morosos"))
        {
            return (AiIntent.OverdueCustomers, null);
        }

        if (normalized.Contains("nomina") &&
            (ContainsAny(normalized, "periodo", "abierto", "pagar", "pago", "pagara")))
        {
            return (AiIntent.PayrollOpenPeriod, null);
        }

        if (normalized.Contains("saldo") &&
            (normalized.Contains("cliente") || normalized.Contains(" del ") || normalized.Contains(" de ")))
        {
            var argument = ExtractCustomerArgument(question);
            return (AiIntent.CustomerBalance, argument);
        }

        if (ContainsAny(normalized, "vendi", "venta", "ventas", "facturado", "facturacion") &&
            ContainsAny(normalized, "hoy", "dia"))
        {
            return (AiIntent.TodaySales, null);
        }

        if (ContainsAny(normalized, "pedido", "pedidos", "orden", "ordenes", "resumen") &&
            ContainsAny(normalized, "hoy", "dia"))
        {
            return (AiIntent.TodaySummary, null);
        }

        if (normalized.Contains("vend") && !normalized.Contains("ayer"))
        {
            return (AiIntent.TodaySales, null);
        }

        return (AiIntent.Unknown, null);
    }

    private static string ExtractCustomerArgument(string question)
    {
        var trimmed = question.Trim();

        var anchors = new[]
        {
            "saldo del cliente ", "saldo de cliente ", "saldo del ", "saldo de ", "del cliente ", "cliente "
        };

        foreach (var anchor in anchors)
        {
            var idx = trimmed.IndexOf(anchor, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var rest = trimmed[(idx + anchor.Length)..].Trim();
                rest = rest.TrimEnd('?', '¿', '.', '!', '¡').Trim();
                if (!string.IsNullOrWhiteSpace(rest))
                {
                    return rest;
                }
            }
        }

        return string.Empty;
    }

    private static bool ContainsAll(string text, params string[] tokens)
        => tokens.All(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));

    private static bool ContainsAny(string text, params string[] tokens)
        => tokens.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));

    private static string Normalize(string value)
    {
        var lowered = value.ToLowerInvariant();
        var formD = lowered.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }
        return sb.ToString();
    }
}

public sealed class AiAskRequest
{
    public string Question { get; set; } = string.Empty;
}

public sealed class AiAskResponse
{
    public AiIntent Intent { get; set; }
    public string Answer { get; set; } = string.Empty;
    public string Echo { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public object? Data { get; set; }
    public IReadOnlyList<string> Suggestions { get; set; } = Array.Empty<string>();
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

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class FinanceEndpoints
{
    public static IEndpointRouteBuilder MapFinanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance").WithTags("Finance");

        group.MapGet("/dashboard", async (NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var cashBalance = await db.CashAccounts.AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => (decimal?)x.CurrentBalance)
                .SumAsync() ?? 0m;

            var bankBalance = await db.BankAccounts.AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => (decimal?)x.CurrentBalance)
                .SumAsync() ?? 0m;

            var salesInvoices = await db.SalesInvoices.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted")
                .ToListAsync();

            var purchaseInvoices = await db.PurchaseInvoices.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted")
                .ToListAsync();

            var receipts = await db.Receipts.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted")
                .ToListAsync();

            var payments = await db.Payments.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted")
                .ToListAsync();

            var creditNotes = await db.CreditNotes.AsNoTracking()
                .Where(x => x.IsActive && x.Status != "cancelled")
                .ToListAsync();

            var receiptApplications = await db.ReceiptApplications.AsNoTracking()
                .Where(x => x.IsActive && x.Status != "cancelled")
                .ToListAsync();

            var draftEntries = await db.Set<AccountingJournalEntry>().AsNoTracking()
                .CountAsync(x => x.IsActive && x.Status == "draft");

            var postedEntries = await db.Set<AccountingJournalEntry>().AsNoTracking()
                .CountAsync(x => x.IsActive && x.Status == "posted");

            var openPeriods = await db.Set<AccountingFiscalPeriod>().AsNoTracking()
                .CountAsync(x => x.IsActive && x.Status == "open");

            var openReceivables = Math.Max(0m,
                salesInvoices.Sum(x => x.Total)
                - creditNotes.Sum(x => x.Total)
                - receiptApplications.Sum(x => x.AppliedAmount));

            var openPayables = Math.Max(0m,
                purchaseInvoices.Sum(x => x.Total)
                - payments.Sum(x => x.Total));

            var companyId = await GetPrimaryCompanyIdAsync(db);
            var budgetRows = companyId.HasValue
                ? (await LoadBudgetRowsAsync(environment, companyId.Value)).Where(x => x.Year == today.Year).ToList()
                : new List<FinanceBudgetRowDto>();
            var monthBudget = budgetRows.Where(x => x.Month == today.Month).Sum(x => x.BudgetAmount);
            var actualMonth = 0m;
            if (companyId.HasValue && budgetRows.Count > 0)
            {
                var actualMap = await BuildActualsByAccountMonthAsync(db, companyId.Value, today.Year);
                actualMonth = budgetRows.Where(x => x.Month == today.Month)
                    .Sum(x => actualMap.TryGetValue((x.AccountId, x.Month), out var value) ? value : 0m);
            }
            var monthVariance = actualMonth - monthBudget;
            var budgetCompliance = monthBudget == 0m ? 0m : Math.Round((actualMonth / monthBudget) * 100m, 2);
            var goalRows = companyId.HasValue
                ? (await LoadGoalRowsAsync(environment, companyId.Value)).Where(x => x.Year == today.Year).ToList()
                : new List<FinanceGoalRowDto>();
            var approvalRows = companyId.HasValue
                ? await BuildApprovalRowsAsync(db, environment, companyId.Value, today)
                : new List<FinanceApprovalRowDto>();
            var alertRows = companyId.HasValue
                ? await BuildAlertRowsAsync(db, environment, companyId.Value, today)
                : new List<FinanceAlertRowDto>();
            var semaphoreRows = companyId.HasValue
                ? await BuildSemaphoreRowsAsync(db, environment, companyId.Value, today)
                : new List<FinanceSemaphoreRowDto>();

            var dto = new FinanceDashboardDto
            {
                CashBalance = cashBalance,
                BankBalance = bankBalance,
                TotalLiquidity = cashBalance + bankBalance,
                OpenReceivables = openReceivables,
                OpenPayables = openPayables,
                WorkingCapital = (cashBalance + bankBalance) + openReceivables - openPayables,
                SalesThisMonth = salesInvoices.Where(x => x.InvoiceDate >= monthStart).Sum(x => x.Total),
                PurchasesThisMonth = purchaseInvoices.Where(x => x.InvoiceDate >= monthStart).Sum(x => x.Total),
                ReceiptsThisMonth = receipts.Where(x => x.ReceiptDate >= monthStart).Sum(x => x.Total),
                PaymentsThisMonth = payments.Where(x => x.PaymentDate >= monthStart).Sum(x => x.Total),
                DraftJournalEntries = draftEntries,
                PostedJournalEntries = postedEntries,
                OpenPeriods = openPeriods,
                BudgetThisMonth = monthBudget,
                ActualThisMonth = actualMonth,
                BudgetVarianceThisMonth = monthVariance,
                BudgetCompliancePercent = budgetCompliance,
                BudgetRowsThisYear = budgetRows.Count,
                GoalRowsThisYear = goalRows.Count,
                PendingApprovals = approvalRows.Count(x => x.Status == "pending"),
                ActiveAlerts = alertRows.Count,
                RedSemaphores = semaphoreRows.Count(x => string.Equals(x.Semaphore, "Rojo", StringComparison.OrdinalIgnoreCase)),
                AmberSemaphores = semaphoreRows.Count(x => string.Equals(x.Semaphore, "Amarillo", StringComparison.OrdinalIgnoreCase)),
                Today = today
            };

            return Results.Ok(dto);
        });

        group.MapGet("/cash-flow", async (int? weeks, NanchesoftDbContext db) =>
        {
            var bucketCount = weeks.GetValueOrDefault(12);
            if (bucketCount <= 0)
            {
                bucketCount = 12;
            }

            if (bucketCount > 26)
            {
                bucketCount = 26;
            }

            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            if (today.DayOfWeek == DayOfWeek.Sunday)
            {
                weekStart = today.AddDays(-6);
            }

            var cashBalance = await db.CashAccounts.AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => (decimal?)x.CurrentBalance)
                .SumAsync() ?? 0m;

            var bankBalance = await db.BankAccounts.AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => (decimal?)x.CurrentBalance)
                .SumAsync() ?? 0m;

            var customers = await db.Customers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x);
            var suppliers = await db.Suppliers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x);
            var receiptApplicationsByInvoice = await db.ReceiptApplications.AsNoTracking()
                .Where(x => x.IsActive && x.Status != "cancelled")
                .GroupBy(x => x.SalesInvoiceId)
                .ToDictionaryAsync(x => x.Key, x => x.Sum(y => y.AppliedAmount));

            var salesInvoices = await db.SalesInvoices.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted" && x.CustomerId != null)
                .ToListAsync();

            var purchaseInvoices = await db.PurchaseInvoices.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted" && x.SupplierId != null)
                .ToListAsync();

            var receivableFlows = salesInvoices
                .Select(invoice =>
                {
                    var applied = receiptApplicationsByInvoice.TryGetValue(invoice.Id, out var amount) ? amount : 0m;
                    var openAmount = Math.Max(0m, invoice.Total - applied);
                    var term = invoice.CustomerId.HasValue && customers.TryGetValue(invoice.CustomerId.Value, out var customer)
                        ? Math.Max(0, customer.PaymentTermDays)
                        : 15;

                    return new ForecastMovement
                    {
                        DueDate = invoice.InvoiceDate.Date.AddDays(term),
                        Amount = openAmount,
                        Type = "inflow"
                    };
                })
                .Where(x => x.Amount > 0m)
                .ToList();

            var payableFlows = purchaseInvoices
                .Select(invoice =>
                {
                    var term = invoice.SupplierId.HasValue && suppliers.TryGetValue(invoice.SupplierId.Value, out var supplier)
                        ? Math.Max(0, supplier.PaymentTermDays)
                        : 30;

                    return new ForecastMovement
                    {
                        DueDate = invoice.InvoiceDate.Date.AddDays(term),
                        Amount = Math.Max(0m, invoice.Total),
                        Type = "outflow"
                    };
                })
                .Where(x => x.Amount > 0m)
                .ToList();

            var rows = new List<FinanceCashFlowRowDto>();
            var running = cashBalance + bankBalance;

            for (var index = 0; index < bucketCount; index++)
            {
                var bucketStart = weekStart.AddDays(index * 7);
                var bucketEnd = bucketStart.AddDays(6);
                var opening = running;
                var inflows = receivableFlows.Where(x => x.DueDate >= bucketStart && x.DueDate <= bucketEnd).Sum(x => x.Amount);
                var outflows = payableFlows.Where(x => x.DueDate >= bucketStart && x.DueDate <= bucketEnd).Sum(x => x.Amount);
                var net = inflows - outflows;
                running += net;

                rows.Add(new FinanceCashFlowRowDto
                {
                    PeriodStart = bucketStart,
                    PeriodEnd = bucketEnd,
                    OpeningBalance = opening,
                    ExpectedInflows = inflows,
                    ExpectedOutflows = outflows,
                    NetFlow = net,
                    ProjectedClosing = running
                });
            }

            return Results.Ok(rows);
        });

        group.MapGet("/document-control", async (NanchesoftDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;
            var receiptApplicationsByInvoice = await db.ReceiptApplications.AsNoTracking()
                .Where(x => x.IsActive && x.Status != "cancelled")
                .GroupBy(x => x.SalesInvoiceId)
                .ToDictionaryAsync(x => x.Key, x => x.Sum(y => y.AppliedAmount));

            var customerMap = await db.Customers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x);
            var supplierMap = await db.Suppliers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x);
            var entries = await db.Set<AccountingJournalEntry>().AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.EntryDate)
                .ToListAsync();

            var rows = new List<FinanceDocumentControlRowDto>();

            var salesInvoices = await db.SalesInvoices.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.InvoiceDate)
                .ToListAsync();

            foreach (var invoice in salesInvoices)
            {
                var accounting = FindAccountingInfo(entries, invoice.Folio);
                var applied = receiptApplicationsByInvoice.TryGetValue(invoice.Id, out var amount) ? amount : 0m;
                var openAmount = Math.Max(0m, invoice.Total - applied);
                var dueDate = invoice.CustomerId.HasValue && customerMap.TryGetValue(invoice.CustomerId.Value, out var customer)
                    ? invoice.InvoiceDate.Date.AddDays(Math.Max(0, customer.PaymentTermDays))
                    : invoice.InvoiceDate.Date.AddDays(15);

                rows.Add(new FinanceDocumentControlRowDto
                {
                    Module = "Ventas",
                    DocumentType = "sales_invoice",
                    DocumentId = invoice.Id,
                    Folio = invoice.Folio,
                    DocumentDate = invoice.InvoiceDate,
                    PartnerName = invoice.CustomerId.HasValue && customerMap.TryGetValue(invoice.CustomerId.Value, out var salesCustomer)
                        ? salesCustomer.Name
                        : string.Empty,
                    Status = invoice.Status,
                    Total = invoice.Total,
                    OpenAmount = openAmount,
                    DueDate = dueDate,
                    AgeDays = (today - invoice.InvoiceDate.Date).Days,
                    IsAccounted = accounting.IsAccounted,
                    AccountingStatus = accounting.Status,
                    Reference = invoice.Notes
                });
            }

            var purchaseInvoices = await db.PurchaseInvoices.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.InvoiceDate)
                .ToListAsync();

            foreach (var invoice in purchaseInvoices)
            {
                var accounting = FindAccountingInfo(entries, invoice.Folio);
                var dueDate = invoice.SupplierId.HasValue && supplierMap.TryGetValue(invoice.SupplierId.Value, out var supplier)
                    ? invoice.InvoiceDate.Date.AddDays(Math.Max(0, supplier.PaymentTermDays))
                    : invoice.InvoiceDate.Date.AddDays(30);

                rows.Add(new FinanceDocumentControlRowDto
                {
                    Module = "Compras",
                    DocumentType = "purchase_invoice",
                    DocumentId = invoice.Id,
                    Folio = invoice.Folio,
                    DocumentDate = invoice.InvoiceDate,
                    PartnerName = invoice.SupplierId.HasValue && supplierMap.TryGetValue(invoice.SupplierId.Value, out var purchaseSupplier)
                        ? purchaseSupplier.Name
                        : string.Empty,
                    Status = invoice.Status,
                    Total = invoice.Total,
                    OpenAmount = Math.Max(0m, invoice.Total),
                    DueDate = dueDate,
                    AgeDays = (today - invoice.InvoiceDate.Date).Days,
                    IsAccounted = accounting.IsAccounted,
                    AccountingStatus = accounting.Status,
                    Reference = string.IsNullOrWhiteSpace(invoice.SupplierInvoiceFolio) ? invoice.Notes : invoice.SupplierInvoiceFolio
                });
            }

            var receipts = await db.Receipts.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.ReceiptDate)
                .ToListAsync();

            foreach (var receipt in receipts)
            {
                var accounting = FindAccountingInfo(entries, receipt.Folio);
                rows.Add(new FinanceDocumentControlRowDto
                {
                    Module = "Tesorería",
                    DocumentType = "receipt",
                    DocumentId = receipt.Id,
                    Folio = receipt.Folio,
                    DocumentDate = receipt.ReceiptDate,
                    PartnerName = receipt.CustomerId.HasValue && customerMap.TryGetValue(receipt.CustomerId.Value, out var receiptCustomer)
                        ? receiptCustomer.Name
                        : string.Empty,
                    Status = receipt.Status,
                    Total = receipt.Total,
                    OpenAmount = 0m,
                    DueDate = null,
                    AgeDays = (today - receipt.ReceiptDate.Date).Days,
                    IsAccounted = accounting.IsAccounted,
                    AccountingStatus = accounting.Status,
                    Reference = receipt.Reference
                });
            }

            var payments = await db.Payments.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.PaymentDate)
                .ToListAsync();

            foreach (var payment in payments)
            {
                var accounting = FindAccountingInfo(entries, payment.Folio);
                rows.Add(new FinanceDocumentControlRowDto
                {
                    Module = "Tesorería",
                    DocumentType = "payment",
                    DocumentId = payment.Id,
                    Folio = payment.Folio,
                    DocumentDate = payment.PaymentDate,
                    PartnerName = payment.SupplierId.HasValue && supplierMap.TryGetValue(payment.SupplierId.Value, out var paymentSupplier)
                        ? paymentSupplier.Name
                        : string.Empty,
                    Status = payment.Status,
                    Total = payment.Total,
                    OpenAmount = 0m,
                    DueDate = null,
                    AgeDays = (today - payment.PaymentDate.Date).Days,
                    IsAccounted = accounting.IsAccounted,
                    AccountingStatus = accounting.Status,
                    Reference = payment.Reference
                });
            }

            return Results.Ok(rows
                .OrderByDescending(x => x.DocumentDate)
                .ThenByDescending(x => x.Total)
                .Take(300)
                .ToList());
        });

        group.MapGet("/exceptions", async (NanchesoftDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;
            var rows = new List<FinanceExceptionRowDto>();

            var documentRows = await BuildDocumentControlRowsAsync(db, today);
            rows.AddRange(documentRows
                .Where(x => x.Status == "posted" && !x.IsAccounted)
                .Select(x => new FinanceExceptionRowDto
                {
                    Severity = "Alta",
                    Category = "Contabilidad",
                    Module = x.Module,
                    Folio = x.Folio,
                    DocumentId = x.DocumentId,
                    DocumentDate = x.DocumentDate,
                    Amount = x.Total,
                    Message = $"{x.DocumentType} posteado sin póliza contable detectada.",
                    SuggestedAction = "Generar o revisar la póliza automática del documento."
                }));

            rows.AddRange(documentRows
                .Where(x => x.DocumentType == "sales_invoice" && x.OpenAmount > 0m && x.AgeDays > 30)
                .Select(x => new FinanceExceptionRowDto
                {
                    Severity = "Media",
                    Category = "Cobranza",
                    Module = x.Module,
                    Folio = x.Folio,
                    DocumentId = x.DocumentId,
                    DocumentDate = x.DocumentDate,
                    Amount = x.OpenAmount,
                    Message = "Factura con saldo abierto y antigüedad mayor a 30 días.",
                    SuggestedAction = "Revisar cobranza, aplicación de recibos o nota de crédito."
                }));

            rows.AddRange(documentRows
                .Where(x => x.DocumentType == "purchase_invoice" && x.OpenAmount > 0m && x.AgeDays > 30)
                .Select(x => new FinanceExceptionRowDto
                {
                    Severity = "Media",
                    Category = "Pagos",
                    Module = x.Module,
                    Folio = x.Folio,
                    DocumentId = x.DocumentId,
                    DocumentDate = x.DocumentDate,
                    Amount = x.OpenAmount,
                    Message = "Factura de proveedor pendiente con antigüedad mayor a 30 días.",
                    SuggestedAction = "Programar pago o documentar reprogramación."
                }));

            var negativeCash = await db.CashAccounts.AsNoTracking()
                .Where(x => x.IsActive && x.CurrentBalance < 0m)
                .Select(x => new { x.Code, x.Name, x.CurrentBalance })
                .ToListAsync();

            rows.AddRange(negativeCash.Select(x => new FinanceExceptionRowDto
            {
                Severity = "Alta",
                Category = "Tesorería",
                Module = "Tesorería",
                Folio = x.Code,
                DocumentDate = null,
                Amount = x.CurrentBalance,
                Message = $"Caja '{x.Name}' con saldo negativo.",
                SuggestedAction = "Revisar movimientos, ajustes o posteos pendientes."
            }));

            var negativeBanks = await db.BankAccounts.AsNoTracking()
                .Where(x => x.IsActive && x.CurrentBalance < 0m)
                .Select(x => new { x.Code, x.Name, x.CurrentBalance })
                .ToListAsync();

            rows.AddRange(negativeBanks.Select(x => new FinanceExceptionRowDto
            {
                Severity = "Alta",
                Category = "Tesorería",
                Module = "Tesorería",
                Folio = x.Code,
                DocumentDate = null,
                Amount = x.CurrentBalance,
                Message = $"Banco '{x.Name}' con saldo negativo.",
                SuggestedAction = "Conciliar movimientos y revisar egresos/pagos."
            }));

            var staleDraftEntries = await db.Set<AccountingJournalEntry>().AsNoTracking()
                .Where(x => x.IsActive && x.Status == "draft" && x.EntryDate.Date < today.AddDays(-7))
                .OrderBy(x => x.EntryDate)
                .Select(x => new { x.Id, x.Folio, x.EntryDate, x.TotalDebit })
                .ToListAsync();

            rows.AddRange(staleDraftEntries.Select(x => new FinanceExceptionRowDto
            {
                Severity = "Baja",
                Category = "Contabilidad",
                Module = "Contabilidad",
                Folio = x.Folio,
                DocumentId = x.Id,
                DocumentDate = x.EntryDate,
                Amount = x.TotalDebit,
                Message = "Póliza en borrador con más de 7 días sin aprobar/postear.",
                SuggestedAction = "Revisar autorización contable y cerrar el ciclo del periodo."
            }));

            return Results.Ok(rows
                .OrderByDescending(x => SeverityRank(x.Severity))
                .ThenByDescending(x => x.DocumentDate)
                .Take(300)
                .ToList());
        });


        group.MapGet("/budgets/lookups", async (NanchesoftDbContext db) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (companyId is null)
            {
                return Results.Ok(new List<FinanceBudgetLookupItemDto>());
            }

            var rows = await db.Set<AccountingAccount>().AsNoTracking()
                .Where(x => x.CompanyId == companyId.Value && x.IsActive && x.AllowsPosting)
                .OrderBy(x => x.Code)
                .Select(x => new FinanceBudgetLookupItemDto
                {
                    AccountId = x.Id,
                    AccountCode = x.Code,
                    AccountName = x.Name,
                    AccountType = x.AccountType,
                    Nature = x.Nature
                })
                .ToListAsync();

            return Results.Ok(rows);
        });

        group.MapGet("/budgets", async (int? year, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var targetYear = year.GetValueOrDefault(DateTime.UtcNow.Year);
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (companyId is null)
            {
                return Results.Ok(new List<FinanceBudgetRowDto>());
            }

            var rows = await LoadBudgetRowsAsync(environment, companyId.Value);
            return Results.Ok(rows.Where(x => x.Year == targetYear).OrderBy(x => x.Month).ThenBy(x => x.AccountCode).ToList());
        });

        group.MapPost("/budgets/save", async (FinanceBudgetSaveRequestDto request, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (companyId is null)
            {
                return Results.BadRequest("No existe empresa base para guardar presupuestos.");
            }

            var existing = await LoadBudgetRowsAsync(environment, companyId.Value);
            var preserved = existing.Where(x => x.Year != request.Year).ToList();
            var cleaned = request.Rows
                .Where(x => x.Year == request.Year && x.Month >= 1 && x.Month <= 12 && x.AccountId != Guid.Empty)
                .Select(x =>
                {
                    x.Id = x.Id == Guid.Empty ? Guid.NewGuid() : x.Id;
                    x.BudgetAmount = Math.Round(x.BudgetAmount, 2);
                    x.AccountCode = x.AccountCode?.Trim() ?? string.Empty;
                    x.AccountName = x.AccountName?.Trim() ?? string.Empty;
                    x.AccountType = x.AccountType?.Trim() ?? string.Empty;
                    x.Nature = x.Nature?.Trim() ?? string.Empty;
                    x.Notes = x.Notes?.Trim() ?? string.Empty;
                    return x;
                })
                .GroupBy(x => new { x.Year, x.Month, x.AccountId })
                .Select(x => x.Last())
                .OrderBy(x => x.Month)
                .ThenBy(x => x.AccountCode)
                .ToList();

            preserved.AddRange(cleaned);
            await SaveBudgetRowsAsync(environment, companyId.Value, preserved);
            return Results.Ok(new { saved = cleaned.Count, year = request.Year });
        });

        group.MapGet("/budgets/vs-actual", async (int? year, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var targetYear = year.GetValueOrDefault(DateTime.UtcNow.Year);
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (companyId is null)
            {
                return Results.Ok(new List<FinanceBudgetVsActualRowDto>());
            }

            var budgets = (await LoadBudgetRowsAsync(environment, companyId.Value))
                .Where(x => x.Year == targetYear)
                .OrderBy(x => x.Month)
                .ThenBy(x => x.AccountCode)
                .ToList();

            var actuals = await BuildActualsByAccountMonthAsync(db, companyId.Value, targetYear);
            var rows = budgets.Select(x =>
            {
                var actual = actuals.TryGetValue((x.AccountId, x.Month), out var amount) ? amount : 0m;
                var variance = actual - x.BudgetAmount;
                var compliance = x.BudgetAmount == 0m ? 0m : Math.Round((actual / x.BudgetAmount) * 100m, 2);
                return new FinanceBudgetVsActualRowDto
                {
                    Year = x.Year,
                    Month = x.Month,
                    AccountId = x.AccountId,
                    AccountCode = x.AccountCode,
                    AccountName = x.AccountName,
                    AccountType = x.AccountType,
                    Nature = x.Nature,
                    BudgetAmount = x.BudgetAmount,
                    ActualAmount = actual,
                    VarianceAmount = variance,
                    CompliancePercent = compliance,
                    Notes = x.Notes
                };
            }).ToList();

            return Results.Ok(rows);
        });

        group.MapGet("/goals/metrics", () => Results.Ok(new List<FinanceGoalMetricItemDto>
        {
            new() { MetricCode = "sales", MetricName = "Ventas" },
            new() { MetricCode = "purchases", MetricName = "Compras" },
            new() { MetricCode = "receipts", MetricName = "Cobros" },
            new() { MetricCode = "payments", MetricName = "Pagos" }
        }));

        group.MapGet("/goals", async (int? year, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var targetYear = year.GetValueOrDefault(DateTime.UtcNow.Year);
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (companyId is null)
            {
                return Results.Ok(new List<FinanceGoalRowDto>());
            }

            var rows = await LoadGoalRowsAsync(environment, companyId.Value);
            return Results.Ok(rows.Where(x => x.Year == targetYear).OrderBy(x => x.Month).ThenBy(x => x.MetricName).ToList());
        });

        group.MapPost("/goals/save", async (FinanceGoalSaveRequestDto request, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (companyId is null)
            {
                return Results.BadRequest("No existe empresa base para guardar metas.");
            }

            var existing = await LoadGoalRowsAsync(environment, companyId.Value);
            var preserved = existing.Where(x => x.Year != request.Year).ToList();
            var cleaned = request.Rows
                .Where(x => x.Year == request.Year && x.Month >= 1 && x.Month <= 12 && !string.IsNullOrWhiteSpace(x.MetricCode))
                .Select(x =>
                {
                    x.Id = x.Id == Guid.Empty ? Guid.NewGuid() : x.Id;
                    x.MetricCode = x.MetricCode.Trim().ToLowerInvariant();
                    x.MetricName = x.MetricName?.Trim() ?? string.Empty;
                    x.TargetAmount = Math.Round(x.TargetAmount, 2);
                    x.Notes = x.Notes?.Trim() ?? string.Empty;
                    return x;
                })
                .GroupBy(x => new { x.Year, x.Month, x.MetricCode })
                .Select(x => x.Last())
                .OrderBy(x => x.Month)
                .ThenBy(x => x.MetricName)
                .ToList();

            preserved.AddRange(cleaned);
            await SaveGoalRowsAsync(environment, companyId.Value, preserved);
            return Results.Ok(new { saved = cleaned.Count, year = request.Year });
        });

        group.MapGet("/goals/progress", async (int? year, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var targetYear = year.GetValueOrDefault(DateTime.UtcNow.Year);
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (companyId is null)
            {
                return Results.Ok(new List<FinanceGoalProgressRowDto>());
            }

            var goals = (await LoadGoalRowsAsync(environment, companyId.Value))
                .Where(x => x.Year == targetYear)
                .OrderBy(x => x.Month)
                .ThenBy(x => x.MetricName)
                .ToList();

            var metrics = goals.Select(x => x.MetricCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var actualMaps = new Dictionary<string, Dictionary<int, decimal>>(StringComparer.OrdinalIgnoreCase);
            foreach (var metric in metrics)
            {
                actualMaps[metric] = await BuildGoalActualsByMonthAsync(db, targetYear, metric);
            }

            var rows = goals.Select(x =>
            {
                var actual = actualMaps.TryGetValue(x.MetricCode, out var map) && map.TryGetValue(x.Month, out var value)
                    ? value
                    : 0m;
                var variance = actual - x.TargetAmount;
                var compliance = x.TargetAmount == 0m ? 0m : Math.Round((actual / x.TargetAmount) * 100m, 2);
                return new FinanceGoalProgressRowDto
                {
                    Year = x.Year,
                    Month = x.Month,
                    MetricCode = x.MetricCode,
                    MetricName = x.MetricName,
                    TargetAmount = x.TargetAmount,
                    ActualAmount = actual,
                    VarianceAmount = variance,
                    CompliancePercent = compliance,
                    Notes = x.Notes
                };
            }).ToList();

            return Results.Ok(rows);
        });

        group.MapGet("/approvals/summary", async (NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (companyId is null)
            {
                return Results.Ok(new FinanceApprovalSummaryDto());
            }

            var today = DateTime.UtcNow.Date;
            var rows = await BuildApprovalRowsAsync(db, environment, companyId.Value, today);
            return Results.Ok(new FinanceApprovalSummaryDto
            {
                PendingCount = rows.Count(x => x.Status == "pending"),
                AuthorizedCount = rows.Count(x => x.Status == "authorized"),
                RejectedCount = rows.Count(x => x.Status == "rejected"),
                HighPriorityCount = rows.Count(x => string.Equals(x.Priority, "Alta", StringComparison.OrdinalIgnoreCase)),
                OverdueCount = rows.Count(x => x.DueDate.HasValue && x.DueDate.Value.Date < today && x.OpenAmount > 0m)
            });
        });

        group.MapGet("/approvals/pending", async (string? status, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (companyId is null)
            {
                return Results.Ok(new List<FinanceApprovalRowDto>());
            }

            var rows = await BuildApprovalRowsAsync(db, environment, companyId.Value, DateTime.UtcNow.Date);
            var requestedStatus = string.IsNullOrWhiteSpace(status) ? "pending" : status.Trim().ToLowerInvariant();
            if (requestedStatus != "all")
            {
                rows = rows.Where(x => string.Equals(x.Status, requestedStatus, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return Results.Ok(rows
                .OrderByDescending(x => PriorityRank(x.Priority))
                .ThenByDescending(x => x.AgeDays)
                .ThenByDescending(x => x.Amount)
                .Take(300)
                .ToList());
        });

        group.MapPost("/approvals/{module}/{documentId:guid}/authorize", async (string module, Guid documentId, FinanceApprovalDecisionRequestDto request, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (companyId is null)
            {
                return Results.BadRequest("No existe empresa base para gestionar autorizaciones.");
            }

            var rows = await BuildApprovalRowsAsync(db, environment, companyId.Value, DateTime.UtcNow.Date);
            var target = rows.FirstOrDefault(x => x.DocumentId == documentId && string.Equals(x.ModuleKey, module, StringComparison.OrdinalIgnoreCase));
            if (target is null)
            {
                return Results.NotFound();
            }

            await UpsertApprovalStateAsync(environment, companyId.Value, new FinanceApprovalStateRow
            {
                ModuleKey = target.ModuleKey,
                DocumentType = target.DocumentType,
                DocumentId = target.DocumentId,
                Status = "authorized",
                Comments = request.Comments?.Trim() ?? string.Empty,
                UpdatedAt = DateTime.UtcNow
            });

            return Results.Ok(new { saved = true, status = "authorized", documentId });
        });

        group.MapPost("/approvals/{module}/{documentId:guid}/reject", async (string module, Guid documentId, FinanceApprovalDecisionRequestDto request, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (companyId is null)
            {
                return Results.BadRequest("No existe empresa base para gestionar autorizaciones.");
            }

            var rows = await BuildApprovalRowsAsync(db, environment, companyId.Value, DateTime.UtcNow.Date);
            var target = rows.FirstOrDefault(x => x.DocumentId == documentId && string.Equals(x.ModuleKey, module, StringComparison.OrdinalIgnoreCase));
            if (target is null)
            {
                return Results.NotFound();
            }

            await UpsertApprovalStateAsync(environment, companyId.Value, new FinanceApprovalStateRow
            {
                ModuleKey = target.ModuleKey,
                DocumentType = target.DocumentType,
                DocumentId = target.DocumentId,
                Status = "rejected",
                Comments = request.Comments?.Trim() ?? string.Empty,
                UpdatedAt = DateTime.UtcNow
            });

            return Results.Ok(new { saved = true, status = "rejected", documentId });
        });

        group.MapGet("/alerts", async (NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (companyId is null)
            {
                return Results.Ok(new List<FinanceAlertRowDto>());
            }

            var rows = await BuildAlertRowsAsync(db, environment, companyId.Value, DateTime.UtcNow.Date);
            return Results.Ok(rows
                .OrderByDescending(x => SeverityRank(x.Severity))
                .ThenBy(x => x.DaysToDue)
                .ThenByDescending(x => x.Amount)
                .Take(300)
                .ToList());
        });

        group.MapGet("/semaphores", async (NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (companyId is null)
            {
                return Results.Ok(new List<FinanceSemaphoreRowDto>());
            }

            var rows = await BuildSemaphoreRowsAsync(db, environment, companyId.Value, DateTime.UtcNow.Date);
            return Results.Ok(rows);
        });



        group.MapGet("/collections-calendar", async (DateTime? from, DateTime? to, NanchesoftDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;
            var start = (from?.Date ?? today.AddDays(-7));
            var end = (to?.Date ?? today.AddDays(45));
            if (end < start)
            {
                (start, end) = (end, start);
            }

            var rows = await BuildCollectionsCalendarRowsAsync(db, start, end, today);
            return Results.Ok(rows);
        });

        group.MapGet("/payments-calendar", async (DateTime? from, DateTime? to, NanchesoftDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;
            var start = (from?.Date ?? today.AddDays(-7));
            var end = (to?.Date ?? today.AddDays(45));
            if (end < start)
            {
                (start, end) = (end, start);
            }

            var rows = await BuildPaymentsCalendarRowsAsync(db, start, end, today);
            return Results.Ok(rows);
        });

        group.MapGet("/scenarios", async (NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (!companyId.HasValue)
            {
                return Results.Ok(new List<FinanceScenarioRowDto>());
            }

            var rows = await LoadScenarioRowsAsync(environment, companyId.Value);
            return Results.Ok(rows.OrderBy(x => x.Name).ThenByDescending(x => x.UpdatedAt).ToList());
        });

        group.MapPost("/scenarios/save", async (FinanceScenarioSaveRequestDto request, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (!companyId.HasValue)
            {
                return Results.BadRequest("No existe empresa para guardar escenarios.");
            }

            var rows = (request.Rows ?? new List<FinanceScenarioRowDto>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .Select(x =>
                {
                    x.Id = x.Id == Guid.Empty ? Guid.NewGuid() : x.Id;
                    x.Name = x.Name.Trim();
                    x.Notes = x.Notes?.Trim() ?? string.Empty;
                    x.HorizonWeeks = x.HorizonWeeks <= 0 ? 8 : Math.Min(26, x.HorizonWeeks);
                    x.UpdatedAt = DateTime.UtcNow;
                    return x;
                })
                .OrderBy(x => x.Name)
                .ToList();

            await SaveScenarioRowsAsync(environment, companyId.Value, rows);
            return Results.Ok(new { Saved = rows.Count });
        });

        group.MapPost("/scenarios/evaluate", async (FinanceScenarioEvaluationRequestDto request, NanchesoftDbContext db) =>
        {
            var scenario = request.Scenario ?? new FinanceScenarioRowDto
            {
                Name = "Escenario",
                HorizonWeeks = 8
            };

            scenario.HorizonWeeks = scenario.HorizonWeeks <= 0 ? 8 : Math.Min(26, scenario.HorizonWeeks);
            var evaluation = await EvaluateScenarioAsync(db, scenario, DateTime.UtcNow.Date);
            return Results.Ok(evaluation);
        });


        group.MapGet("/collection-commitments", async (DateTime? from, DateTime? to, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (!companyId.HasValue)
            {
                return Results.Ok(new List<FinanceCollectionCommitmentRowDto>());
            }

            var today = DateTime.UtcNow.Date;
            var start = from?.Date ?? today.AddDays(-7);
            var end = to?.Date ?? today.AddDays(45);
            if (end < start)
            {
                (start, end) = (end, start);
            }

            var rows = await BuildCollectionCommitmentRowsAsync(db, environment, companyId.Value, start, end, today);
            return Results.Ok(rows);
        });

        group.MapPost("/collection-commitments/save", async (FinanceCollectionCommitmentSaveRequestDto request, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (!companyId.HasValue)
            {
                return Results.BadRequest("No existe empresa para guardar compromisos de cobro.");
            }

            var rows = (request.Rows ?? new List<FinanceCollectionCommitmentRowDto>())
                .Where(x => x.SalesInvoiceId != Guid.Empty)
                .Select(x => new FinanceCollectionCommitmentStateRow
                {
                    DocumentId = x.SalesInvoiceId,
                    PlannedDate = x.PlannedCollectionDate?.Date,
                    Status = string.IsNullOrWhiteSpace(x.CommitmentStatus) ? "Sin gestionar" : x.CommitmentStatus.Trim(),
                    Responsible = x.Responsible?.Trim() ?? string.Empty,
                    Priority = string.IsNullOrWhiteSpace(x.Priority) ? "Media" : x.Priority.Trim(),
                    Notes = x.Notes?.Trim() ?? string.Empty,
                    UpdatedAt = DateTime.UtcNow
                })
                .GroupBy(x => x.DocumentId)
                .Select(x => x.OrderByDescending(y => y.UpdatedAt).First())
                .OrderBy(x => x.PlannedDate)
                .ThenBy(x => x.Priority)
                .ToList();

            await SaveCollectionCommitmentStateRowsAsync(environment, companyId.Value, rows);
            return Results.Ok(new { Saved = rows.Count });
        });

        group.MapGet("/payment-schedule", async (DateTime? from, DateTime? to, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (!companyId.HasValue)
            {
                return Results.Ok(new List<FinancePaymentScheduleRowDto>());
            }

            var today = DateTime.UtcNow.Date;
            var start = from?.Date ?? today.AddDays(-7);
            var end = to?.Date ?? today.AddDays(45);
            if (end < start)
            {
                (start, end) = (end, start);
            }

            var rows = await BuildPaymentScheduleRowsAsync(db, environment, companyId.Value, start, end, today);
            return Results.Ok(rows);
        });

        group.MapPost("/payment-schedule/save", async (FinancePaymentScheduleSaveRequestDto request, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (!companyId.HasValue)
            {
                return Results.BadRequest("No existe empresa para guardar programación de pagos.");
            }

            var rows = (request.Rows ?? new List<FinancePaymentScheduleRowDto>())
                .Where(x => x.PurchaseInvoiceId != Guid.Empty)
                .Select(x => new FinancePaymentScheduleStateRow
                {
                    DocumentId = x.PurchaseInvoiceId,
                    PlannedDate = x.PlannedPaymentDate?.Date,
                    Status = string.IsNullOrWhiteSpace(x.ScheduleStatus) ? "Sin programar" : x.ScheduleStatus.Trim(),
                    Responsible = x.Responsible?.Trim() ?? string.Empty,
                    Priority = string.IsNullOrWhiteSpace(x.Priority) ? "Media" : x.Priority.Trim(),
                    Notes = x.Notes?.Trim() ?? string.Empty,
                    UpdatedAt = DateTime.UtcNow
                })
                .GroupBy(x => x.DocumentId)
                .Select(x => x.OrderByDescending(y => y.UpdatedAt).First())
                .OrderBy(x => x.PlannedDate)
                .ThenBy(x => x.Priority)
                .ToList();

            await SavePaymentScheduleStateRowsAsync(environment, companyId.Value, rows);
            return Results.Ok(new { Saved = rows.Count });
        });

        group.MapGet("/weekly-treasury-plan", async (int? weeks, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (!companyId.HasValue)
            {
                return Results.Ok(new List<FinanceTreasuryWeeklyPlanRowDto>());
            }

            var bucketCount = weeks.GetValueOrDefault(8);
            if (bucketCount <= 0)
            {
                bucketCount = 8;
            }
            if (bucketCount > 26)
            {
                bucketCount = 26;
            }

            var rows = await BuildWeeklyTreasuryPlanRowsAsync(db, environment, companyId.Value, bucketCount, DateTime.UtcNow.Date);
            return Results.Ok(rows);
        });

        group.MapGet("/commitment-follow-up", async (NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (!companyId.HasValue)
            {
                return Results.Ok(new List<FinanceCommitmentFollowUpRowDto>());
            }

            var rows = await BuildCommitmentFollowUpRowsAsync(db, environment, companyId.Value, DateTime.UtcNow.Date);
            return Results.Ok(rows);
        });


        group.MapGet("/monthly-profitability", async (int? year, NanchesoftDbContext db) =>
        {
            var selectedYear = year.GetValueOrDefault(DateTime.UtcNow.Year);
            if (selectedYear < 2000 || selectedYear > 2100)
            {
                selectedYear = DateTime.UtcNow.Year;
            }

            return Results.Ok(await BuildMonthlyProfitabilityRowsAsync(db, selectedYear));
        });

        group.MapGet("/collections-performance", async (int? year, int? month, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (!companyId.HasValue)
            {
                return Results.Ok(new List<FinanceCollectionPerformanceRowDto>());
            }

            var selectedYear = year.GetValueOrDefault(DateTime.UtcNow.Year);
            if (selectedYear < 2000 || selectedYear > 2100)
            {
                selectedYear = DateTime.UtcNow.Year;
            }

            var selectedMonth = month.GetValueOrDefault(DateTime.UtcNow.Month);
            if (selectedMonth < 1 || selectedMonth > 12)
            {
                selectedMonth = DateTime.UtcNow.Month;
            }

            return Results.Ok(await BuildCollectionPerformanceRowsAsync(db, environment, companyId.Value, selectedYear, selectedMonth, DateTime.UtcNow.Date));
        });

        group.MapGet("/payments-performance", async (int? year, int? month, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (!companyId.HasValue)
            {
                return Results.Ok(new List<FinancePaymentPerformanceRowDto>());
            }

            var selectedYear = year.GetValueOrDefault(DateTime.UtcNow.Year);
            if (selectedYear < 2000 || selectedYear > 2100)
            {
                selectedYear = DateTime.UtcNow.Year;
            }

            var selectedMonth = month.GetValueOrDefault(DateTime.UtcNow.Month);
            if (selectedMonth < 1 || selectedMonth > 12)
            {
                selectedMonth = DateTime.UtcNow.Month;
            }

            return Results.Ok(await BuildPaymentPerformanceRowsAsync(db, environment, companyId.Value, selectedYear, selectedMonth, DateTime.UtcNow.Date));
        });

        group.MapGet("/concentration-analysis", async (int? year, int? top, NanchesoftDbContext db) =>
        {
            var selectedYear = year.GetValueOrDefault(DateTime.UtcNow.Year);
            if (selectedYear < 2000 || selectedYear > 2100)
            {
                selectedYear = DateTime.UtcNow.Year;
            }

            var topCount = top.GetValueOrDefault(10);
            if (topCount <= 0)
            {
                topCount = 10;
            }
            if (topCount > 25)
            {
                topCount = 25;
            }

            return Results.Ok(await BuildConcentrationAnalysisAsync(db, selectedYear, topCount));
        });


        group.MapGet("/year-over-year", async (int? year, NanchesoftDbContext db) =>
        {
            var selectedYear = year.GetValueOrDefault(DateTime.UtcNow.Year);
            if (selectedYear < 2000 || selectedYear > 2100)
            {
                selectedYear = DateTime.UtcNow.Year;
            }

            return Results.Ok(await BuildYearOverYearRowsAsync(db, selectedYear));
        });

        group.MapGet("/kpi-scorecard", async (int? year, int? month, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var selectedYear = year.GetValueOrDefault(DateTime.UtcNow.Year);
            if (selectedYear < 2000 || selectedYear > 2100)
            {
                selectedYear = DateTime.UtcNow.Year;
            }

            var selectedMonth = month.GetValueOrDefault(DateTime.UtcNow.Month);
            if (selectedMonth < 1 || selectedMonth > 12)
            {
                selectedMonth = DateTime.UtcNow.Month;
            }

            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (!companyId.HasValue)
            {
                return Results.Ok(new List<FinanceKpiScorecardRowDto>());
            }

            return Results.Ok(await BuildKpiScorecardRowsAsync(db, environment, companyId.Value, selectedYear, selectedMonth));
        });

        group.MapGet("/working-capital-bridge", async (NanchesoftDbContext db) =>
        {
            return Results.Ok(await BuildWorkingCapitalBridgeRowsAsync(db, DateTime.UtcNow.Date));
        });

        group.MapGet("/variation-rankings", async (int? year, int? month, NanchesoftDbContext db) =>
        {
            var selectedYear = year.GetValueOrDefault(DateTime.UtcNow.Year);
            if (selectedYear < 2000 || selectedYear > 2100)
            {
                selectedYear = DateTime.UtcNow.Year;
            }

            var selectedMonth = month.GetValueOrDefault(DateTime.UtcNow.Month);
            if (selectedMonth < 1 || selectedMonth > 12)
            {
                selectedMonth = DateTime.UtcNow.Month;
            }

            return Results.Ok(await BuildVariationRankingAsync(db, selectedYear, selectedMonth));
        });


        group.MapGet("/board-pack", async (int? year, int? month, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var today = DateTime.UtcNow.Date;
            var targetYear = year.GetValueOrDefault(today.Year);
            var targetMonth = month.GetValueOrDefault(today.Month);
            if (targetMonth < 1 || targetMonth > 12)
            {
                targetMonth = today.Month;
            }

            var companyId = await GetPrimaryCompanyIdAsync(db);
            var dto = await BuildBoardPackAsync(db, environment, companyId, targetYear, targetMonth);
            return Results.Ok(dto);
        });

        group.MapGet("/liquidity-radar", async (int? year, int? month, NanchesoftDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;
            var targetYear = year.GetValueOrDefault(today.Year);
            var targetMonth = month.GetValueOrDefault(today.Month);
            if (targetMonth < 1 || targetMonth > 12)
            {
                targetMonth = today.Month;
            }

            var rows = await BuildLiquidityRadarRowsAsync(db, targetYear, targetMonth);
            return Results.Ok(rows);
        });

        group.MapGet("/cash-conversion-cycle", async (int? year, NanchesoftDbContext db) =>
        {
            var targetYear = year.GetValueOrDefault(DateTime.UtcNow.Year);
            var rows = await BuildCashConversionCycleRowsAsync(db, targetYear);
            return Results.Ok(rows);
        });

        group.MapGet("/stress-tests", async (int? year, int? month, NanchesoftDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;
            var targetYear = year.GetValueOrDefault(today.Year);
            var targetMonth = month.GetValueOrDefault(today.Month);
            if (targetMonth < 1 || targetMonth > 12)
            {
                targetMonth = today.Month;
            }

            var dto = await BuildStressTestsAsync(db, targetYear, targetMonth);
            return Results.Ok(dto);
        });


        group.MapGet("/action-center", async (NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var today = DateTime.UtcNow.Date;
            var companyId = await GetPrimaryCompanyIdAsync(db);
            var rows = await BuildActionCenterRowsAsync(db, environment, companyId, today);
            return Results.Ok(rows);
        });

        group.MapGet("/closing-cockpit", async (int? year, int? month, NanchesoftDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;
            var targetYear = year.GetValueOrDefault(today.Year);
            var targetMonth = month.GetValueOrDefault(today.Month);
            if (targetMonth < 1 || targetMonth > 12)
            {
                targetMonth = today.Month;
            }

            var rows = await BuildClosingCockpitRowsAsync(db, targetYear, targetMonth);
            return Results.Ok(rows);
        });

        group.MapGet("/covenants", async (int? year, int? month, NanchesoftDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;
            var targetYear = year.GetValueOrDefault(today.Year);
            var targetMonth = month.GetValueOrDefault(today.Month);
            if (targetMonth < 1 || targetMonth > 12)
            {
                targetMonth = today.Month;
            }

            var rows = await BuildCovenantRowsAsync(db, targetYear, targetMonth);
            return Results.Ok(rows);
        });

        group.MapGet("/executive-agreements", async (int? year, int? month, IHostEnvironment environment) =>
        {
            var today = DateTime.UtcNow.Date;
            var targetYear = year.GetValueOrDefault(today.Year);
            var targetMonth = month.GetValueOrDefault(today.Month);
            if (targetMonth < 1 || targetMonth > 12)
            {
                targetMonth = today.Month;
            }

            var rows = await LoadExecutiveAgreementRowsAsync(environment, targetYear, targetMonth);
            return Results.Ok(rows);
        });

        group.MapPost("/executive-agreements/save", async (FinanceExecutiveAgreementSaveRequestDto request, IHostEnvironment environment) =>
        {
            var rows = request.Rows ?? new List<FinanceExecutiveAgreementRowDto>();
            await SaveExecutiveAgreementRowsAsync(environment, rows);
            return Results.Ok(new { success = true, count = rows.Count });
        });


        group.MapGet("/rolling-forecast", async (int? months, NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (!companyId.HasValue)
            {
                return Results.Ok(new List<FinanceRollingForecastRowDto>());
            }

            var bucketCount = months.GetValueOrDefault(12);
            if (bucketCount < 3)
            {
                bucketCount = 3;
            }
            if (bucketCount > 18)
            {
                bucketCount = 18;
            }

            return Results.Ok(await BuildRollingForecastRowsAsync(db, environment, companyId.Value, bucketCount, DateTime.UtcNow.Date));
        });

        group.MapGet("/credit-policy", async (NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (!companyId.HasValue)
            {
                return Results.Ok(new List<FinanceCreditPolicyRowDto>());
            }

            return Results.Ok(await BuildCreditPolicyRowsAsync(db, environment, companyId.Value, DateTime.UtcNow.Date));
        });

        group.MapGet("/supplier-risk", async (NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (!companyId.HasValue)
            {
                return Results.Ok(new List<FinanceSupplierRiskRowDto>());
            }

            return Results.Ok(await BuildSupplierRiskRowsAsync(db, environment, companyId.Value, DateTime.UtcNow.Date));
        });

        group.MapGet("/recovery-matrix", async (NanchesoftDbContext db, IHostEnvironment environment) =>
        {
            var companyId = await GetPrimaryCompanyIdAsync(db);
            if (!companyId.HasValue)
            {
                return Results.Ok(new List<FinanceRecoveryMatrixRowDto>());
            }

            return Results.Ok(await BuildRecoveryMatrixRowsAsync(db, environment, companyId.Value, DateTime.UtcNow.Date));
        });

        group.MapGet("/customer-radar", async (int? year, int? month, NanchesoftDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;
            var targetYear = year.GetValueOrDefault(today.Year);
            var targetMonth = month.GetValueOrDefault(today.Month);
            if (targetMonth < 1 || targetMonth > 12)
            {
                targetMonth = today.Month;
            }

            return Results.Ok(await BuildCustomerRadarRowsAsync(db, targetYear, targetMonth, today));
        });

        group.MapGet("/supplier-radar", async (int? year, int? month, NanchesoftDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;
            var targetYear = year.GetValueOrDefault(today.Year);
            var targetMonth = month.GetValueOrDefault(today.Month);
            if (targetMonth < 1 || targetMonth > 12)
            {
                targetMonth = today.Month;
            }

            return Results.Ok(await BuildSupplierRadarRowsAsync(db, targetYear, targetMonth, today));
        });

        group.MapGet("/branch-pulse", async (int? year, int? month, NanchesoftDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;
            var targetYear = year.GetValueOrDefault(today.Year);
            var targetMonth = month.GetValueOrDefault(today.Month);
            if (targetMonth < 1 || targetMonth > 12)
            {
                targetMonth = today.Month;
            }

            return Results.Ok(await BuildBranchPulseRowsAsync(db, targetYear, targetMonth, today));
        });

        group.MapGet("/monthly-liquidity-bridge", async (int? year, NanchesoftDbContext db) =>
        {
            var targetYear = year.GetValueOrDefault(DateTime.UtcNow.Year);
            return Results.Ok(await BuildMonthlyLiquidityBridgeRowsAsync(db, targetYear, DateTime.UtcNow.Date));
        });

        return app;
    }

    private static async Task<List<FinanceDocumentControlRowDto>> BuildDocumentControlRowsAsync(NanchesoftDbContext db, DateTime today)
    {
        var receiptApplicationsByInvoice = await db.ReceiptApplications.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled")
            .GroupBy(x => x.SalesInvoiceId)
            .ToDictionaryAsync(x => x.Key, x => x.Sum(y => y.AppliedAmount));

        var customerMap = await db.Customers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x);
        var supplierMap = await db.Suppliers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x);
        var entries = await db.Set<AccountingJournalEntry>().AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.EntryDate)
            .ToListAsync();

        var rows = new List<FinanceDocumentControlRowDto>();

        foreach (var invoice in await db.SalesInvoices.AsNoTracking().Where(x => x.IsActive).ToListAsync())
        {
            var accounting = FindAccountingInfo(entries, invoice.Folio);
            var applied = receiptApplicationsByInvoice.TryGetValue(invoice.Id, out var amount) ? amount : 0m;
            var openAmount = Math.Max(0m, invoice.Total - applied);
            var dueDate = invoice.CustomerId.HasValue && customerMap.TryGetValue(invoice.CustomerId.Value, out var customer)
                ? invoice.InvoiceDate.Date.AddDays(Math.Max(0, customer.PaymentTermDays))
                : invoice.InvoiceDate.Date.AddDays(15);

            rows.Add(new FinanceDocumentControlRowDto
            {
                Module = "Ventas",
                DocumentType = "sales_invoice",
                DocumentId = invoice.Id,
                Folio = invoice.Folio,
                DocumentDate = invoice.InvoiceDate,
                PartnerName = invoice.CustomerId.HasValue && customerMap.TryGetValue(invoice.CustomerId.Value, out var partner)
                    ? partner.Name
                    : string.Empty,
                Status = invoice.Status,
                Total = invoice.Total,
                OpenAmount = openAmount,
                DueDate = dueDate,
                AgeDays = (today - invoice.InvoiceDate.Date).Days,
                IsAccounted = accounting.IsAccounted,
                AccountingStatus = accounting.Status,
                Reference = invoice.Notes
            });
        }

        foreach (var invoice in await db.PurchaseInvoices.AsNoTracking().Where(x => x.IsActive).ToListAsync())
        {
            var accounting = FindAccountingInfo(entries, invoice.Folio);
            var dueDate = invoice.SupplierId.HasValue && supplierMap.TryGetValue(invoice.SupplierId.Value, out var supplier)
                ? invoice.InvoiceDate.Date.AddDays(Math.Max(0, supplier.PaymentTermDays))
                : invoice.InvoiceDate.Date.AddDays(30);

            rows.Add(new FinanceDocumentControlRowDto
            {
                Module = "Compras",
                DocumentType = "purchase_invoice",
                DocumentId = invoice.Id,
                Folio = invoice.Folio,
                DocumentDate = invoice.InvoiceDate,
                PartnerName = invoice.SupplierId.HasValue && supplierMap.TryGetValue(invoice.SupplierId.Value, out var partner)
                    ? partner.Name
                    : string.Empty,
                Status = invoice.Status,
                Total = invoice.Total,
                OpenAmount = Math.Max(0m, invoice.Total),
                DueDate = dueDate,
                AgeDays = (today - invoice.InvoiceDate.Date).Days,
                IsAccounted = accounting.IsAccounted,
                AccountingStatus = accounting.Status,
                Reference = string.IsNullOrWhiteSpace(invoice.SupplierInvoiceFolio) ? invoice.Notes : invoice.SupplierInvoiceFolio
            });
        }

        foreach (var receipt in await db.Receipts.AsNoTracking().Where(x => x.IsActive).ToListAsync())
        {
            var accounting = FindAccountingInfo(entries, receipt.Folio);
            rows.Add(new FinanceDocumentControlRowDto
            {
                Module = "Tesorería",
                DocumentType = "receipt",
                DocumentId = receipt.Id,
                Folio = receipt.Folio,
                DocumentDate = receipt.ReceiptDate,
                PartnerName = receipt.CustomerId.HasValue && customerMap.TryGetValue(receipt.CustomerId.Value, out var partner)
                    ? partner.Name
                    : string.Empty,
                Status = receipt.Status,
                Total = receipt.Total,
                OpenAmount = 0m,
                DueDate = null,
                AgeDays = (today - receipt.ReceiptDate.Date).Days,
                IsAccounted = accounting.IsAccounted,
                AccountingStatus = accounting.Status,
                Reference = receipt.Reference
            });
        }

        foreach (var payment in await db.Payments.AsNoTracking().Where(x => x.IsActive).ToListAsync())
        {
            var accounting = FindAccountingInfo(entries, payment.Folio);
            rows.Add(new FinanceDocumentControlRowDto
            {
                Module = "Tesorería",
                DocumentType = "payment",
                DocumentId = payment.Id,
                Folio = payment.Folio,
                DocumentDate = payment.PaymentDate,
                PartnerName = payment.SupplierId.HasValue && supplierMap.TryGetValue(payment.SupplierId.Value, out var partner)
                    ? partner.Name
                    : string.Empty,
                Status = payment.Status,
                Total = payment.Total,
                OpenAmount = 0m,
                DueDate = null,
                AgeDays = (today - payment.PaymentDate.Date).Days,
                IsAccounted = accounting.IsAccounted,
                AccountingStatus = accounting.Status,
                Reference = payment.Reference
            });
        }

        return rows;
    }

    private static AccountingMatchInfo FindAccountingInfo(IEnumerable<AccountingJournalEntry> entries, string folio)
    {
        if (string.IsNullOrWhiteSpace(folio))
        {
            return new AccountingMatchInfo(false, string.Empty);
        }

        var match = entries.FirstOrDefault(x =>
            (!string.IsNullOrWhiteSpace(x.Reference) && x.Reference.Contains(folio, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(x.Concept) && x.Concept.Contains(folio, StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(x.Folio, folio, StringComparison.OrdinalIgnoreCase));

        return match is null
            ? new AccountingMatchInfo(false, string.Empty)
            : new AccountingMatchInfo(true, match.Status);
    }

    private static int SeverityRank(string severity)
        => severity switch
        {
            "Alta" => 3,
            "Media" => 2,
            _ => 1
        };

    private sealed record ForecastMovement
    {
        public DateTime DueDate { get; init; }
        public decimal Amount { get; init; }
        public string Type { get; init; } = string.Empty;
    }

    private sealed record AccountingMatchInfo(bool IsAccounted, string Status);


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


    private static async Task<List<FinanceMonthlyProfitabilityRowDto>> BuildMonthlyProfitabilityRowsAsync(NanchesoftDbContext db, int year)
    {
        var salesByMonth = await db.SalesInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate.Year == year)
            .GroupBy(x => x.InvoiceDate.Month)
            .Select(x => new { Month = x.Key, Amount = x.Sum(y => y.Total) })
            .ToDictionaryAsync(x => x.Month, x => x.Amount);

        var creditsByMonth = await db.CreditNotes.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled" && x.CreditNoteDate.Year == year)
            .GroupBy(x => x.CreditNoteDate.Month)
            .Select(x => new { Month = x.Key, Amount = x.Sum(y => y.Total) })
            .ToDictionaryAsync(x => x.Month, x => x.Amount);

        var purchasesByMonth = await db.PurchaseInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate.Year == year)
            .GroupBy(x => x.InvoiceDate.Month)
            .Select(x => new { Month = x.Key, Amount = x.Sum(y => y.Total) })
            .ToDictionaryAsync(x => x.Month, x => x.Amount);

        var receiptsByMonth = await db.Receipts.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.ReceiptDate.Year == year)
            .GroupBy(x => x.ReceiptDate.Month)
            .Select(x => new { Month = x.Key, Amount = x.Sum(y => y.Total) })
            .ToDictionaryAsync(x => x.Month, x => x.Amount);

        var paymentsByMonth = await db.Payments.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.PaymentDate.Year == year)
            .GroupBy(x => x.PaymentDate.Month)
            .Select(x => new { Month = x.Key, Amount = x.Sum(y => y.Total) })
            .ToDictionaryAsync(x => x.Month, x => x.Amount);

        var rows = new List<FinanceMonthlyProfitabilityRowDto>();
        decimal cumulativeGross = 0m;
        decimal cumulativeCash = 0m;

        for (var month = 1; month <= 12; month++)
        {
            var grossSales = salesByMonth.TryGetValue(month, out var sales) ? sales : 0m;
            var creditNotes = creditsByMonth.TryGetValue(month, out var credits) ? credits : 0m;
            var netSales = grossSales - creditNotes;
            var purchases = purchasesByMonth.TryGetValue(month, out var purchase) ? purchase : 0m;
            var receipts = receiptsByMonth.TryGetValue(month, out var receipt) ? receipt : 0m;
            var payments = paymentsByMonth.TryGetValue(month, out var payment) ? payment : 0m;
            var grossMargin = netSales - purchases;
            var cashMargin = receipts - payments;
            cumulativeGross += grossMargin;
            cumulativeCash += cashMargin;

            rows.Add(new FinanceMonthlyProfitabilityRowDto
            {
                Year = year,
                Month = month,
                Label = new DateTime(year, month, 1).ToString("MMMM yyyy"),
                GrossSales = grossSales,
                CreditNotes = creditNotes,
                NetSales = netSales,
                Purchases = purchases,
                GrossMargin = grossMargin,
                GrossMarginPercent = netSales == 0m ? 0m : Math.Round((grossMargin / netSales) * 100m, 2),
                Receipts = receipts,
                Payments = payments,
                CashMargin = cashMargin,
                CumulativeGrossMargin = cumulativeGross,
                CumulativeCashMargin = cumulativeCash
            });
        }

        return rows;
    }

    private static async Task<List<FinanceCollectionPerformanceRowDto>> BuildCollectionPerformanceRowsAsync(NanchesoftDbContext db, IHostEnvironment environment, Guid companyId, int year, int month, DateTime today)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).Date;
        var end = start.AddMonths(1).AddDays(-1).Date;
        var rows = await BuildCollectionCommitmentRowsAsync(db, environment, companyId, start, end, today);

        var collectedByCustomer = await db.ReceiptApplications.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled" && x.ApplicationDate.Date >= start && x.ApplicationDate.Date <= end)
            .GroupBy(x => x.CustomerId)
            .Select(x => new { CustomerId = x.Key, Applied = x.Sum(y => y.AppliedAmount) })
            .ToDictionaryAsync(x => x.CustomerId, x => x.Applied);

        return rows
            .GroupBy(x => new { x.CustomerId, x.CustomerName })
            .Select(group =>
            {
                var overdueRows = group.Where(x => x.IsOverdue).ToList();
                var promisedAmount = group.Where(x => x.PlannedCollectionDate.HasValue && x.PlannedCollectionDate.Value.Date >= start && x.PlannedCollectionDate.Value.Date <= end)
                    .Sum(x => x.OpenAmount);
                var collectedAmount = collectedByCustomer.TryGetValue(group.Key.CustomerId, out var applied) ? applied : 0m;
                var overdueAmount = overdueRows.Sum(x => x.OpenAmount);
                var openAmount = group.Sum(x => x.OpenAmount);
                var upcomingWeek = group.Where(x => x.DaysToDue >= 0 && x.DaysToDue <= 7).Sum(x => x.OpenAmount);
                var averagePastDue = overdueRows.Count == 0 ? 0m : Math.Round(overdueRows.Average(x => Math.Abs((decimal)x.DaysToDue)), 2);
                var effectiveness = promisedAmount == 0m ? 0m : Math.Round((collectedAmount / promisedAmount) * 100m, 2);
                var risk = overdueAmount >= Math.Max(1m, openAmount * 0.50m)
                    ? "Alta"
                    : overdueAmount > 0m || group.Any(x => string.IsNullOrWhiteSpace(x.Responsible))
                        ? "Media"
                        : "Baja";

                return new FinanceCollectionPerformanceRowDto
                {
                    CustomerId = group.Key.CustomerId,
                    CustomerName = group.Key.CustomerName,
                    OpenInvoices = group.Count(x => x.OpenAmount > 0m),
                    OpenAmount = openAmount,
                    OverdueAmount = overdueAmount,
                    UpcomingWeekAmount = upcomingWeek,
                    PromisedAmount = promisedAmount,
                    CollectedAmount = collectedAmount,
                    EffectivenessPercent = effectiveness,
                    AveragePastDueDays = averagePastDue,
                    RiskLevel = risk,
                    Route = $"/accounts-receivable/statements?customerId={group.Key.CustomerId}"
                };
            })
            .OrderByDescending(x => x.OverdueAmount)
            .ThenByDescending(x => x.OpenAmount)
            .ThenBy(x => x.CustomerName)
            .ToList();
    }

    private static async Task<List<FinancePaymentPerformanceRowDto>> BuildPaymentPerformanceRowsAsync(NanchesoftDbContext db, IHostEnvironment environment, Guid companyId, int year, int month, DateTime today)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).Date;
        var end = start.AddMonths(1).AddDays(-1).Date;
        var rows = await BuildPaymentScheduleRowsAsync(db, environment, companyId, start, end, today);

        var paidBySupplier = await db.Payments.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.SupplierId != null && x.PaymentDate.Date >= start && x.PaymentDate.Date <= end)
            .GroupBy(x => x.SupplierId!.Value)
            .Select(x => new { SupplierId = x.Key, Paid = x.Sum(y => y.Total) })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Paid);

        return rows
            .GroupBy(x => new { x.SupplierId, x.SupplierName })
            .Select(group =>
            {
                var scheduledAmount = group.Where(x => x.PlannedPaymentDate.HasValue && x.PlannedPaymentDate.Value.Date >= start && x.PlannedPaymentDate.Value.Date <= end)
                    .Sum(x => x.OpenAmount);
                var paidAmount = paidBySupplier.TryGetValue(group.Key.SupplierId, out var paid) ? paid : 0m;
                var overdueAmount = group.Where(x => x.DaysToDue < 0).Sum(x => x.OpenAmount);
                var requiresAuth = group.Count(x => x.NeedsAuthorization);
                var openAmount = group.Sum(x => x.OpenAmount);
                var compliance = scheduledAmount == 0m ? 0m : Math.Round((paidAmount / scheduledAmount) * 100m, 2);
                var risk = overdueAmount >= Math.Max(1m, openAmount * 0.50m) || requiresAuth > 0
                    ? "Alta"
                    : overdueAmount > 0m || group.Any(x => string.IsNullOrWhiteSpace(x.Responsible))
                        ? "Media"
                        : "Baja";

                return new FinancePaymentPerformanceRowDto
                {
                    SupplierId = group.Key.SupplierId,
                    SupplierName = group.Key.SupplierName,
                    PendingInvoices = group.Count(x => x.OpenAmount > 0m),
                    OpenAmount = openAmount,
                    OverdueAmount = overdueAmount,
                    ScheduledAmount = scheduledAmount,
                    PaidAmount = paidAmount,
                    CompliancePercent = compliance,
                    RequiresAuthorizationCount = requiresAuth,
                    RiskLevel = risk,
                    Route = $"/accounts-payable/statements?supplierId={group.Key.SupplierId}"
                };
            })
            .OrderByDescending(x => x.RequiresAuthorizationCount)
            .ThenByDescending(x => x.OverdueAmount)
            .ThenByDescending(x => x.OpenAmount)
            .ThenBy(x => x.SupplierName)
            .ToList();
    }

    private static async Task<FinanceConcentrationAnalysisDto> BuildConcentrationAnalysisAsync(NanchesoftDbContext db, int year, int top)
    {
        var receiptAppliedByInvoice = await db.ReceiptApplications.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled")
            .GroupBy(x => x.SalesInvoiceId)
            .Select(x => new { SalesInvoiceId = x.Key, Amount = x.Sum(y => y.AppliedAmount) })
            .ToDictionaryAsync(x => x.SalesInvoiceId, x => x.Amount);

        var customerMap = await db.Customers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);
        var supplierMap = await db.Suppliers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);

        var salesInvoices = await db.SalesInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.CustomerId != null && x.InvoiceDate.Year == year)
            .ToListAsync();
        var creditNotes = await db.CreditNotes.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled" && x.CustomerId != null && x.CreditNoteDate.Year == year)
            .ToListAsync();
        var purchaseInvoices = await db.PurchaseInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.SupplierId != null && x.InvoiceDate.Year == year)
            .ToListAsync();
        var payments = await db.Payments.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.SupplierId != null && x.PaymentDate.Year == year)
            .ToListAsync();

        var customerCredits = creditNotes.GroupBy(x => x.CustomerId!.Value).ToDictionary(x => x.Key, x => x.Sum(y => y.Total));
        var customerRows = salesInvoices
            .GroupBy(x => x.CustomerId!.Value)
            .Select(group =>
            {
                var gross = group.Sum(x => x.Total);
                var credits = customerCredits.TryGetValue(group.Key, out var credit) ? credit : 0m;
                var net = gross - credits;
                var open = group.Sum(x => Math.Max(0m, x.Total - (receiptAppliedByInvoice.TryGetValue(x.Id, out var applied) ? applied : 0m)));
                return new FinanceConcentrationPartyRowDto
                {
                    PartyId = group.Key,
                    PartyName = customerMap.TryGetValue(group.Key, out var name) ? name : group.Key.ToString(),
                    Amount = net,
                    OpenAmount = open,
                    Documents = group.Count(),
                    Route = $"/accounts-receivable/statements?customerId={group.Key}"
                };
            })
            .Where(x => x.Amount > 0m)
            .OrderByDescending(x => x.Amount)
            .ToList();

        var totalNetSales = customerRows.Sum(x => x.Amount);
        foreach (var row in customerRows)
        {
            row.SharePercent = totalNetSales == 0m ? 0m : Math.Round((row.Amount / totalNetSales) * 100m, 2);
        }

        var paidBySupplier = payments.GroupBy(x => x.SupplierId!.Value).ToDictionary(x => x.Key, x => x.Sum(y => y.Total));
        var supplierRows = purchaseInvoices
            .GroupBy(x => x.SupplierId!.Value)
            .Select(group =>
            {
                var amount = group.Sum(x => x.Total);
                var paid = paidBySupplier.TryGetValue(group.Key, out var supplierPaid) ? supplierPaid : 0m;
                var open = Math.Max(0m, amount - paid);
                return new FinanceConcentrationPartyRowDto
                {
                    PartyId = group.Key,
                    PartyName = supplierMap.TryGetValue(group.Key, out var name) ? name : group.Key.ToString(),
                    Amount = amount,
                    OpenAmount = open,
                    Documents = group.Count(),
                    Route = $"/accounts-payable/statements?supplierId={group.Key}"
                };
            })
            .Where(x => x.Amount > 0m)
            .OrderByDescending(x => x.Amount)
            .ToList();

        var totalPurchases = supplierRows.Sum(x => x.Amount);
        foreach (var row in supplierRows)
        {
            row.SharePercent = totalPurchases == 0m ? 0m : Math.Round((row.Amount / totalPurchases) * 100m, 2);
        }

        var customerHhi = Math.Round(customerRows.Sum(x => Math.Pow((double)(x.SharePercent / 100m), 2d)) * 10000d, 2);
        var supplierHhi = Math.Round(supplierRows.Sum(x => Math.Pow((double)(x.SharePercent / 100m), 2d)) * 10000d, 2);

        return new FinanceConcentrationAnalysisDto
        {
            Year = year,
            Top = top,
            TotalSales = totalNetSales,
            TotalPurchases = totalPurchases,
            Top3CustomerSharePercent = Math.Round(customerRows.Take(3).Sum(x => x.SharePercent), 2),
            Top3SupplierSharePercent = Math.Round(supplierRows.Take(3).Sum(x => x.SharePercent), 2),
            CustomerHhi = customerHhi,
            SupplierHhi = supplierHhi,
            Customers = customerRows.Take(top).ToList(),
            Suppliers = supplierRows.Take(top).ToList()
        };
    }

    private static readonly JsonSerializerOptions FinanceJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static async Task<Guid?> GetPrimaryCompanyIdAsync(NanchesoftDbContext db)
        => await db.Companies.AsNoTracking().OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

    private static string GetFinanceDataDirectory(IHostEnvironment environment)
    {
        var path = Path.Combine(environment.ContentRootPath, "App_Data", "finance");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string GetBudgetFilePath(IHostEnvironment environment, Guid companyId)
        => Path.Combine(GetFinanceDataDirectory(environment), $"budgets-{companyId:N}.json");

    private static string GetGoalFilePath(IHostEnvironment environment, Guid companyId)
        => Path.Combine(GetFinanceDataDirectory(environment), $"goals-{companyId:N}.json");

    private static async Task<List<FinanceBudgetRowDto>> LoadBudgetRowsAsync(IHostEnvironment environment, Guid companyId)
    {
        var path = GetBudgetFilePath(environment, companyId);
        if (!File.Exists(path))
        {
            return new List<FinanceBudgetRowDto>();
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<FinanceBudgetRowDto>>(stream, FinanceJsonOptions)
            ?? new List<FinanceBudgetRowDto>();
    }

    private static async Task SaveBudgetRowsAsync(IHostEnvironment environment, Guid companyId, List<FinanceBudgetRowDto> rows)
    {
        var path = GetBudgetFilePath(environment, companyId);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, rows.OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.AccountCode).ToList(), FinanceJsonOptions);
    }

    private static async Task<List<FinanceGoalRowDto>> LoadGoalRowsAsync(IHostEnvironment environment, Guid companyId)
    {
        var path = GetGoalFilePath(environment, companyId);
        if (!File.Exists(path))
        {
            return new List<FinanceGoalRowDto>();
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<FinanceGoalRowDto>>(stream, FinanceJsonOptions)
            ?? new List<FinanceGoalRowDto>();
    }

    private static async Task SaveGoalRowsAsync(IHostEnvironment environment, Guid companyId, List<FinanceGoalRowDto> rows)
    {
        var path = GetGoalFilePath(environment, companyId);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, rows.OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.MetricCode).ToList(), FinanceJsonOptions);
    }


    private static string GetCollectionCommitmentsFilePath(IHostEnvironment environment, Guid companyId)
        => Path.Combine(GetFinanceDataDirectory(environment), $"collection-commitments-{companyId:N}.json");

    private static string GetPaymentScheduleFilePath(IHostEnvironment environment, Guid companyId)
        => Path.Combine(GetFinanceDataDirectory(environment), $"payment-schedule-{companyId:N}.json");

    private static async Task<List<FinanceCollectionCommitmentStateRow>> LoadCollectionCommitmentStateRowsAsync(IHostEnvironment environment, Guid companyId)
    {
        var path = GetCollectionCommitmentsFilePath(environment, companyId);
        if (!File.Exists(path))
        {
            return new List<FinanceCollectionCommitmentStateRow>();
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<FinanceCollectionCommitmentStateRow>>(stream, FinanceJsonOptions)
            ?? new List<FinanceCollectionCommitmentStateRow>();
    }

    private static async Task SaveCollectionCommitmentStateRowsAsync(IHostEnvironment environment, Guid companyId, List<FinanceCollectionCommitmentStateRow> rows)
    {
        var path = GetCollectionCommitmentsFilePath(environment, companyId);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, rows.OrderBy(x => x.PlannedDate).ThenBy(x => x.Priority).ToList(), FinanceJsonOptions);
    }

    private static async Task<List<FinancePaymentScheduleStateRow>> LoadPaymentScheduleStateRowsAsync(IHostEnvironment environment, Guid companyId)
    {
        var path = GetPaymentScheduleFilePath(environment, companyId);
        if (!File.Exists(path))
        {
            return new List<FinancePaymentScheduleStateRow>();
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<FinancePaymentScheduleStateRow>>(stream, FinanceJsonOptions)
            ?? new List<FinancePaymentScheduleStateRow>();
    }

    private static async Task SavePaymentScheduleStateRowsAsync(IHostEnvironment environment, Guid companyId, List<FinancePaymentScheduleStateRow> rows)
    {
        var path = GetPaymentScheduleFilePath(environment, companyId);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, rows.OrderBy(x => x.PlannedDate).ThenBy(x => x.Priority).ToList(), FinanceJsonOptions);
    }

    private static string GetScenarioFilePath(IHostEnvironment environment, Guid companyId)
        => Path.Combine(GetFinanceDataDirectory(environment), $"scenarios-{companyId:N}.json");

    private static async Task<List<FinanceScenarioRowDto>> LoadScenarioRowsAsync(IHostEnvironment environment, Guid companyId)
    {
        var path = GetScenarioFilePath(environment, companyId);
        if (!File.Exists(path))
        {
            return new List<FinanceScenarioRowDto>();
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<FinanceScenarioRowDto>>(stream, FinanceJsonOptions)
            ?? new List<FinanceScenarioRowDto>();
    }

    private static async Task SaveScenarioRowsAsync(IHostEnvironment environment, Guid companyId, List<FinanceScenarioRowDto> rows)
    {
        var path = GetScenarioFilePath(environment, companyId);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, rows.OrderBy(x => x.Name).ThenByDescending(x => x.UpdatedAt).ToList(), FinanceJsonOptions);
    }

    private static async Task<List<FinanceCollectionCalendarRowDto>> BuildCollectionsCalendarRowsAsync(NanchesoftDbContext db, DateTime start, DateTime end, DateTime today)
    {
        var customerMap = await db.Customers.AsNoTracking().Where(x => x.IsActive).ToDictionaryAsync(x => x.Id, x => x);
        var appliedByInvoice = await db.ReceiptApplications.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled")
            .GroupBy(x => x.SalesInvoiceId)
            .ToDictionaryAsync(x => x.Key, x => x.Sum(y => y.AppliedAmount));

        var invoices = await db.SalesInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.CustomerId != null)
            .OrderBy(x => x.InvoiceDate)
            .ToListAsync();

        return invoices
            .Select(invoice =>
            {
                var applied = appliedByInvoice.TryGetValue(invoice.Id, out var amount) ? amount : 0m;
                var openAmount = Math.Max(0m, invoice.Total - applied);
                var termDays = invoice.CustomerId.HasValue && customerMap.TryGetValue(invoice.CustomerId.Value, out var customer)
                    ? Math.Max(0, customer.PaymentTermDays)
                    : 15;
                var dueDate = invoice.InvoiceDate.Date.AddDays(termDays);
                var dayDiff = (dueDate - today).Days;
                var status = dueDate < today ? "Vencido" : dueDate == today ? "Hoy" : dayDiff <= 7 ? "Próximo" : "Programado";
                return new FinanceCollectionCalendarRowDto
                {
                    CustomerId = invoice.CustomerId ?? Guid.Empty,
                    CustomerName = invoice.CustomerId.HasValue && customerMap.TryGetValue(invoice.CustomerId.Value, out var foundCustomer)
                        ? foundCustomer.Name
                        : "Cliente",
                    SalesInvoiceId = invoice.Id,
                    Folio = invoice.Folio,
                    InvoiceDate = invoice.InvoiceDate,
                    DueDate = dueDate,
                    Total = invoice.Total,
                    AppliedAmount = applied,
                    OpenAmount = openAmount,
                    DaysToDue = dayDiff,
                    Status = status,
                    Route = invoice.CustomerId.HasValue ? $"/accounts-receivable/statements?customerId={invoice.CustomerId.Value}" : "/accounts-receivable/statements"
                };
            })
            .Where(x => x.OpenAmount > 0m && x.DueDate.Date >= start && x.DueDate.Date <= end)
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.CustomerName)
            .ThenBy(x => x.Folio)
            .ToList();
    }

    private static async Task<List<FinancePaymentCalendarRowDto>> BuildPaymentsCalendarRowsAsync(NanchesoftDbContext db, DateTime start, DateTime end, DateTime today)
    {
        var supplierMap = await db.Suppliers.AsNoTracking().Where(x => x.IsActive).ToDictionaryAsync(x => x.Id, x => x);
        var invoices = await db.PurchaseInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.SupplierId != null)
            .OrderBy(x => x.InvoiceDate)
            .ToListAsync();
        var paymentsBySupplier = await db.Payments.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.SupplierId != null)
            .GroupBy(x => x.SupplierId)
            .ToDictionaryAsync(x => x.Key!.Value, x => x.Sum(y => y.Total));

        var rows = new List<FinancePaymentCalendarRowDto>();
        foreach (var group in invoices.Where(x => x.SupplierId.HasValue).GroupBy(x => x.SupplierId!.Value))
        {
            var remainingPayment = paymentsBySupplier.TryGetValue(group.Key, out var totalPaid) ? Math.Max(0m, totalPaid) : 0m;
            foreach (var invoice in group.OrderBy(x => x.InvoiceDate))
            {
                var gross = Math.Max(0m, invoice.Total);
                var applied = Math.Min(gross, remainingPayment);
                remainingPayment = Math.Max(0m, remainingPayment - applied);
                var openAmount = Math.Max(0m, gross - applied);
                if (openAmount <= 0m)
                {
                    continue;
                }

                var termDays = supplierMap.TryGetValue(group.Key, out var supplier)
                    ? Math.Max(0, supplier.PaymentTermDays)
                    : 30;
                var dueDate = invoice.InvoiceDate.Date.AddDays(termDays);
                var dayDiff = (dueDate - today).Days;
                var status = dueDate < today ? "Vencido" : dueDate == today ? "Hoy" : dayDiff <= 7 ? "Próximo" : "Programado";
                rows.Add(new FinancePaymentCalendarRowDto
                {
                    SupplierId = group.Key,
                    SupplierName = supplierMap.TryGetValue(group.Key, out var foundSupplier) ? foundSupplier.Name : "Proveedor",
                    PurchaseInvoiceId = invoice.Id,
                    Folio = invoice.Folio,
                    InvoiceDate = invoice.InvoiceDate,
                    DueDate = dueDate,
                    Total = gross,
                    AppliedAmount = applied,
                    OpenAmount = openAmount,
                    DaysToDue = dayDiff,
                    Status = status,
                    Route = $"/accounts-payable/statements?supplierId={group.Key}"
                });
            }
        }

        return rows
            .Where(x => x.DueDate.Date >= start && x.DueDate.Date <= end)
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.SupplierName)
            .ThenBy(x => x.Folio)
            .ToList();
    }

    private static async Task<List<FinanceCollectionCommitmentRowDto>> BuildCollectionCommitmentRowsAsync(NanchesoftDbContext db, IHostEnvironment environment, Guid companyId, DateTime start, DateTime end, DateTime today)
    {
        var calendarRows = await BuildCollectionsCalendarRowsAsync(db, start, end, today);
        var states = (await LoadCollectionCommitmentStateRowsAsync(environment, companyId)).ToDictionary(x => x.DocumentId, x => x);

        return calendarRows
            .Select(row =>
            {
                states.TryGetValue(row.SalesInvoiceId, out var state);
                return new FinanceCollectionCommitmentRowDto
                {
                    CustomerId = row.CustomerId,
                    SalesInvoiceId = row.SalesInvoiceId,
                    CustomerName = row.CustomerName,
                    Folio = row.Folio,
                    InvoiceDate = row.InvoiceDate,
                    DueDate = row.DueDate,
                    OpenAmount = row.OpenAmount,
                    DaysToDue = row.DaysToDue,
                    PlannedCollectionDate = state?.PlannedDate?.Date ?? row.DueDate.Date,
                    CommitmentStatus = string.IsNullOrWhiteSpace(state?.Status) ? "Sin gestionar" : state!.Status,
                    Responsible = state?.Responsible ?? string.Empty,
                    Priority = string.IsNullOrWhiteSpace(state?.Priority) ? (row.DaysToDue < 0 ? "Alta" : row.DaysToDue <= 7 ? "Media" : "Baja") : state!.Priority,
                    Notes = state?.Notes ?? string.Empty,
                    IsOverdue = row.DueDate.Date < today && row.OpenAmount > 0m,
                    LastUpdatedAt = state?.UpdatedAt,
                    Route = row.Route
                };
            })
            .OrderByDescending(x => x.IsOverdue)
            .ThenBy(x => x.PlannedCollectionDate ?? x.DueDate)
            .ThenByDescending(x => PriorityRank(x.Priority))
            .ThenBy(x => x.CustomerName)
            .ThenBy(x => x.Folio)
            .ToList();
    }

    private static async Task<List<FinancePaymentScheduleRowDto>> BuildPaymentScheduleRowsAsync(NanchesoftDbContext db, IHostEnvironment environment, Guid companyId, DateTime start, DateTime end, DateTime today)
    {
        var calendarRows = await BuildPaymentsCalendarRowsAsync(db, start, end, today);
        var states = (await LoadPaymentScheduleStateRowsAsync(environment, companyId)).ToDictionary(x => x.DocumentId, x => x);

        return calendarRows
            .Select(row =>
            {
                states.TryGetValue(row.PurchaseInvoiceId, out var state);
                return new FinancePaymentScheduleRowDto
                {
                    SupplierId = row.SupplierId,
                    PurchaseInvoiceId = row.PurchaseInvoiceId,
                    SupplierName = row.SupplierName,
                    Folio = row.Folio,
                    InvoiceDate = row.InvoiceDate,
                    DueDate = row.DueDate,
                    OpenAmount = row.OpenAmount,
                    DaysToDue = row.DaysToDue,
                    PlannedPaymentDate = state?.PlannedDate?.Date ?? row.DueDate.Date,
                    ScheduleStatus = string.IsNullOrWhiteSpace(state?.Status) ? "Sin programar" : state!.Status,
                    Responsible = state?.Responsible ?? string.Empty,
                    Priority = string.IsNullOrWhiteSpace(state?.Priority) ? (row.DaysToDue < 0 ? "Alta" : row.DaysToDue <= 7 ? "Media" : "Baja") : state!.Priority,
                    Notes = state?.Notes ?? string.Empty,
                    NeedsAuthorization = row.OpenAmount >= 50000m && !string.Equals(state?.Status, "Autorizado", StringComparison.OrdinalIgnoreCase),
                    LastUpdatedAt = state?.UpdatedAt,
                    Route = row.Route
                };
            })
            .OrderByDescending(x => x.NeedsAuthorization)
            .ThenBy(x => x.PlannedPaymentDate ?? x.DueDate)
            .ThenByDescending(x => PriorityRank(x.Priority))
            .ThenBy(x => x.SupplierName)
            .ThenBy(x => x.Folio)
            .ToList();
    }

    private static async Task<List<FinanceTreasuryWeeklyPlanRowDto>> BuildWeeklyTreasuryPlanRowsAsync(NanchesoftDbContext db, IHostEnvironment environment, Guid companyId, int weeks, DateTime today)
    {
        var start = GetWeekStart(today);
        var end = start.AddDays((weeks * 7) - 1);
        var collections = await BuildCollectionCommitmentRowsAsync(db, environment, companyId, today.AddDays(-30), end, today);
        var payments = await BuildPaymentScheduleRowsAsync(db, environment, companyId, today.AddDays(-30), end, today);

        var openingBalance = (await db.CashAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m)
            + (await db.BankAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m);

        var rows = new List<FinanceTreasuryWeeklyPlanRowDto>();
        var running = openingBalance;
        for (var index = 0; index < weeks; index++)
        {
            var bucketStart = start.AddDays(index * 7);
            var bucketEnd = bucketStart.AddDays(6);
            var opening = running;
            var overdueCollectionsCarryover = collections.Where(x => !string.Equals(x.CommitmentStatus, "Cobrado", StringComparison.OrdinalIgnoreCase) && (x.PlannedCollectionDate ?? x.DueDate).Date < bucketStart).Sum(x => x.OpenAmount);
            var overduePaymentsCarryover = payments.Where(x => !string.Equals(x.ScheduleStatus, "Pagado", StringComparison.OrdinalIgnoreCase) && (x.PlannedPaymentDate ?? x.DueDate).Date < bucketStart).Sum(x => x.OpenAmount);
            var weekCollections = collections.Where(x => !string.Equals(x.CommitmentStatus, "Cobrado", StringComparison.OrdinalIgnoreCase) && (x.PlannedCollectionDate ?? x.DueDate).Date >= bucketStart && (x.PlannedCollectionDate ?? x.DueDate).Date <= bucketEnd).ToList();
            var weekPayments = payments.Where(x => !string.Equals(x.ScheduleStatus, "Pagado", StringComparison.OrdinalIgnoreCase) && (x.PlannedPaymentDate ?? x.DueDate).Date >= bucketStart && (x.PlannedPaymentDate ?? x.DueDate).Date <= bucketEnd).ToList();
            var inflows = weekCollections.Sum(x => x.OpenAmount);
            var outflows = weekPayments.Sum(x => x.OpenAmount);
            var net = inflows - outflows;
            running += net;
            rows.Add(new FinanceTreasuryWeeklyPlanRowDto
            {
                WeekStart = bucketStart,
                WeekEnd = bucketEnd,
                OpeningBalance = opening,
                PlannedCollections = inflows,
                PlannedPayments = outflows,
                NetFlow = net,
                ProjectedClosing = running,
                CollectionCount = weekCollections.Count,
                PaymentCount = weekPayments.Count,
                OverdueCollectionsCarryover = overdueCollectionsCarryover,
                OverduePaymentsCarryover = overduePaymentsCarryover
            });
        }

        return rows;
    }

    private static async Task<List<FinanceCommitmentFollowUpRowDto>> BuildCommitmentFollowUpRowsAsync(NanchesoftDbContext db, IHostEnvironment environment, Guid companyId, DateTime today)
    {
        var collections = await BuildCollectionCommitmentRowsAsync(db, environment, companyId, today.AddDays(-60), today.AddDays(60), today);
        var payments = await BuildPaymentScheduleRowsAsync(db, environment, companyId, today.AddDays(-60), today.AddDays(60), today);
        var rows = new List<FinanceCommitmentFollowUpRowDto>();

        rows.AddRange(collections
            .Where(x => !string.Equals(x.CommitmentStatus, "Cobrado", StringComparison.OrdinalIgnoreCase))
            .Where(x => x.IsOverdue || string.IsNullOrWhiteSpace(x.Responsible) || (x.PlannedCollectionDate ?? x.DueDate).Date <= today.AddDays(3))
            .Select(x => new FinanceCommitmentFollowUpRowDto
            {
                FlowType = "Cobranza",
                DocumentId = x.SalesInvoiceId,
                PartnerName = x.CustomerName,
                Folio = x.Folio,
                DueDate = x.DueDate,
                PlannedDate = x.PlannedCollectionDate,
                OpenAmount = x.OpenAmount,
                Status = x.CommitmentStatus,
                Priority = x.Priority,
                Responsible = x.Responsible,
                DaysToDue = x.DaysToDue,
                ActionRequired = x.IsOverdue ? "Cobranza inmediata" : string.IsNullOrWhiteSpace(x.Responsible) ? "Asignar responsable" : (x.PlannedCollectionDate ?? x.DueDate).Date < today ? "Reagendar compromiso" : "Confirmar promesa",
                Notes = x.Notes,
                Route = x.Route
            }));

        rows.AddRange(payments
            .Where(x => !string.Equals(x.ScheduleStatus, "Pagado", StringComparison.OrdinalIgnoreCase))
            .Where(x => x.NeedsAuthorization || string.IsNullOrWhiteSpace(x.Responsible) || (x.PlannedPaymentDate ?? x.DueDate).Date <= today.AddDays(3) || x.DaysToDue < 0)
            .Select(x => new FinanceCommitmentFollowUpRowDto
            {
                FlowType = "Pago",
                DocumentId = x.PurchaseInvoiceId,
                PartnerName = x.SupplierName,
                Folio = x.Folio,
                DueDate = x.DueDate,
                PlannedDate = x.PlannedPaymentDate,
                OpenAmount = x.OpenAmount,
                Status = x.ScheduleStatus,
                Priority = x.Priority,
                Responsible = x.Responsible,
                DaysToDue = x.DaysToDue,
                ActionRequired = x.NeedsAuthorization ? "Autorizar pago" : string.IsNullOrWhiteSpace(x.Responsible) ? "Asignar responsable" : (x.PlannedPaymentDate ?? x.DueDate).Date < today ? "Reagendar pago" : "Confirmar ejecución",
                Notes = x.Notes,
                Route = x.Route
            }));

        return rows
            .OrderByDescending(x => PriorityRank(x.Priority))
            .ThenBy(x => x.PlannedDate ?? x.DueDate)
            .ThenByDescending(x => x.OpenAmount)
            .ToList();
    }

    private static int PriorityRank(string? priority)
        => priority?.Trim().ToLowerInvariant() switch
        {
            "alta" => 3,
            "media" => 2,
            "baja" => 1,
            _ => 0
        };

    private static async Task<FinanceScenarioEvaluationDto> EvaluateScenarioAsync(NanchesoftDbContext db, FinanceScenarioRowDto scenario, DateTime today)
    {
        var horizonWeeks = scenario.HorizonWeeks <= 0 ? 8 : Math.Min(26, scenario.HorizonWeeks);
        var start = GetWeekStart(today);
        var end = start.AddDays((horizonWeeks * 7) - 1);
        var collections = await BuildCollectionsCalendarRowsAsync(db, start, end, today);
        var payments = await BuildPaymentsCalendarRowsAsync(db, start, end, today);

        var openingBalance = (await db.CashAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m)
            + (await db.BankAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m);

        var baselineRows = BuildProjectionRows(start, horizonWeeks, openingBalance, collections, payments, null);
        var scenarioRows = BuildProjectionRows(start, horizonWeeks, openingBalance, collections, payments, scenario);

        return new FinanceScenarioEvaluationDto
        {
            ScenarioName = scenario.Name,
            OpeningBalance = openingBalance,
            BaselineInflows = baselineRows.Sum(x => x.ExpectedInflows),
            BaselineOutflows = baselineRows.Sum(x => x.ExpectedOutflows),
            BaselineClosingBalance = baselineRows.LastOrDefault()?.ProjectedClosing ?? openingBalance,
            ScenarioInflows = scenarioRows.Sum(x => x.ExpectedInflows),
            ScenarioOutflows = scenarioRows.Sum(x => x.ExpectedOutflows),
            ScenarioClosingBalance = scenarioRows.LastOrDefault()?.ProjectedClosing ?? openingBalance,
            ImpactAmount = (scenarioRows.LastOrDefault()?.ProjectedClosing ?? openingBalance) - (baselineRows.LastOrDefault()?.ProjectedClosing ?? openingBalance),
            Rows = baselineRows.Join(
                scenarioRows,
                left => left.PeriodStart,
                right => right.PeriodStart,
                (left, right) => new FinanceScenarioEvaluationRowDto
                {
                    PeriodStart = left.PeriodStart,
                    PeriodEnd = left.PeriodEnd,
                    BaselineOpening = left.OpeningBalance,
                    BaselineInflows = left.ExpectedInflows,
                    BaselineOutflows = left.ExpectedOutflows,
                    BaselineClosing = left.ProjectedClosing,
                    ScenarioOpening = right.OpeningBalance,
                    ScenarioInflows = right.ExpectedInflows,
                    ScenarioOutflows = right.ExpectedOutflows,
                    ScenarioClosing = right.ProjectedClosing,
                    ImpactAmount = right.ProjectedClosing - left.ProjectedClosing
                })
                .ToList()
        };
    }

    private static List<FinanceCashFlowRowDto> BuildProjectionRows(
        DateTime weekStart,
        int horizonWeeks,
        decimal openingBalance,
        List<FinanceCollectionCalendarRowDto> collections,
        List<FinancePaymentCalendarRowDto> payments,
        FinanceScenarioRowDto? scenario)
    {
        var rows = new List<FinanceCashFlowRowDto>();
        var running = openingBalance;
        var collectionShift = scenario?.CollectionShiftDays ?? 0;
        var paymentShift = scenario?.PaymentShiftDays ?? 0;
        var salesFactor = 1m + ((scenario?.SalesGrowthPercent ?? 0m) / 100m);
        var purchaseFactor = 1m + ((scenario?.PurchaseGrowthPercent ?? 0m) / 100m);
        var expenseReductionFactor = 1m - ((scenario?.ExpenseReductionPercent ?? 0m) / 100m);
        if (expenseReductionFactor < 0m)
        {
            expenseReductionFactor = 0m;
        }

        for (var index = 0; index < horizonWeeks; index++)
        {
            var bucketStart = weekStart.AddDays(index * 7);
            var bucketEnd = bucketStart.AddDays(6);
            var inflows = collections
                .Select(x => new { DueDate = x.DueDate.AddDays(collectionShift), Amount = Math.Max(0m, x.OpenAmount) * salesFactor })
                .Where(x => x.DueDate.Date >= bucketStart && x.DueDate.Date <= bucketEnd)
                .Sum(x => x.Amount);
            var outflows = payments
                .Select(x => new { DueDate = x.DueDate.AddDays(paymentShift), Amount = Math.Max(0m, x.OpenAmount) * purchaseFactor * expenseReductionFactor })
                .Where(x => x.DueDate.Date >= bucketStart && x.DueDate.Date <= bucketEnd)
                .Sum(x => x.Amount);

            if (index == 0 && scenario is not null)
            {
                inflows += Math.Max(0m, scenario.ExtraInflow);
                outflows += Math.Max(0m, scenario.ExtraOutflow);
            }

            var closing = running + inflows - outflows;
            rows.Add(new FinanceCashFlowRowDto
            {
                PeriodStart = bucketStart,
                PeriodEnd = bucketEnd,
                OpeningBalance = running,
                ExpectedInflows = inflows,
                ExpectedOutflows = outflows,
                NetFlow = inflows - outflows,
                ProjectedClosing = closing
            });
            running = closing;
        }

        return rows;
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var candidate = date.Date;
        var diff = (7 + (candidate.DayOfWeek - DayOfWeek.Monday)) % 7;
        return candidate.AddDays(-diff);
    }

    private static decimal NormalizeAccountingAmount(AccountingAccount account, AccountingJournalEntryLine line)
        => string.Equals(account.Nature, "Credit", StringComparison.OrdinalIgnoreCase)
            ? line.Credit - line.Debit
            : line.Debit - line.Credit;


    private static string GetApprovalFilePath(IHostEnvironment environment, Guid companyId)
        => Path.Combine(GetFinanceDataDirectory(environment), $"approvals-{companyId:N}.json");

    private static async Task<List<FinanceApprovalStateRow>> LoadApprovalStatesAsync(IHostEnvironment environment, Guid companyId)
    {
        var path = GetApprovalFilePath(environment, companyId);
        if (!File.Exists(path))
        {
            return new List<FinanceApprovalStateRow>();
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<FinanceApprovalStateRow>>(stream, FinanceJsonOptions)
            ?? new List<FinanceApprovalStateRow>();
    }

    private static async Task SaveApprovalStatesAsync(IHostEnvironment environment, Guid companyId, List<FinanceApprovalStateRow> rows)
    {
        var path = GetApprovalFilePath(environment, companyId);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, rows.OrderBy(x => x.ModuleKey).ThenBy(x => x.DocumentType).ThenBy(x => x.UpdatedAt).ToList(), FinanceJsonOptions);
    }

    private static async Task UpsertApprovalStateAsync(IHostEnvironment environment, Guid companyId, FinanceApprovalStateRow row)
    {
        var rows = await LoadApprovalStatesAsync(environment, companyId);
        var existing = rows.FirstOrDefault(x => x.DocumentId == row.DocumentId && string.Equals(x.DocumentType, row.DocumentType, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            row.Id = row.Id == Guid.Empty ? Guid.NewGuid() : row.Id;
            rows.Add(row);
        }
        else
        {
            existing.ModuleKey = row.ModuleKey;
            existing.DocumentType = row.DocumentType;
            existing.Status = row.Status;
            existing.Comments = row.Comments;
            existing.UpdatedAt = row.UpdatedAt == default ? DateTime.UtcNow : row.UpdatedAt;
        }

        await SaveApprovalStatesAsync(environment, companyId, rows);
    }

    private static async Task<List<FinanceApprovalRowDto>> BuildApprovalRowsAsync(NanchesoftDbContext db, IHostEnvironment environment, Guid companyId, DateTime today)
    {
        var documents = await BuildDocumentControlRowsAsync(db, today);
        var states = await LoadApprovalStatesAsync(environment, companyId);

        return documents
            .Where(x => string.Equals(x.Status, "posted", StringComparison.OrdinalIgnoreCase))
            .Select(x =>
            {
                var state = states.FirstOrDefault(y => y.DocumentId == x.DocumentId && string.Equals(y.DocumentType, x.DocumentType, StringComparison.OrdinalIgnoreCase));
                var priority = "Baja";
                if (!x.IsAccounted || (x.DueDate.HasValue && x.DueDate.Value.Date < today && x.OpenAmount > 0m) || GetAmountOrTotal(x) >= 50000m)
                {
                    priority = "Alta";
                }
                else if (x.OpenAmount > 0m || x.AgeDays > 15)
                {
                    priority = "Media";
                }

                return new FinanceApprovalRowDto
                {
                    Module = x.Module,
                    ModuleKey = GetModuleKey(x.Module),
                    DocumentType = x.DocumentType,
                    DocumentId = x.DocumentId,
                    Folio = x.Folio,
                    PartnerName = x.PartnerName,
                    Amount = GetAmountOrTotal(x),
                    OpenAmount = x.OpenAmount,
                    DocumentDate = x.DocumentDate,
                    DueDate = x.DueDate,
                    AgeDays = x.AgeDays,
                    Priority = priority,
                    Status = state?.Status ?? "pending",
                    Notes = state?.Comments ?? string.Empty,
                    SuggestedAction = !x.IsAccounted ? "Autorizar y generar póliza." : "Autorizar seguimiento financiero."
                };
            })
            .ToList();
    }

    private static async Task<List<FinanceAlertRowDto>> BuildAlertRowsAsync(NanchesoftDbContext db, IHostEnvironment environment, Guid companyId, DateTime today)
    {
        var documents = await BuildDocumentControlRowsAsync(db, today);
        var approvals = await BuildApprovalRowsAsync(db, environment, companyId, today);
        var alerts = new List<FinanceAlertRowDto>();

        alerts.AddRange(documents
            .Where(x => string.Equals(x.Status, "posted", StringComparison.OrdinalIgnoreCase) && !x.IsAccounted)
            .Select(x => new FinanceAlertRowDto
            {
                Severity = "Alta",
                Category = "Contabilidad",
                Module = x.Module,
                Title = "Documento posteado sin póliza",
                Message = $"{x.Folio} está posteado pero todavía no tiene póliza detectada.",
                Folio = x.Folio,
                DocumentId = x.DocumentId,
                Amount = GetAmountOrTotal(x),
                DueDate = x.DueDate,
                DaysToDue = x.DueDate.HasValue ? (x.DueDate.Value.Date - today).Days : null,
                Route = $"/accounting/document-posting?sourceType={x.DocumentType}&sourceId={x.DocumentId}"
            }));

        alerts.AddRange(documents
            .Where(x => x.DueDate.HasValue && x.OpenAmount > 0m && x.DueDate.Value.Date < today)
            .Select(x => new FinanceAlertRowDto
            {
                Severity = "Alta",
                Category = x.DocumentType == "purchase_invoice" ? "Pago vencido" : "Cobranza vencida",
                Module = x.Module,
                Title = "Documento vencido",
                Message = $"{x.Folio} tiene saldo abierto y ya venció.",
                Folio = x.Folio,
                DocumentId = x.DocumentId,
                Amount = x.OpenAmount,
                DueDate = x.DueDate,
                DaysToDue = (x.DueDate!.Value.Date - today).Days,
                Route = "/finance/document-control"
            }));

        alerts.AddRange(documents
            .Where(x => x.DueDate.HasValue && x.OpenAmount > 0m && x.DueDate.Value.Date >= today && (x.DueDate.Value.Date - today).Days <= 7)
            .Select(x => new FinanceAlertRowDto
            {
                Severity = "Media",
                Category = "Próximo vencimiento",
                Module = x.Module,
                Title = "Vence en los próximos 7 días",
                Message = $"{x.Folio} requiere atención próxima por vencimiento.",
                Folio = x.Folio,
                DocumentId = x.DocumentId,
                Amount = x.OpenAmount,
                DueDate = x.DueDate,
                DaysToDue = (x.DueDate!.Value.Date - today).Days,
                Route = "/finance/document-control"
            }));

        alerts.AddRange(approvals
            .Where(x => string.Equals(x.Status, "rejected", StringComparison.OrdinalIgnoreCase) || (string.Equals(x.Status, "pending", StringComparison.OrdinalIgnoreCase) && x.Priority == "Alta"))
            .Select(x => new FinanceAlertRowDto
            {
                Severity = string.Equals(x.Status, "rejected", StringComparison.OrdinalIgnoreCase) ? "Alta" : "Media",
                Category = "Autorizaciones",
                Module = x.Module,
                Title = string.Equals(x.Status, "rejected", StringComparison.OrdinalIgnoreCase) ? "Documento rechazado" : "Autorización pendiente",
                Message = string.Equals(x.Status, "rejected", StringComparison.OrdinalIgnoreCase)
                    ? $"{x.Folio} fue rechazado y necesita revisión."
                    : $"{x.Folio} sigue pendiente de autorización financiera.",
                Folio = x.Folio,
                DocumentId = x.DocumentId,
                Amount = x.Amount,
                DueDate = x.DueDate,
                DaysToDue = x.DueDate.HasValue ? (x.DueDate.Value.Date - today).Days : null,
                Route = "/finance/authorizations"
            }));

        var negativeCash = await db.CashAccounts.AsNoTracking().Where(x => x.IsActive && x.CurrentBalance < 0m).ToListAsync();
        alerts.AddRange(negativeCash.Select(x => new FinanceAlertRowDto
        {
            Severity = "Alta",
            Category = "Tesorería",
            Module = "Tesorería",
            Title = "Caja en negativo",
            Message = $"La caja {x.Code} - {x.Name} presenta saldo negativo.",
            Folio = x.Code,
            DocumentId = x.Id,
            Amount = x.CurrentBalance,
            Route = "/finance/executive-dashboard"
        }));

        var negativeBanks = await db.BankAccounts.AsNoTracking().Where(x => x.IsActive && x.CurrentBalance < 0m).ToListAsync();
        alerts.AddRange(negativeBanks.Select(x => new FinanceAlertRowDto
        {
            Severity = "Alta",
            Category = "Tesorería",
            Module = "Tesorería",
            Title = "Banco en negativo",
            Message = $"La cuenta bancaria {x.Code} - {x.Name} presenta saldo negativo.",
            Folio = x.Code,
            DocumentId = x.Id,
            Amount = x.CurrentBalance,
            Route = "/finance/executive-dashboard"
        }));

        return alerts
            .GroupBy(x => new { x.Category, x.Title, x.Folio, x.DocumentId })
            .Select(x => x.First())
            .ToList();
    }

    private static async Task<List<FinanceSemaphoreRowDto>> BuildSemaphoreRowsAsync(NanchesoftDbContext db, IHostEnvironment environment, Guid companyId, DateTime today)
    {
        var documents = await BuildDocumentControlRowsAsync(db, today);
        var liquidity = (await db.CashAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m)
            + (await db.BankAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m);
        var payables = documents.Where(x => x.DocumentType == "purchase_invoice").Sum(x => x.OpenAmount);
        var receivables = documents.Where(x => x.DocumentType == "sales_invoice").Sum(x => x.OpenAmount);
        var workingCapital = liquidity + receivables - payables;

        var currentYear = today.Year;
        var month = today.Month;
        var budgets = (await LoadBudgetRowsAsync(environment, companyId)).Where(x => x.Year == currentYear && x.Month == month).ToList();
        var budgetAmount = budgets.Sum(x => x.BudgetAmount);
        var actualMap = await BuildActualsByAccountMonthAsync(db, companyId, currentYear);
        var actualAmount = budgets.Sum(x => actualMap.TryGetValue((x.AccountId, month), out var value) ? value : 0m);
        var budgetCompliance = budgetAmount == 0m ? 0m : Math.Round((actualAmount / budgetAmount) * 100m, 2);

        var overdueReceivables = documents.Where(x => x.DocumentType == "sales_invoice" && x.DueDate.HasValue && x.DueDate.Value.Date < today).Sum(x => x.OpenAmount);
        var overduePayables = documents.Where(x => x.DocumentType == "purchase_invoice" && x.DueDate.HasValue && x.DueDate.Value.Date < today).Sum(x => x.OpenAmount);
        var cashFlow = await BuildForecastRowsAsync(db, 4, today);
        var projectedClosing = cashFlow.LastOrDefault()?.ProjectedClosing ?? liquidity;

        return new List<FinanceSemaphoreRowDto>
        {
            CreateSemaphore("Liquidez", "Liquidez vs cuentas por pagar", liquidity, payables, higherIsBetter: true, "Mantener liquidez por encima del corto plazo."),
            CreateSemaphore("Capital", "Capital de trabajo", workingCapital, 0m, higherIsBetter: true, "Evitar que el capital de trabajo caiga en negativo."),
            CreateSemaphore("Presupuesto", "Cumplimiento del presupuesto del mes (%)", budgetCompliance, 100m, higherIsBetter: true, "Revisar desvíos importantes por cuenta contable."),
            CreateSemaphore("Cobranza", "Cartera vencida", overdueReceivables, 0m, higherIsBetter: false, "Empujar cobranza y aplicaciones de recibos."),
            CreateSemaphore("Pagos", "Pasivos vencidos", overduePayables, 0m, higherIsBetter: false, "Programar pagos o renegociar vencimientos."),
            CreateSemaphore("Flujo", "Flujo proyectado a 4 semanas", projectedClosing, 0m, higherIsBetter: true, "Ajustar egresos o fortalecer entradas proyectadas.")
        };
    }

    private static FinanceSemaphoreRowDto CreateSemaphore(string category, string label, decimal currentValue, decimal targetValue, bool higherIsBetter, string suggestion)
    {
        var variance = currentValue - targetValue;
        var variancePercent = targetValue == 0m
            ? (currentValue == 0m ? 0m : 100m)
            : Math.Round((variance / Math.Abs(targetValue)) * 100m, 2);

        string semaphore;
        if (higherIsBetter)
        {
            if (currentValue >= targetValue)
            {
                semaphore = "Verde";
            }
            else if (currentValue >= targetValue * 0.8m)
            {
                semaphore = "Amarillo";
            }
            else
            {
                semaphore = "Rojo";
            }
        }
        else
        {
            if (currentValue <= targetValue)
            {
                semaphore = "Verde";
            }
            else if (currentValue <= Math.Max(1m, targetValue + 10000m))
            {
                semaphore = "Amarillo";
            }
            else
            {
                semaphore = "Rojo";
            }
        }

        return new FinanceSemaphoreRowDto
        {
            Category = category,
            Label = label,
            CurrentValue = currentValue,
            TargetValue = targetValue,
            VarianceAmount = variance,
            VariancePercent = variancePercent,
            Semaphore = semaphore,
            SuggestedAction = suggestion
        };
    }

    private static async Task<List<FinanceCashFlowRowDto>> BuildForecastRowsAsync(NanchesoftDbContext db, int weeks, DateTime today)
    {
        var bucketCount = weeks <= 0 ? 4 : weeks;
        var weekStart = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
        if (today.DayOfWeek == DayOfWeek.Sunday)
        {
            weekStart = today.AddDays(-6);
        }

        var cashBalance = await db.CashAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m;
        var bankBalance = await db.BankAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m;
        var customers = await db.Customers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x);
        var suppliers = await db.Suppliers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x);
        var receiptApplicationsByInvoice = await db.ReceiptApplications.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled")
            .GroupBy(x => x.SalesInvoiceId)
            .ToDictionaryAsync(x => x.Key, x => x.Sum(y => y.AppliedAmount));
        var salesInvoices = await db.SalesInvoices.AsNoTracking().Where(x => x.IsActive && x.Status == "posted" && x.CustomerId != null).ToListAsync();
        var purchaseInvoices = await db.PurchaseInvoices.AsNoTracking().Where(x => x.IsActive && x.Status == "posted" && x.SupplierId != null).ToListAsync();

        var receivableFlows = salesInvoices.Select(invoice =>
        {
            var applied = receiptApplicationsByInvoice.TryGetValue(invoice.Id, out var amount) ? amount : 0m;
            var openAmount = Math.Max(0m, invoice.Total - applied);
            var term = invoice.CustomerId.HasValue && customers.TryGetValue(invoice.CustomerId.Value, out var customer) ? Math.Max(0, customer.PaymentTermDays) : 15;
            return new ForecastMovement { DueDate = invoice.InvoiceDate.Date.AddDays(term), Amount = openAmount, Type = "inflow" };
        }).Where(x => x.Amount > 0m).ToList();

        var payableFlows = purchaseInvoices.Select(invoice =>
        {
            var term = invoice.SupplierId.HasValue && suppliers.TryGetValue(invoice.SupplierId.Value, out var supplier) ? Math.Max(0, supplier.PaymentTermDays) : 30;
            return new ForecastMovement { DueDate = invoice.InvoiceDate.Date.AddDays(term), Amount = Math.Max(0m, invoice.Total), Type = "outflow" };
        }).Where(x => x.Amount > 0m).ToList();

        var rows = new List<FinanceCashFlowRowDto>();
        var running = cashBalance + bankBalance;
        for (var index = 0; index < bucketCount; index++)
        {
            var bucketStart = weekStart.AddDays(index * 7);
            var bucketEnd = bucketStart.AddDays(6);
            var opening = running;
            var inflows = receivableFlows.Where(x => x.DueDate >= bucketStart && x.DueDate <= bucketEnd).Sum(x => x.Amount);
            var outflows = payableFlows.Where(x => x.DueDate >= bucketStart && x.DueDate <= bucketEnd).Sum(x => x.Amount);
            var net = inflows - outflows;
            running += net;
            rows.Add(new FinanceCashFlowRowDto
            {
                PeriodStart = bucketStart,
                PeriodEnd = bucketEnd,
                OpeningBalance = opening,
                ExpectedInflows = inflows,
                ExpectedOutflows = outflows,
                NetFlow = net,
                ProjectedClosing = running
            });
        }

        return rows;
    }

    private static string GetModuleKey(string module)
        => module switch
        {
            "Ventas" => "sales",
            "Compras" => "purchases",
            "Tesorería" => "treasury",
            _ => "finance"
        };

    private static async Task<Dictionary<(Guid accountId, int month), decimal>> BuildActualsByAccountMonthAsync(NanchesoftDbContext db, Guid companyId, int year)
    {
        var entries = await db.Set<AccountingJournalEntry>().AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive && x.Status == "posted" && x.EntryDate.Year == year)
            .ToListAsync();

        if (entries.Count == 0)
        {
            return new Dictionary<(Guid accountId, int month), decimal>();
        }

        var entryIds = entries.Select(x => x.Id).ToHashSet();
        var monthByEntryId = entries.ToDictionary(x => x.Id, x => x.EntryDate.Month);
        var lines = await db.Set<AccountingJournalEntryLine>().AsNoTracking()
            .Where(x => entryIds.Contains(x.JournalEntryId))
            .ToListAsync();

        var accountMap = await db.Set<AccountingAccount>().AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .ToDictionaryAsync(x => x.Id, x => x);

        var result = new Dictionary<(Guid accountId, int month), decimal>();
        foreach (var line in lines)
        {
            if (!accountMap.TryGetValue(line.AccountId, out var account) || !monthByEntryId.TryGetValue(line.JournalEntryId, out var month))
            {
                continue;
            }

            var key = (line.AccountId, month);
            result[key] = result.TryGetValue(key, out var current)
                ? current + NormalizeAccountingAmount(account, line)
                : NormalizeAccountingAmount(account, line);
        }

        return result;
    }

    private static async Task<Dictionary<int, decimal>> BuildGoalActualsByMonthAsync(NanchesoftDbContext db, int year, string metricCode)
    {
        metricCode = metricCode.Trim().ToLowerInvariant();

        return metricCode switch
        {
            "sales" => await db.SalesInvoices.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate.Year == year)
                .GroupBy(x => x.InvoiceDate.Month)
                .ToDictionaryAsync(x => x.Key, x => x.Sum(y => y.Total)),

            "purchases" => await db.PurchaseInvoices.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate.Year == year)
                .GroupBy(x => x.InvoiceDate.Month)
                .ToDictionaryAsync(x => x.Key, x => x.Sum(y => y.Total)),

            "receipts" => await db.Receipts.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted" && x.ReceiptDate.Year == year)
                .GroupBy(x => x.ReceiptDate.Month)
                .ToDictionaryAsync(x => x.Key, x => x.Sum(y => y.Total)),

            "payments" => await db.Payments.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted" && x.PaymentDate.Year == year)
                .GroupBy(x => x.PaymentDate.Month)
                .ToDictionaryAsync(x => x.Key, x => x.Sum(y => y.Total)),

            _ => new Dictionary<int, decimal>()
        };
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


    private static async Task<List<FinanceYearOverYearRowDto>> BuildYearOverYearRowsAsync(NanchesoftDbContext db, int year)
    {
        var salesInvoices = await db.SalesInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate.Year >= year - 1 && x.InvoiceDate.Year <= year)
            .ToListAsync();
        var purchaseInvoices = await db.PurchaseInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate.Year >= year - 1 && x.InvoiceDate.Year <= year)
            .ToListAsync();
        var receipts = await db.Receipts.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.ReceiptDate.Year >= year - 1 && x.ReceiptDate.Year <= year)
            .ToListAsync();
        var payments = await db.Payments.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.PaymentDate.Year >= year - 1 && x.PaymentDate.Year <= year)
            .ToListAsync();
        var creditNotes = await db.CreditNotes.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled" && x.CreditNoteDate.Year >= year - 1 && x.CreditNoteDate.Year <= year)
            .ToListAsync();

        var rows = new List<FinanceYearOverYearRowDto>();
        for (var month = 1; month <= 12; month++)
        {
            var currentSales = salesInvoices.Where(x => x.InvoiceDate.Year == year && x.InvoiceDate.Month == month).Sum(x => x.Total)
                - creditNotes.Where(x => x.CreditNoteDate.Year == year && x.CreditNoteDate.Month == month).Sum(x => x.Total);
            var previousSales = salesInvoices.Where(x => x.InvoiceDate.Year == year - 1 && x.InvoiceDate.Month == month).Sum(x => x.Total)
                - creditNotes.Where(x => x.CreditNoteDate.Year == year - 1 && x.CreditNoteDate.Month == month).Sum(x => x.Total);
            var currentPurchases = purchaseInvoices.Where(x => x.InvoiceDate.Year == year && x.InvoiceDate.Month == month).Sum(x => x.Total);
            var previousPurchases = purchaseInvoices.Where(x => x.InvoiceDate.Year == year - 1 && x.InvoiceDate.Month == month).Sum(x => x.Total);
            var currentReceipts = receipts.Where(x => x.ReceiptDate.Year == year && x.ReceiptDate.Month == month).Sum(x => x.Total);
            var previousReceipts = receipts.Where(x => x.ReceiptDate.Year == year - 1 && x.ReceiptDate.Month == month).Sum(x => x.Total);
            var currentPayments = payments.Where(x => x.PaymentDate.Year == year && x.PaymentDate.Month == month).Sum(x => x.Total);
            var previousPayments = payments.Where(x => x.PaymentDate.Year == year - 1 && x.PaymentDate.Month == month).Sum(x => x.Total);

            rows.Add(new FinanceYearOverYearRowDto
            {
                Year = year,
                Month = month,
                Label = new DateTime(year, month, 1).ToString("MMMM"),
                SalesCurrent = currentSales,
                SalesPrevious = previousSales,
                SalesGrowthPercent = CalculateGrowthPercent(currentSales, previousSales),
                PurchasesCurrent = currentPurchases,
                PurchasesPrevious = previousPurchases,
                PurchasesGrowthPercent = CalculateGrowthPercent(currentPurchases, previousPurchases),
                ReceiptsCurrent = currentReceipts,
                ReceiptsPrevious = previousReceipts,
                ReceiptsGrowthPercent = CalculateGrowthPercent(currentReceipts, previousReceipts),
                PaymentsCurrent = currentPayments,
                PaymentsPrevious = previousPayments,
                PaymentsGrowthPercent = CalculateGrowthPercent(currentPayments, previousPayments)
            });
        }

        return rows;
    }

    private static async Task<List<FinanceKpiScorecardRowDto>> BuildKpiScorecardRowsAsync(NanchesoftDbContext db, IHostEnvironment environment, Guid companyId, int year, int month)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var salesActual = await db.SalesInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate >= start && x.InvoiceDate < end)
            .Select(x => (decimal?)x.Total)
            .SumAsync() ?? 0m;
        var creditNotesActual = await db.CreditNotes.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled" && x.CreditNoteDate >= start && x.CreditNoteDate < end)
            .Select(x => (decimal?)x.Total)
            .SumAsync() ?? 0m;
        salesActual -= creditNotesActual;

        var purchasesActual = await db.PurchaseInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate >= start && x.InvoiceDate < end)
            .Select(x => (decimal?)x.Total)
            .SumAsync() ?? 0m;
        var receiptsActual = await db.Receipts.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.ReceiptDate >= start && x.ReceiptDate < end)
            .Select(x => (decimal?)x.Total)
            .SumAsync() ?? 0m;
        var paymentsActual = await db.Payments.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.PaymentDate >= start && x.PaymentDate < end)
            .Select(x => (decimal?)x.Total)
            .SumAsync() ?? 0m;
        var liquidity = (await db.CashAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m)
            + (await db.BankAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m);
        var inventoryValue = await db.StockBalances.AsNoTracking()
            .Where(x => x.QuantityOnHand > 0)
            .Select(x => (decimal?)(x.QuantityOnHand * (x.AverageCost > 0m ? x.AverageCost : x.LastCost)))
            .SumAsync() ?? 0m;
        var openReceivables = await BuildOpenReceivablesAsync(db);
        var openPayables = await BuildOpenPayablesAsync(db);
        var workingCapital = liquidity + openReceivables + inventoryValue - openPayables;

        var goals = (await LoadGoalRowsAsync(environment, companyId))
            .Where(x => x.Year == year && x.Month == month)
            .ToList();
        decimal GetTarget(string metricCode) => goals.FirstOrDefault(x => x.MetricCode == metricCode)?.TargetAmount ?? 0m;

        return new List<FinanceKpiScorecardRowDto>
        {
            BuildKpiRow("sales","Ventas netas", salesActual, GetTarget("sales"), "Ingresos del mes netos de notas de crédito."),
            BuildKpiRow("purchases","Compras", purchasesActual, GetTarget("purchases"), "Compras posteadas del mes."),
            BuildKpiRow("receipts","Cobros", receiptsActual, GetTarget("receipts"), "Recibos posteados del mes."),
            BuildKpiRow("payments","Pagos", paymentsActual, GetTarget("payments"), "Pagos posteados del mes."),
            BuildKpiRow("liquidity","Liquidez", liquidity, 0m, "Suma de caja y bancos activos."),
            BuildKpiRow("working_capital","Capital de trabajo", workingCapital, 0m, "Liquidez + cuentas por cobrar + inventario - cuentas por pagar.")
        };
    }

    private static FinanceKpiScorecardRowDto BuildKpiRow(string code, string label, decimal actual, decimal target, string commentary)
    {
        var variance = actual - target;
        var compliance = target == 0m ? 0m : Math.Round((actual / target) * 100m, 2);
        var status = target == 0m
            ? (actual >= 0m ? "info" : "warning")
            : compliance >= 100m ? "success" : compliance >= 90m ? "warning" : "danger";
        return new FinanceKpiScorecardRowDto
        {
            MetricCode = code,
            MetricLabel = label,
            ActualAmount = actual,
            TargetAmount = target,
            VarianceAmount = variance,
            CompliancePercent = compliance,
            Status = status,
            Commentary = commentary
        };
    }

    private static async Task<List<FinanceWorkingCapitalBridgeRowDto>> BuildWorkingCapitalBridgeRowsAsync(NanchesoftDbContext db, DateTime today)
    {
        var liquidity = (await db.CashAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m)
            + (await db.BankAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m);
        var receivables = await BuildOpenReceivablesAsync(db);
        var inventory = await db.StockBalances.AsNoTracking()
            .Where(x => x.QuantityOnHand > 0)
            .Select(x => (decimal?)(x.QuantityOnHand * (x.AverageCost > 0m ? x.AverageCost : x.LastCost)))
            .SumAsync() ?? 0m;
        var payables = await BuildOpenPayablesAsync(db);
        var workingCapital = liquidity + receivables + inventory - payables;
        var baseTotal = liquidity + receivables + inventory;

        return new List<FinanceWorkingCapitalBridgeRowDto>
        {
            BuildBridgeRow("Liquidez", liquidity, baseTotal),
            BuildBridgeRow("Cuentas por cobrar", receivables, baseTotal),
            BuildBridgeRow("Inventario valorizado", inventory, baseTotal),
            BuildBridgeRow("Cuentas por pagar", -payables, baseTotal),
            new FinanceWorkingCapitalBridgeRowDto
            {
                Label = "Capital de trabajo",
                Amount = workingCapital,
                WeightPercent = baseTotal == 0m ? 0m : Math.Round((workingCapital / baseTotal) * 100m, 2),
                Impact = workingCapital >= 0m ? "Positivo" : "Negativo",
                Commentary = "Resultado neto después de descontar cuentas por pagar."
            }
        };
    }

    private static FinanceWorkingCapitalBridgeRowDto BuildBridgeRow(string label, decimal amount, decimal baseTotal)
        => new()
        {
            Label = label,
            Amount = amount,
            WeightPercent = baseTotal == 0m ? 0m : Math.Round((amount / baseTotal) * 100m, 2),
            Impact = amount >= 0m ? "A favor" : "En contra",
            Commentary = label switch
            {
                "Liquidez" => "Caja y bancos disponibles inmediatamente.",
                "Cuentas por cobrar" => "Saldo abierto por recuperar de clientes.",
                "Inventario valorizado" => "Existencia valorizada a costo promedio/último costo.",
                _ => "Obligaciones pendientes con proveedores."
            }
        };

    private static async Task<FinanceVariationRankingDto> BuildVariationRankingAsync(NanchesoftDbContext db, int year, int month)
    {
        var currentStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var currentEnd = currentStart.AddMonths(1);
        var previousStart = currentStart.AddMonths(-1);
        var previousEnd = currentStart;

        var customers = await db.Customers.AsNoTracking().Where(x => x.IsActive).ToListAsync();
        var suppliers = await db.Suppliers.AsNoTracking().Where(x => x.IsActive).ToListAsync();
        var salesInvoices = await db.SalesInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate >= previousStart && x.InvoiceDate < currentEnd)
            .ToListAsync();
        var purchaseInvoices = await db.PurchaseInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate >= previousStart && x.InvoiceDate < currentEnd)
            .ToListAsync();

        var customerRows = customers.Select(customer =>
        {
            var currentAmount = salesInvoices.Where(x => x.CustomerId == customer.Id && x.InvoiceDate >= currentStart && x.InvoiceDate < currentEnd).Sum(x => x.Total);
            var previousAmount = salesInvoices.Where(x => x.CustomerId == customer.Id && x.InvoiceDate >= previousStart && x.InvoiceDate < previousEnd).Sum(x => x.Total);
            return new FinanceVariationRankingRowDto
            {
                PartyId = customer.Id,
                PartyName = customer.Name,
                CurrentAmount = currentAmount,
                PreviousAmount = previousAmount,
                VariationAmount = currentAmount - previousAmount,
                VariationPercent = CalculateGrowthPercent(currentAmount, previousAmount),
                Direction = currentAmount - previousAmount >= 0m ? "Sube" : "Baja",
                Route = $"/accounts-receivable/statements?customerId={customer.Id}"
            };
        }).Where(x => x.CurrentAmount != 0m || x.PreviousAmount != 0m).ToList();

        var supplierRows = suppliers.Select(supplier =>
        {
            var currentAmount = purchaseInvoices.Where(x => x.SupplierId == supplier.Id && x.InvoiceDate >= currentStart && x.InvoiceDate < currentEnd).Sum(x => x.Total);
            var previousAmount = purchaseInvoices.Where(x => x.SupplierId == supplier.Id && x.InvoiceDate >= previousStart && x.InvoiceDate < previousEnd).Sum(x => x.Total);
            return new FinanceVariationRankingRowDto
            {
                PartyId = supplier.Id,
                PartyName = supplier.Name,
                CurrentAmount = currentAmount,
                PreviousAmount = previousAmount,
                VariationAmount = currentAmount - previousAmount,
                VariationPercent = CalculateGrowthPercent(currentAmount, previousAmount),
                Direction = currentAmount - previousAmount >= 0m ? "Sube" : "Baja",
                Route = $"/accounts-payable/statements?supplierId={supplier.Id}"
            };
        }).Where(x => x.CurrentAmount != 0m || x.PreviousAmount != 0m).ToList();

        return new FinanceVariationRankingDto
        {
            Year = year,
            Month = month,
            CustomerGrowth = customerRows.OrderByDescending(x => x.VariationAmount).Take(10).ToList(),
            CustomerDecline = customerRows.OrderBy(x => x.VariationAmount).Take(10).ToList(),
            SupplierGrowth = supplierRows.OrderByDescending(x => x.VariationAmount).Take(10).ToList(),
            SupplierDecline = supplierRows.OrderBy(x => x.VariationAmount).Take(10).ToList()
        };
    }

    private static async Task<FinanceBoardPackDto> BuildBoardPackAsync(NanchesoftDbContext db, IHostEnvironment environment, Guid? companyId, int year, int month)
    {
        var monthlyRows = new List<FinanceBoardPackMonthlyRowDto>();
        for (var offset = 5; offset >= 0; offset--)
        {
            var cursor = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-offset);
            var start = cursor;
            var end = cursor.AddMonths(1);

            var sales = await db.SalesInvoices.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate >= start && x.InvoiceDate < end)
                .Select(x => (decimal?)x.Total)
                .SumAsync() ?? 0m;
            var credits = await db.CreditNotes.AsNoTracking()
                .Where(x => x.IsActive && x.Status != "cancelled" && x.CreditNoteDate >= start && x.CreditNoteDate < end)
                .Select(x => (decimal?)x.Total)
                .SumAsync() ?? 0m;
            sales -= credits;

            var purchases = await db.PurchaseInvoices.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate >= start && x.InvoiceDate < end)
                .Select(x => (decimal?)x.Total)
                .SumAsync() ?? 0m;
            var receipts = await db.Receipts.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted" && x.ReceiptDate >= start && x.ReceiptDate < end)
                .Select(x => (decimal?)x.Total)
                .SumAsync() ?? 0m;
            var payments = await db.Payments.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted" && x.PaymentDate >= start && x.PaymentDate < end)
                .Select(x => (decimal?)x.Total)
                .SumAsync() ?? 0m;

            monthlyRows.Add(new FinanceBoardPackMonthlyRowDto
            {
                Year = cursor.Year,
                Month = cursor.Month,
                Label = cursor.ToString("yyyy-MM"),
                Sales = sales,
                Purchases = purchases,
                Receipts = receipts,
                Payments = payments,
                NetCash = receipts - payments
            });
        }

        var current = monthlyRows.Last();
        var liquidity = (await db.CashAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m)
            + (await db.BankAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m);
        var openReceivables = await BuildOpenReceivablesAsync(db);
        var openPayables = await BuildOpenPayablesAsync(db);
        var inventoryValue = await db.StockBalances.AsNoTracking()
            .Where(x => x.QuantityOnHand > 0)
            .Select(x => (decimal?)(x.QuantityOnHand * (x.AverageCost > 0m ? x.AverageCost : x.LastCost)))
            .SumAsync() ?? 0m;

        decimal budget = 0m;
        decimal actual = current.Sales - current.Purchases;
        decimal goal = 0m;
        decimal goalActual = current.Receipts;

        if (companyId.HasValue)
        {
            var budgetRows = (await LoadBudgetRowsAsync(environment, companyId.Value)).Where(x => x.Year == year && x.Month == month).ToList();
            budget = budgetRows.Sum(x => x.BudgetAmount);

            if (budgetRows.Count > 0)
            {
                var actualMap = await BuildActualsByAccountMonthAsync(db, companyId.Value, year);
                actual = budgetRows.Sum(x => actualMap.TryGetValue((x.AccountId, month), out var value) ? value : 0m);
                foreach (var row in monthlyRows)
                {
                    var rows = (await LoadBudgetRowsAsync(environment, companyId.Value)).Where(x => x.Year == row.Year && x.Month == row.Month).ToList();
                    row.Budget = rows.Sum(x => x.BudgetAmount);
                    var rowActualMap = row.Year == year ? actualMap : await BuildActualsByAccountMonthAsync(db, companyId.Value, row.Year);
                    row.Actual = rows.Sum(x => rowActualMap.TryGetValue((x.AccountId, row.Month), out var value) ? value : 0m);
                }
            }

            var goalRows = (await LoadGoalRowsAsync(environment, companyId.Value)).Where(x => x.Year == year && x.Month == month).ToList();
            goal = goalRows.Sum(x => x.TargetAmount);
        }

        return new FinanceBoardPackDto
        {
            Year = year,
            Month = month,
            MonthLabel = new DateTime(year, month, 1).ToString("MMMM yyyy"),
            SalesCurrentMonth = current.Sales,
            PurchasesCurrentMonth = current.Purchases,
            ReceiptsCurrentMonth = current.Receipts,
            PaymentsCurrentMonth = current.Payments,
            NetCashCurrentMonth = current.NetCash,
            OpenReceivables = openReceivables,
            OpenPayables = openPayables,
            InventoryValue = inventoryValue,
            WorkingCapital = liquidity + openReceivables + inventoryValue - openPayables,
            BudgetCurrentMonth = budget,
            ActualCurrentMonth = actual,
            GoalCurrentMonth = goal,
            GoalActualCurrentMonth = goalActual,
            BudgetCompliancePercent = budget == 0m ? 0m : Math.Round((actual / budget) * 100m, 2),
            GoalCompliancePercent = goal == 0m ? 0m : Math.Round((goalActual / goal) * 100m, 2),
            MonthlyRows = monthlyRows
        };
    }

    private static async Task<List<FinanceLiquidityRadarRowDto>> BuildLiquidityRadarRowsAsync(NanchesoftDbContext db, int year, int month)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var openingLiquidity = (await db.CashAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m)
            + (await db.BankAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m);
        var customers = await db.Customers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x);
        var suppliers = await db.Suppliers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x);
        var receiptApplicationsByInvoice = await db.ReceiptApplications.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled")
            .GroupBy(x => x.SalesInvoiceId)
            .ToDictionaryAsync(x => x.Key, x => x.Sum(y => y.AppliedAmount));

        var salesInvoices = await db.SalesInvoices.AsNoTracking().Where(x => x.IsActive && x.Status == "posted" && x.CustomerId != null).ToListAsync();
        var purchaseInvoices = await db.PurchaseInvoices.AsNoTracking().Where(x => x.IsActive && x.Status == "posted" && x.SupplierId != null).ToListAsync();

        var inflows = salesInvoices.Select(invoice =>
        {
            var applied = receiptApplicationsByInvoice.TryGetValue(invoice.Id, out var amount) ? amount : 0m;
            var openAmount = Math.Max(0m, invoice.Total - applied);
            var term = invoice.CustomerId.HasValue && customers.TryGetValue(invoice.CustomerId.Value, out var customer) ? Math.Max(0, customer.PaymentTermDays) : 15;
            return new ForecastMovement { DueDate = invoice.InvoiceDate.Date.AddDays(term), Amount = openAmount, Type = "inflow" };
        }).Where(x => x.Amount > 0m).ToList();

        var outflows = purchaseInvoices.Select(invoice =>
        {
            var term = invoice.SupplierId.HasValue && suppliers.TryGetValue(invoice.SupplierId.Value, out var supplier) ? Math.Max(0, supplier.PaymentTermDays) : 30;
            return new ForecastMovement { DueDate = invoice.InvoiceDate.Date.AddDays(term), Amount = Math.Max(0m, invoice.Total), Type = "outflow" };
        }).Where(x => x.Amount > 0m).ToList();

        var horizons = new[] { 7, 14, 30, 60 };
        var rows = new List<FinanceLiquidityRadarRowDto>();
        foreach (var days in horizons)
        {
            var limit = start.AddDays(days);
            var expectedInflows = inflows.Where(x => x.DueDate >= start && x.DueDate < limit).Sum(x => x.Amount);
            var expectedOutflows = outflows.Where(x => x.DueDate >= start && x.DueDate < limit).Sum(x => x.Amount);
            var net = openingLiquidity + expectedInflows - expectedOutflows;
            var coverage = expectedOutflows == 0m ? 0m : Math.Round((openingLiquidity + expectedInflows) / expectedOutflows, 2);
            var risk = net < 0m ? "Rojo" : coverage < 1.15m ? "Amarillo" : "Verde";
            var action = risk == "Rojo" ? "Ajustar pagos y acelerar cobranza." : risk == "Amarillo" ? "Monitorear programación semanal." : "Mantener disciplina de cobro y pago.";
            rows.Add(new FinanceLiquidityRadarRowDto
            {
                HorizonLabel = $"{days} días",
                Days = days,
                OpeningLiquidity = openingLiquidity,
                ExpectedInflows = expectedInflows,
                ExpectedOutflows = expectedOutflows,
                NetPosition = net,
                CoverageRatio = coverage,
                RiskLevel = risk,
                SuggestedAction = action
            });
        }
        return rows;
    }

    private static async Task<List<FinanceCashConversionCycleRowDto>> BuildCashConversionCycleRowsAsync(NanchesoftDbContext db, int year)
    {
        var rows = new List<FinanceCashConversionCycleRowDto>();
        var openReceivables = await BuildOpenReceivablesAsync(db);
        var openPayables = await BuildOpenPayablesAsync(db);
        var inventoryValue = await db.StockBalances.AsNoTracking()
            .Where(x => x.QuantityOnHand > 0)
            .Select(x => (decimal?)(x.QuantityOnHand * (x.AverageCost > 0m ? x.AverageCost : x.LastCost)))
            .SumAsync() ?? 0m;

        for (var month = 1; month <= 12; month++)
        {
            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);
            var days = DateTime.DaysInMonth(year, month);
            var sales = await db.SalesInvoices.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate >= start && x.InvoiceDate < end)
                .Select(x => (decimal?)x.Total)
                .SumAsync() ?? 0m;
            var credits = await db.CreditNotes.AsNoTracking()
                .Where(x => x.IsActive && x.Status != "cancelled" && x.CreditNoteDate >= start && x.CreditNoteDate < end)
                .Select(x => (decimal?)x.Total)
                .SumAsync() ?? 0m;
            sales -= credits;
            var purchases = await db.PurchaseInvoices.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate >= start && x.InvoiceDate < end)
                .Select(x => (decimal?)x.Total)
                .SumAsync() ?? 0m;

            var dso = sales <= 0m ? 0m : Math.Round((openReceivables / sales) * days, 2);
            var dio = purchases <= 0m ? 0m : Math.Round((inventoryValue / purchases) * days, 2);
            var dpo = purchases <= 0m ? 0m : Math.Round((openPayables / purchases) * days, 2);

            rows.Add(new FinanceCashConversionCycleRowDto
            {
                Year = year,
                Month = month,
                Label = start.ToString("MMMM"),
                Sales = sales,
                Purchases = purchases,
                InventoryValue = inventoryValue,
                OpenReceivables = openReceivables,
                OpenPayables = openPayables,
                DsoDays = dso,
                DioDays = dio,
                DpoDays = dpo,
                CashConversionCycleDays = Math.Round(dso + dio - dpo, 2)
            });
        }

        return rows;
    }

    private static async Task<FinanceStressTestDto> BuildStressTestsAsync(NanchesoftDbContext db, int year, int month)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var liquidity = (await db.CashAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m)
            + (await db.BankAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m);
        var inflows = await db.Receipts.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.ReceiptDate >= start && x.ReceiptDate < end)
            .Select(x => (decimal?)x.Total)
            .SumAsync() ?? 0m;
        var outflows = await db.Payments.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.PaymentDate >= start && x.PaymentDate < end)
            .Select(x => (decimal?)x.Total)
            .SumAsync() ?? 0m;
        var baseClosing = liquidity + inflows - outflows;

        FinanceStressTestScenarioRowDto BuildScenario(string scenario, decimal inflowPct, decimal outflowPct, decimal extraInflow = 0m, decimal extraOutflow = 0m, string notes = "")
        {
            var inflowDelta = Math.Round((inflows * inflowPct) + extraInflow, 2);
            var outflowDelta = Math.Round((outflows * outflowPct) + extraOutflow, 2);
            var closing = liquidity + inflows + inflowDelta - outflows - outflowDelta;
            var variance = closing - baseClosing;
            var impact = closing < 0m ? "Crítico" : variance < 0m ? "Presión" : "Controlado";
            return new FinanceStressTestScenarioRowDto
            {
                Scenario = scenario,
                InflowDelta = inflowDelta,
                OutflowDelta = outflowDelta,
                ClosingCash = closing,
                VarianceVsBase = variance,
                ImpactLevel = impact,
                Notes = notes
            };
        }

        return new FinanceStressTestDto
        {
            Year = year,
            Month = month,
            BaseLiquidity = liquidity,
            BaseInflows = inflows,
            BaseOutflows = outflows,
            BaseClosingCash = baseClosing,
            Rows = new List<FinanceStressTestScenarioRowDto>
            {
                BuildScenario("Base", 0m, 0m, 0m, 0m, "Escenario real del mes seleccionado."),
                BuildScenario("Cobras 10% menos", -0.10m, 0m, 0m, 0m, "Menor entrada de efectivo esperada."),
                BuildScenario("Pagas 10% más", 0m, 0.10m, 0m, 0m, "Mayor presión de salida de caja."),
                BuildScenario("Cobras 15% menos y pagas 10% más", -0.15m, 0.10m, 0m, 0m, "Escenario de tensión comercial y operativa."),
                BuildScenario("Cobro extraordinario", 0m, 0m, inflows * 0.20m, 0m, "Recuperación adicional de cartera."),
                BuildScenario("Salida extraordinaria", 0m, 0m, 0m, outflows * 0.20m, "Pago no previsto o inversión urgente.")
            }
        };
    }


    private static async Task<decimal> BuildOpenReceivablesAsync(NanchesoftDbContext db)
    {
        var sales = await db.SalesInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted")
            .Select(x => (decimal?)x.Total)
            .SumAsync() ?? 0m;
        var credits = await db.CreditNotes.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled")
            .Select(x => (decimal?)x.Total)
            .SumAsync() ?? 0m;
        var applied = await db.ReceiptApplications.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled")
            .Select(x => (decimal?)x.AppliedAmount)
            .SumAsync() ?? 0m;
        return Math.Max(0m, sales - credits - applied);
    }

    private static async Task<decimal> BuildOpenPayablesAsync(NanchesoftDbContext db)
    {
        var purchases = await db.PurchaseInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted")
            .Select(x => (decimal?)x.Total)
            .SumAsync() ?? 0m;
        var payments = await db.Payments.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted")
            .Select(x => (decimal?)x.Total)
            .SumAsync() ?? 0m;
        return Math.Max(0m, purchases - payments);
    }

    private static decimal CalculateGrowthPercent(decimal current, decimal previous)
    {
        if (previous == 0m)
        {
            return current == 0m ? 0m : 100m;
        }
        return Math.Round(((current - previous) / previous) * 100m, 2);
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


    private static async Task<List<FinanceActionCenterRowDto>> BuildActionCenterRowsAsync(NanchesoftDbContext db, IHostEnvironment environment, Guid? companyId, DateTime today)
    {
        var rows = new List<FinanceActionCenterRowDto>();

        if (companyId.HasValue)
        {
            var approvals = await BuildApprovalRowsAsync(db, environment, companyId.Value, today);
            rows.AddRange(approvals.Select(x => new FinanceActionCenterRowDto
            {
                Source = "Autorizaciones",
                Category = x.Module,
                Priority = x.Priority,
                Status = x.Status,
                Owner = "Finanzas",
                DocumentNumber = x.Folio,
                PartyName = x.PartnerName,
                Amount = x.OpenAmount > 0m ? x.OpenAmount : x.Amount,
                DueDate = x.DueDate ?? x.DocumentDate,
                RecommendedAction = x.SuggestedAction,
                Route = "/finance/authorizations"
            }));

            var alerts = await BuildAlertRowsAsync(db, environment, companyId.Value, today);
            rows.AddRange(alerts.Select(x => new FinanceActionCenterRowDto
            {
                Source = "Alertas",
                Category = x.Category,
                Priority = x.Severity,
                Status = x.Module,
                Owner = "Monitoreo",
                DocumentNumber = x.Folio,
                PartyName = x.Title,
                Amount = x.Amount ?? 0m,
                DueDate = x.DueDate,
                RecommendedAction = x.Message,
                Route = x.Route
            }));

            var commitments = await BuildCollectionCommitmentRowsAsync(db, environment, companyId.Value, today.AddDays(-30), today.AddDays(30), today);
            rows.AddRange(commitments.Where(x => x.CommitmentStatus != "done").Select(x => new FinanceActionCenterRowDto
            {
                Source = "Cobranza",
                Category = "Compromiso",
                Priority = x.Priority,
                Status = x.CommitmentStatus,
                Owner = string.IsNullOrWhiteSpace(x.Responsible) ? "Sin asignar" : x.Responsible,
                DocumentNumber = x.Folio,
                PartyName = x.CustomerName,
                Amount = x.OpenAmount,
                DueDate = x.PlannedCollectionDate ?? x.DueDate,
                RecommendedAction = "Dar seguimiento al compromiso de cobro",
                Route = x.Route
            }));

            var schedule = await BuildPaymentScheduleRowsAsync(db, environment, companyId.Value, today.AddDays(-30), today.AddDays(30), today);
            rows.AddRange(schedule.Where(x => x.ScheduleStatus != "done").Select(x => new FinanceActionCenterRowDto
            {
                Source = "Pagos",
                Category = "Programación",
                Priority = x.Priority,
                Status = x.ScheduleStatus,
                Owner = string.IsNullOrWhiteSpace(x.Responsible) ? "Sin asignar" : x.Responsible,
                DocumentNumber = x.Folio,
                PartyName = x.SupplierName,
                Amount = x.OpenAmount,
                DueDate = x.PlannedPaymentDate ?? x.DueDate,
                RecommendedAction = x.NeedsAuthorization ? "Autorizar y programar pago" : "Ejecutar pago programado",
                Route = x.Route
            }));
        }

        return rows
            .OrderByDescending(x => PriorityRank(x.Priority))
            .ThenBy(x => x.DueDate ?? DateTime.MaxValue)
            .Take(120)
            .ToList();
    }

    private static async Task<List<FinanceClosingCockpitRowDto>> BuildClosingCockpitRowsAsync(NanchesoftDbContext db, int year, int month)
    {
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var draftEntries = await db.Set<AccountingJournalEntry>().AsNoTracking()
            .CountAsync(x => x.IsActive && x.Status == "draft" && x.EntryDate >= monthStart && x.EntryDate < monthEnd);

        var openPeriod = await db.Set<AccountingFiscalPeriod>().AsNoTracking()
            .AnyAsync(x => x.IsActive && x.Year == year && x.Month == month && x.Status == "open");

        var salesPosted = await db.SalesInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate >= monthStart && x.InvoiceDate < monthEnd)
            .ToListAsync();

        var purchasePosted = await db.PurchaseInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate >= monthStart && x.InvoiceDate < monthEnd)
            .ToListAsync();

        var receiptsPosted = await db.Receipts.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.ReceiptDate >= monthStart && x.ReceiptDate < monthEnd)
            .ToListAsync();

        var paymentsPosted = await db.Payments.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.PaymentDate >= monthStart && x.PaymentDate < monthEnd)
            .ToListAsync();

        var unreconciledBank = await db.BankMovements.AsNoTracking()
            .CountAsync(x => x.IsActive && !x.IsReconciled && x.MovementDate >= monthStart && x.MovementDate < monthEnd);

        var overdueReceivables = await db.SalesInvoices.AsNoTracking()
            .CountAsync(x => x.IsActive && x.Status == "posted" && x.InvoiceDate < monthStart);

        var overduePayables = await db.PurchaseInvoices.AsNoTracking()
            .CountAsync(x => x.IsActive && x.Status == "posted" && x.InvoiceDate < monthStart);

        return new List<FinanceClosingCockpitRowDto>
        {
            new() { Area = "Contabilidad", CheckName = "Pólizas en borrador", PendingCount = draftEntries, Amount = 0m, Status = draftEntries == 0 ? "ok" : draftEntries <= 5 ? "warning" : "critical", Detail = "Pólizas del periodo pendientes de aprobación/posteo.", Route = "/accounting/journal-entries" },
            new() { Area = "Contabilidad", CheckName = "Periodo contable", PendingCount = openPeriod ? 1 : 0, Amount = 0m, Status = openPeriod ? "warning" : "ok", Detail = openPeriod ? "Periodo aún abierto." : "Periodo ya cerrado.", Route = "/accounting/fiscal-periods" },
            new() { Area = "Ventas", CheckName = "Facturas posteadas", PendingCount = salesPosted.Count, Amount = salesPosted.Sum(x => x.Total), Status = salesPosted.Count == 0 ? "warning" : "ok", Detail = "Volumen del mes para validación comercial y CFDI.", Route = "/sales/invoices" },
            new() { Area = "Compras", CheckName = "Facturas proveedor posteadas", PendingCount = purchasePosted.Count, Amount = purchasePosted.Sum(x => x.Total), Status = purchasePosted.Count == 0 ? "warning" : "ok", Detail = "Volumen del mes para cierre de compras.", Route = "/purchases/invoices" },
            new() { Area = "Tesorería", CheckName = "Movimientos sin conciliar", PendingCount = unreconciledBank, Amount = 0m, Status = unreconciledBank == 0 ? "ok" : unreconciledBank <= 10 ? "warning" : "critical", Detail = "Banco sin conciliación del periodo.", Route = "/treasury/reconciliations" },
            new() { Area = "CxC", CheckName = "Cartera previa abierta", PendingCount = overdueReceivables, Amount = 0m, Status = overdueReceivables == 0 ? "ok" : "warning", Detail = "Facturas con antigüedad anterior al periodo.", Route = "/accounts-receivable/aging" },
            new() { Area = "CxP", CheckName = "Pasivos previos abiertos", PendingCount = overduePayables, Amount = 0m, Status = overduePayables == 0 ? "ok" : "warning", Detail = "Facturas proveedor con antigüedad anterior al periodo.", Route = "/accounts-payable/aging" },
            new() { Area = "Tesorería", CheckName = "Cobros y pagos del mes", PendingCount = receiptsPosted.Count + paymentsPosted.Count, Amount = receiptsPosted.Sum(x => x.Total) + paymentsPosted.Sum(x => x.Total), Status = (receiptsPosted.Count + paymentsPosted.Count) == 0 ? "warning" : "ok", Detail = "Confirmar tesorería del periodo antes del cierre.", Route = "/treasury/dashboard" }
        };
    }

    private static async Task<List<FinanceCovenantRowDto>> BuildCovenantRowsAsync(NanchesoftDbContext db, int year, int month)
    {
        var dashboard = await BuildDashboardSnapshotAsync(db, DateTime.UtcNow.Date);
        var openReceivables = dashboard.OpenReceivables;
        var openPayables = dashboard.OpenPayables;
        var liquidity = dashboard.TotalLiquidity;
        var workingCapital = dashboard.WorkingCapital;

        var companyId = await GetPrimaryCompanyIdAsync(db);
        decimal concentration = 0m;
        if (companyId.HasValue)
        {
            var collectionPerf = await BuildCollectionPerformanceRowsAsync(db, NullHostEnvironment.Instance, companyId.Value, year, month, DateTime.UtcNow.Date);
            var totalOpen = collectionPerf.Sum(x => x.OpenAmount);
            concentration = totalOpen == 0m ? 0m : collectionPerf.OrderByDescending(x => x.OpenAmount).Take(3).Sum(x => x.OpenAmount) / totalOpen;
        }

        var monthlySales = await db.SalesInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate.Year == year && x.InvoiceDate.Month == month)
            .Select(x => (decimal?)x.Total)
            .SumAsync() ?? 0m;

        var rows = new List<FinanceCovenantRowDto>();

        var currentRatio = openPayables == 0m ? liquidity : liquidity / Math.Max(openPayables, 1m);
        rows.Add(BuildCovenant("CR", "Cobertura de liquidez", currentRatio, 1.20m, ">=", "Mide cuánto cubre la liquidez disponible al pasivo abierto."));

        var workingCapRatio = openPayables == 0m ? workingCapital : workingCapital / Math.Max(openPayables, 1m);
        rows.Add(BuildCovenant("WC", "Cobertura de capital trabajo", workingCapRatio, 0.50m, ">=", "Presión sobre capital de trabajo contra cuentas por pagar."));

        var receivableToSales = monthlySales == 0m ? 0m : openReceivables / monthlySales;
        rows.Add(BuildCovenant("AR", "CxC / ventas del mes", receivableToSales, 1.50m, "<=", "Valida acumulación de cartera sobre el ritmo de ventas."));

        var payablePressure = liquidity == 0m ? openPayables : openPayables / Math.Max(liquidity, 1m);
        rows.Add(BuildCovenant("AP", "CxP / liquidez", payablePressure, 1.10m, "<=", "Presión de pagos frente a caja y bancos."));

        rows.Add(BuildCovenant("CC", "Concentración top 3 clientes", concentration, 0.60m, "<=", "Dependencia de la cartera en pocos clientes."));

        return rows;
    }

    private static FinanceCovenantRowDto BuildCovenant(string code, string name, decimal current, decimal threshold, string op, string notes)
    {
        var ok = op == ">=" ? current >= threshold : current <= threshold;
        var headroom = op == ">=" ? current - threshold : threshold - current;
        return new FinanceCovenantRowDto
        {
            CovenantCode = code,
            CovenantName = name,
            CurrentValue = Math.Round(current, 4),
            ThresholdValue = threshold,
            ThresholdOperator = op,
            Headroom = Math.Round(headroom, 4),
            Status = ok ? (Math.Abs(headroom) <= 0.10m ? "warning" : "ok") : "critical",
            Notes = notes
        };
    }

    private static async Task<List<FinanceRollingForecastRowDto>> BuildRollingForecastRowsAsync(NanchesoftDbContext db, IHostEnvironment environment, Guid companyId, int months, DateTime today)
    {
        var bucketCount = Math.Clamp(months, 3, 18);
        var firstMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var horizonEnd = firstMonth.AddMonths(bucketCount).AddDays(-1).Date;

        var scheduledCollections = await BuildCollectionCommitmentRowsAsync(db, environment, companyId, firstMonth, horizonEnd, today);
        var dueCollections = await BuildCollectionsCalendarRowsAsync(db, firstMonth, horizonEnd, today);
        var scheduledPayments = await BuildPaymentScheduleRowsAsync(db, environment, companyId, firstMonth, horizonEnd, today);
        var duePayments = await BuildPaymentsCalendarRowsAsync(db, firstMonth, horizonEnd, today);
        var budgetRows = await LoadBudgetRowsAsync(environment, companyId);
        var goalRows = await LoadGoalRowsAsync(environment, companyId);

        var running = (await db.CashAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m)
            + (await db.BankAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m);

        var rows = new List<FinanceRollingForecastRowDto>();
        for (var offset = 0; offset < bucketCount; offset++)
        {
            var monthStart = firstMonth.AddMonths(offset);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1).Date;
            var opening = running;

            var collectionsScheduled = scheduledCollections
                .Where(x => (x.PlannedCollectionDate ?? x.DueDate).Date >= monthStart.Date && (x.PlannedCollectionDate ?? x.DueDate).Date <= monthEnd)
                .Sum(x => x.OpenAmount);
            var collectionsDue = dueCollections
                .Where(x => x.DueDate.Date >= monthStart.Date && x.DueDate.Date <= monthEnd)
                .Sum(x => x.OpenAmount);
            var paymentsScheduledAmount = scheduledPayments
                .Where(x => (x.PlannedPaymentDate ?? x.DueDate).Date >= monthStart.Date && (x.PlannedPaymentDate ?? x.DueDate).Date <= monthEnd)
                .Sum(x => x.OpenAmount);
            var paymentsDue = duePayments
                .Where(x => x.DueDate.Date >= monthStart.Date && x.DueDate.Date <= monthEnd)
                .Sum(x => x.OpenAmount);

            var projectedInflows = Math.Max(collectionsScheduled, collectionsDue);
            var projectedOutflows = Math.Max(paymentsScheduledAmount, paymentsDue);
            var net = projectedInflows - projectedOutflows;
            var closing = opening + net;

            var budgetAmount = budgetRows
                .Where(x => x.Year == monthStart.Year && x.Month == monthStart.Month)
                .Sum(x => x.BudgetAmount);
            var salesTarget = goalRows
                .Where(x => x.Year == monthStart.Year && x.Month == monthStart.Month && x.MetricCode == "sales")
                .Sum(x => x.TargetAmount);
            var purchasesTarget = goalRows
                .Where(x => x.Year == monthStart.Year && x.Month == monthStart.Month && x.MetricCode == "purchases")
                .Sum(x => x.TargetAmount);

            var risk = closing < 0m
                ? "Alta"
                : projectedOutflows > 0m && closing < (projectedOutflows * 0.25m)
                    ? "Media"
                    : "Baja";

            rows.Add(new FinanceRollingForecastRowDto
            {
                Label = monthStart.ToString("MMMM yyyy"),
                OpeningLiquidity = opening,
                CollectionsScheduled = collectionsScheduled,
                CollectionsDue = collectionsDue,
                PaymentsScheduled = paymentsScheduledAmount,
                PaymentsDue = paymentsDue,
                ProjectedInflows = projectedInflows,
                ProjectedOutflows = projectedOutflows,
                NetFlow = net,
                ClosingLiquidity = closing,
                SalesTarget = salesTarget,
                PurchasesTarget = purchasesTarget,
                BudgetAmount = budgetAmount,
                RiskLevel = risk
            });

            running = closing;
        }

        return rows;
    }

    private static async Task<List<FinanceCreditPolicyRowDto>> BuildCreditPolicyRowsAsync(NanchesoftDbContext db, IHostEnvironment environment, Guid companyId, DateTime today)
    {
        var start = today.AddYears(-1);
        var end = today.AddYears(1);
        var commitments = await BuildCollectionCommitmentRowsAsync(db, environment, companyId, start, end, today);
        var customers = await db.Customers.AsNoTracking()
            .Where(x => x.IsActive && x.CompanyId == companyId)
            .ToDictionaryAsync(x => x.Id);

        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);
        var salesByCustomer = await db.SalesInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.CustomerId != null && x.CompanyId == companyId && x.InvoiceDate >= monthStart && x.InvoiceDate < monthEnd)
            .GroupBy(x => x.CustomerId!.Value)
            .Select(x => new { CustomerId = x.Key, Amount = x.Sum(y => y.Total) })
            .ToDictionaryAsync(x => x.CustomerId, x => x.Amount);
        var creditByCustomer = await db.CreditNotes.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled" && x.CustomerId != null && x.CompanyId == companyId && x.CreditNoteDate >= monthStart && x.CreditNoteDate < monthEnd)
            .GroupBy(x => x.CustomerId!.Value)
            .Select(x => new { CustomerId = x.Key, Amount = x.Sum(y => y.Total) })
            .ToDictionaryAsync(x => x.CustomerId, x => x.Amount);

        return commitments
            .GroupBy(x => x.CustomerId)
            .Select(group =>
            {
                customers.TryGetValue(group.Key, out var customer);
                var openAmount = group.Sum(x => x.OpenAmount);
                var overdueRows = group.Where(x => x.IsOverdue).ToList();
                var overdueAmount = overdueRows.Sum(x => x.OpenAmount);
                var promisedAmount = group.Where(x => (x.PlannedCollectionDate ?? x.DueDate).Date >= today && (x.PlannedCollectionDate ?? x.DueDate).Date <= today.AddDays(30)).Sum(x => x.OpenAmount);
                var creditLimit = customer?.CreditLimit ?? 0m;
                var available = creditLimit - openAmount;
                var utilization = creditLimit <= 0m ? 0m : Math.Round((openAmount / creditLimit) * 100m, 2);
                var monthSales = Math.Max(0m, (salesByCustomer.TryGetValue(group.Key, out var sales) ? sales : 0m) - (creditByCustomer.TryGetValue(group.Key, out var credits) ? credits : 0m));
                var avgPastDue = overdueRows.Count == 0 ? 0m : Math.Round(overdueRows.Average(x => (decimal)Math.Abs(x.DaysToDue)), 2);
                var status = overdueAmount > 0m && utilization >= 90m
                    ? "Crítico"
                    : overdueAmount > 0m || utilization >= 75m
                        ? "Vigilancia"
                        : "Normal";
                var action = status == "Crítico"
                    ? "Restringir crédito y priorizar cobranza."
                    : status == "Vigilancia"
                        ? "Monitorear promesas y validar nuevos pedidos."
                        : "Operación normal con seguimiento preventivo.";

                return new FinanceCreditPolicyRowDto
                {
                    CustomerId = group.Key,
                    CustomerName = customer?.Name ?? group.First().CustomerName,
                    CreditLimit = creditLimit,
                    OpenAmount = openAmount,
                    OverdueAmount = overdueAmount,
                    AvailableCredit = available,
                    UtilizationPercent = utilization,
                    PromisedAmount = promisedAmount,
                    SalesThisMonth = monthSales,
                    AveragePastDueDays = avgPastDue,
                    Status = status,
                    SuggestedAction = action,
                    Route = $"/accounts-receivable/statements?customerId={group.Key}"
                };
            })
            .OrderByDescending(x => x.OverdueAmount)
            .ThenByDescending(x => x.UtilizationPercent)
            .ThenBy(x => x.CustomerName)
            .ToList();
    }

    private static async Task<List<FinanceSupplierRiskRowDto>> BuildSupplierRiskRowsAsync(NanchesoftDbContext db, IHostEnvironment environment, Guid companyId, DateTime today)
    {
        var start = today.AddYears(-1);
        var end = today.AddYears(1);
        var schedules = await BuildPaymentScheduleRowsAsync(db, environment, companyId, start, end, today);
        var suppliers = await db.Suppliers.AsNoTracking()
            .Where(x => x.IsActive && x.CompanyId == companyId)
            .ToDictionaryAsync(x => x.Id);

        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);
        var paidBySupplier = await db.Payments.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.SupplierId != null && x.CompanyId == companyId && x.PaymentDate >= monthStart && x.PaymentDate < monthEnd)
            .GroupBy(x => x.SupplierId!.Value)
            .Select(x => new { SupplierId = x.Key, Amount = x.Sum(y => y.Total) })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Amount);

        var totalOpen = schedules.Sum(x => x.OpenAmount);
        return schedules
            .GroupBy(x => x.SupplierId)
            .Select(group =>
            {
                suppliers.TryGetValue(group.Key, out var supplier);
                var openAmount = group.Sum(x => x.OpenAmount);
                var overdueAmount = group.Where(x => x.DaysToDue < 0).Sum(x => x.OpenAmount);
                var scheduledAmount = group.Where(x => (x.PlannedPaymentDate ?? x.DueDate).Date >= today && (x.PlannedPaymentDate ?? x.DueDate).Date <= today.AddDays(30)).Sum(x => x.OpenAmount);
                var paidAmount = paidBySupplier.TryGetValue(group.Key, out var paid) ? paid : 0m;
                var compliance = scheduledAmount <= 0m ? 0m : Math.Round((paidAmount / scheduledAmount) * 100m, 2);
                var concentration = totalOpen <= 0m ? 0m : Math.Round((openAmount / totalOpen) * 100m, 2);
                var requiresAuthorization = group.Count(x => x.NeedsAuthorization);
                var status = overdueAmount > 0m && requiresAuthorization > 0
                    ? "Crítico"
                    : overdueAmount > 0m || requiresAuthorization > 0 || concentration >= 30m
                        ? "Vigilancia"
                        : "Normal";
                var action = status == "Crítico"
                    ? "Autorizar y reprogramar de inmediato."
                    : status == "Vigilancia"
                        ? "Revisar calendario y concentración."
                        : "Seguir programación habitual.";

                return new FinanceSupplierRiskRowDto
                {
                    SupplierId = group.Key,
                    SupplierName = supplier?.Name ?? group.First().SupplierName,
                    OpenAmount = openAmount,
                    OverdueAmount = overdueAmount,
                    ScheduledAmount = scheduledAmount,
                    PaidAmount = paidAmount,
                    CompliancePercent = compliance,
                    ConcentrationPercent = concentration,
                    RequiresAuthorizationCount = requiresAuthorization,
                    PaymentTermDays = supplier?.PaymentTermDays ?? 0,
                    Status = status,
                    SuggestedAction = action,
                    Route = $"/accounts-payable/statements?supplierId={group.Key}"
                };
            })
            .OrderByDescending(x => x.OverdueAmount)
            .ThenByDescending(x => x.ConcentrationPercent)
            .ThenByDescending(x => x.RequiresAuthorizationCount)
            .ThenBy(x => x.SupplierName)
            .ToList();
    }

    private static async Task<List<FinanceRecoveryMatrixRowDto>> BuildRecoveryMatrixRowsAsync(NanchesoftDbContext db, IHostEnvironment environment, Guid companyId, DateTime today)
    {
        var start = today.AddYears(-2);
        var end = today.AddYears(1);
        var receivables = await BuildCollectionCommitmentRowsAsync(db, environment, companyId, start, end, today);
        var payables = await BuildPaymentScheduleRowsAsync(db, environment, companyId, start, end, today);

        var rows = new List<FinanceRecoveryMatrixRowDto>();
        rows.AddRange(BuildRecoveryRows(
            "CxC",
            receivables.Select(x => (PartyId: x.CustomerId, Amount: x.OpenAmount, DaysToDue: x.DaysToDue)),
            "/accounts-receivable/aging"));
        rows.AddRange(BuildRecoveryRows(
            "CxP",
            payables.Select(x => (PartyId: x.SupplierId, Amount: x.OpenAmount, DaysToDue: x.DaysToDue)),
            "/accounts-payable/aging"));

        return rows;
    }

    private static List<FinanceRecoveryMatrixRowDto> BuildRecoveryRows(string portfolioType, IEnumerable<(Guid PartyId, decimal Amount, int DaysToDue)> source, string route)
    {
        var records = source.Where(x => x.Amount > 0m).Select(x => new
        {
            x.PartyId,
            x.Amount,
            Band = GetRecoveryBand(x.DaysToDue)
        }).ToList();

        var total = records.Sum(x => x.Amount);
        if (total <= 0m)
        {
            return new List<FinanceRecoveryMatrixRowDto>();
        }

        return records
            .GroupBy(x => x.Band)
            .Select(group =>
            {
                var amount = group.Sum(x => x.Amount);
                return new FinanceRecoveryMatrixRowDto
                {
                    PortfolioType = portfolioType,
                    Band = group.Key,
                    Documents = group.Count(),
                    Parties = group.Select(x => x.PartyId).Distinct().Count(),
                    Amount = amount,
                    SharePercent = Math.Round((amount / total) * 100m, 2),
                    RiskLevel = GetRecoveryRiskLevel(group.Key),
                    Route = route
                };
            })
            .OrderBy(x => RecoveryBandRank(x.Band))
            .ToList();
    }

    private static async Task<List<FinanceCustomerRadarRowDto>> BuildCustomerRadarRowsAsync(NanchesoftDbContext db, int year, int month, DateTime today)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var customers = await db.Customers.AsNoTracking().Where(x => x.IsActive).ToDictionaryAsync(x => x.Id);
        var invoices = await db.SalesInvoices.AsNoTracking().Where(x => x.IsActive && x.Status == "posted" && x.CustomerId != null && x.InvoiceDate >= start && x.InvoiceDate < end).ToListAsync();
        var credits = await db.CreditNotes.AsNoTracking().Where(x => x.IsActive && x.Status != "cancelled" && x.CustomerId != null && x.CreditNoteDate >= start && x.CreditNoteDate < end).ToListAsync();
        var receipts = await db.ReceiptApplications.AsNoTracking().Where(x => x.IsActive && x.Status != "cancelled" && x.ApplicationDate >= start && x.ApplicationDate < end).ToListAsync();
        var commitments = await BuildCollectionsCalendarRowsAsync(db, today.AddYears(-1), today.AddYears(1), today);

        var openByCustomer = commitments.GroupBy(x => x.CustomerId).ToDictionary(x => x.Key, x => new
        {
            Open = x.Sum(y => y.OpenAmount),
            Overdue = x.Where(y => y.DaysToDue < 0).Sum(y => y.OpenAmount),
            AvgPastDue = x.Any(y => y.DaysToDue < 0) ? Math.Round(x.Where(y => y.DaysToDue < 0).Average(y => (decimal)Math.Abs(y.DaysToDue)), 2) : 0m
        });
        var invoicesByCustomer = invoices.GroupBy(x => x.CustomerId!.Value).ToDictionary(x => x.Key, x => x.Sum(y => y.Total));
        var creditsByCustomer = credits.GroupBy(x => x.CustomerId!.Value).ToDictionary(x => x.Key, x => x.Sum(y => y.Total));
        var receiptsByCustomer = receipts.GroupBy(x => x.CustomerId).ToDictionary(x => x.Key, x => x.Sum(y => y.AppliedAmount));

        var customerIds = invoicesByCustomer.Keys.Union(creditsByCustomer.Keys).Union(receiptsByCustomer.Keys).Union(openByCustomer.Keys).ToList();
        var totalNetSales = customerIds.Sum(id => Math.Max(0m, (invoicesByCustomer.TryGetValue(id, out var inv) ? inv : 0m) - (creditsByCustomer.TryGetValue(id, out var cr) ? cr : 0m)));

        return customerIds
            .Select(id =>
            {
                customers.TryGetValue(id, out var customer);
                var invoiced = invoicesByCustomer.TryGetValue(id, out var inv) ? inv : 0m;
                var creditAmount = creditsByCustomer.TryGetValue(id, out var cr) ? cr : 0m;
                var netSales = Math.Max(0m, invoiced - creditAmount);
                var receiptsAmount = receiptsByCustomer.TryGetValue(id, out var rec) ? rec : 0m;
                var open = openByCustomer.TryGetValue(id, out var openRow) ? openRow.Open : 0m;
                var overdue = openByCustomer.TryGetValue(id, out openRow) ? openRow.Overdue : 0m;
                var avgPastDue = openByCustomer.TryGetValue(id, out openRow) ? openRow.AvgPastDue : 0m;
                var participation = totalNetSales <= 0m ? 0m : Math.Round((netSales / totalNetSales) * 100m, 2);
                var effectiveness = netSales <= 0m ? 0m : Math.Round((receiptsAmount / netSales) * 100m, 2);
                var risk = overdue >= Math.Max(1m, open * 0.50m)
                    ? "Alta"
                    : overdue > 0m || effectiveness < 80m
                        ? "Media"
                        : "Baja";

                return new FinanceCustomerRadarRowDto
                {
                    CustomerId = id,
                    CustomerCode = customer?.Code ?? string.Empty,
                    CustomerName = customer?.Name ?? id.ToString(),
                    InvoicedAmount = invoiced,
                    CreditNoteAmount = creditAmount,
                    NetSales = netSales,
                    ReceiptsAmount = receiptsAmount,
                    OpenAmount = open,
                    OverdueAmount = overdue,
                    ParticipationPercent = participation,
                    CollectionEffectivenessPercent = effectiveness,
                    AveragePastDueDays = avgPastDue,
                    RiskLevel = risk,
                    Route = $"/accounts-receivable/statements?customerId={id}"
                };
            })
            .OrderByDescending(x => x.ParticipationPercent)
            .ThenByDescending(x => x.OverdueAmount)
            .ThenBy(x => x.CustomerName)
            .ToList();
    }

    private static async Task<List<FinanceSupplierRadarRowDto>> BuildSupplierRadarRowsAsync(NanchesoftDbContext db, int year, int month, DateTime today)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var suppliers = await db.Suppliers.AsNoTracking().Where(x => x.IsActive).ToDictionaryAsync(x => x.Id);
        var purchases = await db.PurchaseInvoices.AsNoTracking().Where(x => x.IsActive && x.Status == "posted" && x.SupplierId != null && x.InvoiceDate >= start && x.InvoiceDate < end).ToListAsync();
        var returns = await db.Set<PurchaseReturn>().AsNoTracking().Where(x => x.IsActive && x.Status != "cancelled" && x.SupplierId != null && x.ReturnDate >= start && x.ReturnDate < end).ToListAsync();
        var payments = await db.Payments.AsNoTracking().Where(x => x.IsActive && x.Status == "posted" && x.SupplierId != null && x.PaymentDate >= start && x.PaymentDate < end).ToListAsync();
        var schedules = await BuildPaymentsCalendarRowsAsync(db, today.AddYears(-1), today.AddYears(1), today);

        var openBySupplier = schedules.GroupBy(x => x.SupplierId).ToDictionary(x => x.Key, x => new
        {
            Open = x.Sum(y => y.OpenAmount),
            Overdue = x.Where(y => y.DaysToDue < 0).Sum(y => y.OpenAmount)
        });
        var purchasesBySupplier = purchases.GroupBy(x => x.SupplierId!.Value).ToDictionary(x => x.Key, x => x.Sum(y => y.Total));
        var returnsBySupplier = returns.GroupBy(x => x.SupplierId!.Value).ToDictionary(x => x.Key, x => x.Sum(y => y.Total));
        var paymentsBySupplier = payments.GroupBy(x => x.SupplierId!.Value).ToDictionary(x => x.Key, x => x.Sum(y => y.Total));

        var supplierIds = purchasesBySupplier.Keys.Union(returnsBySupplier.Keys).Union(paymentsBySupplier.Keys).Union(openBySupplier.Keys).ToList();
        var totalNetPurchases = supplierIds.Sum(id => Math.Max(0m, (purchasesBySupplier.TryGetValue(id, out var pur) ? pur : 0m) - (returnsBySupplier.TryGetValue(id, out var ret) ? ret : 0m)));

        return supplierIds
            .Select(id =>
            {
                suppliers.TryGetValue(id, out var supplier);
                var purchaseAmount = purchasesBySupplier.TryGetValue(id, out var pur) ? pur : 0m;
                var returnAmount = returnsBySupplier.TryGetValue(id, out var ret) ? ret : 0m;
                var netPurchases = Math.Max(0m, purchaseAmount - returnAmount);
                var paymentAmount = paymentsBySupplier.TryGetValue(id, out var pay) ? pay : 0m;
                var open = openBySupplier.TryGetValue(id, out var openRow) ? openRow.Open : 0m;
                var overdue = openBySupplier.TryGetValue(id, out openRow) ? openRow.Overdue : 0m;
                var participation = totalNetPurchases <= 0m ? 0m : Math.Round((netPurchases / totalNetPurchases) * 100m, 2);
                var coverage = netPurchases <= 0m ? 0m : Math.Round((paymentAmount / netPurchases) * 100m, 2);
                var risk = overdue >= Math.Max(1m, open * 0.50m)
                    ? "Alta"
                    : overdue > 0m || coverage < 80m
                        ? "Media"
                        : "Baja";

                return new FinanceSupplierRadarRowDto
                {
                    SupplierId = id,
                    SupplierCode = supplier?.Code ?? string.Empty,
                    SupplierName = supplier?.Name ?? id.ToString(),
                    PurchasesAmount = purchaseAmount,
                    ReturnAmount = returnAmount,
                    NetPurchases = netPurchases,
                    PaymentsAmount = paymentAmount,
                    OpenAmount = open,
                    OverdueAmount = overdue,
                    ParticipationPercent = participation,
                    PaymentCoveragePercent = coverage,
                    PaymentTermDays = supplier?.PaymentTermDays ?? 0,
                    RiskLevel = risk,
                    Route = $"/accounts-payable/statements?supplierId={id}"
                };
            })
            .OrderByDescending(x => x.ParticipationPercent)
            .ThenByDescending(x => x.OverdueAmount)
            .ThenBy(x => x.SupplierName)
            .ToList();
    }

    private static async Task<List<FinanceBranchPulseRowDto>> BuildBranchPulseRowsAsync(NanchesoftDbContext db, int year, int month, DateTime today)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var branches = await db.Set<Branch>().AsNoTracking().Where(x => x.IsActive).ToListAsync();
        var branchMap = branches.ToDictionary(x => x.Id);

        var sales = await db.SalesInvoices.AsNoTracking().Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate >= start && x.InvoiceDate < end).ToListAsync();
        var salesReturns = await db.Set<SalesReturn>().AsNoTracking().Where(x => x.IsActive && x.Status != "cancelled" && x.ReturnDate >= start && x.ReturnDate < end).ToListAsync();
        var purchases = await db.PurchaseInvoices.AsNoTracking().Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate >= start && x.InvoiceDate < end).ToListAsync();
        var purchaseReturns = await db.Set<PurchaseReturn>().AsNoTracking().Where(x => x.IsActive && x.Status != "cancelled" && x.ReturnDate >= start && x.ReturnDate < end).ToListAsync();
        var receipts = await db.Receipts.AsNoTracking().Where(x => x.IsActive && x.Status == "posted" && x.ReceiptDate >= start && x.ReceiptDate < end).ToListAsync();
        var payments = await db.Payments.AsNoTracking().Where(x => x.IsActive && x.Status == "posted" && x.PaymentDate >= start && x.PaymentDate < end).ToListAsync();
        var cashAccounts = await db.CashAccounts.AsNoTracking().Where(x => x.IsActive).ToListAsync();

        var receivableCalendars = await BuildCollectionsCalendarRowsAsync(db, today.AddYears(-1), today.AddYears(1), today);
        var paymentCalendars = await BuildPaymentsCalendarRowsAsync(db, today.AddYears(-1), today.AddYears(1), today);
        var salesInvoiceBranch = await db.SalesInvoices.AsNoTracking().Where(x => x.IsActive && x.Status == "posted").Select(x => new { x.Id, x.BranchId }).ToDictionaryAsync(x => x.Id, x => x.BranchId);
        var purchaseInvoiceBranch = await db.PurchaseInvoices.AsNoTracking().Where(x => x.IsActive && x.Status == "posted").Select(x => new { x.Id, x.BranchId }).ToDictionaryAsync(x => x.Id, x => x.BranchId);

        var openReceivablesByBranch = receivableCalendars
            .Where(x => salesInvoiceBranch.ContainsKey(x.SalesInvoiceId))
            .GroupBy(x => salesInvoiceBranch[x.SalesInvoiceId])
            .ToDictionary(x => x.Key, x => x.Sum(y => y.OpenAmount));
        var openPayablesByBranch = paymentCalendars
            .Where(x => purchaseInvoiceBranch.ContainsKey(x.PurchaseInvoiceId))
            .GroupBy(x => purchaseInvoiceBranch[x.PurchaseInvoiceId])
            .ToDictionary(x => x.Key, x => x.Sum(y => y.OpenAmount));

        return branches
            .Select(branch =>
            {
                var salesAmount = sales.Where(x => x.BranchId == branch.Id).Sum(x => x.Total) - salesReturns.Where(x => x.BranchId == branch.Id).Sum(x => x.Total);
                var purchaseAmount = purchases.Where(x => x.BranchId == branch.Id).Sum(x => x.Total) - purchaseReturns.Where(x => x.BranchId == branch.Id).Sum(x => x.Total);
                var receiptsAmount = receipts.Where(x => x.BranchId == branch.Id).Sum(x => x.Total);
                var paymentsAmount = payments.Where(x => x.BranchId == branch.Id).Sum(x => x.Total);
                var cashBalance = cashAccounts.Where(x => x.BranchId == branch.Id).Sum(x => x.CurrentBalance);
                var openReceivables = openReceivablesByBranch.TryGetValue(branch.Id, out var openRec) ? openRec : 0m;
                var openPayables = openPayablesByBranch.TryGetValue(branch.Id, out var openPay) ? openPay : 0m;
                var netCash = receiptsAmount - paymentsAmount;
                var workingCapital = cashBalance + openReceivables - openPayables;
                var status = workingCapital < 0m ? "Crítico" : netCash < 0m ? "Vigilancia" : "Sano";

                return new FinanceBranchPulseRowDto
                {
                    BranchId = branch.Id,
                    BranchCode = branch.Code,
                    BranchName = branch.Name,
                    SalesAmount = salesAmount,
                    PurchasesAmount = purchaseAmount,
                    ReceiptsAmount = receiptsAmount,
                    PaymentsAmount = paymentsAmount,
                    NetCashMovement = netCash,
                    CashBalance = cashBalance,
                    OpenReceivables = openReceivables,
                    OpenPayables = openPayables,
                    WorkingCapital = workingCapital,
                    Status = status
                };
            })
            .OrderBy(x => x.BranchName)
            .ToList();
    }

    private static async Task<List<FinanceMonthlyLiquidityBridgeRowDto>> BuildMonthlyLiquidityBridgeRowsAsync(NanchesoftDbContext db, int year, DateTime today)
    {
        var currentLiquidity = (await db.CashAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m)
            + (await db.BankAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m);

        var receiptsByMonth = await db.Receipts.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.ReceiptDate.Year == year)
            .GroupBy(x => x.ReceiptDate.Month)
            .Select(x => new { Month = x.Key, Amount = x.Sum(y => y.Total) })
            .ToDictionaryAsync(x => x.Month, x => x.Amount);
        var paymentsByMonth = await db.Payments.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.PaymentDate.Year == year)
            .GroupBy(x => x.PaymentDate.Month)
            .Select(x => new { Month = x.Key, Amount = x.Sum(y => y.Total) })
            .ToDictionaryAsync(x => x.Month, x => x.Amount);

        var receiptApplicationsByInvoice = await db.ReceiptApplications.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled")
            .GroupBy(x => x.SalesInvoiceId)
            .Select(x => new { SalesInvoiceId = x.Key, Amount = x.Sum(y => y.AppliedAmount) })
            .ToDictionaryAsync(x => x.SalesInvoiceId, x => x.Amount);
        var salesInvoices = await db.SalesInvoices.AsNoTracking().Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate.Year <= year).ToListAsync();
        var purchaseInvoices = await db.PurchaseInvoices.AsNoTracking().Where(x => x.IsActive && x.Status == "posted" && x.InvoiceDate.Year <= year).ToListAsync();
        var paymentsBySupplier = await db.Payments.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.SupplierId != null)
            .GroupBy(x => x.SupplierId!.Value)
            .Select(x => new { SupplierId = x.Key, Amount = x.Sum(y => y.Total) })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Amount);

        var rows = new List<FinanceMonthlyLiquidityBridgeRowDto>();
        var opening = currentLiquidity;
        for (var month = 1; month <= 12; month++)
        {
            var labelDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var receipts = receiptsByMonth.TryGetValue(month, out var rec) ? rec : 0m;
            var payments = paymentsByMonth.TryGetValue(month, out var pay) ? pay : 0m;
            var net = receipts - payments;
            var closing = opening + net;
            var monthEnd = labelDate.AddMonths(1);

            var openReceivables = salesInvoices
                .Where(x => x.InvoiceDate < monthEnd)
                .Sum(x => Math.Max(0m, x.Total - (receiptApplicationsByInvoice.TryGetValue(x.Id, out var applied) ? applied : 0m)));

            var supplierRemaining = new Dictionary<Guid, decimal>(paymentsBySupplier);
            decimal openPayables = 0m;
            foreach (var group in purchaseInvoices.Where(x => x.InvoiceDate < monthEnd && x.SupplierId != null).GroupBy(x => x.SupplierId!.Value))
            {
                var remaining = supplierRemaining.TryGetValue(group.Key, out var totalPaid) ? totalPaid : 0m;
                foreach (var invoice in group.OrderBy(x => x.InvoiceDate))
                {
                    var applied = Math.Min(invoice.Total, remaining);
                    remaining = Math.Max(0m, remaining - applied);
                    openPayables += Math.Max(0m, invoice.Total - applied);
                }
            }

            var workingCapital = closing + openReceivables - openPayables;
            var coverage = payments <= 0m ? 0m : Math.Round((closing / payments) * 100m, 2);
            var status = closing < 0m ? "Crítico" : coverage < 100m ? "Vigilancia" : "Sano";

            rows.Add(new FinanceMonthlyLiquidityBridgeRowDto
            {
                Year = year,
                Month = month,
                Label = labelDate.ToString("MMMM"),
                OpeningLiquidity = opening,
                Receipts = receipts,
                Payments = payments,
                NetMovement = net,
                ClosingLiquidity = closing,
                OpenReceivables = openReceivables,
                OpenPayables = openPayables,
                WorkingCapital = workingCapital,
                CoveragePercent = coverage,
                Status = status
            });

            opening = closing;
        }

        return rows;
    }

    private static string GetRecoveryBand(int daysToDue)
    {
        if (daysToDue >= 0)
        {
            return "Corriente";
        }

        var pastDue = Math.Abs(daysToDue);
        if (pastDue <= 7) return "1-7";
        if (pastDue <= 15) return "8-15";
        if (pastDue <= 30) return "16-30";
        if (pastDue <= 60) return "31-60";
        return "61+";
    }

    private static int RecoveryBandRank(string band)
        => band switch
        {
            "Corriente" => 0,
            "1-7" => 1,
            "8-15" => 2,
            "16-30" => 3,
            "31-60" => 4,
            "61+" => 5,
            _ => 99
        };

    private static string GetRecoveryRiskLevel(string band)
        => band switch
        {
            "Corriente" => "Bajo",
            "1-7" => "Bajo",
            "8-15" => "Medio",
            "16-30" => "Medio",
            "31-60" => "Alto",
            "61+" => "Crítico",
            _ => "Medio"
        };

    private static async Task<FinanceDashboardDto> BuildDashboardSnapshotAsync(NanchesoftDbContext db, DateTime today)
    {
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var cashBalance = await db.CashAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m;
        var bankBalance = await db.BankAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => (decimal?)x.CurrentBalance).SumAsync() ?? 0m;

        var salesInvoices = await db.SalesInvoices.AsNoTracking().Where(x => x.IsActive && x.Status == "posted").ToListAsync();
        var purchaseInvoices = await db.PurchaseInvoices.AsNoTracking().Where(x => x.IsActive && x.Status == "posted").ToListAsync();
        var receipts = await db.Receipts.AsNoTracking().Where(x => x.IsActive && x.Status == "posted").ToListAsync();
        var payments = await db.Payments.AsNoTracking().Where(x => x.IsActive && x.Status == "posted").ToListAsync();
        var creditNotes = await db.CreditNotes.AsNoTracking().Where(x => x.IsActive && x.Status != "cancelled").ToListAsync();
        var receiptApplications = await db.ReceiptApplications.AsNoTracking().Where(x => x.IsActive && x.Status != "cancelled").ToListAsync();
        var draftEntries = await db.Set<AccountingJournalEntry>().AsNoTracking().CountAsync(x => x.IsActive && x.Status == "draft");
        var postedEntries = await db.Set<AccountingJournalEntry>().AsNoTracking().CountAsync(x => x.IsActive && x.Status == "posted");
        var openPeriods = await db.Set<AccountingFiscalPeriod>().AsNoTracking().CountAsync(x => x.IsActive && x.Status == "open");

        var openReceivables = Math.Max(0m, salesInvoices.Sum(x => x.Total) - creditNotes.Sum(x => x.Total) - receiptApplications.Sum(x => x.AppliedAmount));
        var openPayables = Math.Max(0m, purchaseInvoices.Sum(x => x.Total) - payments.Sum(x => x.Total));

        return new FinanceDashboardDto
        {
            CashBalance = cashBalance,
            BankBalance = bankBalance,
            TotalLiquidity = cashBalance + bankBalance,
            OpenReceivables = openReceivables,
            OpenPayables = openPayables,
            WorkingCapital = (cashBalance + bankBalance) + openReceivables - openPayables,
            SalesThisMonth = salesInvoices.Where(x => x.InvoiceDate >= monthStart).Sum(x => x.Total),
            PurchasesThisMonth = purchaseInvoices.Where(x => x.InvoiceDate >= monthStart).Sum(x => x.Total),
            ReceiptsThisMonth = receipts.Where(x => x.ReceiptDate >= monthStart).Sum(x => x.Total),
            PaymentsThisMonth = payments.Where(x => x.PaymentDate >= monthStart).Sum(x => x.Total),
            DraftJournalEntries = draftEntries,
            PostedJournalEntries = postedEntries,
            OpenPeriods = openPeriods
        };
    }

    private static string GetExecutiveAgreementsPath(IHostEnvironment environment)
    {
        var folder = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "finance_executive_agreements.json");
    }

    private static async Task<List<FinanceExecutiveAgreementRowDto>> LoadExecutiveAgreementRowsAsync(IHostEnvironment environment, int year, int month)
    {
        var path = GetExecutiveAgreementsPath(environment);
        if (!File.Exists(path))
        {
            return new List<FinanceExecutiveAgreementRowDto>();
        }

        await using var stream = File.OpenRead(path);
        var rows = await JsonSerializer.DeserializeAsync<List<FinanceExecutiveAgreementRowDto>>(stream, FinanceJsonOptions) ?? new List<FinanceExecutiveAgreementRowDto>();
        return rows.Where(x => x.Year == year && x.Month == month).OrderByDescending(x => PriorityRank(x.Priority)).ThenBy(x => x.DueDate ?? DateTime.MaxValue).ToList();
    }

    private static async Task SaveExecutiveAgreementRowsAsync(IHostEnvironment environment, List<FinanceExecutiveAgreementRowDto> rows)
    {
        var path = GetExecutiveAgreementsPath(environment);
        List<FinanceExecutiveAgreementRowDto> existing;
        if (File.Exists(path))
        {
            await using var read = File.OpenRead(path);
            existing = await JsonSerializer.DeserializeAsync<List<FinanceExecutiveAgreementRowDto>>(read, FinanceJsonOptions) ?? new List<FinanceExecutiveAgreementRowDto>();
        }
        else
        {
            existing = new List<FinanceExecutiveAgreementRowDto>();
        }

        var targetPeriods = rows.Select(x => (x.Year, x.Month)).Distinct().ToHashSet();
        existing.RemoveAll(x => targetPeriods.Contains((x.Year, x.Month)));
        foreach (var row in rows)
        {
            if (row.Id == Guid.Empty)
            {
                row.Id = Guid.NewGuid();
            }
        }
        existing.AddRange(rows);

        await using var write = File.Create(path);
        await JsonSerializer.SerializeAsync(write, existing, FinanceJsonOptions);
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


    private sealed class NullHostEnvironment : IHostEnvironment
    {
        public static readonly NullHostEnvironment Instance = new();
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Nanchesoft.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class FinanceCollectionCommitmentStateRow
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DocumentId { get; set; }
        public DateTime? PlannedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Responsible { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class FinancePaymentScheduleStateRow
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DocumentId { get; set; }
        public DateTime? PlannedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Responsible { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class FinanceApprovalStateRow
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ModuleKey { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public Guid DocumentId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    private static decimal GetAmountOrTotal(FinanceDocumentControlRowDto row)
        => row.OpenAmount > 0m ? row.OpenAmount : row.Total;

}

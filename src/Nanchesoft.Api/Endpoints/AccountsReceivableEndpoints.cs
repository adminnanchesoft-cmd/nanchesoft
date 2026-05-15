using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class AccountsReceivableEndpoints
{
    public static IEndpointRouteBuilder MapAccountsReceivableEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts-receivable").WithTags("AccountsReceivable");

        group.MapGet("/lookups", async (Guid? customerId, NanchesoftDbContext db) =>
        {
            var customers = await db.Customers.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new ArLookupItem
                {
                    Id = x.Id,
                    CustomerId = x.Id,
                    Name = x.Code + " · " + x.Name
                })
                .ToListAsync();

            var receiptBaseRows = await db.Receipts.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted" && (!customerId.HasValue || x.CustomerId == customerId))
                .OrderByDescending(x => x.ReceiptDate)
                .Select(x => new
                {
                    x.Id,
                    x.CustomerId,
                    x.Folio,
                    x.Total,
                    x.ReceiptDate
                })
                .ToListAsync();

            var appliedByReceipt = await db.ReceiptApplications.AsNoTracking()
                .Where(x => x.IsActive && x.Status != "cancelled" && (!customerId.HasValue || x.CustomerId == customerId.Value))
                .GroupBy(x => x.ReceiptId)
                .Select(g => new { ReceiptId = g.Key, Applied = g.Sum(x => x.AppliedAmount) })
                .ToDictionaryAsync(x => x.ReceiptId, x => x.Applied);

            var receipts = receiptBaseRows
                .Select(x =>
                {
                    var applied = appliedByReceipt.TryGetValue(x.Id, out var value) ? value : 0m;
                    var available = x.Total - applied;
                    return new ArLookupItem
                    {
                        Id = x.Id,
                        CustomerId = x.CustomerId,
                        Name = $"{x.Folio} · disponible {Math.Max(available, 0m):N2}",
                        TotalAmount = x.Total,
                        OpenAmount = Math.Max(available, 0m),
                        MovementDate = x.ReceiptDate
                    };
                })
                .Where(x => x.OpenAmount > 0m)
                .OrderByDescending(x => x.MovementDate)
                .ToList();

            var invoiceBaseRows = await db.SalesInvoices.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted" && x.CustomerId != null && (!customerId.HasValue || x.CustomerId == customerId))
                .OrderByDescending(x => x.InvoiceDate)
                .Select(x => new
                {
                    x.Id,
                    CustomerId = x.CustomerId!.Value,
                    x.Folio,
                    x.Total,
                    x.InvoiceDate
                })
                .ToListAsync();

            var appliedByInvoice = await db.ReceiptApplications.AsNoTracking()
                .Where(x => x.IsActive && x.Status != "cancelled" && (!customerId.HasValue || x.CustomerId == customerId.Value))
                .GroupBy(x => x.SalesInvoiceId)
                .Select(g => new { SalesInvoiceId = g.Key, Applied = g.Sum(x => x.AppliedAmount) })
                .ToDictionaryAsync(x => x.SalesInvoiceId, x => x.Applied);

            var invoices = invoiceBaseRows
                .Select(x =>
                {
                    var applied = appliedByInvoice.TryGetValue(x.Id, out var value) ? value : 0m;
                    var openAmount = x.Total - applied;
                    return new ArLookupItem
                    {
                        Id = x.Id,
                        CustomerId = x.CustomerId,
                        Name = $"{x.Folio} · saldo {Math.Max(openAmount, 0m):N2}",
                        TotalAmount = x.Total,
                        OpenAmount = Math.Max(openAmount, 0m),
                        MovementDate = x.InvoiceDate
                    };
                })
                .Where(x => x.OpenAmount > 0m)
                .OrderByDescending(x => x.MovementDate)
                .ToList();

            return Results.Ok(new ArLookupsDto
            {
                Customers = customers,
                Receipts = receipts,
                SalesInvoices = invoices
            });
        });

        group.MapGet("/balances", async (NanchesoftDbContext db) => Results.Ok(await BuildBalancesAsync(db)));

        group.MapGet("/statements", async (Guid? customerId, DateTime? fromDate, DateTime? toDate, NanchesoftDbContext db) =>
        {
            var invoiceRows = await db.SalesInvoices.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted" && (!customerId.HasValue || x.CustomerId == customerId))
                .Where(x => !fromDate.HasValue || x.InvoiceDate.Date >= fromDate.Value.Date)
                .Where(x => !toDate.HasValue || x.InvoiceDate.Date <= toDate.Value.Date)
                .Select(x => new ArStatementRowDto
                {
                    CustomerId = x.CustomerId,
                    MovementDate = x.InvoiceDate,
                    DocumentType = "sales_invoice",
                    Folio = x.Folio,
                    Reference = x.Notes,
                    ChargeAmount = x.Total,
                    CreditAmount = 0m
                })
                .ToListAsync();

            var creditRows = await db.CreditNotes.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted" && (!customerId.HasValue || x.CustomerId == customerId))
                .Where(x => !fromDate.HasValue || x.CreditNoteDate.Date >= fromDate.Value.Date)
                .Where(x => !toDate.HasValue || x.CreditNoteDate.Date <= toDate.Value.Date)
                .Select(x => new ArStatementRowDto
                {
                    CustomerId = x.CustomerId,
                    MovementDate = x.CreditNoteDate,
                    DocumentType = "credit_note",
                    Folio = x.Folio,
                    Reference = x.Reason,
                    ChargeAmount = 0m,
                    CreditAmount = x.Total
                })
                .ToListAsync();

            var applicationRows = await (from appRow in db.ReceiptApplications.AsNoTracking()
                                         join receipt in db.Receipts.AsNoTracking() on appRow.ReceiptId equals receipt.Id
                                         where appRow.IsActive && appRow.Status != "cancelled" && (!customerId.HasValue || appRow.CustomerId == customerId)
                                         where !fromDate.HasValue || appRow.ApplicationDate.Date >= fromDate.Value.Date
                                         where !toDate.HasValue || appRow.ApplicationDate.Date <= toDate.Value.Date
                                         select new ArStatementRowDto
                                         {
                                             CustomerId = appRow.CustomerId,
                                             MovementDate = appRow.ApplicationDate,
                                             DocumentType = "receipt_application",
                                             Folio = receipt.Folio,
                                             Reference = appRow.Reference,
                                             ChargeAmount = 0m,
                                             CreditAmount = appRow.AppliedAmount
                                         }).ToListAsync();

            var customerNames = await db.Customers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);

            var rows = invoiceRows.Concat(creditRows).Concat(applicationRows)
                .OrderBy(x => x.CustomerId)
                .ThenBy(x => x.MovementDate)
                .ThenBy(x => x.Folio)
                .ToList();

            Guid? currentCustomerId = null;
            decimal runningBalance = 0m;
            foreach (var row in rows)
            {
                if (currentCustomerId != row.CustomerId)
                {
                    currentCustomerId = row.CustomerId;
                    runningBalance = 0m;
                }

                runningBalance += row.ChargeAmount - row.CreditAmount;
                row.BalanceAfter = runningBalance;
                row.CustomerName = row.CustomerId.HasValue && customerNames.TryGetValue(row.CustomerId.Value, out var name)
                    ? name
                    : string.Empty;
            }

            return Results.Ok(rows.OrderByDescending(x => x.MovementDate).ThenByDescending(x => x.Folio).ToList());
        });

        group.MapGet("/aging", async (Guid? customerId, NanchesoftDbContext db) => Results.Ok(await BuildAgingAsync(db, customerId)));

        group.MapGet("/applications", async (Guid? customerId, NanchesoftDbContext db) =>
        {
            var rows = await (from application in db.ReceiptApplications.AsNoTracking()
                              join customer in db.Customers.AsNoTracking() on application.CustomerId equals customer.Id into customerJoin
                              from customer in customerJoin.DefaultIfEmpty()
                              join receipt in db.Receipts.AsNoTracking() on application.ReceiptId equals receipt.Id
                              join invoice in db.SalesInvoices.AsNoTracking() on application.SalesInvoiceId equals invoice.Id
                              where application.IsActive && (!customerId.HasValue || application.CustomerId == customerId)
                              orderby application.ApplicationDate descending, application.CreatedAt descending
                              select new ArReceiptApplicationRowDto
                              {
                                  ReceiptApplicationId = application.Id,
                                  CustomerId = application.CustomerId,
                                  CustomerName = customer != null ? customer.Name : string.Empty,
                                  ReceiptId = application.ReceiptId,
                                  ReceiptFolio = receipt.Folio,
                                  SalesInvoiceId = application.SalesInvoiceId,
                                  SalesInvoiceFolio = invoice.Folio,
                                  ApplicationDate = application.ApplicationDate,
                                  AppliedAmount = application.AppliedAmount,
                                  Status = application.Status,
                                  Reference = application.Reference
                              }).ToListAsync();

            return Results.Ok(rows);
        });

        group.MapPost("/apply-receipt", async (ArApplyReceiptRequest request, NanchesoftDbContext db) =>
        {
            if (!request.CustomerId.HasValue || !request.ReceiptId.HasValue || !request.SalesInvoiceId.HasValue || request.AppliedAmount <= 0m)
                return Results.BadRequest(new { message = "Debes indicar cliente, recibo, factura e importe a aplicar." });

            var invoice = await db.SalesInvoices.FirstOrDefaultAsync(x => x.Id == request.SalesInvoiceId.Value && x.IsActive && x.Status == "posted");
            if (invoice is null)
                return Results.NotFound(new { message = "No se encontró la factura de venta posteada." });

            var receipt = await db.Receipts.FirstOrDefaultAsync(x => x.Id == request.ReceiptId.Value && x.IsActive && x.Status == "posted");
            if (receipt is null)
                return Results.NotFound(new { message = "No se encontró el recibo posteado." });

            if (invoice.CustomerId != request.CustomerId || receipt.CustomerId != request.CustomerId)
                return Results.BadRequest(new { message = "La factura y el recibo deben pertenecer al mismo cliente." });

            var appliedToInvoice = await db.ReceiptApplications
                .Where(x => x.SalesInvoiceId == invoice.Id && x.IsActive && x.Status != "cancelled")
                .SumAsync(x => (decimal?)x.AppliedAmount) ?? 0m;

            var remainingInvoice = invoice.Total - appliedToInvoice;
            if (remainingInvoice <= 0m)
                return Results.BadRequest(new { message = "La factura ya está liquidada." });

            var receiptTotalApplied = await db.ReceiptApplications
                .Where(x => x.ReceiptId == receipt.Id && x.IsActive && x.Status != "cancelled")
                .SumAsync(x => (decimal?)x.AppliedAmount) ?? 0m;

            var remainingReceipt = receipt.Total - receiptTotalApplied;
            if (remainingReceipt <= 0m)
                return Results.BadRequest(new { message = "El recibo ya no tiene saldo disponible." });

            var amountToApply = Math.Min(request.AppliedAmount, Math.Min(remainingInvoice, remainingReceipt));
            if (amountToApply <= 0m)
                return Results.BadRequest(new { message = "El importe a aplicar es inválido." });

            var account = await db.AccountsReceivableAccounts.FirstOrDefaultAsync(x => x.CompanyId == invoice.CompanyId && x.CustomerId == request.CustomerId.Value);
            if (account is null)
            {
                var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.CustomerId.Value);
                account = new AccountsReceivableAccount
                {
                    TenantId = invoice.TenantId,
                    CompanyId = invoice.CompanyId,
                    BranchId = invoice.BranchId,
                    CustomerId = request.CustomerId.Value,
                    CurrencyId = invoice.CurrencyId,
                    Code = customer is null ? $"CXC-{request.CustomerId.Value:N}" : $"CXC-{customer.Code}",
                    Status = "active",
                    CreatedBy = "web-api",
                    CreatedAt = DateTime.UtcNow
                };
                db.AccountsReceivableAccounts.Add(account);
            }

            var applicationDate = EnsureUtcDate(request.ApplicationDate ?? DateTime.UtcNow);
            var balanceBefore = await CalculateCurrentBalanceAsync(db, request.CustomerId.Value);
            var reference = string.IsNullOrWhiteSpace(request.Reference)
                ? $"Aplicación {receipt.Folio} a {invoice.Folio}"
                : request.Reference.Trim();

            var movement = new AccountsReceivableMovement
            {
                TenantId = invoice.TenantId,
                CompanyId = invoice.CompanyId,
                BranchId = invoice.BranchId,
                AccountsReceivableAccount = account,
                CustomerId = request.CustomerId.Value,
                ReceiptId = receipt.Id,
                SalesInvoiceId = invoice.Id,
                MovementDate = applicationDate,
                MovementType = "receipt_application",
                DocumentType = "receipt",
                DocumentId = receipt.Id,
                Reference = reference,
                Status = "posted",
                ChargeAmount = 0m,
                CreditAmount = amountToApply,
                BalanceAfter = balanceBefore - amountToApply,
                CreatedBy = "web-api",
                CreatedAt = DateTime.UtcNow
            };
            db.AccountsReceivableMovements.Add(movement);

            var application = new ReceiptApplication
            {
                TenantId = invoice.TenantId,
                CompanyId = invoice.CompanyId,
                BranchId = invoice.BranchId,
                CustomerId = request.CustomerId.Value,
                ReceiptId = receipt.Id,
                SalesInvoiceId = invoice.Id,
                AccountsReceivableMovement = movement,
                ApplicationDate = applicationDate,
                AppliedAmount = amountToApply,
                Reference = reference,
                Status = "posted",
                CreatedBy = "web-api",
                CreatedAt = DateTime.UtcNow
            };
            db.ReceiptApplications.Add(application);

            await db.SaveChangesAsync();
            await SyncAccountProjectionAsync(db, account.Id);

            return Results.Ok(new { success = true, id = application.Id, appliedAmount = amountToApply });
        });

        group.MapPost("/applications/{applicationId:guid}/cancel", async (Guid applicationId, NanchesoftDbContext db) =>
        {
            var application = await db.ReceiptApplications.FirstOrDefaultAsync(x => x.Id == applicationId && x.IsActive);
            if (application is null)
                return Results.NotFound(new { message = "No se encontró la aplicación de recibo." });

            if (application.Status == "cancelled")
                return Results.BadRequest(new { message = "La aplicación ya estaba cancelada." });

            application.Status = "cancelled";
            application.UpdatedAt = DateTime.UtcNow;
            application.UpdatedBy = "web-api";

            if (application.AccountsReceivableMovementId.HasValue)
            {
                var movement = await db.AccountsReceivableMovements.FirstOrDefaultAsync(x => x.Id == application.AccountsReceivableMovementId.Value);
                if (movement is not null)
                {
                    movement.Status = "cancelled";
                    movement.UpdatedAt = DateTime.UtcNow;
                    movement.UpdatedBy = "web-api";
                    movement.Reference = AppendCancellationSuffix(movement.Reference);
                }
            }
            else
            {
                var movement = await db.AccountsReceivableMovements.FirstOrDefaultAsync(x => x.ReceiptId == application.ReceiptId && x.SalesInvoiceId == application.SalesInvoiceId && x.CreditAmount == application.AppliedAmount && x.Status != "cancelled");
                if (movement is not null)
                {
                    movement.Status = "cancelled";
                    movement.UpdatedAt = DateTime.UtcNow;
                    movement.UpdatedBy = "web-api";
                    movement.Reference = AppendCancellationSuffix(movement.Reference);
                }
            }

            await db.SaveChangesAsync();

            var account = await db.AccountsReceivableAccounts.FirstOrDefaultAsync(x => x.CustomerId == application.CustomerId && x.CompanyId == application.CompanyId);
            if (account is not null)
            {
                await SyncAccountProjectionAsync(db, account.Id);
            }

            return Results.Ok(new { success = true });
        });

        group.MapGet("/dashboard/summary", async (NanchesoftDbContext db) =>
        {
            var balances = await BuildBalancesAsync(db);
            var aging = await BuildAgingAsync(db, null);

            return Results.Ok(new ArDashboardSummaryDto
            {
                ActiveCustomersWithBalance = balances.Count(x => x.CurrentBalance > 0m),
                TotalOpenBalance = balances.Sum(x => x.CurrentBalance),
                OverdueBalance = aging.Sum(x => x.Bucket31To60 + x.Bucket61To90 + x.BucketOver90),
                CurrentBalance = aging.Sum(x => x.BucketCurrent),
                AppliedReceiptsCount = await db.ReceiptApplications.CountAsync(x => x.IsActive && x.Status != "cancelled")
            });
        });

        group.MapGet("/dashboard/recent", async (NanchesoftDbContext db) =>
        {
            var invoiceRows = await db.SalesInvoices.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "posted" && x.CustomerId != null)
                .OrderByDescending(x => x.InvoiceDate)
                .Take(10)
                .Select(x => new ArDashboardRecentRowDto
                {
                    MovementDate = x.InvoiceDate,
                    DocumentType = "Factura",
                    Folio = x.Folio,
                    CustomerName = x.Customer != null ? x.Customer.Name : string.Empty,
                    ChargeAmount = x.Total,
                    CreditAmount = 0m
                }).ToListAsync();

            var applicationRows = await (from appRow in db.ReceiptApplications.AsNoTracking()
                                         join receipt in db.Receipts.AsNoTracking() on appRow.ReceiptId equals receipt.Id
                                         join customer in db.Customers.AsNoTracking() on appRow.CustomerId equals customer.Id into customerJoin
                                         from customer in customerJoin.DefaultIfEmpty()
                                         where appRow.IsActive && appRow.Status != "cancelled"
                                         orderby appRow.ApplicationDate descending
                                         select new ArDashboardRecentRowDto
                                         {
                                             MovementDate = appRow.ApplicationDate,
                                             DocumentType = "Aplicación",
                                             Folio = receipt.Folio,
                                             CustomerName = customer != null ? customer.Name : string.Empty,
                                             ChargeAmount = 0m,
                                             CreditAmount = appRow.AppliedAmount
                                         }).Take(10).ToListAsync();

            return Results.Ok(invoiceRows.Concat(applicationRows)
                .OrderByDescending(x => x.MovementDate)
                .Take(15)
                .ToList());
        });

        return app;
    }

    private static async Task<List<ArBalanceRowDto>> BuildBalancesAsync(NanchesoftDbContext db)
    {
        var customers = await db.Customers.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.CompanyId, x.Code, x.Name, x.CreditLimit })
            .ToListAsync();

        var chargeRows = await db.SalesInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.CustomerId != null)
            .GroupBy(x => x.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, Amount = g.Sum(x => x.Total), LastDate = g.Max(x => x.InvoiceDate) })
            .ToListAsync();

        var creditNoteRows = await db.CreditNotes.AsNoTracking()
            .Where(x => x.IsActive && x.Status == "posted" && x.CustomerId != null)
            .GroupBy(x => x.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, Amount = g.Sum(x => x.Total), LastDate = g.Max(x => x.CreditNoteDate) })
            .ToListAsync();

        var applicationRows = await db.ReceiptApplications.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled")
            .GroupBy(x => x.CustomerId)
            .Select(g => new { CustomerId = g.Key, Amount = g.Sum(x => x.AppliedAmount), LastDate = g.Max(x => x.ApplicationDate) })
            .ToListAsync();

        var chargeMap = chargeRows.ToDictionary(x => x.CustomerId, x => x);
        var creditMap = creditNoteRows.ToDictionary(x => x.CustomerId, x => x);
        var applicationMap = applicationRows.ToDictionary(x => x.CustomerId, x => x);

        return customers.Select(customer =>
        {
            chargeMap.TryGetValue(customer.Id, out var charges);
            creditMap.TryGetValue(customer.Id, out var credits);
            applicationMap.TryGetValue(customer.Id, out var applications);
            var totalCharges = charges?.Amount ?? 0m;
            var totalCredits = (credits?.Amount ?? 0m) + (applications?.Amount ?? 0m);
            var lastMovementAt = new[] { charges?.LastDate, credits?.LastDate, applications?.LastDate }
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .DefaultIfEmpty()
                .Max();

            return new ArBalanceRowDto
            {
                CustomerId = customer.Id,
                CompanyId = customer.CompanyId,
                CustomerCode = customer.Code,
                CustomerName = customer.Name,
                CreditLimit = customer.CreditLimit,
                TotalCharges = totalCharges,
                TotalCredits = totalCredits,
                CurrentBalance = totalCharges - totalCredits,
                LastMovementAt = lastMovementAt == default ? null : lastMovementAt
            };
        })
        .Where(x => x.TotalCharges != 0m || x.TotalCredits != 0m || x.CurrentBalance != 0m)
        .OrderByDescending(x => x.CurrentBalance)
        .ThenBy(x => x.CustomerName)
        .ToList();
    }

    private static async Task<List<ArAgingRowDto>> BuildAgingAsync(NanchesoftDbContext db, Guid? customerId)
    {
        var appliedByInvoice = await db.ReceiptApplications.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled" && (!customerId.HasValue || x.CustomerId == customerId.Value))
            .GroupBy(x => x.SalesInvoiceId)
            .Select(g => new { SalesInvoiceId = g.Key, Applied = g.Sum(x => x.AppliedAmount) })
            .ToDictionaryAsync(x => x.SalesInvoiceId, x => x.Applied);

        var invoices = await (from invoice in db.SalesInvoices.AsNoTracking()
                              join customer in db.Customers.AsNoTracking() on invoice.CustomerId equals customer.Id into customerJoin
                              from customer in customerJoin.DefaultIfEmpty()
                              where invoice.IsActive && invoice.Status == "posted" && invoice.CustomerId != null && (!customerId.HasValue || invoice.CustomerId == customerId)
                              orderby invoice.InvoiceDate
                              select new
                              {
                                  invoice.Id,
                                  invoice.CustomerId,
                                  invoice.CompanyId,
                                  invoice.InvoiceDate,
                                  invoice.Folio,
                                  invoice.Total,
                                  CustomerCode = customer != null ? customer.Code : string.Empty,
                                  CustomerName = customer != null ? customer.Name : string.Empty
                              }).ToListAsync();

        var today = DateTime.UtcNow.Date;
        return invoices.Select(invoice =>
        {
            var applied = appliedByInvoice.TryGetValue(invoice.Id, out var totalApplied) ? totalApplied : 0m;
            var openAmount = invoice.Total - applied;
            var ageDays = (today - invoice.InvoiceDate.Date).Days;
            return new ArAgingRowDto
            {
                SalesInvoiceId = invoice.Id,
                CustomerId = invoice.CustomerId,
                CompanyId = invoice.CompanyId,
                CustomerCode = invoice.CustomerCode,
                CustomerName = invoice.CustomerName,
                Folio = invoice.Folio,
                InvoiceDate = invoice.InvoiceDate,
                OpenAmount = openAmount < 0m ? 0m : openAmount,
                BucketCurrent = ageDays <= 30 ? openAmount : 0m,
                Bucket31To60 = ageDays is >= 31 and <= 60 ? openAmount : 0m,
                Bucket61To90 = ageDays is >= 61 and <= 90 ? openAmount : 0m,
                BucketOver90 = ageDays > 90 ? openAmount : 0m
            };
        })
        .Where(x => x.OpenAmount > 0m)
        .OrderByDescending(x => x.OpenAmount)
        .ToList();
    }

    private static async Task<decimal> CalculateCurrentBalanceAsync(NanchesoftDbContext db, Guid customerId)
    {
        var charges = await db.SalesInvoices.Where(x => x.CustomerId == customerId && x.IsActive && x.Status == "posted")
            .SumAsync(x => (decimal?)x.Total) ?? 0m;
        var creditNotes = await db.CreditNotes.Where(x => x.CustomerId == customerId && x.IsActive && x.Status == "posted")
            .SumAsync(x => (decimal?)x.Total) ?? 0m;
        var applied = await db.ReceiptApplications.Where(x => x.CustomerId == customerId && x.IsActive && x.Status != "cancelled")
            .SumAsync(x => (decimal?)x.AppliedAmount) ?? 0m;
        return charges - creditNotes - applied;
    }

    private static async Task SyncAccountProjectionAsync(NanchesoftDbContext db, Guid accountId)
    {
        var account = await db.AccountsReceivableAccounts.FirstOrDefaultAsync(x => x.Id == accountId);
        if (account is null)
            return;

        var charges = await db.SalesInvoices.Where(x => x.CustomerId == account.CustomerId && x.IsActive && x.Status == "posted")
            .SumAsync(x => (decimal?)x.Total) ?? 0m;
        var creditNotes = await db.CreditNotes.Where(x => x.CustomerId == account.CustomerId && x.IsActive && x.Status == "posted")
            .SumAsync(x => (decimal?)x.Total) ?? 0m;
        var applied = await db.ReceiptApplications.Where(x => x.CustomerId == account.CustomerId && x.IsActive && x.Status != "cancelled")
            .SumAsync(x => (decimal?)x.AppliedAmount) ?? 0m;

        account.TotalCharges = charges;
        account.TotalCredits = creditNotes + applied;
        account.CurrentBalance = charges - creditNotes - applied;
        account.LastMovementAt = DateTime.UtcNow;
        account.UpdatedAt = DateTime.UtcNow;
        account.UpdatedBy = "web-api";
        await db.SaveChangesAsync();
    }

    private static DateTime EnsureUtcDate(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value.Date
            : DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    }

    private static string AppendCancellationSuffix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Cancelado";

        return value.Contains("cancelado", StringComparison.OrdinalIgnoreCase)
            ? value
            : $"{value} · cancelado";
    }
}

public sealed class ArLookupsDto
{
    public List<ArLookupItem> Customers { get; set; } = new();
    public List<ArLookupItem> Receipts { get; set; } = new();
    public List<ArLookupItem> SalesInvoices { get; set; } = new();
}

public sealed class ArLookupItem
{
    public Guid Id { get; set; }
    public Guid? CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal OpenAmount { get; set; }
    public DateTime? MovementDate { get; set; }
}

public sealed class ArBalanceRowDto
{
    public Guid CustomerId { get; set; }
    public Guid CompanyId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal TotalCharges { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime? LastMovementAt { get; set; }
}

public sealed class ArStatementRowDto
{
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime MovementDate { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal ChargeAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal BalanceAfter { get; set; }
}

public sealed class ArAgingRowDto
{
    public Guid SalesInvoiceId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid CompanyId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal OpenAmount { get; set; }
    public decimal BucketCurrent { get; set; }
    public decimal Bucket31To60 { get; set; }
    public decimal Bucket61To90 { get; set; }
    public decimal BucketOver90 { get; set; }
}

public sealed class ArReceiptApplicationRowDto
{
    public Guid ReceiptApplicationId { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid ReceiptId { get; set; }
    public string ReceiptFolio { get; set; } = string.Empty;
    public Guid SalesInvoiceId { get; set; }
    public string SalesInvoiceFolio { get; set; } = string.Empty;
    public DateTime ApplicationDate { get; set; }
    public decimal AppliedAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
}

public sealed class ArApplyReceiptRequest
{
    public Guid? CustomerId { get; set; }
    public Guid? ReceiptId { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    public DateTime? ApplicationDate { get; set; }
    public decimal AppliedAmount { get; set; }
    public string? Reference { get; set; }
}

public sealed class ArDashboardSummaryDto
{
    public int ActiveCustomersWithBalance { get; set; }
    public decimal TotalOpenBalance { get; set; }
    public decimal OverdueBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public int AppliedReceiptsCount { get; set; }
}

public sealed class ArDashboardRecentRowDto
{
    public DateTime MovementDate { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal ChargeAmount { get; set; }
    public decimal CreditAmount { get; set; }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class AccountsPayableEndpoints
{
    public static IEndpointRouteBuilder MapAccountsPayableEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts-payable").WithTags("AccountsPayable");

        group.MapGet("/lookups", async (NanchesoftDbContext db) =>
        {
            var suppliers = await db.Suppliers.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new ApLookupItem
                {
                    Id = x.Id,
                    SupplierId = x.Id,
                    Name = x.Code + " · " + x.Name
                })
                .ToListAsync();

            var paymentAppliedMap = await BuildPaymentAppliedMapAsync(db);
            var invoiceOpenMap = await BuildInvoiceOpenMapAsync(db);

            var payments = await db.Payments.AsNoTracking()
                .Where(x => x.IsActive && x.Status != "cancelled" && x.SupplierId != null)
                .OrderByDescending(x => x.PaymentDate)
                .Select(x => new
                {
                    x.Id,
                    x.SupplierId,
                    x.Folio,
                    x.Total,
                    x.Status,
                    x.PaymentDate
                })
                .ToListAsync();

            var paymentItems = payments
                .Select(x =>
                {
                    var applied = paymentAppliedMap.TryGetValue(x.Id, out var value) ? value : 0m;
                    var available = x.Total - applied;
                    return new ApLookupItem
                    {
                        Id = x.Id,
                        SupplierId = x.SupplierId,
                        Amount = available < 0m ? 0m : available,
                        Name = $"{x.Folio} · disponible {(available < 0m ? 0m : available):N2}"
                    };
                })
                .Where(x => x.Amount > 0m)
                .ToList();

            var invoices = await db.PurchaseInvoices.AsNoTracking()
                .Where(x => x.IsActive && x.Status != "cancelled" && x.SupplierId != null)
                .OrderByDescending(x => x.InvoiceDate)
                .Select(x => new
                {
                    x.Id,
                    x.SupplierId,
                    x.Folio,
                    x.Total,
                    x.InvoiceDate
                })
                .ToListAsync();

            var invoiceItems = invoices
                .Select(x =>
                {
                    var open = invoiceOpenMap.TryGetValue(x.Id, out var value) ? value : x.Total;
                    return new ApLookupItem
                    {
                        Id = x.Id,
                        SupplierId = x.SupplierId,
                        Amount = open < 0m ? 0m : open,
                        Name = $"{x.Folio} · abierto {(open < 0m ? 0m : open):N2}"
                    };
                })
                .Where(x => x.Amount > 0m)
                .ToList();

            return Results.Ok(new ApLookupsDto
            {
                Suppliers = suppliers,
                Payments = paymentItems,
                PurchaseInvoices = invoiceItems
            });
        });

        group.MapGet("/balances", async (NanchesoftDbContext db) => Results.Ok(await BuildBalancesAsync(db)));

        group.MapGet("/statements", async (Guid? supplierId, NanchesoftDbContext db) =>
        {
            var invoiceRows = await db.PurchaseInvoices.AsNoTracking()
                .Where(x => x.IsActive && x.Status != "cancelled" && (!supplierId.HasValue || x.SupplierId == supplierId))
                .Select(x => new ApStatementRowDto
                {
                    SupplierId = x.SupplierId,
                    MovementDate = x.InvoiceDate,
                    DocumentType = "purchase_invoice",
                    Folio = x.Folio,
                    Reference = x.SupplierInvoiceFolio,
                    ChargeAmount = x.Total,
                    CreditAmount = 0m
                })
                .ToListAsync();

            var returnRows = await db.PurchaseReturns.AsNoTracking()
                .Where(x => x.IsActive && x.Status != "cancelled" && (!supplierId.HasValue || x.SupplierId == supplierId))
                .Select(x => new ApStatementRowDto
                {
                    SupplierId = x.SupplierId,
                    MovementDate = x.ReturnDate,
                    DocumentType = "purchase_return",
                    Folio = x.Folio,
                    Reference = x.Reason,
                    ChargeAmount = 0m,
                    CreditAmount = x.Total
                })
                .ToListAsync();

            var applicationRows = await (from line in db.PaymentLines.AsNoTracking()
                                         join payment in db.Payments.AsNoTracking() on line.PaymentId equals payment.Id
                                         where line.IsActive && line.PurchaseInvoiceId != null && payment.IsActive && payment.Status != "cancelled"
                                               && (!supplierId.HasValue || payment.SupplierId == supplierId)
                                         select new ApStatementRowDto
                                         {
                                             SupplierId = payment.SupplierId,
                                             MovementDate = line.CreatedAt,
                                             DocumentType = "payment_application",
                                             Folio = payment.Folio,
                                             Reference = string.IsNullOrWhiteSpace(line.Description) ? payment.Reference : line.Description,
                                             ChargeAmount = 0m,
                                             CreditAmount = line.Amount
                                         }).ToListAsync();

            var supplierNames = await db.Suppliers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);

            var rows = invoiceRows.Concat(returnRows).Concat(applicationRows)
                .OrderBy(x => x.SupplierId)
                .ThenBy(x => x.MovementDate)
                .ThenBy(x => x.Folio)
                .ToList();

            Guid? currentSupplierId = null;
            decimal runningBalance = 0m;
            foreach (var row in rows)
            {
                if (currentSupplierId != row.SupplierId)
                {
                    currentSupplierId = row.SupplierId;
                    runningBalance = 0m;
                }

                runningBalance += row.ChargeAmount - row.CreditAmount;
                row.BalanceAfter = runningBalance;
                row.SupplierName = row.SupplierId.HasValue && supplierNames.TryGetValue(row.SupplierId.Value, out var name)
                    ? name
                    : string.Empty;
            }

            return Results.Ok(rows.OrderByDescending(x => x.MovementDate).ThenByDescending(x => x.Folio).ToList());
        });

        group.MapGet("/aging", async (NanchesoftDbContext db) => Results.Ok(await BuildAgingAsync(db)));

        group.MapGet("/applications", async (NanchesoftDbContext db) =>
        {
            var rows = await (from line in db.PaymentLines.AsNoTracking()
                              join payment in db.Payments.AsNoTracking() on line.PaymentId equals payment.Id
                              join invoice in db.PurchaseInvoices.AsNoTracking() on line.PurchaseInvoiceId equals invoice.Id
                              join supplier in db.Suppliers.AsNoTracking() on payment.SupplierId equals supplier.Id into supplierJoin
                              from supplier in supplierJoin.DefaultIfEmpty()
                              where line.IsActive && line.PurchaseInvoiceId != null && payment.IsActive && payment.Status != "cancelled"
                              orderby line.CreatedAt descending
                              select new ApPaymentApplicationRowDto
                              {
                                  PaymentLineId = line.Id,
                                  SupplierId = payment.SupplierId,
                                  SupplierName = supplier != null ? supplier.Name : string.Empty,
                                  PaymentId = payment.Id,
                                  PaymentFolio = payment.Folio,
                                  PurchaseInvoiceId = invoice.Id,
                                  PurchaseInvoiceFolio = invoice.Folio,
                                  ApplicationDate = line.CreatedAt,
                                  AppliedAmount = line.Amount,
                                  Status = payment.Status,
                                  Notes = line.Description
                              }).ToListAsync();

            return Results.Ok(rows);
        });

        group.MapPost("/apply-payment", async (ApApplyPaymentRequest request, NanchesoftDbContext db) =>
        {
            if (!request.SupplierId.HasValue || !request.PaymentId.HasValue || !request.PurchaseInvoiceId.HasValue || request.AppliedAmount <= 0m)
                return Results.BadRequest(new { message = "Debes indicar proveedor, pago, factura e importe a aplicar." });

            var invoice = await db.PurchaseInvoices.FirstOrDefaultAsync(x => x.Id == request.PurchaseInvoiceId.Value && x.IsActive && x.Status != "cancelled");
            if (invoice is null)
                return Results.NotFound(new { message = "No se encontró la factura de proveedor." });

            var payment = await db.Payments.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == request.PaymentId.Value && x.IsActive && x.Status != "cancelled");
            if (payment is null)
                return Results.NotFound(new { message = "No se encontró el pago." });

            if (invoice.SupplierId != request.SupplierId || payment.SupplierId != request.SupplierId)
                return Results.BadRequest(new { message = "La factura y el pago deben pertenecer al mismo proveedor." });

            var invoiceCredits = await CalculateInvoiceCreditsAsync(db, invoice.Id);
            var remainingInvoice = invoice.Total - invoiceCredits;
            if (remainingInvoice <= 0m)
                return Results.BadRequest(new { message = "La factura ya está liquidada." });

            var paymentAllocated = await db.PaymentLines.AsNoTracking()
                .Where(x => x.PaymentId == payment.Id && x.IsActive && x.PurchaseInvoiceId != null)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            var remainingPayment = payment.Total - paymentAllocated;
            if (remainingPayment <= 0m)
                return Results.BadRequest(new { message = "El pago ya no tiene saldo disponible para aplicar." });

            var amountToApply = Math.Min(request.AppliedAmount, Math.Min(remainingInvoice, remainingPayment));
            if (amountToApply <= 0m)
                return Results.BadRequest(new { message = "El importe a aplicar es inválido." });

            var applicationDate = ToUtcDate(request.ApplicationDate);
            var lineNumber = (payment.Lines?.Count ?? 0) + 1;

            payment.Lines.Add(new Nanchesoft.Domain.Entities.PaymentLine
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                LineNumber = lineNumber,
                Description = string.IsNullOrWhiteSpace(request.Notes)
                    ? $"Aplicación {payment.Folio} a {invoice.Folio}"
                    : request.Notes.Trim(),
                Amount = amountToApply,
                PurchaseInvoiceId = invoice.Id,
                CreatedAt = applicationDate,
                CreatedBy = "web-api",
                IsActive = true
            });

            payment.UpdatedAt = DateTime.UtcNow;
            payment.UpdatedBy = "web-api";
            await db.SaveChangesAsync();

            return Results.Ok(new { success = true, appliedAmount = amountToApply });
        });

        group.MapGet("/dashboard/summary", async (NanchesoftDbContext db) =>
        {
            var balances = await BuildBalancesAsync(db);
            var aging = await BuildAgingAsync(db);
            var appliedCount = await db.PaymentLines.CountAsync(x => x.IsActive && x.PurchaseInvoiceId != null);

            return Results.Ok(new ApDashboardSummaryDto
            {
                ActiveSuppliersWithBalance = balances.Count(x => x.CurrentBalance > 0m),
                TotalOpenBalance = balances.Sum(x => x.CurrentBalance),
                OverdueBalance = aging.Sum(x => x.Bucket31To60 + x.Bucket61To90 + x.BucketOver90),
                CurrentBalance = aging.Sum(x => x.BucketCurrent),
                AppliedPaymentsCount = appliedCount
            });
        });

        group.MapGet("/dashboard/recent", async (NanchesoftDbContext db) =>
        {
            var invoiceRows = await db.PurchaseInvoices.AsNoTracking()
                .Where(x => x.IsActive && x.Status != "cancelled" && x.SupplierId != null)
                .OrderByDescending(x => x.InvoiceDate)
                .Take(10)
                .Select(x => new ApDashboardRecentRowDto
                {
                    MovementDate = x.InvoiceDate,
                    DocumentType = "Factura proveedor",
                    Folio = x.Folio,
                    SupplierName = x.Supplier != null ? x.Supplier.Name : string.Empty,
                    ChargeAmount = x.Total,
                    CreditAmount = 0m
                }).ToListAsync();

            var paymentRows = await (from line in db.PaymentLines.AsNoTracking()
                                     join payment in db.Payments.AsNoTracking() on line.PaymentId equals payment.Id
                                     join supplier in db.Suppliers.AsNoTracking() on payment.SupplierId equals supplier.Id into supplierJoin
                                     from supplier in supplierJoin.DefaultIfEmpty()
                                     where line.IsActive && line.PurchaseInvoiceId != null && payment.IsActive && payment.Status != "cancelled"
                                     orderby line.CreatedAt descending
                                     select new ApDashboardRecentRowDto
                                     {
                                         MovementDate = line.CreatedAt,
                                         DocumentType = "Aplicación pago",
                                         Folio = payment.Folio,
                                         SupplierName = supplier != null ? supplier.Name : string.Empty,
                                         ChargeAmount = 0m,
                                         CreditAmount = line.Amount
                                     }).Take(10).ToListAsync();

            return Results.Ok(invoiceRows.Concat(paymentRows)
                .OrderByDescending(x => x.MovementDate)
                .Take(15)
                .ToList());
        });

        return app;
    }

    private static async Task<List<ApBalanceRowDto>> BuildBalancesAsync(NanchesoftDbContext db)
    {
        var suppliers = await db.Suppliers.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.CompanyId, x.Code, x.Name })
            .ToListAsync();

        var chargeRows = await db.PurchaseInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled" && x.SupplierId != null)
            .GroupBy(x => x.SupplierId!.Value)
            .Select(g => new { SupplierId = g.Key, Amount = g.Sum(x => x.Total), LastDate = g.Max(x => x.InvoiceDate) })
            .ToListAsync();

        var returnRows = await db.PurchaseReturns.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled" && x.SupplierId != null)
            .GroupBy(x => x.SupplierId!.Value)
            .Select(g => new { SupplierId = g.Key, Amount = g.Sum(x => x.Total), LastDate = g.Max(x => x.ReturnDate) })
            .ToListAsync();

        var paymentRows = await (from line in db.PaymentLines.AsNoTracking()
                                 join payment in db.Payments.AsNoTracking() on line.PaymentId equals payment.Id
                                 where line.IsActive && line.PurchaseInvoiceId != null && payment.IsActive && payment.Status != "cancelled" && payment.SupplierId != null
                                 group new { line, payment } by payment.SupplierId!.Value into g
                                 select new
                                 {
                                     SupplierId = g.Key,
                                     Amount = g.Sum(x => x.line.Amount),
                                     LastDate = g.Max(x => x.line.CreatedAt)
                                 }).ToListAsync();

        var chargeMap = chargeRows.ToDictionary(x => x.SupplierId, x => x);
        var returnMap = returnRows.ToDictionary(x => x.SupplierId, x => x);
        var paymentMap = paymentRows.ToDictionary(x => x.SupplierId, x => x);

        return suppliers.Select(supplier =>
        {
            chargeMap.TryGetValue(supplier.Id, out var charges);
            returnMap.TryGetValue(supplier.Id, out var returns);
            paymentMap.TryGetValue(supplier.Id, out var payments);
            var totalCharges = charges?.Amount ?? 0m;
            var totalCredits = (returns?.Amount ?? 0m) + (payments?.Amount ?? 0m);
            var lastMovementAt = new[] { charges?.LastDate, returns?.LastDate, payments?.LastDate }
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .DefaultIfEmpty()
                .Max();

            return new ApBalanceRowDto
            {
                SupplierId = supplier.Id,
                CompanyId = supplier.CompanyId,
                SupplierCode = supplier.Code,
                SupplierName = supplier.Name,
                TotalCharges = totalCharges,
                TotalCredits = totalCredits,
                CurrentBalance = totalCharges - totalCredits,
                LastMovementAt = lastMovementAt == default ? null : lastMovementAt
            };
        })
        .Where(x => x.TotalCharges != 0m || x.TotalCredits != 0m || x.CurrentBalance != 0m)
        .OrderByDescending(x => x.CurrentBalance)
        .ThenBy(x => x.SupplierName)
        .ToList();
    }

    private static async Task<List<ApAgingRowDto>> BuildAgingAsync(NanchesoftDbContext db)
    {
        var paidByInvoice = await (from line in db.PaymentLines.AsNoTracking()
                                   join payment in db.Payments.AsNoTracking() on line.PaymentId equals payment.Id
                                   where line.IsActive && line.PurchaseInvoiceId != null && payment.IsActive && payment.Status != "cancelled"
                                   group line by line.PurchaseInvoiceId!.Value into g
                                   select new { PurchaseInvoiceId = g.Key, Applied = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.PurchaseInvoiceId, x => x.Applied);

        var returnedByInvoice = await db.PurchaseReturns.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled" && x.PurchaseInvoiceId != null)
            .GroupBy(x => x.PurchaseInvoiceId!.Value)
            .Select(g => new { PurchaseInvoiceId = g.Key, Amount = g.Sum(x => x.Total) })
            .ToDictionaryAsync(x => x.PurchaseInvoiceId, x => x.Amount);

        var invoices = await (from invoice in db.PurchaseInvoices.AsNoTracking()
                              join supplier in db.Suppliers.AsNoTracking() on invoice.SupplierId equals supplier.Id into supplierJoin
                              from supplier in supplierJoin.DefaultIfEmpty()
                              where invoice.IsActive && invoice.Status != "cancelled" && invoice.SupplierId != null
                              orderby invoice.InvoiceDate
                              select new
                              {
                                  invoice.Id,
                                  invoice.SupplierId,
                                  invoice.CompanyId,
                                  invoice.InvoiceDate,
                                  invoice.Folio,
                                  invoice.Total,
                                  SupplierCode = supplier != null ? supplier.Code : string.Empty,
                                  SupplierName = supplier != null ? supplier.Name : string.Empty
                              }).ToListAsync();

        var today = DateTime.UtcNow.Date;
        return invoices.Select(invoice =>
        {
            var applied = paidByInvoice.TryGetValue(invoice.Id, out var paid) ? paid : 0m;
            var returned = returnedByInvoice.TryGetValue(invoice.Id, out var returnedAmount) ? returnedAmount : 0m;
            var openAmount = invoice.Total - applied - returned;
            var normalizedOpen = openAmount < 0m ? 0m : openAmount;
            var ageDays = (today - invoice.InvoiceDate.Date).Days;

            return new ApAgingRowDto
            {
                PurchaseInvoiceId = invoice.Id,
                SupplierId = invoice.SupplierId,
                CompanyId = invoice.CompanyId,
                SupplierCode = invoice.SupplierCode,
                SupplierName = invoice.SupplierName,
                Folio = invoice.Folio,
                InvoiceDate = invoice.InvoiceDate,
                OpenAmount = normalizedOpen,
                BucketCurrent = ageDays <= 30 ? normalizedOpen : 0m,
                Bucket31To60 = ageDays is >= 31 and <= 60 ? normalizedOpen : 0m,
                Bucket61To90 = ageDays is >= 61 and <= 90 ? normalizedOpen : 0m,
                BucketOver90 = ageDays > 90 ? normalizedOpen : 0m
            };
        })
        .Where(x => x.OpenAmount > 0m)
        .OrderByDescending(x => x.OpenAmount)
        .ToList();
    }

    private static async Task<Dictionary<Guid, decimal>> BuildPaymentAppliedMapAsync(NanchesoftDbContext db)
    {
        return await db.PaymentLines.AsNoTracking()
            .Where(x => x.IsActive && x.PurchaseInvoiceId != null)
            .GroupBy(x => x.PaymentId)
            .Select(g => new { PaymentId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.PaymentId, x => x.Amount);
    }

    private static async Task<Dictionary<Guid, decimal>> BuildInvoiceOpenMapAsync(NanchesoftDbContext db)
    {
        var paymentsByInvoice = await (from line in db.PaymentLines.AsNoTracking()
                                       join payment in db.Payments.AsNoTracking() on line.PaymentId equals payment.Id
                                       where line.IsActive && line.PurchaseInvoiceId != null && payment.IsActive && payment.Status != "cancelled"
                                       group line by line.PurchaseInvoiceId!.Value into g
                                       select new { PurchaseInvoiceId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.PurchaseInvoiceId, x => x.Amount);

        var returnsByInvoice = await db.PurchaseReturns.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled" && x.PurchaseInvoiceId != null)
            .GroupBy(x => x.PurchaseInvoiceId!.Value)
            .Select(g => new { PurchaseInvoiceId = g.Key, Amount = g.Sum(x => x.Total) })
            .ToDictionaryAsync(x => x.PurchaseInvoiceId, x => x.Amount);

        var invoices = await db.PurchaseInvoices.AsNoTracking()
            .Where(x => x.IsActive && x.Status != "cancelled")
            .Select(x => new { x.Id, x.Total })
            .ToListAsync();

        return invoices.ToDictionary(
            x => x.Id,
            x =>
            {
                var paid = paymentsByInvoice.TryGetValue(x.Id, out var paymentAmount) ? paymentAmount : 0m;
                var returned = returnsByInvoice.TryGetValue(x.Id, out var returnAmount) ? returnAmount : 0m;
                var open = x.Total - paid - returned;
                return open < 0m ? 0m : open;
            });
    }

    private static async Task<decimal> CalculateInvoiceCreditsAsync(NanchesoftDbContext db, Guid purchaseInvoiceId)
    {
        var paid = await (from line in db.PaymentLines
                          join payment in db.Payments on line.PaymentId equals payment.Id
                          where line.IsActive && line.PurchaseInvoiceId == purchaseInvoiceId && payment.IsActive && payment.Status != "cancelled"
                          select line.Amount)
            .SumAsync(x => (decimal?)x) ?? 0m;

        var returned = await db.PurchaseReturns
            .Where(x => x.IsActive && x.Status != "cancelled" && x.PurchaseInvoiceId == purchaseInvoiceId)
            .SumAsync(x => (decimal?)x.Total) ?? 0m;

        return paid + returned;
    }

    private static DateTime ToUtcDate(DateTime? value)
    {
        var source = value?.Date ?? DateTime.UtcNow.Date;
        return DateTime.SpecifyKind(source, DateTimeKind.Utc);
    }
}

public sealed class ApLookupsDto
{
    public List<ApLookupItem> Suppliers { get; set; } = new();
    public List<ApLookupItem> Payments { get; set; } = new();
    public List<ApLookupItem> PurchaseInvoices { get; set; } = new();
}

public sealed class ApLookupItem
{
    public Guid Id { get; set; }
    public Guid? SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
}

public sealed class ApBalanceRowDto
{
    public Guid SupplierId { get; set; }
    public Guid CompanyId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal TotalCharges { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime? LastMovementAt { get; set; }
}

public sealed class ApStatementRowDto
{
    public Guid? SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateTime MovementDate { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal ChargeAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal BalanceAfter { get; set; }
}

public sealed class ApAgingRowDto
{
    public Guid PurchaseInvoiceId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid CompanyId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal OpenAmount { get; set; }
    public decimal BucketCurrent { get; set; }
    public decimal Bucket31To60 { get; set; }
    public decimal Bucket61To90 { get; set; }
    public decimal BucketOver90 { get; set; }
}

public sealed class ApPaymentApplicationRowDto
{
    public Guid PaymentLineId { get; set; }
    public Guid? SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public Guid PaymentId { get; set; }
    public string PaymentFolio { get; set; } = string.Empty;
    public Guid PurchaseInvoiceId { get; set; }
    public string PurchaseInvoiceFolio { get; set; } = string.Empty;
    public DateTime ApplicationDate { get; set; }
    public decimal AppliedAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class ApApplyPaymentRequest
{
    public Guid? SupplierId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? PurchaseInvoiceId { get; set; }
    public DateTime? ApplicationDate { get; set; }
    public decimal AppliedAmount { get; set; }
    public string? Notes { get; set; }
}

public sealed class ApDashboardSummaryDto
{
    public int ActiveSuppliersWithBalance { get; set; }
    public decimal TotalOpenBalance { get; set; }
    public decimal OverdueBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public int AppliedPaymentsCount { get; set; }
}

public sealed class ApDashboardRecentRowDto
{
    public DateTime MovementDate { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal ChargeAmount { get; set; }
    public decimal CreditAmount { get; set; }
}

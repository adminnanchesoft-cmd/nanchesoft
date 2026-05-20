using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PaymentCentralEndpoints
{
    public static IEndpointRouteBuilder MapPaymentCentralEndpoints(this IEndpointRouteBuilder app)
    {
        MapPendingPaymentsEndpoints(app);
        MapBatchEndpoints(app);
        MapExecutiveEndpoints(app);
        return app;
    }

    // =================================================================
    // Pending payments (consolidado multiempresa) y catálogos de soporte
    // =================================================================
    private static void MapPendingPaymentsEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payment-central").WithTags("PaymentCentral");

        // Listado global multiempresa de facturas de proveedor pendientes de pago
        group.MapGet("/pending", async (
            Guid? companyId,
            Guid? supplierId,
            Guid? currencyId,
            int? overdueDays,
            string? priority,
            NanchesoftDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;

            // Aplicado (pagos)
            var paidMap = await (from line in db.PaymentLines.AsNoTracking()
                                 join payment in db.Payments.AsNoTracking() on line.PaymentId equals payment.Id
                                 where line.IsActive && line.PurchaseInvoiceId != null
                                       && payment.IsActive && payment.Status != "cancelled"
                                 group line.Amount by line.PurchaseInvoiceId!.Value into g
                                 select new { InvoiceId = g.Key, Applied = g.Sum() })
                .ToDictionaryAsync(x => x.InvoiceId, x => x.Applied);

            // Devuelto (notas de devolución)
            var returnMap = await db.PurchaseReturns.AsNoTracking()
                .Where(x => x.IsActive && x.Status != "cancelled" && x.PurchaseInvoiceId != null)
                .GroupBy(x => x.PurchaseInvoiceId!.Value)
                .Select(g => new { InvoiceId = g.Key, Amount = g.Sum(x => x.Total) })
                .ToDictionaryAsync(x => x.InvoiceId, x => x.Amount);

            // Líneas ya comprometidas en lotes vivos (no rechazadas/canceladas/ejecutadas)
            var committedMap = await db.PaymentBatchLines.AsNoTracking()
                .Where(x => x.PurchaseInvoiceId != null
                            && x.LineStatus != "rejected"
                            && x.LineStatus != "cancelled"
                            && x.LineStatus != "executed"
                            && x.IsActive)
                .GroupBy(x => x.PurchaseInvoiceId!.Value)
                .Select(g => new { InvoiceId = g.Key, Amount = g.Sum(x => x.AmountToPay) })
                .ToDictionaryAsync(x => x.InvoiceId, x => x.Amount);

            var invoicesQuery =
                from invoice in db.PurchaseInvoices.AsNoTracking()
                join supplier in db.Suppliers.AsNoTracking() on invoice.SupplierId equals supplier.Id into sj
                from supplier in sj.DefaultIfEmpty()
                join company in db.Companies.AsNoTracking() on invoice.CompanyId equals company.Id into cj
                from company in cj.DefaultIfEmpty()
                join currency in db.Currencies.AsNoTracking() on invoice.CurrencyId equals currency.Id into curj
                from currency in curj.DefaultIfEmpty()
                where invoice.IsActive
                      && invoice.Status != "cancelled"
                      && invoice.Status != "draft"
                select new
                {
                    invoice.Id,
                    invoice.CompanyId,
                    CompanyName = company != null ? company.Name : string.Empty,
                    invoice.SupplierId,
                    SupplierName = supplier != null ? supplier.Name : "—",
                    SupplierCode = supplier != null ? supplier.Code : string.Empty,
                    PaymentTermDays = supplier != null ? supplier.PaymentTermDays : 0,
                    invoice.Folio,
                    invoice.SupplierInvoiceFolio,
                    invoice.InvoiceDate,
                    invoice.Total,
                    invoice.CurrencyId,
                    CurrencyCode = currency != null ? currency.Code : string.Empty,
                    invoice.ExchangeRate
                };

            if (companyId.HasValue) invoicesQuery = invoicesQuery.Where(x => x.CompanyId == companyId.Value);
            if (supplierId.HasValue) invoicesQuery = invoicesQuery.Where(x => x.SupplierId == supplierId.Value);
            if (currencyId.HasValue) invoicesQuery = invoicesQuery.Where(x => x.CurrencyId == currencyId.Value);

            var invoices = await invoicesQuery.OrderBy(x => x.InvoiceDate).ToListAsync();

            var rows = new List<PendingPaymentRowDto>();
            foreach (var invoice in invoices)
            {
                paidMap.TryGetValue(invoice.Id, out var applied);
                returnMap.TryGetValue(invoice.Id, out var returned);
                committedMap.TryGetValue(invoice.Id, out var committed);
                var pending = invoice.Total - applied - returned;
                if (pending <= 0.01m) continue;

                var dueDate = invoice.InvoiceDate.AddDays(invoice.PaymentTermDays);
                var days = (int)Math.Floor((today - dueDate).TotalDays);
                var rowPriority = PriorityFromDays(days);
                if (!string.IsNullOrWhiteSpace(priority) && !string.Equals(rowPriority, priority, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (overdueDays.HasValue && days < overdueDays.Value) continue;

                rows.Add(new PendingPaymentRowDto
                {
                    PurchaseInvoiceId = invoice.Id,
                    CompanyId = invoice.CompanyId,
                    CompanyName = invoice.CompanyName,
                    SupplierId = invoice.SupplierId ?? Guid.Empty,
                    SupplierCode = invoice.SupplierCode,
                    SupplierName = invoice.SupplierName,
                    Folio = invoice.Folio,
                    SupplierInvoiceFolio = invoice.SupplierInvoiceFolio,
                    InvoiceDate = invoice.InvoiceDate,
                    DueDate = dueDate,
                    DaysOverdue = days,
                    PaymentTermDays = invoice.PaymentTermDays,
                    CurrencyId = invoice.CurrencyId,
                    CurrencyCode = invoice.CurrencyCode,
                    ExchangeRate = invoice.ExchangeRate,
                    Total = invoice.Total,
                    PaidAmount = applied + returned,
                    PendingAmount = pending,
                    CommittedAmount = committed,
                    AvailableToPay = Math.Max(0m, pending - committed),
                    Priority = rowPriority
                });
            }

            return Results.Ok(rows
                .OrderByDescending(x => x.DaysOverdue)
                .ThenByDescending(x => x.PendingAmount)
                .ToList());
        });

        // Lookups (empresas, cuentas, proveedores, monedas, tipos de pago)
        group.MapGet("/lookups", async (NanchesoftDbContext db) =>
        {
            var companies = await db.Companies.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new IdNameDto { Id = x.Id, Name = x.Code + " · " + x.Name })
                .ToListAsync();

            var bankAccounts = await (from acc in db.BankAccounts.AsNoTracking()
                                      join bank in db.Banks.AsNoTracking() on acc.BankId equals bank.Id into bj
                                      from bank in bj.DefaultIfEmpty()
                                      join company in db.Companies.AsNoTracking() on acc.CompanyId equals company.Id into cj
                                      from company in cj.DefaultIfEmpty()
                                      where acc.IsActive
                                      orderby acc.Name
                                      select new BankAccountLookupDto
                                      {
                                          Id = acc.Id,
                                          CompanyId = acc.CompanyId,
                                          CompanyName = company != null ? company.Name : string.Empty,
                                          BankId = acc.BankId,
                                          BankName = bank != null ? bank.Name : string.Empty,
                                          AccountNumber = acc.AccountNumber,
                                          CurrencyId = acc.CurrencyId,
                                          Name = (bank != null ? bank.ShortName + " " : "") + acc.Name + " · " + acc.AccountNumber,
                                          CurrentBalance = acc.CurrentBalance
                                      }).ToListAsync();

            var cashAccounts = await (from acc in db.CashAccounts.AsNoTracking()
                                      where acc.IsActive
                                      select new BankAccountLookupDto
                                      {
                                          Id = acc.Id,
                                          CompanyId = acc.CompanyId,
                                          BankId = null,
                                          BankName = "Caja",
                                          AccountNumber = string.Empty,
                                          CurrencyId = acc.CurrencyId,
                                          Name = "Caja · " + acc.Name,
                                          CurrentBalance = acc.CurrentBalance
                                      }).ToListAsync();

            var suppliers = await db.Suppliers.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new IdNameDto { Id = x.Id, Name = x.Code + " · " + x.Name })
                .ToListAsync();

            var currencies = await db.Currencies.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Code)
                .Select(x => new IdNameDto { Id = x.Id, Name = x.Code + " · " + x.Name })
                .ToListAsync();

            return Results.Ok(new PaymentCentralLookupsDto
            {
                Companies = companies,
                BankAccounts = bankAccounts,
                CashAccounts = cashAccounts,
                Suppliers = suppliers,
                Currencies = currencies,
                PaymentTypes = new()
                {
                    new() { Code = "transfer", Name = "Transferencia", FolioPrefix = "TR" },
                    new() { Code = "check",    Name = "Cheque",        FolioPrefix = "CH" },
                    new() { Code = "spei",     Name = "SPEI",          FolioPrefix = "SPEI" },
                    new() { Code = "deposit",  Name = "Depósito",      FolioPrefix = "DEP" },
                    new() { Code = "cash",     Name = "Efectivo",      FolioPrefix = "EF" },
                    new() { Code = "card",     Name = "Tarjeta",       FolioPrefix = "TJ" },
                    new() { Code = "offset",   Name = "Compensación",  FolioPrefix = "COMP" },
                    new() { Code = "other",    Name = "Otros",         FolioPrefix = "OTR" }
                }
            });
        });
    }

    // =================================================================
    // Lotes de pre-pago
    // =================================================================
    private static void MapBatchEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payment-central/batches").WithTags("PaymentCentralBatches");

        group.MapGet("/", async (string? status, DateTime? from, DateTime? to, NanchesoftDbContext db) =>
        {
            var query = db.PaymentBatches.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
            if (from.HasValue) query = query.Where(x => x.BatchDate >= from.Value.Date);
            if (to.HasValue) query = query.Where(x => x.BatchDate <= to.Value.Date.AddDays(1).AddTicks(-1));
            var rows = await query.OrderByDescending(x => x.BatchDate).ThenByDescending(x => x.CreatedAt)
                .Select(x => new PaymentBatchListRowDto
                {
                    Id = x.Id,
                    Folio = x.Folio,
                    BatchDate = x.BatchDate,
                    ScheduledDate = x.ScheduledDate,
                    Status = x.Status,
                    Priority = x.Priority,
                    LineCount = x.LineCount,
                    CompanyCount = x.CompanyCount,
                    SupplierCount = x.SupplierCount,
                    TotalAmount = x.TotalAmount,
                    AuthorizedAmount = x.AuthorizedAmount,
                    ExecutedAmount = x.ExecutedAmount,
                    RequestedByName = x.RequestedByName,
                    AuthorizedByName = x.AuthorizedByName,
                    AuthorizedAt = x.AuthorizedAt,
                    ExecutedAt = x.ExecutedAt
                }).ToListAsync();
            return Results.Ok(rows);
        });

        group.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.PaymentBatches.AsNoTracking()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "Lote no encontrado." });
            return Results.Ok(MapBatchDetail(entity));
        });

        group.MapPost("/", async (PaymentBatchSaveRequestDto request, NanchesoftDbContext db) =>
        {
            if (request.Lines.Count == 0)
                return Results.BadRequest(new { message = "Selecciona al menos un pago para crear el lote." });

            var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync();
            if (tenant is null) return Results.BadRequest(new { message = "No hay tenant configurado." });

            var folio = await GenerateBatchFolioAsync(db, tenant.Id);
            var batch = new PaymentBatch
            {
                TenantId = tenant.Id,
                Folio = folio,
                BatchDate = request.BatchDate?.Date ?? DateTime.UtcNow.Date,
                ScheduledDate = request.ScheduledDate?.Date,
                Status = "pending",
                Priority = NormalizePriority(request.Priority),
                Notes = (request.Notes ?? string.Empty).Trim(),
                RequestedByUserId = request.RequestedByUserId,
                RequestedByName = (request.RequestedByName ?? string.Empty).Trim(),
                CreatedBy = "web-api"
            };

            decimal total = 0m;
            var companies = new HashSet<Guid>();
            var suppliers = new HashSet<Guid>();

            foreach (var line in request.Lines)
            {
                if (line.AmountToPay <= 0m) continue;
                if (line.PurchaseInvoiceId == Guid.Empty) continue;

                var invoice = await db.PurchaseInvoices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == line.PurchaseInvoiceId);
                if (invoice is null) continue;

                companies.Add(invoice.CompanyId);
                if (invoice.SupplierId.HasValue) suppliers.Add(invoice.SupplierId.Value);

                Guid? currencyId = invoice.CurrencyId;
                string currencyCode = string.Empty;
                if (currencyId.HasValue)
                {
                    var cur = await db.Currencies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == currencyId.Value);
                    currencyCode = cur?.Code ?? string.Empty;
                }

                batch.Lines.Add(new PaymentBatchLine
                {
                    TenantId = tenant.Id,
                    CompanyId = invoice.CompanyId,
                    SupplierId = invoice.SupplierId,
                    SupplierName = (line.SupplierName ?? string.Empty).Trim(),
                    PurchaseInvoiceId = invoice.Id,
                    InvoiceFolio = invoice.Folio,
                    InvoiceDate = invoice.InvoiceDate,
                    DueDate = line.DueDate,
                    DaysOverdue = line.DaysOverdue,
                    CurrencyId = currencyId,
                    CurrencyCode = currencyCode,
                    ExchangeRate = invoice.ExchangeRate,
                    OriginalAmount = invoice.Total,
                    AmountDue = line.AmountDue,
                    AmountToPay = line.AmountToPay,
                    Priority = NormalizePriority(line.Priority),
                    PaymentType = NormalizePaymentType(line.PaymentType),
                    BankAccountId = line.BankAccountId,
                    CashAccountId = line.CashAccountId,
                    CheckBookId = line.CheckBookId,
                    ScheduledDate = line.ScheduledDate?.Date ?? batch.ScheduledDate ?? batch.BatchDate,
                    Reference = (line.Reference ?? string.Empty).Trim(),
                    Notes = (line.Notes ?? string.Empty).Trim(),
                    LineStatus = "pending",
                    CreatedBy = "web-api"
                });
                total += line.AmountToPay;
            }

            if (batch.Lines.Count == 0)
                return Results.BadRequest(new { message = "Ninguna línea válida fue capturada." });

            batch.LineCount = batch.Lines.Count;
            batch.CompanyCount = companies.Count;
            batch.SupplierCount = suppliers.Count;
            batch.TotalAmount = total;

            db.PaymentBatches.Add(batch);
            db.PaymentBatchAudits.Add(new PaymentBatchAudit
            {
                TenantId = tenant.Id,
                PaymentBatchId = batch.Id,
                Action = "created",
                UserId = request.RequestedByUserId,
                UserName = batch.RequestedByName,
                NewValue = $"{batch.Folio} - {batch.LineCount} pagos - {batch.TotalAmount:N2}",
                CreatedBy = "web-api"
            });
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = batch.Id, folio = batch.Folio });
        });

        // Autorizar (total o parcial). Permite ajustar montos antes de autorizar
        group.MapPost("/{id:guid}/authorize", async (Guid id, PaymentBatchAuthorizeRequestDto request, NanchesoftDbContext db) =>
        {
            var batch = await db.PaymentBatches.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (batch is null) return Results.NotFound(new { message = "Lote no encontrado." });
            if (batch.Status is "executed" or "cancelled")
                return Results.BadRequest(new { message = "El lote ya está cerrado." });

            // Aplicar overrides por línea
            if (request.LineOverrides is not null)
            {
                foreach (var ov in request.LineOverrides)
                {
                    var line = batch.Lines.FirstOrDefault(x => x.Id == ov.LineId);
                    if (line is null) continue;
                    if (ov.AmountToPay.HasValue) line.AmountToPay = ov.AmountToPay.Value;
                    if (ov.BankAccountId.HasValue) line.BankAccountId = ov.BankAccountId.Value;
                    if (ov.CashAccountId.HasValue) line.CashAccountId = ov.CashAccountId.Value;
                    if (!string.IsNullOrWhiteSpace(ov.PaymentType)) line.PaymentType = NormalizePaymentType(ov.PaymentType);
                    if (!string.IsNullOrWhiteSpace(ov.Priority)) line.Priority = NormalizePriority(ov.Priority);
                    if (ov.ScheduledDate.HasValue) line.ScheduledDate = ov.ScheduledDate.Value.Date;
                    if (ov.Reject)
                    {
                        line.LineStatus = "rejected";
                        line.RejectedReason = ov.RejectedReason ?? string.Empty;
                    }
                    else
                    {
                        line.LineStatus = "authorized";
                    }
                    line.UpdatedAt = DateTime.UtcNow;
                    line.UpdatedBy = "web-api";
                }
            }
            else
            {
                foreach (var line in batch.Lines.Where(x => x.LineStatus == "pending"))
                {
                    line.LineStatus = "authorized";
                }
            }

            var authorizedLines = batch.Lines.Where(x => x.LineStatus == "authorized").ToList();
            batch.AuthorizedAmount = authorizedLines.Sum(x => x.AmountToPay);
            batch.AuthorizedByUserId = request.AuthorizedByUserId;
            batch.AuthorizedByName = (request.AuthorizedByName ?? string.Empty).Trim();
            batch.AuthorizedAt = DateTime.UtcNow;
            batch.Status = authorizedLines.Count == batch.Lines.Count ? "authorized" :
                (authorizedLines.Count == 0 ? "rejected" : "in_review");
            batch.UpdatedAt = DateTime.UtcNow;
            batch.UpdatedBy = "web-api";

            db.PaymentBatchAudits.Add(new PaymentBatchAudit
            {
                TenantId = batch.TenantId,
                PaymentBatchId = batch.Id,
                Action = batch.Status,
                UserId = request.AuthorizedByUserId,
                UserName = batch.AuthorizedByName,
                NewValue = $"Autorizadas {authorizedLines.Count}/{batch.Lines.Count} por {batch.AuthorizedAmount:N2}",
                Notes = request.Notes ?? string.Empty,
                CreatedBy = "web-api"
            });
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, batch.Status, batch.AuthorizedAmount });
        });

        // Rechazar lote completo
        group.MapPost("/{id:guid}/reject", async (Guid id, PaymentBatchRejectRequestDto request, NanchesoftDbContext db) =>
        {
            var batch = await db.PaymentBatches.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (batch is null) return Results.NotFound(new { message = "Lote no encontrado." });
            if (batch.Status is "executed" or "cancelled")
                return Results.BadRequest(new { message = "El lote ya está cerrado." });
            batch.Status = "rejected";
            batch.RejectedAt = DateTime.UtcNow;
            batch.RejectedByUserId = request.UserId;
            batch.RejectedByName = (request.UserName ?? string.Empty).Trim();
            batch.RejectedReason = (request.Reason ?? string.Empty).Trim();
            foreach (var line in batch.Lines.Where(x => x.LineStatus == "pending"))
            {
                line.LineStatus = "rejected";
                line.RejectedReason = batch.RejectedReason;
            }
            db.PaymentBatchAudits.Add(new PaymentBatchAudit
            {
                TenantId = batch.TenantId,
                PaymentBatchId = batch.Id,
                Action = "rejected",
                UserId = request.UserId,
                UserName = batch.RejectedByName,
                NewValue = batch.RejectedReason,
                CreatedBy = "web-api"
            });
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // Cancelar lote (sólo en borrador / pendiente)
        group.MapPost("/{id:guid}/cancel", async (Guid id, NanchesoftDbContext db) =>
        {
            var batch = await db.PaymentBatches.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (batch is null) return Results.NotFound(new { message = "Lote no encontrado." });
            if (batch.Status is "executed")
                return Results.BadRequest(new { message = "El lote ya fue ejecutado." });
            batch.Status = "cancelled";
            foreach (var line in batch.Lines)
            {
                if (line.LineStatus != "executed") line.LineStatus = "cancelled";
            }
            db.PaymentBatchAudits.Add(new PaymentBatchAudit
            {
                TenantId = batch.TenantId,
                PaymentBatchId = batch.Id,
                Action = "cancelled",
                CreatedBy = "web-api"
            });
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // Ejecutar lote: genera Payment + aplicaciones + cheque / movimiento bancario
        group.MapPost("/{id:guid}/execute", async (Guid id, PaymentBatchExecuteRequestDto request, NanchesoftDbContext db) =>
        {
            var batch = await db.PaymentBatches.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (batch is null) return Results.NotFound(new { message = "Lote no encontrado." });
            if (batch.Status is "executed" or "cancelled" or "rejected")
                return Results.BadRequest(new { message = "El lote ya está cerrado." });
            if (batch.Status != "authorized" && batch.Status != "in_review")
                return Results.BadRequest(new { message = "El lote debe estar autorizado antes de ejecutarse." });

            decimal executedAmount = 0m;
            int executedCount = 0;

            foreach (var line in batch.Lines.Where(x => x.LineStatus == "authorized"))
            {
                var invoice = line.PurchaseInvoiceId.HasValue
                    ? await db.PurchaseInvoices.FirstOrDefaultAsync(x => x.Id == line.PurchaseInvoiceId.Value)
                    : null;
                if (invoice is null)
                {
                    line.LineStatus = "rejected";
                    line.RejectedReason = "Factura no encontrada";
                    continue;
                }

                BankAccount? bankAccount = null;
                if (line.BankAccountId.HasValue)
                    bankAccount = await db.BankAccounts.FirstOrDefaultAsync(x => x.Id == line.BankAccountId.Value);

                CashAccount? cashAccount = null;
                if (line.CashAccountId.HasValue)
                    cashAccount = await db.CashAccounts.FirstOrDefaultAsync(x => x.Id == line.CashAccountId.Value);

                if (bankAccount is null && cashAccount is null)
                {
                    line.LineStatus = "rejected";
                    line.RejectedReason = "Sin cuenta bancaria/caja asignada";
                    continue;
                }

                if (line.AmountToPay <= 0m)
                {
                    line.LineStatus = "rejected";
                    line.RejectedReason = "Importe inválido";
                    continue;
                }

                if (bankAccount is not null && bankAccount.CurrentBalance < line.AmountToPay)
                {
                    line.LineStatus = "rejected";
                    line.RejectedReason = "Saldo bancario insuficiente";
                    continue;
                }
                if (cashAccount is not null && cashAccount.CurrentBalance < line.AmountToPay)
                {
                    line.LineStatus = "rejected";
                    line.RejectedReason = "Saldo de caja insuficiente";
                    continue;
                }

                var folioPrefix = FolioPrefixFor(line.PaymentType);
                var folio = await GenerateExecutionFolioAsync(db, batch.TenantId, folioPrefix);

                var payment = new Payment
                {
                    TenantId = batch.TenantId,
                    CompanyId = invoice.CompanyId,
                    BranchId = invoice.BranchId,
                    SupplierId = invoice.SupplierId,
                    CurrencyId = invoice.CurrencyId,
                    BankAccountId = bankAccount?.Id,
                    CashAccountId = cashAccount?.Id,
                    Folio = folio,
                    PaymentDate = line.ScheduledDate ?? batch.BatchDate,
                    SourceType = bankAccount is not null ? "bank" : "cash",
                    Status = "posted",
                    Reference = string.IsNullOrWhiteSpace(line.Reference) ? line.PaymentType.ToUpperInvariant() : line.Reference,
                    Total = line.AmountToPay,
                    PostedAt = DateTime.UtcNow,
                    CreatedBy = "web-api"
                };
                payment.Lines.Add(new PaymentLine
                {
                    PaymentId = payment.Id,
                    LineNumber = 1,
                    Description = $"Pago factura {invoice.Folio} - lote {batch.Folio}",
                    Amount = line.AmountToPay,
                    PurchaseInvoiceId = invoice.Id,
                    CreatedBy = "web-api"
                });
                db.Payments.Add(payment);

                if (bankAccount is not null)
                {
                    bankAccount.CurrentBalance -= line.AmountToPay;
                    bankAccount.UpdatedAt = DateTime.UtcNow;
                    bankAccount.UpdatedBy = "web-api";

                    var movement = new BankMovement
                    {
                        TenantId = bankAccount.TenantId,
                        CompanyId = bankAccount.CompanyId,
                        BankAccountId = bankAccount.Id,
                        MovementDate = payment.PaymentDate,
                        MovementType = line.PaymentType == "check" ? "withdrawal" : "withdrawal",
                        DocumentType = line.PaymentType,
                        DocumentId = payment.Id,
                        Reference = $"{folio} · {invoice.Folio}",
                        AmountIn = 0m,
                        AmountOut = line.AmountToPay,
                        BalanceAfter = bankAccount.CurrentBalance,
                        CreatedBy = "web-api"
                    };
                    db.BankMovements.Add(movement);
                    line.BankMovementId = movement.Id;

                    if (line.PaymentType == "check")
                    {
                        var check = new Check
                        {
                            TenantId = bankAccount.TenantId,
                            CompanyId = bankAccount.CompanyId,
                            BankAccountId = bankAccount.Id,
                            CheckBookId = line.CheckBookId,
                            SupplierId = invoice.SupplierId,
                            Folio = folio,
                            IssueDate = payment.PaymentDate,
                            PostingDate = payment.PaymentDate,
                            BeneficiaryType = "supplier",
                            BeneficiaryName = line.SupplierName,
                            Amount = line.AmountToPay,
                            Concept = $"Pago factura {invoice.Folio}",
                            Reference = batch.Folio,
                            Status = "issued",
                            BankMovementId = movement.Id,
                            CreatedBy = "web-api"
                        };
                        db.Checks.Add(check);
                        line.CheckId = check.Id;
                    }
                }
                else if (cashAccount is not null)
                {
                    cashAccount.CurrentBalance -= line.AmountToPay;
                    cashAccount.UpdatedAt = DateTime.UtcNow;
                    cashAccount.UpdatedBy = "web-api";

                    var movement = new CashMovement
                    {
                        TenantId = cashAccount.TenantId,
                        CompanyId = cashAccount.CompanyId,
                        BranchId = cashAccount.BranchId,
                        CashAccountId = cashAccount.Id,
                        MovementDate = payment.PaymentDate,
                        MovementType = "withdrawal",
                        DocumentType = "payment",
                        DocumentId = payment.Id,
                        Reference = $"{folio} · {invoice.Folio}",
                        AmountIn = 0m,
                        AmountOut = line.AmountToPay,
                        BalanceAfter = cashAccount.CurrentBalance,
                        CreatedBy = "web-api"
                    };
                    db.CashMovements.Add(movement);
                }

                line.LineStatus = "executed";
                line.PaymentId = payment.Id;
                line.ExecutedFolio = folio;
                line.ExecutedAt = DateTime.UtcNow;
                line.UpdatedAt = DateTime.UtcNow;
                line.UpdatedBy = "web-api";
                executedAmount += line.AmountToPay;
                executedCount++;
            }

            batch.ExecutedAmount = executedAmount;
            batch.ExecutedAt = DateTime.UtcNow;
            batch.ExecutedByUserId = request.UserId;
            batch.ExecutedByName = (request.UserName ?? string.Empty).Trim();
            var pendingLines = batch.Lines.Count(x => x.LineStatus == "authorized" || x.LineStatus == "pending");
            batch.Status = pendingLines == 0
                ? (executedCount == batch.Lines.Count ? "executed" : "partially_executed")
                : "partially_executed";
            batch.UpdatedAt = DateTime.UtcNow;
            batch.UpdatedBy = "web-api";

            db.PaymentBatchAudits.Add(new PaymentBatchAudit
            {
                TenantId = batch.TenantId,
                PaymentBatchId = batch.Id,
                Action = "executed",
                UserId = request.UserId,
                UserName = batch.ExecutedByName,
                NewValue = $"Ejecutados {executedCount}/{batch.Lines.Count} por {batch.ExecutedAmount:N2}",
                CreatedBy = "web-api"
            });
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, batch.Status, batch.ExecutedAmount, executedCount });
        });

        // Auditoría del lote
        group.MapGet("/{id:guid}/audit", async (Guid id, NanchesoftDbContext db) =>
        {
            var rows = await db.PaymentBatchAudits.AsNoTracking()
                .Where(x => x.PaymentBatchId == id)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new PaymentBatchAuditRowDto
                {
                    Id = x.Id,
                    Action = x.Action,
                    UserName = x.UserName,
                    Timestamp = x.CreatedAt,
                    PreviousValue = x.PreviousValue,
                    NewValue = x.NewValue,
                    Notes = x.Notes
                }).ToListAsync();
            return Results.Ok(rows);
        });
    }

    // =================================================================
    // Pantalla ejecutiva
    // =================================================================
    private static void MapExecutiveEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payment-central/executive").WithTags("PaymentCentralExecutive");

        group.MapGet("/", async (NanchesoftDbContext db) =>
        {
            var pending = await db.PaymentBatches.AsNoTracking()
                .Where(x => x.Status == "pending" || x.Status == "in_review")
                .ToListAsync();
            var authorized = await db.PaymentBatches.AsNoTracking()
                .Where(x => x.Status == "authorized")
                .ToListAsync();
            var executed = await db.PaymentBatches.AsNoTracking()
                .Where(x => x.Status == "executed" || x.Status == "partially_executed")
                .Where(x => x.ExecutedAt >= DateTime.UtcNow.Date.AddDays(-30))
                .ToListAsync();

            var bankBalances = await (from acc in db.BankAccounts.AsNoTracking()
                                      join bank in db.Banks.AsNoTracking() on acc.BankId equals bank.Id into bj
                                      from bank in bj.DefaultIfEmpty()
                                      join company in db.Companies.AsNoTracking() on acc.CompanyId equals company.Id into cj
                                      from company in cj.DefaultIfEmpty()
                                      where acc.IsActive
                                      orderby acc.CurrentBalance
                                      select new BankAccountBalanceDto
                                      {
                                          BankAccountId = acc.Id,
                                          CompanyName = company != null ? company.Name : string.Empty,
                                          BankName = bank != null ? bank.Name : "—",
                                          AccountName = acc.Name,
                                          AccountNumber = acc.AccountNumber,
                                          Balance = acc.CurrentBalance,
                                          ReconciledBalance = acc.ReconciledBalance
                                      }).Take(10).ToListAsync();

            // Próximos vencimientos (facturas pendientes con DueDate <= today+14)
            var paidMap = await (from line in db.PaymentLines.AsNoTracking()
                                 join payment in db.Payments.AsNoTracking() on line.PaymentId equals payment.Id
                                 where line.IsActive && line.PurchaseInvoiceId != null
                                       && payment.IsActive && payment.Status != "cancelled"
                                 group line.Amount by line.PurchaseInvoiceId!.Value into g
                                 select new { InvoiceId = g.Key, Applied = g.Sum() })
                .ToDictionaryAsync(x => x.InvoiceId, x => x.Applied);

            var upcomingRaw = await (from invoice in db.PurchaseInvoices.AsNoTracking()
                                     join supplier in db.Suppliers.AsNoTracking() on invoice.SupplierId equals supplier.Id into sj
                                     from supplier in sj.DefaultIfEmpty()
                                     where invoice.IsActive
                                           && invoice.Status != "cancelled"
                                           && invoice.Status != "draft"
                                     select new
                                     {
                                         invoice.Id,
                                         invoice.InvoiceDate,
                                         PaymentTermDays = supplier != null ? supplier.PaymentTermDays : 0,
                                         SupplierName = supplier != null ? supplier.Name : "—",
                                         invoice.Total,
                                         invoice.Folio
                                     }).ToListAsync();

            var today = DateTime.UtcNow.Date;
            var upcoming = upcomingRaw
                .Select(x => new
                {
                    x.Id,
                    x.InvoiceDate,
                    DueDate = x.InvoiceDate.AddDays(x.PaymentTermDays),
                    x.SupplierName,
                    x.Total,
                    Paid = paidMap.TryGetValue(x.Id, out var p) ? p : 0m,
                    x.Folio
                })
                .Where(x => x.Total - x.Paid > 0.01m && x.DueDate <= today.AddDays(14))
                .OrderBy(x => x.DueDate)
                .Take(20)
                .Select(x => new UpcomingDueDto
                {
                    InvoiceFolio = x.Folio,
                    SupplierName = x.SupplierName,
                    DueDate = x.DueDate,
                    DaysToDue = (int)(x.DueDate - today).TotalDays,
                    PendingAmount = x.Total - x.Paid
                }).ToList();

            var totalAvailable = bankBalances.Sum(x => x.Balance);
            var totalCommitted = pending.Sum(x => x.TotalAmount) + authorized.Sum(x => x.AuthorizedAmount);

            return Results.Ok(new PaymentCentralExecutiveDto
            {
                PendingBatches = pending.Count,
                PendingAmount = pending.Sum(x => x.TotalAmount),
                AuthorizedBatches = authorized.Count,
                AuthorizedAmount = authorized.Sum(x => x.AuthorizedAmount),
                ExecutedBatches30d = executed.Count,
                ExecutedAmount30d = executed.Sum(x => x.ExecutedAmount),
                CommittedFlow = totalCommitted,
                AvailableFlow = totalAvailable,
                RiskRatio = totalAvailable > 0m ? Math.Round(totalCommitted / totalAvailable, 2) : 0m,
                LowBalanceAccounts = bankBalances,
                UpcomingDue = upcoming
            });
        });
    }

    // =================================================================
    // Helpers
    // =================================================================
    private static async Task<string> GenerateBatchFolioAsync(NanchesoftDbContext db, Guid tenantId)
    {
        var prefix = "LOTE-";
        var year = DateTime.UtcNow.Year;
        var count = await db.PaymentBatches.Where(x => x.TenantId == tenantId && x.BatchDate.Year == year).CountAsync();
        return $"{prefix}{year}-{(count + 1):D5}";
    }

    private static async Task<string> GenerateExecutionFolioAsync(NanchesoftDbContext db, Guid tenantId, string prefix)
    {
        var count = await db.PaymentBatchLines.Where(x => x.TenantId == tenantId && x.ExecutedFolio.StartsWith(prefix + "-")).CountAsync();
        return $"{prefix}-{(count + 1):D6}";
    }

    private static string NormalizePriority(string? value) => (value ?? "normal").Trim().ToLowerInvariant() switch
    {
        "critical" or "high" or "low" or "normal" => (value ?? "normal").Trim().ToLowerInvariant(),
        _ => "normal"
    };

    private static string NormalizePaymentType(string? value) => (value ?? "transfer").Trim().ToLowerInvariant() switch
    {
        "transfer" or "check" or "spei" or "deposit" or "cash" or "card" or "offset" or "other" => (value ?? "transfer").Trim().ToLowerInvariant(),
        _ => "transfer"
    };

    private static string FolioPrefixFor(string paymentType) => paymentType switch
    {
        "transfer" => "TR",
        "check" => "CH",
        "spei" => "SPEI",
        "deposit" => "DEP",
        "cash" => "EF",
        "card" => "TJ",
        "offset" => "COMP",
        _ => "OTR"
    };

    private static string PriorityFromDays(int days) =>
        days > 30 ? "critical" :
        days > 7 ? "high" :
        days >= 0 ? "normal" : "low";

    private static PaymentBatchDetailDto MapBatchDetail(PaymentBatch entity) => new()
    {
        Id = entity.Id,
        Folio = entity.Folio,
        BatchDate = entity.BatchDate,
        ScheduledDate = entity.ScheduledDate,
        Status = entity.Status,
        Priority = entity.Priority,
        LineCount = entity.LineCount,
        CompanyCount = entity.CompanyCount,
        SupplierCount = entity.SupplierCount,
        TotalAmount = entity.TotalAmount,
        AuthorizedAmount = entity.AuthorizedAmount,
        ExecutedAmount = entity.ExecutedAmount,
        Notes = entity.Notes,
        RequestedByName = entity.RequestedByName,
        AuthorizedByName = entity.AuthorizedByName,
        AuthorizedAt = entity.AuthorizedAt,
        RejectedReason = entity.RejectedReason,
        RejectedAt = entity.RejectedAt,
        ExecutedAt = entity.ExecutedAt,
        Lines = entity.Lines.OrderBy(x => x.CreatedAt).Select(line => new PaymentBatchLineDto
        {
            Id = line.Id,
            CompanyId = line.CompanyId,
            SupplierId = line.SupplierId,
            SupplierName = line.SupplierName,
            PurchaseInvoiceId = line.PurchaseInvoiceId,
            InvoiceFolio = line.InvoiceFolio,
            InvoiceDate = line.InvoiceDate,
            DueDate = line.DueDate,
            DaysOverdue = line.DaysOverdue,
            CurrencyId = line.CurrencyId,
            CurrencyCode = line.CurrencyCode,
            ExchangeRate = line.ExchangeRate,
            OriginalAmount = line.OriginalAmount,
            AmountDue = line.AmountDue,
            AmountToPay = line.AmountToPay,
            Priority = line.Priority,
            PaymentType = line.PaymentType,
            BankAccountId = line.BankAccountId,
            CashAccountId = line.CashAccountId,
            CheckBookId = line.CheckBookId,
            ScheduledDate = line.ScheduledDate,
            Reference = line.Reference,
            Notes = line.Notes,
            LineStatus = line.LineStatus,
            PaymentId = line.PaymentId,
            CheckId = line.CheckId,
            BankMovementId = line.BankMovementId,
            ExecutedFolio = line.ExecutedFolio,
            ExecutedAt = line.ExecutedAt,
            RejectedReason = line.RejectedReason
        }).ToList()
    };
}

// ============================== DTOs ==============================
public sealed class IdNameDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class BankAccountLookupDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? BankId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public Guid? CurrencyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
}

public sealed class PaymentTypeOptionDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FolioPrefix { get; set; } = string.Empty;
}

public sealed class PaymentCentralLookupsDto
{
    public List<IdNameDto> Companies { get; set; } = new();
    public List<BankAccountLookupDto> BankAccounts { get; set; } = new();
    public List<BankAccountLookupDto> CashAccounts { get; set; } = new();
    public List<IdNameDto> Suppliers { get; set; } = new();
    public List<IdNameDto> Currencies { get; set; } = new();
    public List<PaymentTypeOptionDto> PaymentTypes { get; set; } = new();
}

public sealed class PendingPaymentRowDto
{
    public Guid PurchaseInvoiceId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public string SupplierInvoiceFolio { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public int PaymentTermDays { get; set; }
    public Guid? CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal AvailableToPay { get; set; }
    public string Priority { get; set; } = "normal";
}

public sealed class PaymentBatchListRowDto
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime BatchDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public int CompanyCount { get; set; }
    public int SupplierCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AuthorizedAmount { get; set; }
    public decimal ExecutedAmount { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public string AuthorizedByName { get; set; } = string.Empty;
    public DateTime? AuthorizedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
}

public sealed class PaymentBatchDetailDto
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime BatchDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public int CompanyCount { get; set; }
    public int SupplierCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AuthorizedAmount { get; set; }
    public decimal ExecutedAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string RequestedByName { get; set; } = string.Empty;
    public string AuthorizedByName { get; set; } = string.Empty;
    public DateTime? AuthorizedAt { get; set; }
    public string RejectedReason { get; set; } = string.Empty;
    public DateTime? RejectedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public List<PaymentBatchLineDto> Lines { get; set; } = new();
}

public sealed class PaymentBatchLineDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public Guid? PurchaseInvoiceId { get; set; }
    public string InvoiceFolio { get; set; } = string.Empty;
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public Guid? CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountToPay { get; set; }
    public string Priority { get; set; } = "normal";
    public string PaymentType { get; set; } = "transfer";
    public Guid? BankAccountId { get; set; }
    public Guid? CashAccountId { get; set; }
    public Guid? CheckBookId { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string LineStatus { get; set; } = "pending";
    public Guid? PaymentId { get; set; }
    public Guid? CheckId { get; set; }
    public Guid? BankMovementId { get; set; }
    public string ExecutedFolio { get; set; } = string.Empty;
    public DateTime? ExecutedAt { get; set; }
    public string RejectedReason { get; set; } = string.Empty;
}

public sealed class PaymentBatchSaveRequestDto
{
    public DateTime? BatchDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string Priority { get; set; } = "normal";
    public string? Notes { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public string? RequestedByName { get; set; }
    public List<PaymentBatchLineSaveDto> Lines { get; set; } = new();
}

public sealed class PaymentBatchLineSaveDto
{
    public Guid PurchaseInvoiceId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime? DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountToPay { get; set; }
    public string Priority { get; set; } = "normal";
    public string PaymentType { get; set; } = "transfer";
    public Guid? BankAccountId { get; set; }
    public Guid? CashAccountId { get; set; }
    public Guid? CheckBookId { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public sealed class PaymentBatchAuthorizeRequestDto
{
    public Guid? AuthorizedByUserId { get; set; }
    public string? AuthorizedByName { get; set; }
    public string? Notes { get; set; }
    public List<PaymentBatchLineOverrideDto>? LineOverrides { get; set; }
}

public sealed class PaymentBatchLineOverrideDto
{
    public Guid LineId { get; set; }
    public decimal? AmountToPay { get; set; }
    public Guid? BankAccountId { get; set; }
    public Guid? CashAccountId { get; set; }
    public string? PaymentType { get; set; }
    public string? Priority { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public bool Reject { get; set; }
    public string? RejectedReason { get; set; }
}

public sealed class PaymentBatchRejectRequestDto
{
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Reason { get; set; }
}

public sealed class PaymentBatchExecuteRequestDto
{
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
}

public sealed class PaymentBatchAuditRowDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string PreviousValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class BankAccountBalanceDto
{
    public Guid BankAccountId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal ReconciledBalance { get; set; }
}

public sealed class UpcomingDueDto
{
    public string InvoiceFolio { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int DaysToDue { get; set; }
    public decimal PendingAmount { get; set; }
}

public sealed class PaymentCentralExecutiveDto
{
    public int PendingBatches { get; set; }
    public decimal PendingAmount { get; set; }
    public int AuthorizedBatches { get; set; }
    public decimal AuthorizedAmount { get; set; }
    public int ExecutedBatches30d { get; set; }
    public decimal ExecutedAmount30d { get; set; }
    public decimal CommittedFlow { get; set; }
    public decimal AvailableFlow { get; set; }
    public decimal RiskRatio { get; set; }
    public List<BankAccountBalanceDto> LowBalanceAccounts { get; set; } = new();
    public List<UpcomingDueDto> UpcomingDue { get; set; } = new();
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PurchaseEndpoints
{
    public static IEndpointRouteBuilder MapPurchaseEndpoints(this IEndpointRouteBuilder app)
    {
        var requisitions = app.MapGroup("/api/purchases/requisitions").WithTags("PurchaseRequisitions");
        requisitions.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.PurchaseRequisitions
                .AsNoTracking()
                .OrderByDescending(x => x.RequisitionDate)
                .Select(x => new
                {
                    PurchaseRequisitionId = x.Id,
                    x.CompanyId,
                    x.BranchId,
                    x.Folio,
                    x.RequisitionDate,
                    x.RequestedByName,
                    x.Status,
                    x.Notes,
                    x.ApprovedAt,
                    x.IsActive
                })
                .ToListAsync()));

        requisitions.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.PurchaseRequisitions
                .AsNoTracking()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);

            return entity is null
                ? Results.NotFound(new { message = "No se encontró la requisición." })
                : Results.Ok(new PurchaseRequisitionRequest
                {
                    PurchaseRequisitionId = entity.Id,
                    CompanyId = entity.CompanyId,
                    BranchId = entity.BranchId,
                    Folio = entity.Folio,
                    RequisitionDate = entity.RequisitionDate,
                    RequestedByName = entity.RequestedByName,
                    Status = entity.Status,
                    Notes = entity.Notes,
                    ApprovedAt = entity.ApprovedAt,
                    IsActive = entity.IsActive,
                    Lines = entity.Lines
                        .OrderBy(x => x.LineNumber)
                        .Select(x => new PurchaseLineRequest
                        {
                            Id = x.Id,
                            LineNumber = x.LineNumber,
                            ItemId = x.ItemId,
                            UnitId = x.UnitId,
                            Description = x.Description,
                            Quantity = x.Quantity,
                            Notes = x.Notes
                        })
                        .ToList()
                });
        });

        requisitions.MapPost("/", async (PurchaseRequisitionRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetDefaultCompanyAsync(db, request.CompanyId);
            var branch = await GetDefaultBranchAsync(db, company.Id, request.BranchId);

            var entity = new Nanchesoft.Domain.Entities.PurchaseRequisition
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                Folio = NormalizeFolio(request.Folio, "REQ"),
                RequisitionDate = request.RequisitionDate?.Date ?? DateTime.UtcNow.Date,
                RequestedByName = (request.RequestedByName ?? string.Empty).Trim(),
                Status = NormalizeStatus(request.Status),
                Notes = (request.Notes ?? string.Empty).Trim(),
                ApprovedAt = request.ApprovedAt,
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };

            ApplyRequisitionLines(entity, request.Lines);

            db.PurchaseRequisitions.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        requisitions.MapPut("/{id:guid}", async (Guid id, PurchaseRequisitionRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.PurchaseRequisitions.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
                return Results.NotFound(new { message = "No se encontró la requisición." });

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BranchId = request.BranchId ?? entity.BranchId;
            entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
            entity.RequisitionDate = request.RequisitionDate?.Date ?? entity.RequisitionDate;
            entity.RequestedByName = string.IsNullOrWhiteSpace(request.RequestedByName) ? entity.RequestedByName : request.RequestedByName.Trim();
            entity.Status = NormalizeStatus(request.Status, entity.Status);
            entity.Notes = request.Notes?.Trim() ?? entity.Notes;
            entity.ApprovedAt = request.ApprovedAt;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";

            ApplyRequisitionLines(entity, request.Lines);

            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        requisitions.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.PurchaseRequisitions.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la requisición." });
            db.PurchaseRequisitions.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        var orders = app.MapGroup("/api/purchases/orders").WithTags("PurchaseOrders");
        orders.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.PurchaseOrders
                .AsNoTracking()
                .OrderByDescending(x => x.OrderDate)
                .Select(x => new
                {
                    PurchaseOrderId = x.Id,
                    x.CompanyId,
                    x.BranchId,
                    x.SupplierId,
                    x.CurrencyId,
                    x.PurchaseRequisitionId,
                    x.Folio,
                    x.OrderDate,
                    x.Status,
                    x.PaymentTermDays,
                    x.ExchangeRate,
                    x.Subtotal,
                    x.TaxAmount,
                    x.Total,
                    x.Notes,
                    x.ApprovedAt,
                    x.ClosedAt,
                    x.IsActive
                })
                .ToListAsync()));

        orders.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.PurchaseOrders
                .AsNoTracking()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);

            return entity is null
                ? Results.NotFound(new { message = "No se encontró la orden." })
                : Results.Ok(new PurchaseOrderRequest
                {
                    PurchaseOrderId = entity.Id,
                    CompanyId = entity.CompanyId,
                    BranchId = entity.BranchId,
                    SupplierId = entity.SupplierId,
                    CurrencyId = entity.CurrencyId,
                    PurchaseRequisitionId = entity.PurchaseRequisitionId,
                    Folio = entity.Folio,
                    OrderDate = entity.OrderDate,
                    Status = entity.Status,
                    PaymentTermDays = entity.PaymentTermDays,
                    ExchangeRate = entity.ExchangeRate,
                    Subtotal = entity.Subtotal,
                    TaxAmount = entity.TaxAmount,
                    Total = entity.Total,
                    Notes = entity.Notes,
                    ApprovedAt = entity.ApprovedAt,
                    ClosedAt = entity.ClosedAt,
                    IsActive = entity.IsActive,
                    Lines = entity.Lines
                        .OrderBy(x => x.LineNumber)
                        .Select(x => new PurchaseLineRequest
                        {
                            Id = x.Id,
                            LineNumber = x.LineNumber,
                            ItemId = x.ItemId,
                            UnitId = x.UnitId,
                            TaxId = x.TaxId,
                            Description = x.Description,
                            Quantity = x.Quantity,
                            ReceivedQuantity = x.ReceivedQuantity,
                            PendingQuantity = x.PendingQuantity,
                            UnitPrice = x.UnitPrice,
                            DiscountAmount = x.DiscountAmount,
                            TaxAmount = x.TaxAmount,
                            LineTotal = x.LineTotal
                        })
                        .ToList()
                });
        });

        orders.MapPost("/", async (PurchaseOrderRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetDefaultCompanyAsync(db, request.CompanyId);
            var branch = await GetDefaultBranchAsync(db, company.Id, request.BranchId);
            var supplierId = request.SupplierId ?? await db.Suppliers.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
            var currencyId = request.CurrencyId ?? await db.Currencies.OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

            var entity = new Nanchesoft.Domain.Entities.PurchaseOrder
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                SupplierId = supplierId,
                CurrencyId = currencyId,
                PurchaseRequisitionId = request.PurchaseRequisitionId,
                Folio = NormalizeFolio(request.Folio, "OC"),
                OrderDate = request.OrderDate?.Date ?? DateTime.UtcNow.Date,
                Status = NormalizeStatus(request.Status),
                PaymentTermDays = request.PaymentTermDays,
                ExchangeRate = request.ExchangeRate <= 0 ? 1m : request.ExchangeRate,
                Notes = (request.Notes ?? string.Empty).Trim(),
                ApprovedAt = request.ApprovedAt,
                ClosedAt = request.ClosedAt,
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };

            ApplyOrderLines(entity, request.Lines);
            RecalculateOrderTotals(entity);

            db.PurchaseOrders.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        orders.MapPut("/{id:guid}", async (Guid id, PurchaseOrderRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.PurchaseOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
                return Results.NotFound(new { message = "No se encontró la orden." });

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BranchId = request.BranchId ?? entity.BranchId;
            entity.SupplierId = request.SupplierId ?? entity.SupplierId;
            entity.CurrencyId = request.CurrencyId ?? entity.CurrencyId;
            entity.PurchaseRequisitionId = request.PurchaseRequisitionId;
            entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
            entity.OrderDate = request.OrderDate?.Date ?? entity.OrderDate;
            entity.Status = NormalizeStatus(request.Status, entity.Status);
            entity.PaymentTermDays = request.PaymentTermDays;
            entity.ExchangeRate = request.ExchangeRate <= 0 ? entity.ExchangeRate : request.ExchangeRate;
            entity.Notes = request.Notes?.Trim() ?? entity.Notes;
            entity.ApprovedAt = request.ApprovedAt;
            entity.ClosedAt = request.ClosedAt;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";

            ApplyOrderLines(entity, request.Lines);
            RecalculateOrderTotals(entity);

            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        orders.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la orden." });
            db.PurchaseOrders.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        var receipts = app.MapGroup("/api/purchases/receipts").WithTags("PurchaseReceipts");
        receipts.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.PurchaseReceipts
                .AsNoTracking()
                .OrderByDescending(x => x.ReceiptDate)
                .Select(x => new
                {
                    PurchaseReceiptId = x.Id,
                    x.CompanyId,
                    x.BranchId,
                    x.WarehouseId,
                    x.SupplierId,
                    x.PurchaseOrderId,
                    x.Folio,
                    x.ReceiptDate,
                    x.Status,
                    x.Notes,
                    x.PostedAt,
                    x.IsActive
                })
                .ToListAsync()));

        receipts.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.PurchaseReceipts
                .AsNoTracking()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);

            return entity is null
                ? Results.NotFound(new { message = "No se encontró la recepción." })
                : Results.Ok(new PurchaseReceiptRequest
                {
                    PurchaseReceiptId = entity.Id,
                    CompanyId = entity.CompanyId,
                    BranchId = entity.BranchId,
                    WarehouseId = entity.WarehouseId,
                    SupplierId = entity.SupplierId,
                    PurchaseOrderId = entity.PurchaseOrderId,
                    Folio = entity.Folio,
                    ReceiptDate = entity.ReceiptDate,
                    Status = entity.Status,
                    Notes = entity.Notes,
                    PostedAt = entity.PostedAt,
                    IsActive = entity.IsActive,
                    Lines = entity.Lines
                        .OrderBy(x => x.LineNumber)
                        .Select(x => new PurchaseLineRequest
                        {
                            Id = x.Id,
                            LineNumber = x.LineNumber,
                            PurchaseOrderLineId = x.PurchaseOrderLineId,
                            ItemId = x.ItemId,
                            UnitId = x.UnitId,
                            Description = x.Description,
                            Quantity = x.Quantity
                        })
                        .ToList()
                });
        });

        receipts.MapPost("/", async (PurchaseReceiptRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetDefaultCompanyAsync(db, request.CompanyId);
            var branch = await GetDefaultBranchAsync(db, company.Id, request.BranchId);
            var warehouseId = request.WarehouseId ?? await db.Warehouses.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
            var supplierId = request.SupplierId ?? await db.Suppliers.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

            var entity = new Nanchesoft.Domain.Entities.PurchaseReceipt
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                WarehouseId = warehouseId,
                SupplierId = supplierId,
                PurchaseOrderId = request.PurchaseOrderId,
                Folio = NormalizeFolio(request.Folio, "REC"),
                ReceiptDate = request.ReceiptDate?.Date ?? DateTime.UtcNow.Date,
                Status = NormalizeStatus(request.Status),
                Notes = (request.Notes ?? string.Empty).Trim(),
                PostedAt = request.PostedAt,
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };

            ApplyReceiptLines(entity, request.Lines);
            db.PurchaseReceipts.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        receipts.MapPut("/{id:guid}", async (Guid id, PurchaseReceiptRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.PurchaseReceipts.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
                return Results.NotFound(new { message = "No se encontró la recepción." });

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BranchId = request.BranchId ?? entity.BranchId;
            entity.WarehouseId = request.WarehouseId ?? entity.WarehouseId;
            entity.SupplierId = request.SupplierId ?? entity.SupplierId;
            entity.PurchaseOrderId = request.PurchaseOrderId;
            entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
            entity.ReceiptDate = request.ReceiptDate?.Date ?? entity.ReceiptDate;
            entity.Status = NormalizeStatus(request.Status, entity.Status);
            entity.Notes = request.Notes?.Trim() ?? entity.Notes;
            entity.PostedAt = request.PostedAt;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";

            ApplyReceiptLines(entity, request.Lines);

            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        receipts.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.PurchaseReceipts.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la recepción." });
            db.PurchaseReceipts.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        var invoices = app.MapGroup("/api/purchases/invoices").WithTags("PurchaseInvoices");
        invoices.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.PurchaseInvoices
                .AsNoTracking()
                .OrderByDescending(x => x.InvoiceDate)
                .Select(x => new
                {
                    PurchaseInvoiceId = x.Id,
                    x.CompanyId,
                    x.BranchId,
                    x.SupplierId,
                    x.PurchaseOrderId,
                    x.PurchaseReceiptId,
                    x.CurrencyId,
                    x.Folio,
                    x.SupplierInvoiceFolio,
                    x.InvoiceDate,
                    x.Status,
                    x.ExchangeRate,
                    x.Subtotal,
                    x.TaxAmount,
                    x.Total,
                    x.Notes,
                    x.ApprovedAt,
                    x.PostedAt,
                    x.IsActive
                })
                .ToListAsync()));

        invoices.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.PurchaseInvoices
                .AsNoTracking()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);

            return entity is null
                ? Results.NotFound(new { message = "No se encontró la factura." })
                : Results.Ok(new PurchaseInvoiceRequest
                {
                    PurchaseInvoiceId = entity.Id,
                    CompanyId = entity.CompanyId,
                    BranchId = entity.BranchId,
                    SupplierId = entity.SupplierId,
                    PurchaseOrderId = entity.PurchaseOrderId,
                    PurchaseReceiptId = entity.PurchaseReceiptId,
                    CurrencyId = entity.CurrencyId,
                    Folio = entity.Folio,
                    SupplierInvoiceFolio = entity.SupplierInvoiceFolio,
                    InvoiceDate = entity.InvoiceDate,
                    Status = entity.Status,
                    ExchangeRate = entity.ExchangeRate,
                    Subtotal = entity.Subtotal,
                    TaxAmount = entity.TaxAmount,
                    Total = entity.Total,
                    Notes = entity.Notes,
                    ApprovedAt = entity.ApprovedAt,
                    PostedAt = entity.PostedAt,
                    IsActive = entity.IsActive,
                    Lines = entity.Lines
                        .OrderBy(x => x.LineNumber)
                        .Select(x => new PurchaseLineRequest
                        {
                            Id = x.Id,
                            LineNumber = x.LineNumber,
                            ItemId = x.ItemId,
                            UnitId = x.UnitId,
                            TaxId = x.TaxId,
                            Description = x.Description,
                            Quantity = x.Quantity,
                            UnitPrice = x.UnitPrice,
                            DiscountAmount = x.DiscountAmount,
                            TaxAmount = x.TaxAmount,
                            LineTotal = x.LineTotal
                        })
                        .ToList()
                });
        });

        invoices.MapPost("/", async (PurchaseInvoiceRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetDefaultCompanyAsync(db, request.CompanyId);
            var branch = await GetDefaultBranchAsync(db, company.Id, request.BranchId);
            var supplierId = request.SupplierId ?? await db.Suppliers.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
            var currencyId = request.CurrencyId ?? await db.Currencies.OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

            var entity = new Nanchesoft.Domain.Entities.PurchaseInvoice
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                SupplierId = supplierId,
                PurchaseOrderId = request.PurchaseOrderId,
                PurchaseReceiptId = request.PurchaseReceiptId,
                CurrencyId = currencyId,
                Folio = NormalizeFolio(request.Folio, "FCP"),
                SupplierInvoiceFolio = (request.SupplierInvoiceFolio ?? string.Empty).Trim(),
                InvoiceDate = request.InvoiceDate?.Date ?? DateTime.UtcNow.Date,
                Status = NormalizeStatus(request.Status),
                ExchangeRate = request.ExchangeRate <= 0 ? 1m : request.ExchangeRate,
                Notes = (request.Notes ?? string.Empty).Trim(),
                ApprovedAt = request.ApprovedAt,
                PostedAt = request.PostedAt,
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };

            ApplyInvoiceLines(entity, request.Lines);
            RecalculateInvoiceTotals(entity);

            db.PurchaseInvoices.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        invoices.MapPut("/{id:guid}", async (Guid id, PurchaseInvoiceRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.PurchaseInvoices.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
                return Results.NotFound(new { message = "No se encontró la factura." });

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BranchId = request.BranchId ?? entity.BranchId;
            entity.SupplierId = request.SupplierId ?? entity.SupplierId;
            entity.PurchaseOrderId = request.PurchaseOrderId;
            entity.PurchaseReceiptId = request.PurchaseReceiptId;
            entity.CurrencyId = request.CurrencyId ?? entity.CurrencyId;
            entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
            entity.SupplierInvoiceFolio = request.SupplierInvoiceFolio?.Trim() ?? entity.SupplierInvoiceFolio;
            entity.InvoiceDate = request.InvoiceDate?.Date ?? entity.InvoiceDate;
            entity.Status = NormalizeStatus(request.Status, entity.Status);
            entity.ExchangeRate = request.ExchangeRate <= 0 ? entity.ExchangeRate : request.ExchangeRate;
            entity.Notes = request.Notes?.Trim() ?? entity.Notes;
            entity.ApprovedAt = request.ApprovedAt;
            entity.PostedAt = request.PostedAt;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";

            ApplyInvoiceLines(entity, request.Lines);
            RecalculateInvoiceTotals(entity);

            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        invoices.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.PurchaseInvoices.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la factura." });
            db.PurchaseInvoices.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        var returns = app.MapGroup("/api/purchases/returns").WithTags("PurchaseReturns");
        returns.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.PurchaseReturns
                .AsNoTracking()
                .OrderByDescending(x => x.ReturnDate)
                .Select(x => new
                {
                    PurchaseReturnId = x.Id,
                    x.CompanyId,
                    x.BranchId,
                    x.WarehouseId,
                    x.SupplierId,
                    x.PurchaseReceiptId,
                    x.PurchaseInvoiceId,
                    x.Folio,
                    x.ReturnDate,
                    x.Reason,
                    x.Status,
                    x.Subtotal,
                    x.TaxAmount,
                    x.Total,
                    x.ApprovedAt,
                    x.PostedAt,
                    x.IsActive
                })
                .ToListAsync()));

        returns.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.PurchaseReturns
                .AsNoTracking()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);

            return entity is null
                ? Results.NotFound(new { message = "No se encontró la devolución." })
                : Results.Ok(new PurchaseReturnRequest
                {
                    PurchaseReturnId = entity.Id,
                    CompanyId = entity.CompanyId,
                    BranchId = entity.BranchId,
                    WarehouseId = entity.WarehouseId,
                    SupplierId = entity.SupplierId,
                    PurchaseReceiptId = entity.PurchaseReceiptId,
                    PurchaseInvoiceId = entity.PurchaseInvoiceId,
                    Folio = entity.Folio,
                    ReturnDate = entity.ReturnDate,
                    Reason = entity.Reason,
                    Status = entity.Status,
                    Subtotal = entity.Subtotal,
                    TaxAmount = entity.TaxAmount,
                    Total = entity.Total,
                    ApprovedAt = entity.ApprovedAt,
                    PostedAt = entity.PostedAt,
                    IsActive = entity.IsActive,
                    Lines = entity.Lines
                        .OrderBy(x => x.LineNumber)
                        .Select(x => new PurchaseLineRequest
                        {
                            Id = x.Id,
                            LineNumber = x.LineNumber,
                            SourceLineId = x.SourceLineId,
                            ItemId = x.ItemId,
                            UnitId = x.UnitId,
                            TaxId = x.TaxId,
                            Description = x.Description,
                            Quantity = x.Quantity,
                            UnitPrice = x.UnitPrice,
                            TaxAmount = x.TaxAmount,
                            LineTotal = x.LineTotal
                        })
                        .ToList()
                });
        });

        returns.MapPost("/", async (PurchaseReturnRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetDefaultCompanyAsync(db, request.CompanyId);
            var branch = await GetDefaultBranchAsync(db, company.Id, request.BranchId);
            var warehouseId = request.WarehouseId ?? await db.Warehouses.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
            var supplierId = request.SupplierId ?? await db.Suppliers.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

            var entity = new Nanchesoft.Domain.Entities.PurchaseReturn
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                WarehouseId = warehouseId,
                SupplierId = supplierId,
                PurchaseReceiptId = request.PurchaseReceiptId,
                PurchaseInvoiceId = request.PurchaseInvoiceId,
                Folio = NormalizeFolio(request.Folio, "DEV"),
                ReturnDate = request.ReturnDate?.Date ?? DateTime.UtcNow.Date,
                Reason = (request.Reason ?? string.Empty).Trim(),
                Status = NormalizeStatus(request.Status),
                ApprovedAt = request.ApprovedAt,
                PostedAt = request.PostedAt,
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };

            ApplyReturnLines(entity, request.Lines);
            RecalculateReturnTotals(entity);

            db.PurchaseReturns.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        returns.MapPut("/{id:guid}", async (Guid id, PurchaseReturnRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.PurchaseReturns.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
                return Results.NotFound(new { message = "No se encontró la devolución." });

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BranchId = request.BranchId ?? entity.BranchId;
            entity.WarehouseId = request.WarehouseId ?? entity.WarehouseId;
            entity.SupplierId = request.SupplierId ?? entity.SupplierId;
            entity.PurchaseReceiptId = request.PurchaseReceiptId;
            entity.PurchaseInvoiceId = request.PurchaseInvoiceId;
            entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
            entity.ReturnDate = request.ReturnDate?.Date ?? entity.ReturnDate;
            entity.Reason = request.Reason?.Trim() ?? entity.Reason;
            entity.Status = NormalizeStatus(request.Status, entity.Status);
            entity.ApprovedAt = request.ApprovedAt;
            entity.PostedAt = request.PostedAt;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";

            ApplyReturnLines(entity, request.Lines);
            RecalculateReturnTotals(entity);

            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        returns.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.PurchaseReturns.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la devolución." });
            db.PurchaseReturns.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        app.MapGet("/api/purchases/lookups", async (NanchesoftDbContext db) => Results.Ok(new
        {
            companies = await db.Companies.AsNoTracking().OrderBy(x => x.Name).Select(x => new { Id = x.Id.ToString(), Name = x.Name }).ToListAsync(),
            branches = await db.Branches.AsNoTracking().OrderBy(x => x.Name).Select(x => new { Id = x.Id.ToString(), Name = x.Name }).ToListAsync(),
            warehouses = await db.Warehouses.AsNoTracking().OrderBy(x => x.Name).Select(x => new { Id = x.Id.ToString(), Name = x.Name }).ToListAsync(),
            suppliers = await db.Suppliers.AsNoTracking().OrderBy(x => x.Name).Select(x => new { Id = x.Id.ToString(), Name = x.Name }).ToListAsync(),
            currencies = await db.Currencies.AsNoTracking().OrderBy(x => x.Code).Select(x => new { Id = x.Id.ToString(), Name = x.Code + " · " + x.Name }).ToListAsync(),
            requisitions = await db.PurchaseRequisitions.AsNoTracking().OrderByDescending(x => x.RequisitionDate).Select(x => new { Id = x.Id.ToString(), Name = x.Folio }).ToListAsync(),
            orders = await db.PurchaseOrders.AsNoTracking().OrderByDescending(x => x.OrderDate).Select(x => new { Id = x.Id.ToString(), Name = x.Folio }).ToListAsync(),
            receipts = await db.PurchaseReceipts.AsNoTracking().OrderByDescending(x => x.ReceiptDate).Select(x => new { Id = x.Id.ToString(), Name = x.Folio }).ToListAsync(),
            invoices = await db.PurchaseInvoices.AsNoTracking().OrderByDescending(x => x.InvoiceDate).Select(x => new { Id = x.Id.ToString(), Name = x.Folio }).ToListAsync(),
            returns = await db.PurchaseReturns.AsNoTracking().OrderByDescending(x => x.ReturnDate).Select(x => new { Id = x.Id.ToString(), Name = x.Folio }).ToListAsync(),
            items = await db.Items.AsNoTracking().OrderBy(x => x.Name).Select(x => new { Id = x.Id.ToString(), Name = x.Code + " · " + x.Name }).ToListAsync(),
            units = await db.Units.AsNoTracking().OrderBy(x => x.Name).Select(x => new { Id = x.Id.ToString(), Name = x.Name }).ToListAsync(),
            taxes = await db.Taxes.AsNoTracking().OrderBy(x => x.Name).Select(x => new { Id = x.Id.ToString(), Name = x.Name }).ToListAsync()
        }));

        app.MapGet("/api/purchases/dashboard/summary", async (NanchesoftDbContext db) => Results.Ok(new
        {
            PendingRequisitions = await db.PurchaseRequisitions.CountAsync(x => x.Status == "draft" || x.Status == "pending_approval"),
            OpenOrders = await db.PurchaseOrders.CountAsync(x => x.Status == "approved" || x.Status == "draft"),
            RecentReceipts = await db.PurchaseReceipts.CountAsync(x => x.ReceiptDate >= DateTime.UtcNow.Date.AddDays(-30)),
            RecentInvoices = await db.PurchaseInvoices.CountAsync(x => x.InvoiceDate >= DateTime.UtcNow.Date.AddDays(-30)),
            PeriodPurchased = await db.PurchaseOrders.Where(x => x.OrderDate >= DateTime.UtcNow.Date.AddDays(-30)).SumAsync(x => (decimal?)x.Total) ?? 0m,
            ReturnsAmount = await db.PurchaseReturns.Where(x => x.ReturnDate >= DateTime.UtcNow.Date.AddDays(-30)).SumAsync(x => (decimal?)x.Total) ?? 0m
        })).WithTags("PurchaseDashboard");

        return app;
    }

    private static async Task<Nanchesoft.Domain.Entities.Company> GetDefaultCompanyAsync(NanchesoftDbContext db, Guid? requestedCompanyId)
    {
        if (requestedCompanyId.HasValue)
        {
            var requested = await db.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.Id == requestedCompanyId.Value);
            if (requested is not null)
            {
                return requested;
            }
        }

        return await db.Companies.OrderBy(x => x.CreatedAt).FirstAsync();
    }

    private static async Task<Nanchesoft.Domain.Entities.Branch> GetDefaultBranchAsync(NanchesoftDbContext db, Guid companyId, Guid? requestedBranchId)
    {
        if (requestedBranchId.HasValue)
        {
            var requested = await db.Branches.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.Id == requestedBranchId.Value);
            if (requested is not null)
            {
                return requested;
            }
        }

        return await db.Branches.Where(x => x.CompanyId == companyId).OrderBy(x => x.CreatedAt).FirstAsync();
    }

    private static string NormalizeFolio(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();

    private static string NormalizeStatus(string? value, string fallback = "draft")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();

    private static void ApplyRequisitionLines(Nanchesoft.Domain.Entities.PurchaseRequisition entity, List<PurchaseLineRequest> lines)
    {
        entity.Lines.Clear();
        var sequence = 1;

        foreach (var line in lines.Where(IsMeaningfulLine))
        {
            entity.Lines.Add(new Nanchesoft.Domain.Entities.PurchaseRequisitionLine
            {
                LineNumber = sequence++,
                ItemId = line.ItemId,
                UnitId = line.UnitId,
                Description = (line.Description ?? string.Empty).Trim(),
                Quantity = line.Quantity,
                Notes = (line.Notes ?? string.Empty).Trim(),
                CreatedBy = "web-api"
            });
        }
    }

    private static void ApplyOrderLines(Nanchesoft.Domain.Entities.PurchaseOrder entity, List<PurchaseLineRequest> lines)
    {
        entity.Lines.Clear();
        var sequence = 1;

        foreach (var line in lines.Where(IsMeaningfulLine))
        {
            var quantity = line.Quantity < 0 ? 0 : line.Quantity;
            var received = line.ReceivedQuantity < 0 ? 0 : line.ReceivedQuantity;
            var pending = quantity - received;
            if (pending < 0) pending = 0;

            entity.Lines.Add(new Nanchesoft.Domain.Entities.PurchaseOrderLine
            {
                LineNumber = sequence++,
                ItemId = line.ItemId,
                UnitId = line.UnitId,
                TaxId = line.TaxId,
                Description = (line.Description ?? string.Empty).Trim(),
                Quantity = quantity,
                ReceivedQuantity = received,
                PendingQuantity = pending,
                UnitPrice = line.UnitPrice,
                DiscountAmount = line.DiscountAmount,
                TaxAmount = line.TaxAmount,
                LineTotal = line.LineTotal,
                CreatedBy = "web-api"
            });
        }
    }

    private static void ApplyReceiptLines(Nanchesoft.Domain.Entities.PurchaseReceipt entity, List<PurchaseLineRequest> lines)
    {
        entity.Lines.Clear();
        var sequence = 1;

        foreach (var line in lines.Where(IsMeaningfulLine))
        {
            entity.Lines.Add(new Nanchesoft.Domain.Entities.PurchaseReceiptLine
            {
                LineNumber = sequence++,
                PurchaseOrderLineId = line.PurchaseOrderLineId,
                ItemId = line.ItemId,
                UnitId = line.UnitId,
                Description = (line.Description ?? string.Empty).Trim(),
                Quantity = line.Quantity < 0 ? 0 : line.Quantity,
                CreatedBy = "web-api"
            });
        }
    }

    private static void ApplyInvoiceLines(Nanchesoft.Domain.Entities.PurchaseInvoice entity, List<PurchaseLineRequest> lines)
    {
        entity.Lines.Clear();
        var sequence = 1;

        foreach (var line in lines.Where(IsMeaningfulLine))
        {
            entity.Lines.Add(new Nanchesoft.Domain.Entities.PurchaseInvoiceLine
            {
                LineNumber = sequence++,
                ItemId = line.ItemId,
                UnitId = line.UnitId,
                TaxId = line.TaxId,
                Description = (line.Description ?? string.Empty).Trim(),
                Quantity = line.Quantity < 0 ? 0 : line.Quantity,
                UnitPrice = line.UnitPrice,
                DiscountAmount = line.DiscountAmount,
                TaxAmount = line.TaxAmount,
                LineTotal = line.LineTotal,
                CreatedBy = "web-api"
            });
        }
    }

    private static void ApplyReturnLines(Nanchesoft.Domain.Entities.PurchaseReturn entity, List<PurchaseLineRequest> lines)
    {
        entity.Lines.Clear();
        var sequence = 1;

        foreach (var line in lines.Where(IsMeaningfulLine))
        {
            entity.Lines.Add(new Nanchesoft.Domain.Entities.PurchaseReturnLine
            {
                LineNumber = sequence++,
                SourceLineId = line.SourceLineId,
                ItemId = line.ItemId,
                UnitId = line.UnitId,
                TaxId = line.TaxId,
                Description = (line.Description ?? string.Empty).Trim(),
                Quantity = line.Quantity < 0 ? 0 : line.Quantity,
                UnitPrice = line.UnitPrice,
                TaxAmount = line.TaxAmount,
                LineTotal = line.LineTotal,
                CreatedBy = "web-api"
            });
        }
    }

    private static void RecalculateOrderTotals(Nanchesoft.Domain.Entities.PurchaseOrder entity)
    {
        entity.Subtotal = entity.Lines.Sum(x => x.Quantity * x.UnitPrice) - entity.Lines.Sum(x => x.DiscountAmount);
        entity.TaxAmount = entity.Lines.Sum(x => x.TaxAmount);
        entity.Total = entity.Subtotal + entity.TaxAmount;

        foreach (var line in entity.Lines)
        {
            line.LineTotal = (line.Quantity * line.UnitPrice) - line.DiscountAmount + line.TaxAmount;
        }
    }

    private static void RecalculateInvoiceTotals(Nanchesoft.Domain.Entities.PurchaseInvoice entity)
    {
        entity.Subtotal = entity.Lines.Sum(x => x.Quantity * x.UnitPrice) - entity.Lines.Sum(x => x.DiscountAmount);
        entity.TaxAmount = entity.Lines.Sum(x => x.TaxAmount);
        entity.Total = entity.Subtotal + entity.TaxAmount;

        foreach (var line in entity.Lines)
        {
            line.LineTotal = (line.Quantity * line.UnitPrice) - line.DiscountAmount + line.TaxAmount;
        }
    }

    private static void RecalculateReturnTotals(Nanchesoft.Domain.Entities.PurchaseReturn entity)
    {
        entity.Subtotal = entity.Lines.Sum(x => x.Quantity * x.UnitPrice);
        entity.TaxAmount = entity.Lines.Sum(x => x.TaxAmount);
        entity.Total = entity.Subtotal + entity.TaxAmount;

        foreach (var line in entity.Lines)
        {
            line.LineTotal = (line.Quantity * line.UnitPrice) + line.TaxAmount;
        }
    }

    private static bool IsMeaningfulLine(PurchaseLineRequest line)
        => line.ItemId.HasValue || !string.IsNullOrWhiteSpace(line.Description) || line.Quantity > 0 || line.UnitPrice > 0;
}

public sealed class PurchaseLineRequest
{
    public Guid? Id { get; set; }
    public int LineNumber { get; set; }
    public Guid? PurchaseOrderLineId { get; set; }
    public Guid? SourceLineId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? TaxId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal PendingQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
}

public sealed class PurchaseRequisitionRequest
{
    public Guid? PurchaseRequisitionId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? Folio { get; set; }
    public DateTime? RequisitionDate { get; set; }
    public string? RequestedByName { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PurchaseLineRequest> Lines { get; set; } = [];
}

public sealed class PurchaseOrderRequest
{
    public Guid? PurchaseOrderId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? CurrencyId { get; set; }
    public Guid? PurchaseRequisitionId { get; set; }
    public string? Folio { get; set; }
    public DateTime? OrderDate { get; set; }
    public string? Status { get; set; }
    public int PaymentTermDays { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PurchaseLineRequest> Lines { get; set; } = [];
}

public sealed class PurchaseReceiptRequest
{
    public Guid? PurchaseReceiptId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string? Folio { get; set; }
    public DateTime? ReceiptDate { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PurchaseLineRequest> Lines { get; set; } = [];
}

public sealed class PurchaseInvoiceRequest
{
    public Guid? PurchaseInvoiceId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? PurchaseReceiptId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string? Folio { get; set; }
    public string? SupplierInvoiceFolio { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? Status { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PurchaseLineRequest> Lines { get; set; } = [];
}

public sealed class PurchaseReturnRequest
{
    public Guid? PurchaseReturnId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? PurchaseReceiptId { get; set; }
    public Guid? PurchaseInvoiceId { get; set; }
    public string? Folio { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PurchaseLineRequest> Lines { get; set; } = [];
}

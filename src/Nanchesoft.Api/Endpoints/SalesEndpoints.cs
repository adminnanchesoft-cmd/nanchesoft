using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class SalesEndpoints
{
    public static IEndpointRouteBuilder MapSalesEndpoints(this IEndpointRouteBuilder app)
    {
        var quotes = app.MapGroup("/api/sales/quotes").WithTags("SalesQuotes");
        quotes.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.SalesQuotes
                .AsNoTracking()
                .OrderByDescending(x => x.QuoteDate)
                .Select(x => new
                {
                    SalesQuoteId = x.Id,
                    x.CompanyId,
                    x.BranchId,
                    x.CustomerId,
                    x.CurrencyId,
                    x.Folio,
                    x.QuoteDate,
                    x.ValidUntil,
                    x.Status,
                    x.ExchangeRate,
                    x.Subtotal,
                    x.DiscountAmount,
                    x.TaxAmount,
                    x.Total,
                    x.Notes,
                    x.ApprovedAt,
                    x.ClosedAt,
                    x.IsActive
                })
                .ToListAsync()));

        quotes.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.SalesQuotes.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            return entity is null
                ? Results.NotFound(new { message = "No se encontró la cotización." })
                : Results.Ok(new SalesQuoteRequest
                {
                    SalesQuoteId = entity.Id,
                    CompanyId = entity.CompanyId,
                    BranchId = entity.BranchId,
                    CustomerId = entity.CustomerId,
                    CurrencyId = entity.CurrencyId,
                    Folio = entity.Folio,
                    QuoteDate = entity.QuoteDate,
                    ValidUntil = entity.ValidUntil,
                    Status = entity.Status,
                    ExchangeRate = entity.ExchangeRate,
                    Subtotal = entity.Subtotal,
                    DiscountAmount = entity.DiscountAmount,
                    TaxAmount = entity.TaxAmount,
                    Total = entity.Total,
                    Notes = entity.Notes,
                    ApprovedAt = entity.ApprovedAt,
                    ClosedAt = entity.ClosedAt,
                    IsActive = entity.IsActive,
                    Lines = entity.Lines.OrderBy(x => x.LineNumber).Select(x => new SalesLineRequest
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
                    }).ToList()
                });
        });

        quotes.MapPost("/", async (SalesQuoteRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetDefaultCompanyAsync(db, request.CompanyId);
            var branch = await GetDefaultBranchAsync(db, company.Id, request.BranchId);
            var customerId = request.CustomerId ?? await db.Customers.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
            var currencyId = request.CurrencyId ?? await db.Currencies.OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

            var entity = new Nanchesoft.Domain.Entities.SalesQuote
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                CustomerId = customerId,
                CurrencyId = currencyId,
                Folio = NormalizeFolio(request.Folio, "COT"),
                QuoteDate = request.QuoteDate?.Date ?? DateTime.UtcNow.Date,
                ValidUntil = request.ValidUntil?.Date,
                Status = NormalizeStatus(request.Status),
                ExchangeRate = request.ExchangeRate <= 0 ? 1m : request.ExchangeRate,
                Notes = (request.Notes ?? string.Empty).Trim(),
                ApprovedAt = request.ApprovedAt,
                ClosedAt = request.ClosedAt,
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };

            ApplyQuoteLines(entity, request.Lines);
            RecalculateQuoteTotals(entity);
            db.SalesQuotes.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        quotes.MapPut("/{id:guid}", async (Guid id, SalesQuoteRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.SalesQuotes.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la cotización." });

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BranchId = request.BranchId ?? entity.BranchId;
            entity.CustomerId = request.CustomerId ?? entity.CustomerId;
            entity.CurrencyId = request.CurrencyId ?? entity.CurrencyId;
            entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
            entity.QuoteDate = request.QuoteDate?.Date ?? entity.QuoteDate;
            entity.ValidUntil = request.ValidUntil?.Date;
            entity.Status = NormalizeStatus(request.Status, entity.Status);
            entity.ExchangeRate = request.ExchangeRate <= 0 ? entity.ExchangeRate : request.ExchangeRate;
            entity.Notes = request.Notes?.Trim() ?? entity.Notes;
            entity.ApprovedAt = request.ApprovedAt;
            entity.ClosedAt = request.ClosedAt;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";

            ApplyQuoteLines(entity, request.Lines);
            RecalculateQuoteTotals(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        quotes.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.SalesQuotes.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la cotización." });
            db.SalesQuotes.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        var orders = app.MapGroup("/api/sales/orders").WithTags("SalesOrders");
        orders.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.SalesOrders.AsNoTracking().OrderByDescending(x => x.OrderDate).Select(x => new
            {
                SalesOrderId = x.Id,
                x.CompanyId,
                x.BranchId,
                x.CustomerId,
                x.CurrencyId,
                x.SalesQuoteId,
                x.Folio,
                x.OrderDate,
                x.Status,
                x.PaymentTermDays,
                x.ExchangeRate,
                x.Subtotal,
                x.DiscountAmount,
                x.TaxAmount,
                x.Total,
                x.Notes,
                x.ApprovedAt,
                x.ClosedAt,
                x.IsActive
            }).ToListAsync()));

        orders.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.SalesOrders.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            return entity is null
                ? Results.NotFound(new { message = "No se encontró el pedido." })
                : Results.Ok(new SalesOrderRequest
                {
                    SalesOrderId = entity.Id,
                    CompanyId = entity.CompanyId,
                    BranchId = entity.BranchId,
                    CustomerId = entity.CustomerId,
                    CurrencyId = entity.CurrencyId,
                    SalesQuoteId = entity.SalesQuoteId,
                    Folio = entity.Folio,
                    OrderDate = entity.OrderDate,
                    Status = entity.Status,
                    PaymentTermDays = entity.PaymentTermDays,
                    ExchangeRate = entity.ExchangeRate,
                    Subtotal = entity.Subtotal,
                    DiscountAmount = entity.DiscountAmount,
                    TaxAmount = entity.TaxAmount,
                    Total = entity.Total,
                    Notes = entity.Notes,
                    ApprovedAt = entity.ApprovedAt,
                    ClosedAt = entity.ClosedAt,
                    IsActive = entity.IsActive,
                    Lines = entity.Lines.OrderBy(x => x.LineNumber).Select(x => new SalesLineRequest
                    {
                        Id = x.Id,
                        LineNumber = x.LineNumber,
                        ItemId = x.ItemId,
                        UnitId = x.UnitId,
                        TaxId = x.TaxId,
                        Description = x.Description,
                        Quantity = x.Quantity,
                        ShippedQuantity = x.ShippedQuantity,
                        InvoicedQuantity = x.InvoicedQuantity,
                        PendingQuantity = x.PendingQuantity,
                        UnitPrice = x.UnitPrice,
                        DiscountAmount = x.DiscountAmount,
                        TaxAmount = x.TaxAmount,
                        LineTotal = x.LineTotal
                    }).ToList()
                });
        });

        orders.MapPost("/", async (SalesOrderRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetDefaultCompanyAsync(db, request.CompanyId);
            var branch = await GetDefaultBranchAsync(db, company.Id, request.BranchId);
            var customerId = request.CustomerId ?? await db.Customers.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
            var currencyId = request.CurrencyId ?? await db.Currencies.OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

            var entity = new Nanchesoft.Domain.Entities.SalesOrder
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                CustomerId = customerId,
                CurrencyId = currencyId,
                SalesQuoteId = request.SalesQuoteId,
                Folio = NormalizeFolio(request.Folio, "PED"),
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
            db.SalesOrders.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        orders.MapPut("/{id:guid}", async (Guid id, SalesOrderRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.SalesOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el pedido." });

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BranchId = request.BranchId ?? entity.BranchId;
            entity.CustomerId = request.CustomerId ?? entity.CustomerId;
            entity.CurrencyId = request.CurrencyId ?? entity.CurrencyId;
            entity.SalesQuoteId = request.SalesQuoteId;
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
            var entity = await db.SalesOrders.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el pedido." });
            db.SalesOrders.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        var shipments = app.MapGroup("/api/sales/shipments").WithTags("SalesShipments");
        shipments.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.SalesShipments.AsNoTracking().OrderByDescending(x => x.ShipmentDate).Select(x => new
            {
                SalesShipmentId = x.Id,
                x.CompanyId,
                x.BranchId,
                x.WarehouseId,
                x.CustomerId,
                x.SalesOrderId,
                x.Folio,
                x.ShipmentDate,
                x.Status,
                x.Notes,
                x.ApprovedAt,
                x.PostedAt,
                x.IsActive
            }).ToListAsync()));

        shipments.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.SalesShipments.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            return entity is null
                ? Results.NotFound(new { message = "No se encontró la remisión." })
                : Results.Ok(new SalesShipmentRequest
                {
                    SalesShipmentId = entity.Id,
                    CompanyId = entity.CompanyId,
                    BranchId = entity.BranchId,
                    WarehouseId = entity.WarehouseId,
                    CustomerId = entity.CustomerId,
                    SalesOrderId = entity.SalesOrderId,
                    Folio = entity.Folio,
                    ShipmentDate = entity.ShipmentDate,
                    Status = entity.Status,
                    Notes = entity.Notes,
                    ApprovedAt = entity.ApprovedAt,
                    PostedAt = entity.PostedAt,
                    IsActive = entity.IsActive,
                    Lines = entity.Lines.OrderBy(x => x.LineNumber).Select(x => new SalesLineRequest
                    {
                        Id = x.Id,
                        LineNumber = x.LineNumber,
                        SalesOrderLineId = x.SalesOrderLineId,
                        ItemId = x.ItemId,
                        UnitId = x.UnitId,
                        Description = x.Description,
                        Quantity = x.Quantity
                    }).ToList()
                });
        });

        shipments.MapPost("/", async (SalesShipmentRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetDefaultCompanyAsync(db, request.CompanyId);
            var branch = await GetDefaultBranchAsync(db, company.Id, request.BranchId);
            var customerId = request.CustomerId ?? await db.Customers.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
            var warehouseId = request.WarehouseId ?? await db.Warehouses.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

            var entity = new Nanchesoft.Domain.Entities.SalesShipment
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                WarehouseId = warehouseId,
                CustomerId = customerId,
                SalesOrderId = request.SalesOrderId,
                Folio = NormalizeFolio(request.Folio, "REM"),
                ShipmentDate = request.ShipmentDate?.Date ?? DateTime.UtcNow.Date,
                Status = NormalizeStatus(request.Status),
                Notes = (request.Notes ?? string.Empty).Trim(),
                ApprovedAt = request.ApprovedAt,
                PostedAt = request.PostedAt,
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };

            ApplyShipmentLines(entity, request.Lines);
            db.SalesShipments.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        shipments.MapPut("/{id:guid}", async (Guid id, SalesShipmentRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.SalesShipments.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la remisión." });

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BranchId = request.BranchId ?? entity.BranchId;
            entity.WarehouseId = request.WarehouseId ?? entity.WarehouseId;
            entity.CustomerId = request.CustomerId ?? entity.CustomerId;
            entity.SalesOrderId = request.SalesOrderId;
            entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
            entity.ShipmentDate = request.ShipmentDate?.Date ?? entity.ShipmentDate;
            entity.Status = NormalizeStatus(request.Status, entity.Status);
            entity.Notes = request.Notes?.Trim() ?? entity.Notes;
            entity.ApprovedAt = request.ApprovedAt;
            entity.PostedAt = request.PostedAt;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";

            ApplyShipmentLines(entity, request.Lines);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        shipments.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.SalesShipments.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la remisión." });
            db.SalesShipments.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        var invoices = app.MapGroup("/api/sales/invoices").WithTags("SalesInvoices");
        invoices.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.SalesInvoices.AsNoTracking().OrderByDescending(x => x.InvoiceDate).Select(x => new
            {
                SalesInvoiceId = x.Id,
                x.CompanyId,
                x.BranchId,
                x.CustomerId,
                x.SalesOrderId,
                x.SalesShipmentId,
                x.CurrencyId,
                x.Folio,
                x.InvoiceDate,
                x.Status,
                x.ExchangeRate,
                x.Subtotal,
                x.DiscountAmount,
                x.TaxAmount,
                x.Total,
                x.Notes,
                x.ApprovedAt,
                x.PostedAt,
                x.IsActive
            }).ToListAsync()));

        invoices.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.SalesInvoices.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            return entity is null
                ? Results.NotFound(new { message = "No se encontró la factura." })
                : Results.Ok(new SalesInvoiceRequest
                {
                    SalesInvoiceId = entity.Id,
                    CompanyId = entity.CompanyId,
                    BranchId = entity.BranchId,
                    CustomerId = entity.CustomerId,
                    SalesOrderId = entity.SalesOrderId,
                    SalesShipmentId = entity.SalesShipmentId,
                    CurrencyId = entity.CurrencyId,
                    Folio = entity.Folio,
                    InvoiceDate = entity.InvoiceDate,
                    Status = entity.Status,
                    ExchangeRate = entity.ExchangeRate,
                    Subtotal = entity.Subtotal,
                    DiscountAmount = entity.DiscountAmount,
                    TaxAmount = entity.TaxAmount,
                    Total = entity.Total,
                    Notes = entity.Notes,
                    ApprovedAt = entity.ApprovedAt,
                    PostedAt = entity.PostedAt,
                    IsActive = entity.IsActive,
                    Lines = entity.Lines.OrderBy(x => x.LineNumber).Select(x => new SalesLineRequest
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
                    }).ToList()
                });
        });

        invoices.MapPost("/", async (SalesInvoiceRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetDefaultCompanyAsync(db, request.CompanyId);
            var branch = await GetDefaultBranchAsync(db, company.Id, request.BranchId);
            var customerId = request.CustomerId ?? await db.Customers.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
            var currencyId = request.CurrencyId ?? await db.Currencies.OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

            var entity = new Nanchesoft.Domain.Entities.SalesInvoice
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                CustomerId = customerId,
                SalesOrderId = request.SalesOrderId,
                SalesShipmentId = request.SalesShipmentId,
                CurrencyId = currencyId,
                Folio = NormalizeFolio(request.Folio, "FACV"),
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
            db.SalesInvoices.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        invoices.MapPut("/{id:guid}", async (Guid id, SalesInvoiceRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.SalesInvoices.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la factura." });

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BranchId = request.BranchId ?? entity.BranchId;
            entity.CustomerId = request.CustomerId ?? entity.CustomerId;
            entity.SalesOrderId = request.SalesOrderId;
            entity.SalesShipmentId = request.SalesShipmentId;
            entity.CurrencyId = request.CurrencyId ?? entity.CurrencyId;
            entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
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
            var entity = await db.SalesInvoices.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la factura." });
            db.SalesInvoices.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        var returns = app.MapGroup("/api/sales/returns").WithTags("SalesReturns");
        returns.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.SalesReturns.AsNoTracking().OrderByDescending(x => x.ReturnDate).Select(x => new
            {
                SalesReturnId = x.Id,
                x.CompanyId,
                x.BranchId,
                x.WarehouseId,
                x.CustomerId,
                x.SalesShipmentId,
                x.SalesInvoiceId,
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
            }).ToListAsync()));

        returns.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.SalesReturns.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            return entity is null
                ? Results.NotFound(new { message = "No se encontró la devolución." })
                : Results.Ok(new SalesReturnRequest
                {
                    SalesReturnId = entity.Id,
                    CompanyId = entity.CompanyId,
                    BranchId = entity.BranchId,
                    WarehouseId = entity.WarehouseId,
                    CustomerId = entity.CustomerId,
                    SalesShipmentId = entity.SalesShipmentId,
                    SalesInvoiceId = entity.SalesInvoiceId,
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
                    Lines = entity.Lines.OrderBy(x => x.LineNumber).Select(x => new SalesLineRequest
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
                    }).ToList()
                });
        });

        returns.MapPost("/", async (SalesReturnRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetDefaultCompanyAsync(db, request.CompanyId);
            var branch = await GetDefaultBranchAsync(db, company.Id, request.BranchId);
            var customerId = request.CustomerId ?? await db.Customers.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
            var warehouseId = request.WarehouseId ?? await db.Warehouses.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

            var entity = new Nanchesoft.Domain.Entities.SalesReturn
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                WarehouseId = warehouseId,
                CustomerId = customerId,
                SalesShipmentId = request.SalesShipmentId,
                SalesInvoiceId = request.SalesInvoiceId,
                Folio = NormalizeFolio(request.Folio, "DEVV"),
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
            db.SalesReturns.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        returns.MapPut("/{id:guid}", async (Guid id, SalesReturnRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.SalesReturns.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la devolución." });

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BranchId = request.BranchId ?? entity.BranchId;
            entity.WarehouseId = request.WarehouseId ?? entity.WarehouseId;
            entity.CustomerId = request.CustomerId ?? entity.CustomerId;
            entity.SalesShipmentId = request.SalesShipmentId;
            entity.SalesInvoiceId = request.SalesInvoiceId;
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
            var entity = await db.SalesReturns.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la devolución." });
            db.SalesReturns.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        var creditNotes = app.MapGroup("/api/sales/credit-notes").WithTags("CreditNotes");
        creditNotes.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.CreditNotes.AsNoTracking().OrderByDescending(x => x.CreditNoteDate).Select(x => new
            {
                CreditNoteId = x.Id,
                x.CompanyId,
                x.BranchId,
                x.CustomerId,
                x.SalesInvoiceId,
                x.Folio,
                x.CreditNoteDate,
                x.Reason,
                x.Status,
                x.Subtotal,
                x.TaxAmount,
                x.Total,
                x.ApprovedAt,
                x.PostedAt,
                x.IsActive
            }).ToListAsync()));

        creditNotes.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.CreditNotes.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            return entity is null
                ? Results.NotFound(new { message = "No se encontró la nota de crédito." })
                : Results.Ok(new CreditNoteRequest
                {
                    CreditNoteId = entity.Id,
                    CompanyId = entity.CompanyId,
                    BranchId = entity.BranchId,
                    CustomerId = entity.CustomerId,
                    SalesInvoiceId = entity.SalesInvoiceId,
                    Folio = entity.Folio,
                    CreditNoteDate = entity.CreditNoteDate,
                    Reason = entity.Reason,
                    Status = entity.Status,
                    Subtotal = entity.Subtotal,
                    TaxAmount = entity.TaxAmount,
                    Total = entity.Total,
                    ApprovedAt = entity.ApprovedAt,
                    PostedAt = entity.PostedAt,
                    IsActive = entity.IsActive,
                    Lines = entity.Lines.OrderBy(x => x.LineNumber).Select(x => new SalesLineRequest
                    {
                        Id = x.Id,
                        LineNumber = x.LineNumber,
                        SalesInvoiceLineId = x.SalesInvoiceLineId,
                        ItemId = x.ItemId,
                        UnitId = x.UnitId,
                        TaxId = x.TaxId,
                        Description = x.Description,
                        Quantity = x.Quantity,
                        UnitPrice = x.UnitPrice,
                        TaxAmount = x.TaxAmount,
                        LineTotal = x.LineTotal
                    }).ToList()
                });
        });

        creditNotes.MapPost("/", async (CreditNoteRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetDefaultCompanyAsync(db, request.CompanyId);
            var branch = await GetDefaultBranchAsync(db, company.Id, request.BranchId);
            var customerId = request.CustomerId ?? await db.Customers.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

            var entity = new Nanchesoft.Domain.Entities.CreditNote
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                CustomerId = customerId,
                SalesInvoiceId = request.SalesInvoiceId,
                Folio = NormalizeFolio(request.Folio, "NC"),
                CreditNoteDate = request.CreditNoteDate?.Date ?? DateTime.UtcNow.Date,
                Reason = (request.Reason ?? string.Empty).Trim(),
                Status = NormalizeStatus(request.Status),
                ApprovedAt = request.ApprovedAt,
                PostedAt = request.PostedAt,
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };

            ApplyCreditNoteLines(entity, request.Lines);
            RecalculateCreditNoteTotals(entity);
            db.CreditNotes.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        creditNotes.MapPut("/{id:guid}", async (Guid id, CreditNoteRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.CreditNotes.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la nota de crédito." });

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BranchId = request.BranchId ?? entity.BranchId;
            entity.CustomerId = request.CustomerId ?? entity.CustomerId;
            entity.SalesInvoiceId = request.SalesInvoiceId;
            entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
            entity.CreditNoteDate = request.CreditNoteDate?.Date ?? entity.CreditNoteDate;
            entity.Reason = request.Reason?.Trim() ?? entity.Reason;
            entity.Status = NormalizeStatus(request.Status, entity.Status);
            entity.ApprovedAt = request.ApprovedAt;
            entity.PostedAt = request.PostedAt;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";

            ApplyCreditNoteLines(entity, request.Lines);
            RecalculateCreditNoteTotals(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        creditNotes.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.CreditNotes.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la nota de crédito." });
            db.CreditNotes.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        app.MapGet("/api/sales/lookups", async (NanchesoftDbContext db) => Results.Ok(new
        {
            Companies = await LookupAsync(db.Companies.OrderBy(x => x.Name).Select(x => new LookupRow(x.Id, x.Name)).ToListAsync()),
            Branches = await LookupAsync(db.Branches.OrderBy(x => x.Name).Select(x => new LookupRow(x.Id, x.Name)).ToListAsync()),
            Warehouses = await LookupAsync(db.Warehouses.OrderBy(x => x.Name).Select(x => new LookupRow(x.Id, x.Name)).ToListAsync()),
            Customers = await LookupAsync(db.Customers.OrderBy(x => x.Name).Select(x => new LookupRow(x.Id, x.Name)).ToListAsync()),
            Currencies = await LookupAsync(db.Currencies.OrderBy(x => x.Name).Select(x => new LookupRow(x.Id, x.Name)).ToListAsync()),
            Quotes = await LookupAsync(db.SalesQuotes.OrderByDescending(x => x.QuoteDate).Select(x => new LookupRow(x.Id, x.Folio)).ToListAsync()),
            Orders = await LookupAsync(db.SalesOrders.OrderByDescending(x => x.OrderDate).Select(x => new LookupRow(x.Id, x.Folio)).ToListAsync()),
            Shipments = await LookupAsync(db.SalesShipments.OrderByDescending(x => x.ShipmentDate).Select(x => new LookupRow(x.Id, x.Folio)).ToListAsync()),
            Invoices = await LookupAsync(db.SalesInvoices.OrderByDescending(x => x.InvoiceDate).Select(x => new LookupRow(x.Id, x.Folio)).ToListAsync()),
            Returns = await LookupAsync(db.SalesReturns.OrderByDescending(x => x.ReturnDate).Select(x => new LookupRow(x.Id, x.Folio)).ToListAsync()),
            CreditNotes = await LookupAsync(db.CreditNotes.OrderByDescending(x => x.CreditNoteDate).Select(x => new LookupRow(x.Id, x.Folio)).ToListAsync()),
            Items = await LookupAsync(db.Items.OrderBy(x => x.Name).Select(x => new LookupRow(x.Id, x.Name)).ToListAsync()),
            Units = await LookupAsync(db.Units.OrderBy(x => x.Name).Select(x => new LookupRow(x.Id, x.Name)).ToListAsync()),
            Taxes = await LookupAsync(db.Taxes.OrderBy(x => x.Name).Select(x => new LookupRow(x.Id, x.Name)).ToListAsync())
        }));

        app.MapGet("/api/sales/dashboard/summary", async (NanchesoftDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;
            var since = today.AddDays(-30);
            string[] openStatuses = ["draft", "pending_approval", "approved"];

            var recentInvoices = await db.SalesInvoices.CountAsync(x => x.InvoiceDate >= since);
            var periodSales = await db.SalesInvoices
                .Where(x => x.InvoiceDate >= since)
                .SumAsync(x => (decimal?)x.Total) ?? 0m;
            var returnsAmount = await db.SalesReturns
                .Where(x => x.ReturnDate >= since)
                .SumAsync(x => (decimal?)x.Total) ?? 0m;
            var creditNotesAmount = await db.CreditNotes
                .Where(x => x.CreditNoteDate >= since)
                .SumAsync(x => (decimal?)x.Total) ?? 0m;

            return Results.Ok(new
            {
                OpenQuotes = await db.SalesQuotes.CountAsync(x => openStatuses.Contains(x.Status)),
                OpenOrders = await db.SalesOrders.CountAsync(x => openStatuses.Contains(x.Status)),
                RecentShipments = await db.SalesShipments.CountAsync(x => x.ShipmentDate >= since),
                RecentInvoices = recentInvoices,
                PeriodSales = periodSales,
                ReturnsAmount = returnsAmount,
                CreditNotesAmount = creditNotesAmount,
                NetSales = periodSales - returnsAmount - creditNotesAmount,
                OpenQuoteAmount = await db.SalesQuotes
                    .Where(x => openStatuses.Contains(x.Status))
                    .SumAsync(x => (decimal?)x.Total) ?? 0m,
                OpenOrderAmount = await db.SalesOrders
                    .Where(x => openStatuses.Contains(x.Status))
                    .SumAsync(x => (decimal?)x.Total) ?? 0m,
                ExpiredQuotes = await db.SalesQuotes.CountAsync(x => x.ValidUntil.HasValue && x.ValidUntil.Value.Date < today && openStatuses.Contains(x.Status)),
                ApprovedOrders = await db.SalesOrders.CountAsync(x => x.Status == "approved"),
                AverageInvoiceTicket = recentInvoices == 0 ? 0m : Math.Round(periodSales / recentInvoices, 2)
            });
        });

        return app;
    }

    private static async Task<List<object>> LookupAsync(Task<List<LookupRow>> rowsTask)
    {
        var rows = await rowsTask;
        return rows.Select(x => new { id = x.Id, name = x.Name }).Cast<object>().ToList();
    }

    private static async Task<Nanchesoft.Domain.Entities.Company> GetDefaultCompanyAsync(NanchesoftDbContext db, Guid? companyId)
    {
        if (companyId.HasValue)
        {
            var company = await db.Companies.FirstOrDefaultAsync(x => x.Id == companyId.Value);
            if (company is not null) return company;
        }
        return await db.Companies.OrderBy(x => x.CreatedAt).FirstAsync();
    }

    private static async Task<Nanchesoft.Domain.Entities.Branch> GetDefaultBranchAsync(NanchesoftDbContext db, Guid companyId, Guid? branchId)
    {
        if (branchId.HasValue)
        {
            var branch = await db.Branches.FirstOrDefaultAsync(x => x.Id == branchId.Value);
            if (branch is not null) return branch;
        }
        return await db.Branches.Where(x => x.CompanyId == companyId).OrderBy(x => x.CreatedAt).FirstAsync();
    }

    private static string NormalizeFolio(string? folio, string fallbackPrefix)
    {
        var normalized = (folio ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? $"{fallbackPrefix}-{DateTime.UtcNow:yyyyMMddHHmmss}" : normalized;
    }

    private static string NormalizeStatus(string? status, string fallback = "draft")
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static bool IsMeaningfulLine(SalesLineRequest line)
        => line.ItemId.HasValue || !string.IsNullOrWhiteSpace(line.Description) || line.Quantity > 0 || line.UnitPrice > 0;

    private static void ApplyQuoteLines(Nanchesoft.Domain.Entities.SalesQuote entity, List<SalesLineRequest> lines)
    {
        entity.Lines.Clear();
        var sequence = 1;
        foreach (var line in lines.Where(IsMeaningfulLine))
        {
            entity.Lines.Add(new Nanchesoft.Domain.Entities.SalesQuoteLine
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

    private static void ApplyOrderLines(Nanchesoft.Domain.Entities.SalesOrder entity, List<SalesLineRequest> lines)
    {
        entity.Lines.Clear();
        var sequence = 1;
        foreach (var line in lines.Where(IsMeaningfulLine))
        {
            var quantity = line.Quantity < 0 ? 0 : line.Quantity;
            var shipped = line.ShippedQuantity < 0 ? 0 : line.ShippedQuantity;
            var invoiced = line.InvoicedQuantity < 0 ? 0 : line.InvoicedQuantity;
            var pending = quantity - Math.Max(shipped, invoiced);
            if (pending < 0) pending = 0;
            entity.Lines.Add(new Nanchesoft.Domain.Entities.SalesOrderLine
            {
                LineNumber = sequence++,
                ItemId = line.ItemId,
                UnitId = line.UnitId,
                TaxId = line.TaxId,
                Description = (line.Description ?? string.Empty).Trim(),
                Quantity = quantity,
                ShippedQuantity = shipped,
                InvoicedQuantity = invoiced,
                PendingQuantity = pending,
                UnitPrice = line.UnitPrice,
                DiscountAmount = line.DiscountAmount,
                TaxAmount = line.TaxAmount,
                LineTotal = line.LineTotal,
                CreatedBy = "web-api"
            });
        }
    }

    private static void ApplyShipmentLines(Nanchesoft.Domain.Entities.SalesShipment entity, List<SalesLineRequest> lines)
    {
        entity.Lines.Clear();
        var sequence = 1;
        foreach (var line in lines.Where(IsMeaningfulLine))
        {
            entity.Lines.Add(new Nanchesoft.Domain.Entities.SalesShipmentLine
            {
                LineNumber = sequence++,
                SalesOrderLineId = line.SalesOrderLineId,
                ItemId = line.ItemId,
                UnitId = line.UnitId,
                Description = (line.Description ?? string.Empty).Trim(),
                Quantity = line.Quantity < 0 ? 0 : line.Quantity,
                CreatedBy = "web-api"
            });
        }
    }

    private static void ApplyInvoiceLines(Nanchesoft.Domain.Entities.SalesInvoice entity, List<SalesLineRequest> lines)
    {
        entity.Lines.Clear();
        var sequence = 1;
        foreach (var line in lines.Where(IsMeaningfulLine))
        {
            entity.Lines.Add(new Nanchesoft.Domain.Entities.SalesInvoiceLine
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

    private static void ApplyReturnLines(Nanchesoft.Domain.Entities.SalesReturn entity, List<SalesLineRequest> lines)
    {
        entity.Lines.Clear();
        var sequence = 1;
        foreach (var line in lines.Where(IsMeaningfulLine))
        {
            entity.Lines.Add(new Nanchesoft.Domain.Entities.SalesReturnLine
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

    private static void ApplyCreditNoteLines(Nanchesoft.Domain.Entities.CreditNote entity, List<SalesLineRequest> lines)
    {
        entity.Lines.Clear();
        var sequence = 1;
        foreach (var line in lines.Where(IsMeaningfulLine))
        {
            entity.Lines.Add(new Nanchesoft.Domain.Entities.CreditNoteLine
            {
                LineNumber = sequence++,
                SalesInvoiceLineId = line.SalesInvoiceLineId,
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

    private static void RecalculateQuoteTotals(Nanchesoft.Domain.Entities.SalesQuote entity)
    {
        entity.Subtotal = entity.Lines.Sum(x => x.Quantity * x.UnitPrice);
        entity.DiscountAmount = entity.Lines.Sum(x => x.DiscountAmount);
        entity.TaxAmount = entity.Lines.Sum(x => x.TaxAmount);
        entity.Total = entity.Subtotal - entity.DiscountAmount + entity.TaxAmount;
        foreach (var line in entity.Lines)
            line.LineTotal = (line.Quantity * line.UnitPrice) - line.DiscountAmount + line.TaxAmount;
    }

    private static void RecalculateOrderTotals(Nanchesoft.Domain.Entities.SalesOrder entity)
    {
        entity.Subtotal = entity.Lines.Sum(x => x.Quantity * x.UnitPrice);
        entity.DiscountAmount = entity.Lines.Sum(x => x.DiscountAmount);
        entity.TaxAmount = entity.Lines.Sum(x => x.TaxAmount);
        entity.Total = entity.Subtotal - entity.DiscountAmount + entity.TaxAmount;
        foreach (var line in entity.Lines)
            line.LineTotal = (line.Quantity * line.UnitPrice) - line.DiscountAmount + line.TaxAmount;
    }

    private static void RecalculateInvoiceTotals(Nanchesoft.Domain.Entities.SalesInvoice entity)
    {
        entity.Subtotal = entity.Lines.Sum(x => x.Quantity * x.UnitPrice);
        entity.DiscountAmount = entity.Lines.Sum(x => x.DiscountAmount);
        entity.TaxAmount = entity.Lines.Sum(x => x.TaxAmount);
        entity.Total = entity.Subtotal - entity.DiscountAmount + entity.TaxAmount;
        foreach (var line in entity.Lines)
            line.LineTotal = (line.Quantity * line.UnitPrice) - line.DiscountAmount + line.TaxAmount;
    }

    private static void RecalculateReturnTotals(Nanchesoft.Domain.Entities.SalesReturn entity)
    {
        entity.Subtotal = entity.Lines.Sum(x => x.Quantity * x.UnitPrice);
        entity.TaxAmount = entity.Lines.Sum(x => x.TaxAmount);
        entity.Total = entity.Subtotal + entity.TaxAmount;
        foreach (var line in entity.Lines)
            line.LineTotal = (line.Quantity * line.UnitPrice) + line.TaxAmount;
    }

    private static void RecalculateCreditNoteTotals(Nanchesoft.Domain.Entities.CreditNote entity)
    {
        entity.Subtotal = entity.Lines.Sum(x => x.Quantity * x.UnitPrice);
        entity.TaxAmount = entity.Lines.Sum(x => x.TaxAmount);
        entity.Total = entity.Subtotal + entity.TaxAmount;
        foreach (var line in entity.Lines)
            line.LineTotal = (line.Quantity * line.UnitPrice) + line.TaxAmount;
    }
}

public sealed record LookupRow(Guid Id, string Name);

public sealed class SalesLineRequest
{
    public Guid? Id { get; set; }
    public int LineNumber { get; set; }
    public Guid? SalesOrderLineId { get; set; }
    public Guid? SalesInvoiceLineId { get; set; }
    public Guid? SourceLineId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? TaxId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal ShippedQuantity { get; set; }
    public decimal InvoicedQuantity { get; set; }
    public decimal PendingQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class SalesQuoteRequest
{
    public Guid? SalesQuoteId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string? Folio { get; set; }
    public DateTime? QuoteDate { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? Status { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SalesLineRequest> Lines { get; set; } = [];
}

public sealed class SalesOrderRequest
{
    public Guid? SalesOrderId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CurrencyId { get; set; }
    public Guid? SalesQuoteId { get; set; }
    public string? Folio { get; set; }
    public DateTime? OrderDate { get; set; }
    public string? Status { get; set; }
    public int PaymentTermDays { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SalesLineRequest> Lines { get; set; } = [];
}

public sealed class SalesShipmentRequest
{
    public Guid? SalesShipmentId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public string? Folio { get; set; }
    public DateTime? ShipmentDate { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SalesLineRequest> Lines { get; set; } = [];
}

public sealed class SalesInvoiceRequest
{
    public Guid? SalesInvoiceId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public Guid? SalesShipmentId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string? Folio { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? Status { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SalesLineRequest> Lines { get; set; } = [];
}

public sealed class SalesReturnRequest
{
    public Guid? SalesReturnId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SalesShipmentId { get; set; }
    public Guid? SalesInvoiceId { get; set; }
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
    public List<SalesLineRequest> Lines { get; set; } = [];
}

public sealed class CreditNoteRequest
{
    public Guid? CreditNoteId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    public string? Folio { get; set; }
    public DateTime? CreditNoteDate { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SalesLineRequest> Lines { get; set; } = [];
}

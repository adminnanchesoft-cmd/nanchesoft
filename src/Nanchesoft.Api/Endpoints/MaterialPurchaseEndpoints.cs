using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class MaterialPurchaseEndpoints
{
    public static IEndpointRouteBuilder MapMaterialPurchaseEndpoints(this IEndpointRouteBuilder app)
    {
        MapSupplierEndpoints(app);
        MapMaterialOrderEndpoints(app);
        MapReceiptEndpoints(app);
        MapPaymentEndpoints(app);
        MapInventoryEndpoints(app);
        MapReportEndpoints(app);
        MapLookupsEndpoints(app);
        return app;
    }

    // ══════════════════════════════════════════════════════════
    // SUPPLIERS
    // ══════════════════════════════════════════════════════════
    private static void MapSupplierEndpoints(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/mat/suppliers").WithTags("MatSuppliers");

        g.MapGet("/", async (NanchesoftDbContext db, Guid? companyId, string? q, int page = 1, int pageSize = 50) =>
        {
            var query = db.Suppliers.AsNoTracking().AsQueryable();
            if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.Name.Contains(q) || x.Code.Contains(q) || x.TaxId.Contains(q));

            var total = await query.CountAsync();
            var items = await query.OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id, x.CompanyId, x.Code, x.Name, x.ShortName, x.LegalName, x.TaxId,
                    x.FiscalRegime, x.Email, x.Phone, x.City, x.State, x.PaymentTermDays,
                    x.CreditLimit, x.CurrentBalance, x.PreferredPaymentMethod, x.IsActive, x.Classification
                }).ToListAsync();
            return Results.Ok(new { total, page, pageSize, items });
        });

        g.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var s = await db.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return s is null ? Results.NotFound() : Results.Ok(s);
        });

        g.MapPost("/", async (SupplierUpsertRequest req, NanchesoftDbContext db) =>
        {
            var company = await db.Companies.FirstOrDefaultAsync(x => x.Id == req.CompanyId)
                ?? await db.Companies.OrderBy(x => x.CreatedAt).FirstAsync();
            var supplier = new Supplier
            {
                TenantId = company.TenantId, CompanyId = company.Id,
                Code = req.Code?.Trim() ?? string.Empty,
                Name = req.Name?.Trim() ?? string.Empty,
                ShortName = req.ShortName?.Trim() ?? string.Empty,
                LegalName = req.LegalName?.Trim() ?? string.Empty,
                Classification = req.Classification?.Trim() ?? string.Empty,
                TaxId = req.TaxId?.Trim() ?? string.Empty,
                FiscalRegime = req.FiscalRegime?.Trim() ?? string.Empty,
                CfdiUse = req.CfdiUse?.Trim() ?? string.Empty,
                Address = req.Address?.Trim() ?? string.Empty,
                PostalCode = req.PostalCode?.Trim() ?? string.Empty,
                Colony = req.Colony?.Trim() ?? string.Empty,
                City = req.City?.Trim() ?? string.Empty,
                State = req.State?.Trim() ?? string.Empty,
                Country = req.Country?.Trim() ?? "México",
                Email = req.Email?.Trim() ?? string.Empty,
                Phone = req.Phone?.Trim() ?? string.Empty,
                Phone2 = req.Phone2?.Trim() ?? string.Empty,
                Fax = req.Fax?.Trim() ?? string.Empty,
                SalesContact = req.SalesContact?.Trim() ?? string.Empty,
                CollectionContact = req.CollectionContact?.Trim() ?? string.Empty,
                PaymentTermDays = req.PaymentTermDays,
                CreditLimit = req.CreditLimit,
                AccountingAccount = req.AccountingAccount?.Trim() ?? string.Empty,
                DiscountPromptPayment = req.DiscountPromptPayment,
                Discount1 = req.Discount1, Discount2 = req.Discount2,
                Discount3 = req.Discount3, Discount4 = req.Discount4,
                PreferredPaymentMethod = req.PreferredPaymentMethod?.Trim() ?? "transfer",
                BankClabe = req.BankClabe?.Trim() ?? string.Empty,
                BankName = req.BankName?.Trim() ?? string.Empty,
                BankAccount = req.BankAccount?.Trim() ?? string.Empty,
                Notes = req.Notes?.Trim() ?? string.Empty,
                IsActive = req.IsActive,
                CreatedBy = "web-api"
            };
            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = supplier.Id });
        });

        g.MapPut("/{id:guid}", async (Guid id, SupplierUpsertRequest req, NanchesoftDbContext db) =>
        {
            var s = await db.Suppliers.FirstOrDefaultAsync(x => x.Id == id);
            if (s is null) return Results.NotFound();
            s.Code = req.Code?.Trim() ?? s.Code;
            s.Name = req.Name?.Trim() ?? s.Name;
            s.ShortName = req.ShortName?.Trim() ?? s.ShortName;
            s.LegalName = req.LegalName?.Trim() ?? s.LegalName;
            s.Classification = req.Classification?.Trim() ?? s.Classification;
            s.TaxId = req.TaxId?.Trim() ?? s.TaxId;
            s.FiscalRegime = req.FiscalRegime?.Trim() ?? s.FiscalRegime;
            s.CfdiUse = req.CfdiUse?.Trim() ?? s.CfdiUse;
            s.Address = req.Address?.Trim() ?? s.Address;
            s.PostalCode = req.PostalCode?.Trim() ?? s.PostalCode;
            s.Colony = req.Colony?.Trim() ?? s.Colony;
            s.City = req.City?.Trim() ?? s.City;
            s.State = req.State?.Trim() ?? s.State;
            s.Country = req.Country?.Trim() ?? s.Country;
            s.Email = req.Email?.Trim() ?? s.Email;
            s.Phone = req.Phone?.Trim() ?? s.Phone;
            s.Phone2 = req.Phone2?.Trim() ?? s.Phone2;
            s.Fax = req.Fax?.Trim() ?? s.Fax;
            s.SalesContact = req.SalesContact?.Trim() ?? s.SalesContact;
            s.CollectionContact = req.CollectionContact?.Trim() ?? s.CollectionContact;
            s.PaymentTermDays = req.PaymentTermDays;
            s.CreditLimit = req.CreditLimit;
            s.AccountingAccount = req.AccountingAccount?.Trim() ?? s.AccountingAccount;
            s.DiscountPromptPayment = req.DiscountPromptPayment;
            s.Discount1 = req.Discount1; s.Discount2 = req.Discount2;
            s.Discount3 = req.Discount3; s.Discount4 = req.Discount4;
            s.PreferredPaymentMethod = req.PreferredPaymentMethod?.Trim() ?? s.PreferredPaymentMethod;
            s.BankClabe = req.BankClabe?.Trim() ?? s.BankClabe;
            s.BankName = req.BankName?.Trim() ?? s.BankName;
            s.BankAccount = req.BankAccount?.Trim() ?? s.BankAccount;
            s.Notes = req.Notes?.Trim() ?? s.Notes;
            s.IsActive = req.IsActive;
            s.UpdatedAt = DateTime.UtcNow; s.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var s = await db.Suppliers.FirstOrDefaultAsync(x => x.Id == id);
            if (s is null) return Results.NotFound();
            s.IsActive = false; s.UpdatedAt = DateTime.UtcNow; s.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // Purchase history for a supplier
        g.MapGet("/{id:guid}/history", async (Guid id, NanchesoftDbContext db) =>
        {
            var orders = await db.PurchaseOrders.AsNoTracking()
                .Where(x => x.SupplierId == id)
                .OrderByDescending(x => x.OrderDate)
                .Select(x => new { x.Id, x.Folio, x.OrderDate, x.Status, x.Total, x.OrderType })
                .Take(50).ToListAsync();

            var receipts = await db.PurchaseReceipts.AsNoTracking()
                .Where(x => x.SupplierId == id)
                .OrderByDescending(x => x.ReceiptDate)
                .Select(x => new { x.Id, x.Folio, x.ReceiptDate, x.Status, x.Total, x.ReceiptType, x.PaymentStatus })
                .Take(50).ToListAsync();

            return Results.Ok(new { orders, receipts });
        });
    }

    // ══════════════════════════════════════════════════════════
    // PURCHASE ORDERS (MATERIALES)
    // ══════════════════════════════════════════════════════════
    private static void MapMaterialOrderEndpoints(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/mat/orders").WithTags("MatOrders");

        g.MapGet("/", async (NanchesoftDbContext db, Guid? companyId, string? status, int page = 1, int pageSize = 50) =>
        {
            var query = db.PurchaseOrders.AsNoTracking()
                .Include(x => x.Supplier)
                .Where(x => x.OrderType == "materials");
            if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(x => x.OrderDate)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id, x.Folio, x.OrderDate, x.SupplierDeliveryDate, x.Status,
                    SupplierName = x.Supplier != null ? x.Supplier.Name : "",
                    x.Total, x.Subtotal, x.TaxAmount, x.ReceivedTotal,
                    x.BuyerName, x.Notes, x.ApprovedAt, x.CompanyId, x.BranchId, x.WarehouseId,
                    LineCount = x.Lines.Count
                }).ToListAsync();
            return Results.Ok(new { total, page, pageSize, items });
        });

        g.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var order = await db.PurchaseOrders.AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.Lines).ThenInclude(l => l.MaterialItem)
                .Include(x => x.Lines).ThenInclude(l => l.Unit)
                .Include(x => x.Lines).ThenInclude(l => l.Tax)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (order is null) return Results.NotFound();
            var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == order.CompanyId);
            var warehouse = await db.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == order.WarehouseId);
            return Results.Ok(new
            {
                order.Id, order.Folio, order.CompanyId, order.BranchId, order.SupplierId,
                order.CurrencyId, order.WarehouseId, order.OrderDate, order.SupplierDeliveryDate,
                order.ExchangeRate, order.PaymentTermDays, order.BuyerName, order.Notes,
                order.Status, order.Subtotal, order.TaxAmount, order.Total, order.ReceivedTotal,
                order.ApprovedAt, order.ApprovedBy,
                SupplierName = order.Supplier != null ? order.Supplier.Name : "",
                SupplierRfc = order.Supplier != null ? order.Supplier.TaxId : "",
                SupplierAddress = order.Supplier != null ? $"{order.Supplier.City}, {order.Supplier.State}" : "",
                WarehouseName = warehouse != null ? warehouse.Name : "",
                CompanyName = company != null ? (string.IsNullOrWhiteSpace(company.LegalName) ? company.Name : company.LegalName) : "",
                CompanyRfc = company != null ? company.TaxId : "",
                Lines = order.Lines.Select(l => new
                {
                    l.Id, l.MaterialItemId, l.ItemId, l.UnitId, l.TaxId,
                    l.Description, l.Quantity, l.ReceivedQuantity, l.PendingQuantity,
                    l.UnitPrice, l.DiscountAmount, l.TaxAmount, l.LineTotal, l.Notes,
                    MaterialName = l.MaterialItem != null ? l.MaterialItem.Name : l.Description,
                    MaterialCode = l.MaterialItem != null ? l.MaterialItem.Code : "",
                    UnitName = l.Unit != null ? l.Unit.Name : ""
                }).OrderBy(l => l.Description).ToList()
            });
        });

        g.MapPost("/", async (MatOrderUpsertRequest req, NanchesoftDbContext db) =>
        {
            var company = await db.Companies.FirstOrDefaultAsync(x => x.Id == req.CompanyId)
                ?? await db.Companies.OrderBy(x => x.CreatedAt).FirstAsync();
            var branch = await db.Branches.FirstOrDefaultAsync(x => x.Id == req.BranchId)
                ?? await db.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstAsync();

            var folio = await NextFolioAsync(db, company.Id, "OC-MAT", "OC-MAT");

            var order = new PurchaseOrder
            {
                TenantId = company.TenantId, CompanyId = company.Id, BranchId = branch.Id,
                SupplierId = req.SupplierId, CurrencyId = req.CurrencyId, WarehouseId = req.WarehouseId,
                OrderType = "materials",
                Folio = folio,
                OrderDate = req.OrderDate?.Date ?? DateTime.UtcNow.Date,
                SupplierDeliveryDate = req.SupplierDeliveryDate,
                Status = "draft",
                ExchangeRate = req.ExchangeRate <= 0 ? 1m : req.ExchangeRate,
                PaymentTermDays = req.PaymentTermDays,
                BuyerName = req.BuyerName?.Trim() ?? string.Empty,
                Notes = req.Notes?.Trim() ?? string.Empty,
                CreatedBy = "web-api"
            };

            ApplyMatOrderLines(order, req.Lines);
            RecalcOrderTotals(order);

            db.PurchaseOrders.Add(order);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = order.Id, folio = order.Folio });
        });

        g.MapPut("/{id:guid}", async (Guid id, MatOrderUpsertRequest req, NanchesoftDbContext db) =>
        {
            var order = await db.PurchaseOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (order is null) return Results.NotFound();
            if (order.Status != "draft")
                return Results.BadRequest(new { message = "Solo se puede editar una orden en borrador." });

            order.SupplierId = req.SupplierId ?? order.SupplierId;
            order.CurrencyId = req.CurrencyId ?? order.CurrencyId;
            order.WarehouseId = req.WarehouseId ?? order.WarehouseId;
            order.OrderDate = req.OrderDate?.Date ?? order.OrderDate;
            order.SupplierDeliveryDate = req.SupplierDeliveryDate ?? order.SupplierDeliveryDate;
            order.ExchangeRate = req.ExchangeRate > 0 ? req.ExchangeRate : order.ExchangeRate;
            order.PaymentTermDays = req.PaymentTermDays;
            order.BuyerName = req.BuyerName?.Trim() ?? order.BuyerName;
            order.Notes = req.Notes?.Trim() ?? order.Notes;
            order.UpdatedAt = DateTime.UtcNow; order.UpdatedBy = "web-api";

            ApplyMatOrderLines(order, req.Lines);
            RecalcOrderTotals(order);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // AUTHORIZE order
        g.MapPost("/{id:guid}/authorize", async (Guid id, AuthorizeRequest req, NanchesoftDbContext db) =>
        {
            var order = await db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id);
            if (order is null) return Results.NotFound();
            if (order.Status != "draft")
                return Results.BadRequest(new { message = "La orden debe estar en borrador para autorizarse." });

            order.Status = "authorized";
            order.ApprovedAt = DateTime.UtcNow;
            order.ApprovedBy = req.UserName?.Trim() ?? "sistema";
            order.UpdatedAt = DateTime.UtcNow; order.UpdatedBy = req.UserName ?? "sistema";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, status = order.Status });
        });

        // CANCEL order
        g.MapPost("/{id:guid}/cancel", async (Guid id, CancelRequest req, NanchesoftDbContext db) =>
        {
            var order = await db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id);
            if (order is null) return Results.NotFound();
            if (order.Status is "received" or "closed")
                return Results.BadRequest(new { message = "No se puede cancelar una orden ya recibida o cerrada." });

            order.Status = "cancelled";
            order.CancelledAt = DateTime.UtcNow;
            order.CancelledBy = req.UserName?.Trim() ?? "sistema";
            order.UpdatedAt = DateTime.UtcNow; order.UpdatedBy = req.UserName ?? "sistema";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // Get authorized orders ready for receipt
        g.MapGet("/authorized", async (NanchesoftDbContext db, Guid? companyId) =>
        {
            var query = db.PurchaseOrders.AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.Lines).ThenInclude(l => l.MaterialItem)
                .Where(x => x.OrderType == "materials" &&
                       (x.Status == "authorized" || x.Status == "partially_received"));
            if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId);
            var items = await query.OrderByDescending(x => x.OrderDate)
                .Select(x => new
                {
                    x.Id, x.Folio, x.OrderDate, x.SupplierDeliveryDate, x.Status,
                    SupplierName = x.Supplier != null ? x.Supplier.Name : "",
                    x.Total, x.ReceivedTotal,
                    PendingTotal = x.Total - x.ReceivedTotal,
                    Lines = x.Lines.Select(l => new
                    {
                        l.Id, l.LineNumber,
                        MaterialItemId = l.MaterialItemId,
                        MaterialName = l.MaterialItem != null ? l.MaterialItem.Name : l.Description,
                        MaterialCode = l.MaterialItem != null ? l.MaterialItem.Code : "",
                        l.Description, l.Quantity, l.ReceivedQuantity, l.PendingQuantity,
                        l.UnitPrice, l.TaxAmount, l.LineTotal, l.UnitId
                    }).ToList()
                }).ToListAsync();
            return Results.Ok(items);
        });
    }

    // ══════════════════════════════════════════════════════════
    // RECEIPTS (ENTRADAS POR REVISIÓN + COMPRAS CON FACTURA)
    // ══════════════════════════════════════════════════════════
    private static void MapReceiptEndpoints(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/mat/receipts").WithTags("MatReceipts");

        g.MapGet("/", async (NanchesoftDbContext db, Guid? companyId, string? receiptType,
            string? status, string? paymentStatus, int page = 1, int pageSize = 50) =>
        {
            var query = db.PurchaseReceipts.AsNoTracking()
                .Include(x => x.Supplier).Include(x => x.PurchaseOrder);
            var filtered = query.AsQueryable();
            if (companyId.HasValue) filtered = filtered.Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(receiptType)) filtered = filtered.Where(x => x.ReceiptType == receiptType);
            if (!string.IsNullOrWhiteSpace(status)) filtered = filtered.Where(x => x.Status == status);
            if (!string.IsNullOrWhiteSpace(paymentStatus)) filtered = filtered.Where(x => x.PaymentStatus == paymentStatus);

            var total = await filtered.CountAsync();
            var items = await filtered.OrderByDescending(x => x.ReceiptDate)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id, x.Folio, x.ReceiptDate, x.ReceiptType, x.Status, x.PaymentStatus,
                    SupplierName = x.Supplier != null ? x.Supplier.Name : "",
                    OrderFolio = x.PurchaseOrder != null ? x.PurchaseOrder.Folio : "",
                    x.SupplierDocumentNumber, x.Total, x.PaidAmount, x.ReviewedAt, x.AuthorizedAt,
                    x.HasDifferences, x.DifferencesAuthorized, x.CompanyId, x.BranchId,
                    x.ConvertedToInvoiceId
                }).ToListAsync();
            return Results.Ok(new { total, page, pageSize, items });
        });

        g.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var r = await db.PurchaseReceipts.AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.PurchaseOrder)
                .Include(x => x.Lines).ThenInclude(l => l.MaterialItem)
                .Include(x => x.Lines).ThenInclude(l => l.Unit)
                .Include(x => x.Lines).ThenInclude(l => l.Tax)
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (r is null) return Results.NotFound();
            var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == r.CompanyId);
            var warehouse = await db.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == r.WarehouseId);
            return Results.Ok(new
            {
                r.Id, r.Folio, r.CompanyId, r.BranchId, r.SupplierId, r.WarehouseId,
                r.PurchaseOrderId, r.ReceiptType, r.ReceiptDate, r.SupplierDocumentNumber,
                r.SupplierDocumentDate, r.Notes, r.Subtotal, r.TaxAmount, r.Total,
                r.Status, r.PaymentStatus, r.PaidAmount, r.HasDifferences, r.DifferencesAuthorized,
                r.ReviewedAt, r.ReviewedBy, r.AuthorizedAt, r.AuthorizedBy, r.ConvertedToInvoiceId,
                SupplierName = r.Supplier != null ? r.Supplier.Name : "",
                SupplierRfc = r.Supplier != null ? r.Supplier.TaxId : "",
                OrderFolio = r.PurchaseOrder != null ? r.PurchaseOrder.Folio : "",
                WarehouseName = warehouse != null ? warehouse.Name : "",
                CompanyName = company != null ? (string.IsNullOrWhiteSpace(company.LegalName) ? company.Name : company.LegalName) : "",
                CompanyRfc = company != null ? company.TaxId : "",
                Lines = r.Lines.Select(l => new
                {
                    l.Id, l.PurchaseOrderLineId, l.MaterialItemId, l.ItemId, l.UnitId, l.TaxId,
                    l.Description, l.Quantity, l.UnitPrice, l.DiscountAmount, l.TaxAmount, l.LineTotal,
                    l.OrderedQuantity, l.OrderedUnitPrice, l.Notes,
                    MaterialName = l.MaterialItem != null ? l.MaterialItem.Name : l.Description,
                    MaterialCode = l.MaterialItem != null ? l.MaterialItem.Code : "",
                    UnitName = l.Unit != null ? l.Unit.Name : ""
                }).OrderBy(l => l.Description).ToList(),
                Payments = r.Payments.Select(p => new
                {
                    p.Id, p.Folio, p.PaymentDate, p.PaymentMethod, p.Amount, p.Reference, p.Status
                }).OrderByDescending(p => p.PaymentDate).ToList()
            });
        });

        g.MapPost("/", async (MatReceiptUpsertRequest req, NanchesoftDbContext db) =>
        {
            var company = await db.Companies.FirstOrDefaultAsync(x => x.Id == req.CompanyId)
                ?? await db.Companies.OrderBy(x => x.CreatedAt).FirstAsync();
            var branch = await db.Branches.FirstOrDefaultAsync(x => x.Id == req.BranchId)
                ?? await db.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstAsync();

            var receiptType = (req.ReceiptType ?? "review").Trim().ToLower();
            var seriesCode = receiptType == "invoice" ? "COMP-FAC" : "ENT-REV";
            var folio = await NextFolioAsync(db, company.Id, seriesCode, seriesCode);

            var receipt = new PurchaseReceipt
            {
                TenantId = company.TenantId, CompanyId = company.Id, BranchId = branch.Id,
                SupplierId = req.SupplierId, WarehouseId = req.WarehouseId,
                PurchaseOrderId = req.PurchaseOrderId,
                ReceiptType = receiptType,
                Folio = folio,
                ReceiptDate = req.ReceiptDate?.Date ?? DateTime.UtcNow.Date,
                Status = "draft",
                PaymentStatus = "pending",
                SupplierDocumentNumber = req.SupplierDocumentNumber?.Trim() ?? string.Empty,
                SupplierDocumentDate = req.SupplierDocumentDate,
                Notes = req.Notes?.Trim() ?? string.Empty,
                CreatedBy = "web-api"
            };

            ApplyReceiptLines(receipt, req.Lines);
            RecalcReceiptTotals(receipt);

            using var tx = await db.Database.BeginTransactionAsync();
            db.PurchaseReceipts.Add(receipt);
            await db.SaveChangesAsync();

            // Impact inventory immediately on save
            await ImpactInventoryAsync(db, receipt, company.TenantId, "web-api");

            // Update PO received quantities
            if (receipt.PurchaseOrderId.HasValue)
                await UpdateOrderReceivedAsync(db, receipt.PurchaseOrderId.Value);

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return Results.Ok(new { success = true, id = receipt.Id, folio = receipt.Folio });
        });

        g.MapPut("/{id:guid}", async (Guid id, MatReceiptUpsertRequest req, NanchesoftDbContext db) =>
        {
            var receipt = await db.PurchaseReceipts.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (receipt is null) return Results.NotFound();
            if (receipt.Status != "draft")
                return Results.BadRequest(new { message = "No se puede modificar: la recepción ya fue revisada o autorizada." });

            receipt.SupplierId = req.SupplierId ?? receipt.SupplierId;
            receipt.WarehouseId = req.WarehouseId ?? receipt.WarehouseId;
            receipt.PurchaseOrderId = req.PurchaseOrderId ?? receipt.PurchaseOrderId;
            receipt.ReceiptDate = req.ReceiptDate?.Date ?? receipt.ReceiptDate;
            receipt.SupplierDocumentNumber = req.SupplierDocumentNumber?.Trim() ?? receipt.SupplierDocumentNumber;
            receipt.SupplierDocumentDate = req.SupplierDocumentDate ?? receipt.SupplierDocumentDate;
            receipt.Notes = req.Notes?.Trim() ?? receipt.Notes;
            receipt.UpdatedAt = DateTime.UtcNow; receipt.UpdatedBy = "web-api";

            ApplyReceiptLines(receipt, req.Lines);
            RecalcReceiptTotals(receipt);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // MARK AS REVIEWED
        g.MapPost("/{id:guid}/review", async (Guid id, AuthorizeRequest req, NanchesoftDbContext db) =>
        {
            var receipt = await db.PurchaseReceipts.FirstOrDefaultAsync(x => x.Id == id);
            if (receipt is null) return Results.NotFound();
            if (receipt.Status != "draft")
                return Results.BadRequest(new { message = "Solo se puede revisar una recepción en borrador." });

            receipt.Status = "reviewed";
            receipt.ReviewedAt = DateTime.UtcNow;
            receipt.ReviewedBy = req.UserName?.Trim() ?? "sistema";
            receipt.UpdatedAt = DateTime.UtcNow; receipt.UpdatedBy = req.UserName ?? "sistema";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, status = receipt.Status });
        });

        // GET COMPARISON OC vs RECEIPT
        g.MapGet("/{id:guid}/compare", async (Guid id, NanchesoftDbContext db) =>
        {
            var receipt = await db.PurchaseReceipts.AsNoTracking()
                .Include(x => x.Lines).ThenInclude(l => l.MaterialItem)
                .Include(x => x.PurchaseOrder).ThenInclude(o => o!.Lines).ThenInclude(l => l.MaterialItem)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (receipt is null) return Results.NotFound();

            var diffs = BuildDiffs(receipt);
            return Results.Ok(new
            {
                receipt.Id, receipt.Folio, receipt.ReceiptType,
                HasDifferences = diffs.Any(),
                Differences = diffs
            });
        });

        // AUTHORIZE WITH DIFFERENCES
        g.MapPost("/{id:guid}/authorize", async (Guid id, AuthorizeDiffRequest req, NanchesoftDbContext db) =>
        {
            var receipt = await db.PurchaseReceipts.Include(x => x.Lines).ThenInclude(l => l.MaterialItem)
                .Include(x => x.PurchaseOrder).ThenInclude(o => o!.Lines).ThenInclude(l => l.MaterialItem)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (receipt is null) return Results.NotFound();
            if (receipt.Status is "authorized" or "cancelled")
                return Results.BadRequest(new { message = "La recepción ya está en un estado final." });

            var diffs = BuildDiffs(receipt);
            if (diffs.Any() && !req.AuthorizeDifferences)
                return Results.BadRequest(new { message = "Existen diferencias. Debe confirmar la autorización con diferencias." });

            receipt.Status = "authorized";
            receipt.AuthorizedAt = DateTime.UtcNow;
            receipt.AuthorizedBy = req.UserName?.Trim() ?? "sistema";
            receipt.HasDifferences = diffs.Any();
            receipt.DifferencesAuthorized = diffs.Any();
            if (diffs.Any())
            {
                receipt.DifferencesAuthorizedAt = DateTime.UtcNow;
                receipt.DifferencesAuthorizedBy = req.UserName?.Trim() ?? "sistema";

                // Persist diff record
                var diff = new PurchaseReceiptDiff
                {
                    TenantId = receipt.TenantId, CompanyId = receipt.CompanyId,
                    PurchaseReceiptId = receipt.Id, PurchaseOrderId = receipt.PurchaseOrderId,
                    Authorized = true, AuthorizedAt = DateTime.UtcNow,
                    AuthorizedBy = req.UserName?.Trim() ?? "sistema",
                    AuthorizationNotes = req.Notes,
                    CreatedBy = "web-api"
                };
                foreach (var d in diffs)
                {
                    diff.Lines.Add(new PurchaseReceiptDiffLine
                    {
                        MaterialItemId = d.MaterialItemId,
                        MaterialName = d.MaterialName, DiffType = d.DiffType,
                        OrderedQuantity = d.OrderedQuantity, ReceivedQuantity = d.ReceivedQuantity,
                        QuantityDiff = d.QuantityDiff,
                        OrderedUnitPrice = d.OrderedUnitPrice, ReceivedUnitPrice = d.ReceivedUnitPrice,
                        PriceDiff = d.PriceDiff,
                        OrderedTotal = d.OrderedTotal, ReceivedTotal = d.ReceivedTotal, TotalDiff = d.TotalDiff,
                        CreatedBy = "web-api"
                    });
                }
                db.PurchaseReceiptDiffs.Add(diff);
            }
            receipt.UpdatedAt = DateTime.UtcNow; receipt.UpdatedBy = req.UserName ?? "sistema";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, status = receipt.Status, diffsFound = diffs.Count });
        });

        // REJECT receipt
        g.MapPost("/{id:guid}/reject", async (Guid id, RejectRequest req, NanchesoftDbContext db) =>
        {
            var receipt = await db.PurchaseReceipts.FirstOrDefaultAsync(x => x.Id == id);
            if (receipt is null) return Results.NotFound();
            receipt.Status = "rejected";
            receipt.RejectedAt = DateTime.UtcNow;
            receipt.RejectedBy = req.UserName?.Trim() ?? "sistema";
            receipt.RejectionReason = req.Reason?.Trim();
            receipt.UpdatedAt = DateTime.UtcNow; receipt.UpdatedBy = req.UserName ?? "sistema";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // CONVERT review → invoice
        g.MapPost("/{id:guid}/convert-to-invoice", async (Guid id, ConvertToInvoiceRequest req, NanchesoftDbContext db) =>
        {
            var review = await db.PurchaseReceipts.Include(x => x.Lines).ThenInclude(l => l.MaterialItem)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (review is null) return Results.NotFound();
            if (review.ReceiptType != "review")
                return Results.BadRequest(new { message = "Solo se puede convertir una Entrada por Revisión." });
            if (review.ConvertedToInvoiceId.HasValue)
                return Results.BadRequest(new { message = "Esta entrada ya fue convertida a factura." });

            var company = await db.Companies.FirstOrDefaultAsync(x => x.Id == review.CompanyId)
                ?? await db.Companies.OrderBy(x => x.CreatedAt).FirstAsync();
            var folio = await NextFolioAsync(db, company.Id, "COMP-FAC", "COMP-FAC");

            var invoice = new PurchaseReceipt
            {
                TenantId = review.TenantId, CompanyId = review.CompanyId, BranchId = review.BranchId,
                SupplierId = review.SupplierId, WarehouseId = review.WarehouseId,
                PurchaseOrderId = review.PurchaseOrderId,
                ReceiptType = "invoice",
                Folio = folio,
                ReceiptDate = req.InvoiceDate?.Date ?? DateTime.UtcNow.Date,
                Status = review.Status,
                PaymentStatus = review.PaymentStatus,
                PaidAmount = review.PaidAmount,
                SupplierDocumentNumber = req.SupplierInvoiceNumber?.Trim() ?? review.SupplierDocumentNumber,
                SupplierDocumentDate = req.InvoiceDate ?? review.SupplierDocumentDate,
                Subtotal = review.Subtotal, TaxAmount = review.TaxAmount, Total = review.Total,
                ReviewedAt = review.ReviewedAt, ReviewedBy = review.ReviewedBy,
                AuthorizedAt = review.AuthorizedAt, AuthorizedBy = review.AuthorizedBy,
                HasDifferences = review.HasDifferences, DifferencesAuthorized = review.DifferencesAuthorized,
                Notes = review.Notes,
                CreatedBy = "web-api"
            };

            foreach (var line in review.Lines)
            {
                invoice.Lines.Add(new PurchaseReceiptLine
                {
                    LineNumber = line.LineNumber, MaterialItemId = line.MaterialItemId,
                    ItemId = line.ItemId, UnitId = line.UnitId, TaxId = line.TaxId,
                    Description = line.Description, Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice, DiscountAmount = line.DiscountAmount,
                    TaxAmount = line.TaxAmount, LineTotal = line.LineTotal,
                    OrderedQuantity = line.OrderedQuantity, OrderedUnitPrice = line.OrderedUnitPrice,
                    Notes = line.Notes, CreatedBy = "web-api"
                });
            }

            review.ConvertedToInvoiceId = invoice.Id;
            review.ConvertedAt = DateTime.UtcNow;
            review.ConvertedBy = req.UserName?.Trim() ?? "sistema";

            db.PurchaseReceipts.Add(invoice);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = invoice.Id, folio = invoice.Folio });
        });
    }

    // ══════════════════════════════════════════════════════════
    // PAYMENTS
    // ══════════════════════════════════════════════════════════
    private static void MapPaymentEndpoints(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/mat/payments").WithTags("MatPayments");

        g.MapGet("/", async (NanchesoftDbContext db, Guid? companyId, int page = 1, int pageSize = 50) =>
        {
            var query = db.PurchasePayments.AsNoTracking().Include(x => x.Supplier).Include(x => x.BankAccount);
            var filtered = query.AsQueryable();
            if (companyId.HasValue) filtered = filtered.Where(x => x.CompanyId == companyId);
            var total = await filtered.CountAsync();
            var items = await filtered.OrderByDescending(x => x.PaymentDate)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id, x.Folio, x.PaymentDate, x.PaymentMethod, x.Amount, x.Reference, x.Status,
                    SupplierName = x.Supplier != null ? x.Supplier.Name : "",
                    BankAccountName = x.BankAccount != null ? x.BankAccount.AccountNumber : "",
                    x.PurchaseReceiptId
                }).ToListAsync();
            return Results.Ok(new { total, page, pageSize, items });
        });

        g.MapPost("/", async (MatPaymentRequest req, NanchesoftDbContext db) =>
        {
            var receipt = await db.PurchaseReceipts.FirstOrDefaultAsync(x => x.Id == req.PurchaseReceiptId);
            if (receipt is null) return Results.NotFound(new { message = "Recepción no encontrada." });
            if (receipt.Status != "authorized")
                return Results.BadRequest(new { message = "La recepción debe estar autorizada para registrar pagos." });

            var company = await db.Companies.FirstOrDefaultAsync(x => x.Id == receipt.CompanyId)
                ?? await db.Companies.OrderBy(x => x.CreatedAt).FirstAsync();
            var branch = await db.Branches.FirstOrDefaultAsync(x => x.Id == receipt.BranchId)
                ?? await db.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstAsync();

            var folio = await NextFolioAsync(db, company.Id, "PAG-PROV", "PAG-PROV");
            var payment = new PurchasePayment
            {
                TenantId = company.TenantId, CompanyId = company.Id, BranchId = branch.Id,
                PurchaseReceiptId = receipt.Id, SupplierId = receipt.SupplierId,
                BankAccountId = req.BankAccountId,
                Folio = folio,
                PaymentDate = req.PaymentDate?.Date ?? DateTime.UtcNow.Date,
                PaymentMethod = req.PaymentMethod?.Trim() ?? "transfer",
                Amount = req.Amount,
                Reference = req.Reference?.Trim() ?? string.Empty,
                Notes = req.Notes?.Trim() ?? string.Empty,
                Status = "posted",
                CreatedBy = "web-api"
            };
            db.PurchasePayments.Add(payment);

            // Update receipt payment status
            receipt.PaidAmount += req.Amount;
            if (receipt.PaidAmount >= receipt.Total)
                receipt.PaymentStatus = "paid";
            else
                receipt.PaymentStatus = "partial";
            receipt.UpdatedAt = DateTime.UtcNow; receipt.UpdatedBy = "web-api";

            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = payment.Id, folio = payment.Folio, paymentStatus = receipt.PaymentStatus });
        });

        g.MapGet("/by-receipt/{receiptId:guid}", async (Guid receiptId, NanchesoftDbContext db) =>
        {
            var payments = await db.PurchasePayments.AsNoTracking()
                .Where(x => x.PurchaseReceiptId == receiptId)
                .OrderByDescending(x => x.PaymentDate)
                .Select(x => new { x.Id, x.Folio, x.PaymentDate, x.PaymentMethod, x.Amount, x.Reference, x.Status })
                .ToListAsync();
            return Results.Ok(payments);
        });

        g.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var p = await db.PurchasePayments.AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.BankAccount)
                .Include(x => x.PurchaseReceipt)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return Results.NotFound();
            var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == p.CompanyId);
            return Results.Ok(new
            {
                p.Id, p.Folio, p.PaymentDate, p.PaymentMethod, p.Amount,
                p.Reference, p.Notes, p.Status,
                SupplierName = p.Supplier != null ? p.Supplier.Name : "",
                SupplierRfc = p.Supplier != null ? p.Supplier.TaxId : "",
                BankAccount = p.BankAccount != null ? p.BankAccount.AccountNumber : "",
                ReceiptFolio = p.PurchaseReceipt != null ? p.PurchaseReceipt.Folio : "",
                ReceiptDate = p.PurchaseReceipt != null ? (DateTime?)p.PurchaseReceipt.ReceiptDate : null,
                ReceiptTotal = p.PurchaseReceipt != null ? p.PurchaseReceipt.Total : 0m,
                CompanyName = company != null ? (string.IsNullOrWhiteSpace(company.LegalName) ? company.Name : company.LegalName) : "",
                CompanyRfc = company != null ? company.TaxId : ""
            });
        });
    }

    // ══════════════════════════════════════════════════════════
    // MATERIAL INVENTORY
    // ══════════════════════════════════════════════════════════
    private static void MapInventoryEndpoints(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/mat/inventory").WithTags("MatInventory");

        g.MapGet("/balances", async (NanchesoftDbContext db, Guid? companyId, Guid? warehouseId, string? q) =>
        {
            var query = db.MaterialStockBalances.AsNoTracking()
                .Include(x => x.MaterialItem).ThenInclude(m => m!.MaterialSubfamily).ThenInclude(s => s!.MaterialFamily)
                .Include(x => x.Warehouse);
            var filtered = query.AsQueryable();
            if (companyId.HasValue) filtered = filtered.Where(x => x.CompanyId == companyId);
            if (warehouseId.HasValue) filtered = filtered.Where(x => x.WarehouseId == warehouseId);
            if (!string.IsNullOrWhiteSpace(q))
                filtered = filtered.Where(x =>
                    x.MaterialItem != null &&
                    (x.MaterialItem.Name.Contains(q) || x.MaterialItem.Code.Contains(q)));

            var items = await filtered
                .OrderBy(x => x.MaterialItem != null ? x.MaterialItem.Name : "")
                .Select(x => new
                {
                    x.Id, x.MaterialItemId, x.WarehouseId,
                    MaterialCode = x.MaterialItem != null ? x.MaterialItem.Code : "",
                    MaterialName = x.MaterialItem != null ? x.MaterialItem.Name : "",
                    FamilyName = x.MaterialItem != null && x.MaterialItem.MaterialSubfamily != null
                        && x.MaterialItem.MaterialSubfamily.MaterialFamily != null
                        ? x.MaterialItem.MaterialSubfamily.MaterialFamily.Name : "",
                    SubfamilyName = x.MaterialItem != null && x.MaterialItem.MaterialSubfamily != null
                        ? x.MaterialItem.MaterialSubfamily.Name : "",
                    WarehouseName = x.Warehouse != null ? x.Warehouse.Name : "",
                    x.QuantityOnHand, x.QuantityReserved, x.QuantityAvailable,
                    x.AverageCost, x.LastCost, x.LastMovementAt
                }).ToListAsync();
            return Results.Ok(items);
        });

        g.MapGet("/kardex/{materialItemId:guid}", async (Guid materialItemId, NanchesoftDbContext db,
            Guid? warehouseId, DateTime? from, DateTime? to) =>
        {
            var query = db.MaterialInventoryMovements.AsNoTracking()
                .Where(x => x.MaterialItemId == materialItemId);
            if (warehouseId.HasValue) query = query.Where(x => x.WarehouseId == warehouseId);
            if (from.HasValue) query = query.Where(x => x.MovementDate >= from.Value);
            if (to.HasValue) query = query.Where(x => x.MovementDate <= to.Value);

            var movements = await query.OrderBy(x => x.MovementDate).ThenBy(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id, x.MovementDate, x.MovementType, x.DocumentType, x.DocumentFolio,
                    x.QuantityIn, x.QuantityOut, x.BalanceAfter, x.UnitCost, x.TotalCost, x.Notes, x.UserName
                }).ToListAsync();

            var material = await db.MaterialItems.AsNoTracking()
                .Where(x => x.Id == materialItemId)
                .Select(x => new { x.Code, x.Name }).FirstOrDefaultAsync();

            return Results.Ok(new { material, movements });
        });

        g.MapGet("/movement/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var mov = await db.MaterialInventoryMovements.AsNoTracking()
                .Include(x => x.MaterialItem)
                .Include(x => x.Warehouse)
                .Include(x => x.Supplier)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (mov is null) return Results.NotFound();
            var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == mov.CompanyId);
            return Results.Ok(new
            {
                mov.Id, mov.MovementDate, mov.MovementType, mov.DocumentType, mov.DocumentFolio,
                mov.QuantityIn, mov.QuantityOut, mov.BalanceAfter, mov.UnitCost, mov.TotalCost,
                mov.Notes, mov.UserName,
                MaterialCode = mov.MaterialItem != null ? mov.MaterialItem.Code : "",
                MaterialName = mov.MaterialItem != null ? mov.MaterialItem.Name : "",
                WarehouseName = mov.Warehouse != null ? mov.Warehouse.Name : "",
                SupplierName = mov.Supplier != null ? mov.Supplier.Name : "",
                CompanyName = company != null ? (string.IsNullOrWhiteSpace(company.LegalName) ? company.Name : company.LegalName) : "",
                CompanyRfc = company != null ? company.TaxId : ""
            });
        });
    }

    // ══════════════════════════════════════════════════════════
    // REPORTS
    // ══════════════════════════════════════════════════════════
    private static void MapReportEndpoints(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/mat/reports").WithTags("MatReports");

        // Report: OC por material (cantidades pedidas, recibidas, faltantes)
        g.MapGet("/orders-by-material", async (NanchesoftDbContext db, Guid? companyId,
            DateTime? from, DateTime? to, string? status) =>
        {
            var query = db.PurchaseOrderLines.AsNoTracking()
                .Include(x => x.PurchaseOrder).ThenInclude(o => o!.Supplier)
                .Include(x => x.MaterialItem)
                .Where(x => x.PurchaseOrder != null && x.PurchaseOrder.OrderType == "materials");

            if (companyId.HasValue)
                query = query.Where(x => x.PurchaseOrder != null && x.PurchaseOrder.CompanyId == companyId);
            if (from.HasValue)
                query = query.Where(x => x.PurchaseOrder != null && x.PurchaseOrder.OrderDate >= from);
            if (to.HasValue)
                query = query.Where(x => x.PurchaseOrder != null && x.PurchaseOrder.OrderDate <= to);
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(x => x.PurchaseOrder != null && x.PurchaseOrder.Status == status);

            var items = await query.Select(x => new
            {
                OrderId = x.PurchaseOrder != null ? x.PurchaseOrder.Id : Guid.Empty,
                OrderFolio = x.PurchaseOrder != null ? x.PurchaseOrder.Folio : "",
                OrderDate = x.PurchaseOrder != null ? x.PurchaseOrder.OrderDate : DateTime.MinValue,
                SupplierDeliveryDate = x.PurchaseOrder != null ? x.PurchaseOrder.SupplierDeliveryDate : null,
                SupplierName = x.PurchaseOrder != null && x.PurchaseOrder.Supplier != null ? x.PurchaseOrder.Supplier.Name : "",
                OrderStatus = x.PurchaseOrder != null ? x.PurchaseOrder.Status : "",
                MaterialCode = x.MaterialItem != null ? x.MaterialItem.Code : "",
                MaterialName = x.MaterialItem != null ? x.MaterialItem.Name : x.Description,
                x.Quantity, x.ReceivedQuantity, x.PendingQuantity, x.UnitPrice, x.LineTotal
            }).OrderBy(x => x.MaterialName).ThenBy(x => x.OrderDate).ToListAsync();

            return Results.Ok(items);
        });

        // Report: Pendientes por recibir
        g.MapGet("/pending-receipts", async (NanchesoftDbContext db, Guid? companyId) =>
        {
            var items = await db.PurchaseOrderLines.AsNoTracking()
                .Include(x => x.PurchaseOrder).ThenInclude(o => o!.Supplier)
                .Include(x => x.MaterialItem)
                .Where(x => x.PurchaseOrder != null &&
                       x.PurchaseOrder.OrderType == "materials" &&
                       (x.PurchaseOrder.Status == "authorized" || x.PurchaseOrder.Status == "partially_received") &&
                       x.PendingQuantity > 0)
                .Select(x => new
                {
                    OrderFolio = x.PurchaseOrder != null ? x.PurchaseOrder.Folio : "",
                    SupplierName = x.PurchaseOrder != null && x.PurchaseOrder.Supplier != null ? x.PurchaseOrder.Supplier.Name : "",
                    DeliveryDate = x.PurchaseOrder != null ? x.PurchaseOrder.SupplierDeliveryDate : null,
                    DaysLate = x.PurchaseOrder != null && x.PurchaseOrder.SupplierDeliveryDate.HasValue
                        ? (int)(DateTime.UtcNow.Date - x.PurchaseOrder.SupplierDeliveryDate.Value.Date).TotalDays
                        : 0,
                    MaterialCode = x.MaterialItem != null ? x.MaterialItem.Code : "",
                    MaterialName = x.MaterialItem != null ? x.MaterialItem.Name : x.Description,
                    x.Quantity, x.ReceivedQuantity, x.PendingQuantity, x.UnitPrice
                })
                .OrderByDescending(x => x.DaysLate).ToListAsync();
            return Results.Ok(items);
        });

        // Report: Auxiliar recepciones por OC
        g.MapGet("/receipt-detail", async (NanchesoftDbContext db, Guid? companyId, Guid? orderId) =>
        {
            var query = db.PurchaseReceiptLines.AsNoTracking()
                .Include(x => x.PurchaseReceipt).ThenInclude(r => r!.Supplier)
                .Include(x => x.PurchaseReceipt).ThenInclude(r => r!.PurchaseOrder)
                .Include(x => x.MaterialItem)
                .Where(x => x.PurchaseReceipt != null);

            if (companyId.HasValue)
                query = query.Where(x => x.PurchaseReceipt != null && x.PurchaseReceipt.CompanyId == companyId);
            if (orderId.HasValue)
                query = query.Where(x => x.PurchaseReceipt != null && x.PurchaseReceipt.PurchaseOrderId == orderId);

            var items = await query.Select(x => new
            {
                ReceiptId = x.PurchaseReceipt != null ? x.PurchaseReceipt.Id : Guid.Empty,
                ReceiptFolio = x.PurchaseReceipt != null ? x.PurchaseReceipt.Folio : "",
                ReceiptType = x.PurchaseReceipt != null ? x.PurchaseReceipt.ReceiptType : "",
                ReceiptDate = x.PurchaseReceipt != null ? x.PurchaseReceipt.ReceiptDate : DateTime.MinValue,
                SupplierName = x.PurchaseReceipt != null && x.PurchaseReceipt.Supplier != null ? x.PurchaseReceipt.Supplier.Name : "",
                OrderFolio = x.PurchaseReceipt != null && x.PurchaseReceipt.PurchaseOrder != null ? x.PurchaseReceipt.PurchaseOrder.Folio : "",
                SupplierDocument = x.PurchaseReceipt != null ? x.PurchaseReceipt.SupplierDocumentNumber : "",
                MaterialCode = x.MaterialItem != null ? x.MaterialItem.Code : "",
                MaterialName = x.MaterialItem != null ? x.MaterialItem.Name : x.Description,
                x.Quantity, x.UnitPrice, x.LineTotal,
                PaymentStatus = x.PurchaseReceipt != null ? x.PurchaseReceipt.PaymentStatus : ""
            }).OrderByDescending(x => x.ReceiptDate).ToListAsync();
            return Results.Ok(items);
        });

        // Report: Diferencias OC vs Recepción
        g.MapGet("/differences", async (NanchesoftDbContext db, Guid? companyId, DateTime? from, DateTime? to) =>
        {
            var query = db.PurchaseReceiptDiffs.AsNoTracking()
                .Include(x => x.Lines).ThenInclude(l => l.MaterialItem)
                .Include(x => x.PurchaseOrder)
                .Include(x => x.PurchaseReceipt).ThenInclude(r => r!.Supplier)
                .AsQueryable();
            if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId);
            if (from.HasValue) query = query.Where(x => x.CreatedAt >= from);
            if (to.HasValue) query = query.Where(x => x.CreatedAt <= to);

            var items = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
            return Results.Ok(items);
        });

        // Dashboard summary
        g.MapGet("/dashboard", async (NanchesoftDbContext db, Guid? companyId) =>
        {
            var baseOrderQ = db.PurchaseOrders.AsNoTracking().Where(x => x.OrderType == "materials");
            var baseReceiptQ = db.PurchaseReceipts.AsNoTracking();
            if (companyId.HasValue)
            {
                baseOrderQ = baseOrderQ.Where(x => x.CompanyId == companyId);
                baseReceiptQ = baseReceiptQ.Where(x => x.CompanyId == companyId);
            }

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var pendingOrders = await baseOrderQ.CountAsync(x => x.Status == "authorized" || x.Status == "partially_received");
            var draftOrders = await baseOrderQ.CountAsync(x => x.Status == "draft");
            var monthPurchased = await baseReceiptQ.Where(x => x.ReceiptDate >= monthStart && x.Status == "authorized")
                .SumAsync(x => (decimal?)x.Total) ?? 0m;
            var pendingPayment = await baseReceiptQ.CountAsync(x => x.PaymentStatus == "pending" && x.Status == "authorized");
            var partialPayment = await baseReceiptQ.CountAsync(x => x.PaymentStatus == "partial");
            var withDiffs = await baseReceiptQ.CountAsync(x => x.HasDifferences && !x.DifferencesAuthorized);

            var topSuppliers = await (companyId.HasValue
                ? db.PurchaseReceipts.AsNoTracking().Where(x => x.CompanyId == companyId && x.ReceiptDate >= monthStart)
                : db.PurchaseReceipts.AsNoTracking().Where(x => x.ReceiptDate >= monthStart))
                .GroupBy(x => x.SupplierId)
                .Select(g => new { SupplierId = g.Key, Total = g.Sum(x => x.Total) })
                .OrderByDescending(x => x.Total).Take(5)
                .Join(db.Suppliers, x => x.SupplierId, s => s.Id, (x, s) => new { s.Name, x.Total })
                .ToListAsync();

            return Results.Ok(new
            {
                pendingOrders, draftOrders, monthPurchased, pendingPayment, partialPayment, withDiffs, topSuppliers
            });
        });
    }

    // ══════════════════════════════════════════════════════════
    // LOOKUPS
    // ══════════════════════════════════════════════════════════
    private static void MapLookupsEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/mat/lookups", async (NanchesoftDbContext db, Guid? companyId) =>
        {
            var suppliers = await db.Suppliers.AsNoTracking()
                .Where(x => !companyId.HasValue || x.CompanyId == companyId)
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.Code, x.PaymentTermDays, x.PreferredPaymentMethod })
                .ToListAsync();

            var materials = await db.MaterialItems.AsNoTracking()
                .Where(x => x.IsActive && (!companyId.HasValue || x.CompanyId == companyId))
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.Code, x.AuthorizedCost, x.PurchaseUnitId })
                .ToListAsync();

            var warehouses = await db.Warehouses.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name })
                .ToListAsync();

            var units = await db.Units.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name })
                .ToListAsync();

            var taxes = await db.Taxes.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name })
                .ToListAsync();

            var currencies = await db.Currencies.AsNoTracking()
                .OrderBy(x => x.Code)
                .Select(x => new { x.Id, x.Code, x.Name })
                .ToListAsync();

            var bankAccounts = await db.BankAccounts.AsNoTracking()
                .OrderBy(x => x.AccountNumber)
                .Select(x => new { x.Id, x.AccountNumber })
                .ToListAsync();

            var series = await db.DocumentSeries.AsNoTracking()
                .Where(x => !companyId.HasValue || x.CompanyId == companyId)
                .Where(x => x.DocumentType.StartsWith("purchase_") || x.DocumentType.StartsWith("OC"))
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Code, x.Name, x.Prefix, x.DocumentType, x.CurrentNumber })
                .ToListAsync();

            return Results.Ok(new { suppliers, materials, warehouses, units, taxes, currencies, bankAccounts, series });
        }).WithTags("MatLookups");
    }

    // ══════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════

    private static async Task<string> NextFolioAsync(NanchesoftDbContext db, Guid companyId, string seriesCode, string prefix)
    {
        var series = await db.DocumentSeries.FirstOrDefaultAsync(
            s => s.CompanyId == companyId && s.Code == seriesCode);
        if (series is null)
        {
            // Auto-create the series if missing
            var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId);
            series = new DocumentSeries
            {
                TenantId = company?.TenantId ?? Guid.Empty, CompanyId = companyId,
                Code = seriesCode, Name = seriesCode, Prefix = prefix,
                DocumentType = seriesCode.ToLower().Replace("-", "_"),
                CurrentNumber = 0, NumberLength = 5, IsDefault = false,
                CreatedBy = "auto"
            };
            db.DocumentSeries.Add(series);
        }
        series.CurrentNumber++;
        var number = series.CurrentNumber.ToString().PadLeft(series.NumberLength, '0');
        return $"{series.Prefix}-{number}";
    }

    private static void ApplyMatOrderLines(PurchaseOrder order, List<MatOrderLineRequest> lines)
    {
        order.Lines.Clear();
        var seq = 1;
        foreach (var l in lines.Where(x => x.MaterialItemId.HasValue || !string.IsNullOrWhiteSpace(x.Description)))
        {
            var qty = Math.Max(0, l.Quantity);
            order.Lines.Add(new PurchaseOrderLine
            {
                LineNumber = seq++,
                MaterialItemId = l.MaterialItemId,
                ItemId = l.ItemId,
                UnitId = l.UnitId,
                TaxId = l.TaxId,
                Description = (l.Description ?? string.Empty).Trim(),
                Quantity = qty,
                ReceivedQuantity = 0,
                PendingQuantity = qty,
                UnitPrice = Math.Max(0, l.UnitPrice),
                DiscountAmount = Math.Max(0, l.DiscountAmount),
                TaxAmount = Math.Max(0, l.TaxAmount),
                LineTotal = qty * Math.Max(0, l.UnitPrice) - Math.Max(0, l.DiscountAmount) + Math.Max(0, l.TaxAmount),
                Notes = (l.Notes ?? string.Empty).Trim(),
                CreatedBy = "web-api"
            });
        }
    }

    private static void RecalcOrderTotals(PurchaseOrder order)
    {
        order.Subtotal = order.Lines.Sum(l => l.Quantity * l.UnitPrice - l.DiscountAmount);
        order.TaxAmount = order.Lines.Sum(l => l.TaxAmount);
        order.Total = order.Subtotal + order.TaxAmount;
        foreach (var l in order.Lines)
            l.LineTotal = l.Quantity * l.UnitPrice - l.DiscountAmount + l.TaxAmount;
    }

    private static void ApplyReceiptLines(PurchaseReceipt receipt, List<MatReceiptLineRequest> lines)
    {
        receipt.Lines.Clear();
        var seq = 1;
        foreach (var l in lines.Where(x => x.MaterialItemId.HasValue || !string.IsNullOrWhiteSpace(x.Description)))
        {
            var qty = Math.Max(0, l.Quantity);
            receipt.Lines.Add(new PurchaseReceiptLine
            {
                LineNumber = seq++,
                PurchaseOrderLineId = l.PurchaseOrderLineId,
                MaterialItemId = l.MaterialItemId,
                ItemId = l.ItemId,
                UnitId = l.UnitId,
                TaxId = l.TaxId,
                Description = (l.Description ?? string.Empty).Trim(),
                Quantity = qty,
                UnitPrice = Math.Max(0, l.UnitPrice),
                DiscountAmount = Math.Max(0, l.DiscountAmount),
                TaxAmount = Math.Max(0, l.TaxAmount),
                LineTotal = qty * Math.Max(0, l.UnitPrice) - Math.Max(0, l.DiscountAmount) + Math.Max(0, l.TaxAmount),
                OrderedQuantity = Math.Max(0, l.OrderedQuantity),
                OrderedUnitPrice = Math.Max(0, l.OrderedUnitPrice),
                Notes = (l.Notes ?? string.Empty).Trim(),
                CreatedBy = "web-api"
            });
        }
    }

    private static void RecalcReceiptTotals(PurchaseReceipt receipt)
    {
        receipt.Subtotal = receipt.Lines.Sum(l => l.Quantity * l.UnitPrice - l.DiscountAmount);
        receipt.TaxAmount = receipt.Lines.Sum(l => l.TaxAmount);
        receipt.Total = receipt.Subtotal + receipt.TaxAmount;
        foreach (var l in receipt.Lines)
            l.LineTotal = l.Quantity * l.UnitPrice - l.DiscountAmount + l.TaxAmount;
    }

    private static async Task ImpactInventoryAsync(NanchesoftDbContext db, PurchaseReceipt receipt,
        Guid tenantId, string userName)
    {
        if (!receipt.WarehouseId.HasValue) return;

        foreach (var line in receipt.Lines.Where(l => l.MaterialItemId.HasValue && l.Quantity > 0))
        {
            var balance = await db.MaterialStockBalances.FirstOrDefaultAsync(b =>
                b.MaterialItemId == line.MaterialItemId && b.WarehouseId == receipt.WarehouseId);

            if (balance is null)
            {
                balance = new MaterialStockBalance
                {
                    TenantId = tenantId, CompanyId = receipt.CompanyId,
                    WarehouseId = receipt.WarehouseId.Value,
                    MaterialItemId = line.MaterialItemId!.Value,
                    QuantityOnHand = 0, QuantityReserved = 0, QuantityAvailable = 0,
                    AverageCost = 0, LastCost = 0,
                    CreatedBy = userName
                };
                db.MaterialStockBalances.Add(balance);
            }

            // Weighted average cost
            var prevQty = balance.QuantityOnHand;
            var prevCost = balance.AverageCost;
            var newQty = prevQty + line.Quantity;
            balance.AverageCost = newQty > 0
                ? (prevQty * prevCost + line.Quantity * line.UnitPrice) / newQty
                : line.UnitPrice;
            balance.LastCost = line.UnitPrice;
            balance.QuantityOnHand = newQty;
            balance.QuantityAvailable = newQty - balance.QuantityReserved;
            balance.LastMovementAt = DateTime.UtcNow;

            // Kardex
            db.MaterialInventoryMovements.Add(new MaterialInventoryMovement
            {
                TenantId = tenantId, CompanyId = receipt.CompanyId,
                WarehouseId = receipt.WarehouseId.Value,
                MaterialItemId = line.MaterialItemId!.Value,
                SupplierId = receipt.SupplierId,
                MovementType = "entry",
                DocumentType = receipt.ReceiptType == "invoice" ? "purchase_invoice" : "review_receipt",
                DocumentId = receipt.Id,
                DocumentFolio = receipt.Folio,
                MovementDate = receipt.ReceiptDate,
                QuantityIn = line.Quantity, QuantityOut = 0,
                BalanceAfter = balance.QuantityOnHand,
                UnitCost = line.UnitPrice,
                TotalCost = line.Quantity * line.UnitPrice,
                Notes = $"Recepción {receipt.Folio}",
                UserName = userName,
                CreatedBy = userName
            });
        }
    }

    private static async Task UpdateOrderReceivedAsync(NanchesoftDbContext db, Guid orderId)
    {
        var order = await db.PurchaseOrders.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == orderId);
        if (order is null) return;

        // Sum received qty from all receipts for each line
        var receiptLines = await db.PurchaseReceiptLines.AsNoTracking()
            .Where(x => x.PurchaseOrderLineId != null &&
                   db.PurchaseReceiptLines
                       .Where(rl => rl.PurchaseOrderLineId != null)
                       .Select(rl => rl.PurchaseOrderLineId)
                       .Contains(x.PurchaseOrderLineId))
            .ToListAsync();

        foreach (var orderLine in order.Lines)
        {
            var received = await db.PurchaseReceiptLines.AsNoTracking()
                .Where(x => x.PurchaseOrderLineId == orderLine.Id)
                .SumAsync(x => (decimal?)x.Quantity) ?? 0m;
            orderLine.ReceivedQuantity = received;
            orderLine.PendingQuantity = Math.Max(0, orderLine.Quantity - received);
        }

        order.ReceivedTotal = order.Lines.Sum(l => l.ReceivedQuantity * l.UnitPrice);

        var totalPending = order.Lines.Sum(l => l.PendingQuantity);
        order.Status = totalPending <= 0 ? "received" : "partially_received";
    }

    private static List<DiffItem> BuildDiffs(PurchaseReceipt receipt)
    {
        var diffs = new List<DiffItem>();
        if (receipt.PurchaseOrder is null) return diffs;

        var orderLines = receipt.PurchaseOrder.Lines.ToList();
        var receiptLines = receipt.Lines.ToList();

        foreach (var rl in receiptLines)
        {
            var ol = orderLines.FirstOrDefault(o => o.Id == rl.PurchaseOrderLineId);
            if (ol is null)
            {
                // Material adicional no pedido
                diffs.Add(new DiffItem
                {
                    MaterialItemId = rl.MaterialItemId,
                    MaterialName = rl.MaterialItem?.Name ?? rl.Description,
                    DiffType = "material_added",
                    ReceivedQuantity = rl.Quantity, ReceivedUnitPrice = rl.UnitPrice,
                    ReceivedTotal = rl.LineTotal
                });
            }
            else
            {
                if (Math.Abs(rl.Quantity - ol.Quantity) > 0.001m)
                    diffs.Add(new DiffItem
                    {
                        MaterialItemId = rl.MaterialItemId ?? ol.MaterialItemId,
                        MaterialName = rl.MaterialItem?.Name ?? rl.Description,
                        DiffType = "quantity_diff",
                        OrderedQuantity = ol.Quantity, ReceivedQuantity = rl.Quantity,
                        QuantityDiff = rl.Quantity - ol.Quantity,
                        OrderedUnitPrice = ol.UnitPrice, ReceivedUnitPrice = rl.UnitPrice,
                        OrderedTotal = ol.LineTotal, ReceivedTotal = rl.LineTotal,
                        TotalDiff = rl.LineTotal - ol.LineTotal
                    });

                if (Math.Abs(rl.UnitPrice - ol.UnitPrice) > 0.001m)
                    diffs.Add(new DiffItem
                    {
                        MaterialItemId = rl.MaterialItemId ?? ol.MaterialItemId,
                        MaterialName = rl.MaterialItem?.Name ?? rl.Description,
                        DiffType = "cost_diff",
                        OrderedQuantity = ol.Quantity, ReceivedQuantity = rl.Quantity,
                        OrderedUnitPrice = ol.UnitPrice, ReceivedUnitPrice = rl.UnitPrice,
                        PriceDiff = rl.UnitPrice - ol.UnitPrice,
                        OrderedTotal = ol.LineTotal, ReceivedTotal = rl.LineTotal,
                        TotalDiff = rl.LineTotal - ol.LineTotal
                    });
            }
        }

        // Materiales faltantes (en OC pero no en recepción)
        foreach (var ol in orderLines)
        {
            var found = receiptLines.Any(rl => rl.PurchaseOrderLineId == ol.Id);
            if (!found)
                diffs.Add(new DiffItem
                {
                    MaterialItemId = ol.MaterialItemId,
                    MaterialName = ol.MaterialItem?.Name ?? ol.Description,
                    DiffType = "material_missing",
                    OrderedQuantity = ol.Quantity, OrderedUnitPrice = ol.UnitPrice,
                    OrderedTotal = ol.LineTotal
                });
        }

        return diffs;
    }

    private sealed record DiffItem
    {
        public Guid? MaterialItemId { get; init; }
        public string MaterialName { get; init; } = string.Empty;
        public string DiffType { get; init; } = string.Empty;
        public decimal OrderedQuantity { get; init; }
        public decimal ReceivedQuantity { get; init; }
        public decimal QuantityDiff { get; init; }
        public decimal OrderedUnitPrice { get; init; }
        public decimal ReceivedUnitPrice { get; init; }
        public decimal PriceDiff { get; init; }
        public decimal OrderedTotal { get; init; }
        public decimal ReceivedTotal { get; init; }
        public decimal TotalDiff { get; init; }
    }

    // ══════════════════════════════════════════════════════════
    // REQUEST MODELS
    // ══════════════════════════════════════════════════════════

    private sealed record SupplierUpsertRequest(
        Guid? CompanyId, string? Code, string? Name, string? ShortName, string? LegalName,
        string? Classification, string? TaxId, string? FiscalRegime, string? CfdiUse,
        string? Address, string? PostalCode, string? Colony, string? City, string? State, string? Country,
        string? Email, string? Phone, string? Phone2, string? Fax,
        string? SalesContact, string? CollectionContact,
        int PaymentTermDays, decimal CreditLimit, string? AccountingAccount,
        decimal DiscountPromptPayment, decimal Discount1, decimal Discount2, decimal Discount3, decimal Discount4,
        string? PreferredPaymentMethod, string? BankClabe, string? BankName, string? BankAccount,
        string? Notes, bool IsActive = true);

    private sealed record MatOrderUpsertRequest(
        Guid? CompanyId, Guid? BranchId, Guid? SupplierId, Guid? CurrencyId, Guid? WarehouseId,
        DateTime? OrderDate, DateTime? SupplierDeliveryDate,
        decimal ExchangeRate, int PaymentTermDays, string? BuyerName, string? Notes,
        List<MatOrderLineRequest> Lines);

    private sealed record MatOrderLineRequest(
        Guid? MaterialItemId, Guid? ItemId, Guid? UnitId, Guid? TaxId,
        string? Description, decimal Quantity, decimal UnitPrice,
        decimal DiscountAmount, decimal TaxAmount, string? Notes);

    private sealed record MatReceiptUpsertRequest(
        Guid? CompanyId, Guid? BranchId, Guid? SupplierId, Guid? WarehouseId,
        Guid? PurchaseOrderId, string? ReceiptType,
        DateTime? ReceiptDate, string? SupplierDocumentNumber, DateTime? SupplierDocumentDate,
        string? Notes, List<MatReceiptLineRequest> Lines);

    private sealed record MatReceiptLineRequest(
        Guid? PurchaseOrderLineId, Guid? MaterialItemId, Guid? ItemId, Guid? UnitId, Guid? TaxId,
        string? Description, decimal Quantity, decimal UnitPrice,
        decimal DiscountAmount, decimal TaxAmount, decimal OrderedQuantity, decimal OrderedUnitPrice,
        string? Notes);

    private sealed record MatPaymentRequest(
        Guid PurchaseReceiptId, Guid? BankAccountId, DateTime? PaymentDate,
        string? PaymentMethod, decimal Amount, string? Reference, string? Notes);

    private sealed record AuthorizeRequest(string? UserName, string? Notes);
    private sealed record AuthorizeDiffRequest(string? UserName, bool AuthorizeDifferences, string? Notes);
    private sealed record RejectRequest(string? UserName, string? Reason);
    private sealed record CancelRequest(string? UserName, string? Reason);
    private sealed record ConvertToInvoiceRequest(string? UserName, DateTime? InvoiceDate, string? SupplierInvoiceNumber);
}

using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class PurchasesSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        const string seedUser = "seed-sprint5";

        var tenant = await dbContext.Tenants.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var company = await dbContext.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (tenant is null || company is null)
            return;

        var branch = await dbContext.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var warehouse = await dbContext.Warehouses.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var supplier = await dbContext.Suppliers.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var currency = await dbContext.Currencies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.Code == "MXN")
                      ?? await dbContext.Currencies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var item = await dbContext.Items.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var unit = await dbContext.Units.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var tax = await dbContext.Taxes.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();

        if (branch is null || warehouse is null || supplier is null || item is null)
            return;

        if (!await dbContext.PurchaseRequisitions.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "REQ-0001"))
        {
            dbContext.PurchaseRequisitions.Add(new PurchaseRequisition
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                Folio = "REQ-0001",
                RequisitionDate = DateTime.UtcNow.Date,
                RequestedByName = "Invitado",
                Status = "approved",
                Notes = "Requisición demo sprint 5.",
                ApprovedAt = DateTime.UtcNow,
                CreatedBy = seedUser,
                Lines =
                [
                    new PurchaseRequisitionLine
                    {
                        LineNumber = 1,
                        ItemId = item.Id,
                        UnitId = unit?.Id,
                        Description = item.Name,
                        Quantity = 5,
                        Notes = "Compra inicial",
                        CreatedBy = seedUser
                    }
                ]
            });
            await dbContext.SaveChangesAsync();
        }

        var requisition = await dbContext.PurchaseRequisitions.FirstAsync(x => x.CompanyId == company.Id && x.Folio == "REQ-0001");

        if (!await dbContext.PurchaseOrders.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "OC-0001"))
        {
            dbContext.PurchaseOrders.Add(new PurchaseOrder
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                SupplierId = supplier.Id,
                CurrencyId = currency?.Id,
                PurchaseRequisitionId = requisition.Id,
                Folio = "OC-0001",
                OrderDate = DateTime.UtcNow.Date,
                Status = "approved",
                ExchangeRate = 1m,
                PaymentTermDays = supplier.PaymentTermDays,
                Subtotal = 45000m,
                TaxAmount = 7200m,
                Total = 52200m,
                Notes = "Orden demo sprint 5.",
                ApprovedAt = DateTime.UtcNow,
                CreatedBy = seedUser,
                Lines =
                [
                    new PurchaseOrderLine
                    {
                        LineNumber = 1,
                        ItemId = item.Id,
                        UnitId = unit?.Id,
                        TaxId = tax?.Id,
                        Description = item.Name,
                        Quantity = 5,
                        ReceivedQuantity = 2,
                        PendingQuantity = 3,
                        UnitPrice = 9000m,
                        DiscountAmount = 0m,
                        TaxAmount = 7200m,
                        LineTotal = 52200m,
                        CreatedBy = seedUser
                    }
                ]
            });
            await dbContext.SaveChangesAsync();
        }

        var order = await dbContext.PurchaseOrders.FirstAsync(x => x.CompanyId == company.Id && x.Folio == "OC-0001");
        var orderLine = await dbContext.PurchaseOrderLines.FirstAsync(x => x.PurchaseOrderId == order.Id);

        if (!await dbContext.PurchaseReceipts.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "REC-0001"))
        {
            dbContext.PurchaseReceipts.Add(new PurchaseReceipt
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                WarehouseId = warehouse.Id,
                SupplierId = supplier.Id,
                PurchaseOrderId = order.Id,
                Folio = "REC-0001",
                ReceiptDate = DateTime.UtcNow.Date,
                Status = "posted",
                Notes = "Recepción parcial demo.",
                PostedAt = DateTime.UtcNow,
                CreatedBy = seedUser,
                Lines =
                [
                    new PurchaseReceiptLine
                    {
                        LineNumber = 1,
                        PurchaseOrderLineId = orderLine.Id,
                        ItemId = item.Id,
                        UnitId = unit?.Id,
                        Description = item.Name,
                        Quantity = 2,
                        CreatedBy = seedUser
                    }
                ]
            });
            await dbContext.SaveChangesAsync();
        }

        var receipt = await dbContext.PurchaseReceipts.FirstAsync(x => x.CompanyId == company.Id && x.Folio == "REC-0001");
        var receiptLine = await dbContext.PurchaseReceiptLines.FirstAsync(x => x.PurchaseReceiptId == receipt.Id);

        if (!await dbContext.PurchaseInvoices.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "FACP-0001"))
        {
            dbContext.PurchaseInvoices.Add(new PurchaseInvoice
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                SupplierId = supplier.Id,
                PurchaseOrderId = order.Id,
                PurchaseReceiptId = receipt.Id,
                CurrencyId = currency?.Id,
                Folio = "FACP-0001",
                SupplierInvoiceFolio = "A-1550",
                InvoiceDate = DateTime.UtcNow.Date,
                Status = "posted",
                ExchangeRate = 1m,
                Subtotal = 18000m,
                TaxAmount = 2880m,
                Total = 20880m,
                Notes = "Factura proveedor demo.",
                ApprovedAt = DateTime.UtcNow,
                PostedAt = DateTime.UtcNow,
                CreatedBy = seedUser,
                Lines =
                [
                    new PurchaseInvoiceLine
                    {
                        LineNumber = 1,
                        ItemId = item.Id,
                        UnitId = unit?.Id,
                        TaxId = tax?.Id,
                        Description = item.Name,
                        Quantity = 2,
                        UnitPrice = 9000m,
                        DiscountAmount = 0m,
                        TaxAmount = 2880m,
                        LineTotal = 20880m,
                        CreatedBy = seedUser
                    }
                ]
            });
            await dbContext.SaveChangesAsync();
        }

        var invoice = await dbContext.PurchaseInvoices.FirstAsync(x => x.CompanyId == company.Id && x.Folio == "FACP-0001");

        if (!await dbContext.PurchaseReturns.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "DEVP-0001"))
        {
            dbContext.PurchaseReturns.Add(new PurchaseReturn
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                WarehouseId = warehouse.Id,
                SupplierId = supplier.Id,
                PurchaseReceiptId = receipt.Id,
                PurchaseInvoiceId = invoice.Id,
                Folio = "DEVP-0001",
                ReturnDate = DateTime.UtcNow.Date,
                Reason = "Material con defecto demo.",
                Status = "approved",
                Subtotal = 9000m,
                TaxAmount = 1440m,
                Total = 10440m,
                ApprovedAt = DateTime.UtcNow,
                CreatedBy = seedUser,
                Lines =
                [
                    new PurchaseReturnLine
                    {
                        LineNumber = 1,
                        SourceLineId = receiptLine.Id,
                        ItemId = item.Id,
                        UnitId = unit?.Id,
                        TaxId = tax?.Id,
                        Description = item.Name,
                        Quantity = 1,
                        UnitPrice = 9000m,
                        TaxAmount = 1440m,
                        LineTotal = 10440m,
                        CreatedBy = seedUser
                    }
                ]
            });
            await dbContext.SaveChangesAsync();
        }
    }
}

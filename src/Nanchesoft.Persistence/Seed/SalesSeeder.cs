using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class SalesSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        const string seedUser = "seed-sprint7";

        var tenant = await dbContext.Tenants.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var company = await dbContext.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (tenant is null || company is null)
            return;

        var branch = await dbContext.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var warehouse = await dbContext.Warehouses.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var customer = await dbContext.Customers.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var currency = await dbContext.Currencies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.Code == "MXN")
                      ?? await dbContext.Currencies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var item = await dbContext.Items.Where(x => x.CompanyId == company.Id && x.IsSaleItem).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var unit = await dbContext.Units.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var tax = await dbContext.Taxes.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();

        if (branch is null || warehouse is null || customer is null || item is null)
            return;

        if (!await dbContext.SalesQuotes.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "COT-0001"))
        {
            dbContext.SalesQuotes.Add(new SalesQuote
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                CustomerId = customer.Id,
                CurrencyId = currency?.Id,
                Folio = "COT-0001",
                QuoteDate = DateTime.UtcNow.Date,
                ValidUntil = DateTime.UtcNow.Date.AddDays(15),
                Status = "approved",
                ExchangeRate = 1m,
                Subtotal = 24000m,
                DiscountAmount = 0m,
                TaxAmount = 3840m,
                Total = 27840m,
                Notes = "Cotización demo sprint 7.",
                ApprovedAt = DateTime.UtcNow,
                CreatedBy = seedUser,
                Lines =
                [
                    new SalesQuoteLine
                    {
                        LineNumber = 1,
                        ItemId = item.Id,
                        UnitId = unit?.Id,
                        TaxId = tax?.Id,
                        Description = item.Name,
                        Quantity = 3,
                        UnitPrice = 8000m,
                        DiscountAmount = 0m,
                        TaxAmount = 3840m,
                        LineTotal = 27840m,
                        CreatedBy = seedUser
                    }
                ]
            });
            await dbContext.SaveChangesAsync();
        }

        var quote = await dbContext.SalesQuotes.FirstAsync(x => x.CompanyId == company.Id && x.Folio == "COT-0001");

        if (!await dbContext.SalesOrders.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "PED-0001"))
        {
            dbContext.SalesOrders.Add(new SalesOrder
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                CustomerId = customer.Id,
                CurrencyId = currency?.Id,
                SalesQuoteId = quote.Id,
                Folio = "PED-0001",
                OrderDate = DateTime.UtcNow.Date,
                Status = "approved",
                ExchangeRate = 1m,
                PaymentTermDays = customer.PaymentTermDays,
                Subtotal = 24000m,
                DiscountAmount = 0m,
                TaxAmount = 3840m,
                Total = 27840m,
                Notes = "Pedido demo sprint 7.",
                ApprovedAt = DateTime.UtcNow,
                CreatedBy = seedUser,
                Lines =
                [
                    new SalesOrderLine
                    {
                        LineNumber = 1,
                        ItemId = item.Id,
                        UnitId = unit?.Id,
                        TaxId = tax?.Id,
                        Description = item.Name,
                        Quantity = 3,
                        ShippedQuantity = 2,
                        InvoicedQuantity = 1,
                        PendingQuantity = 1,
                        UnitPrice = 8000m,
                        DiscountAmount = 0m,
                        TaxAmount = 3840m,
                        LineTotal = 27840m,
                        CreatedBy = seedUser
                    }
                ]
            });
            await dbContext.SaveChangesAsync();
        }

        var order = await dbContext.SalesOrders.FirstAsync(x => x.CompanyId == company.Id && x.Folio == "PED-0001");
        var orderLine = await dbContext.SalesOrderLines.FirstAsync(x => x.SalesOrderId == order.Id);

        if (!await dbContext.SalesShipments.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "REM-0001"))
        {
            dbContext.SalesShipments.Add(new SalesShipment
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                WarehouseId = warehouse.Id,
                CustomerId = customer.Id,
                SalesOrderId = order.Id,
                Folio = "REM-0001",
                ShipmentDate = DateTime.UtcNow.Date,
                Status = "posted",
                Notes = "Remisión parcial demo.",
                ApprovedAt = DateTime.UtcNow,
                PostedAt = DateTime.UtcNow,
                CreatedBy = seedUser,
                Lines =
                [
                    new SalesShipmentLine
                    {
                        LineNumber = 1,
                        SalesOrderLineId = orderLine.Id,
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

        var shipment = await dbContext.SalesShipments.FirstAsync(x => x.CompanyId == company.Id && x.Folio == "REM-0001");
        var shipmentLine = await dbContext.SalesShipmentLines.FirstAsync(x => x.SalesShipmentId == shipment.Id);

        if (!await dbContext.SalesInvoices.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "FACV-0001"))
        {
            dbContext.SalesInvoices.Add(new SalesInvoice
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                CustomerId = customer.Id,
                SalesOrderId = order.Id,
                SalesShipmentId = shipment.Id,
                CurrencyId = currency?.Id,
                Folio = "FACV-0001",
                InvoiceDate = DateTime.UtcNow.Date,
                Status = "posted",
                ExchangeRate = 1m,
                Subtotal = 16000m,
                DiscountAmount = 0m,
                TaxAmount = 2560m,
                Total = 18560m,
                Notes = "Factura venta demo.",
                ApprovedAt = DateTime.UtcNow,
                PostedAt = DateTime.UtcNow,
                CreatedBy = seedUser,
                Lines =
                [
                    new SalesInvoiceLine
                    {
                        LineNumber = 1,
                        ItemId = item.Id,
                        UnitId = unit?.Id,
                        TaxId = tax?.Id,
                        Description = item.Name,
                        Quantity = 2,
                        UnitPrice = 8000m,
                        DiscountAmount = 0m,
                        TaxAmount = 2560m,
                        LineTotal = 18560m,
                        CreatedBy = seedUser
                    }
                ]
            });
            await dbContext.SaveChangesAsync();
        }

        var invoice = await dbContext.SalesInvoices.FirstAsync(x => x.CompanyId == company.Id && x.Folio == "FACV-0001");
        var invoiceLine = await dbContext.SalesInvoiceLines.FirstAsync(x => x.SalesInvoiceId == invoice.Id);

        if (!await dbContext.SalesReturns.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "DEVV-0001"))
        {
            dbContext.SalesReturns.Add(new SalesReturn
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                WarehouseId = warehouse.Id,
                CustomerId = customer.Id,
                SalesShipmentId = shipment.Id,
                SalesInvoiceId = invoice.Id,
                Folio = "DEVV-0001",
                ReturnDate = DateTime.UtcNow.Date,
                Reason = "Devolución demo por empaque.",
                Status = "approved",
                Subtotal = 8000m,
                TaxAmount = 1280m,
                Total = 9280m,
                ApprovedAt = DateTime.UtcNow,
                CreatedBy = seedUser,
                Lines =
                [
                    new SalesReturnLine
                    {
                        LineNumber = 1,
                        SourceLineId = shipmentLine.Id,
                        ItemId = item.Id,
                        UnitId = unit?.Id,
                        TaxId = tax?.Id,
                        Description = item.Name,
                        Quantity = 1,
                        UnitPrice = 8000m,
                        TaxAmount = 1280m,
                        LineTotal = 9280m,
                        CreatedBy = seedUser
                    }
                ]
            });
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.CreditNotes.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "NC-0001"))
        {
            dbContext.CreditNotes.Add(new CreditNote
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                CustomerId = customer.Id,
                SalesInvoiceId = invoice.Id,
                Folio = "NC-0001",
                CreditNoteDate = DateTime.UtcNow.Date,
                Reason = "Bonificación comercial demo.",
                Status = "approved",
                Subtotal = 4000m,
                TaxAmount = 640m,
                Total = 4640m,
                ApprovedAt = DateTime.UtcNow,
                CreatedBy = seedUser,
                Lines =
                [
                    new CreditNoteLine
                    {
                        LineNumber = 1,
                        SalesInvoiceLineId = invoiceLine.Id,
                        ItemId = item.Id,
                        UnitId = unit?.Id,
                        TaxId = tax?.Id,
                        Description = item.Name,
                        Quantity = 0.5m,
                        UnitPrice = 8000m,
                        TaxAmount = 640m,
                        LineTotal = 4640m,
                        CreatedBy = seedUser
                    }
                ]
            });
            await dbContext.SaveChangesAsync();
        }
    }
}

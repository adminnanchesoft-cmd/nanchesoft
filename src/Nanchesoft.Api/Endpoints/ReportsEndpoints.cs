using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ReportsEndpoints
{
    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports");

        group.MapGet("/operational/summary", async (NanchesoftDbContext db) =>
        {
            var since = DateTime.UtcNow.Date.AddDays(-30);

            var purchaseOrdersOpen = await db.PurchaseOrders.CountAsync(x => x.IsActive && x.Status != "cancelled" && x.Status != "closed");
            var purchaseInvoicesPeriod = await db.PurchaseInvoices.Where(x => x.IsActive && x.InvoiceDate >= since).SumAsync(x => (decimal?)x.Total) ?? 0m;
            var salesOrdersOpen = await db.SalesOrders.CountAsync(x => x.IsActive && x.Status != "cancelled" && x.Status != "closed");
            var salesInvoicesPeriod = await db.SalesInvoices.Where(x => x.IsActive && x.InvoiceDate >= since).SumAsync(x => (decimal?)x.Total) ?? 0m;
            var stockRows = await db.StockBalances.CountAsync(x => x.IsActive);
            var stockOnHand = await db.StockBalances.SumAsync(x => (decimal?)x.QuantityOnHand) ?? 0m;
            var stockValue = await db.StockBalances.SumAsync(x => (decimal?)(x.QuantityOnHand * x.LastCost)) ?? 0m;
            var cashBalance = await db.CashAccounts.SumAsync(x => (decimal?)x.CurrentBalance) ?? 0m;
            var bankBalance = await db.BankAccounts.SumAsync(x => (decimal?)x.CurrentBalance) ?? 0m;

            return Results.Ok(new ReportsOperationalSummaryDto
            {
                PurchaseOrdersOpen = purchaseOrdersOpen,
                PurchaseInvoicesPeriod = purchaseInvoicesPeriod,
                SalesOrdersOpen = salesOrdersOpen,
                SalesInvoicesPeriod = salesInvoicesPeriod,
                StockRows = stockRows,
                StockOnHand = stockOnHand,
                StockValue = stockValue,
                CashBalance = cashBalance,
                BankBalance = bankBalance,
                CombinedTreasuryBalance = cashBalance + bankBalance
            });
        });

        group.MapGet("/operational/purchases", async (NanchesoftDbContext db) =>
            Results.Ok(await db.PurchaseInvoices
                .AsNoTracking()
                .OrderByDescending(x => x.InvoiceDate)
                .Take(250)
                .Select(x => new ReportsPurchaseRowDto
                {
                    Id = x.Id,
                    Folio = x.Folio,
                    Date = x.InvoiceDate,
                    Status = x.Status,
                    PartyName = x.Supplier != null ? x.Supplier.Name : string.Empty,
                    Subtotal = x.Subtotal,
                    TaxAmount = x.TaxAmount,
                    Total = x.Total,
                    Reference = x.SupplierInvoiceFolio ?? string.Empty
                })
                .ToListAsync()));

        group.MapGet("/operational/sales", async (NanchesoftDbContext db) =>
            Results.Ok(await db.SalesInvoices
                .AsNoTracking()
                .OrderByDescending(x => x.InvoiceDate)
                .Take(250)
                .Select(x => new ReportsSalesRowDto
                {
                    Id = x.Id,
                    Folio = x.Folio,
                    Date = x.InvoiceDate,
                    Status = x.Status,
                    PartyName = x.Customer != null ? x.Customer.Name : string.Empty,
                    Subtotal = x.Subtotal,
                    TaxAmount = x.TaxAmount,
                    Total = x.Total,
                    Reference = x.Notes ?? string.Empty
                })
                .ToListAsync()));

        group.MapGet("/operational/inventory", async (NanchesoftDbContext db) =>
            Results.Ok(await BuildInventoryRowsQuery(db)
                .OrderByDescending(x => x.UpdatedAt)
                .Take(250)
                .Select(x => new ReportsInventoryRowDto
                {
                    Id = x.Id,
                    WarehouseName = x.WarehouseName,
                    ItemCode = x.ItemCode,
                    ItemName = x.ItemName,
                    QuantityOnHand = x.QuantityOnHand,
                    QuantityAvailable = x.QuantityAvailable,
                    AverageCost = x.AverageCost,
                    LastCost = x.LastCost,
                    ExtendedValue = x.QuantityOnHand * x.LastCost
                })
                .ToListAsync()));

        group.MapGet("/operational/treasury", async (NanchesoftDbContext db) =>
        {
            var cash = await db.CashAccounts.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new ReportsTreasuryRowDto
                {
                    Id = x.Id,
                    AccountType = "Caja",
                    Code = x.Code,
                    Name = x.Name,
                    Status = x.Status,
                    CurrencyId = x.CurrencyId,
                    Balance = x.CurrentBalance,
                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,
                    Extra = string.Empty
                })
                .ToListAsync();

            var bank = await db.BankAccounts.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new ReportsTreasuryRowDto
                {
                    Id = x.Id,
                    AccountType = "Banco",
                    Code = x.Code,
                    Name = x.Name,
                    Status = x.Status,
                    CurrencyId = x.CurrencyId,
                    Balance = x.CurrentBalance,
                    CompanyId = x.CompanyId,
                    BranchId = null,
                    Extra = x.AccountNumber
                })
                .ToListAsync();

            return Results.Ok(cash.Concat(bank).OrderBy(x => x.AccountType).ThenBy(x => x.Name).ToList());
        });

        group.MapGet("/executive/summary", async (NanchesoftDbContext db) =>
        {
            var since = DateTime.UtcNow.Date.AddDays(-30);
            var activeCustomers = await db.Customers.CountAsync(x => x.IsActive);
            var activeSuppliers = await db.Suppliers.CountAsync(x => x.IsActive);
            var activeItems = await db.Items.CountAsync(x => x.IsActive);
            var purchaseTotal = await db.PurchaseInvoices.Where(x => x.IsActive && x.InvoiceDate >= since).SumAsync(x => (decimal?)x.Total) ?? 0m;
            var salesTotal = await db.SalesInvoices.Where(x => x.IsActive && x.InvoiceDate >= since).SumAsync(x => (decimal?)x.Total) ?? 0m;
            var grossMargin = salesTotal - purchaseTotal;
            var inventoryValue = await db.StockBalances.SumAsync(x => (decimal?)(x.QuantityOnHand * x.LastCost)) ?? 0m;
            var treasuryAvailable = (await db.CashAccounts.SumAsync(x => (decimal?)x.CurrentBalance) ?? 0m) +
                                    (await db.BankAccounts.SumAsync(x => (decimal?)x.CurrentBalance) ?? 0m);
            var pendingReconciliations = await db.Reconciliations.CountAsync(x => x.IsActive && x.Status != "closed");

            var recentSales = await db.SalesInvoices.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.InvoiceDate)
                .Take(10)
                .Select(x => new ExecutiveTopRowDto { Label = x.Folio, Secondary = x.Customer != null ? x.Customer.Name : string.Empty, Amount = x.Total })
                .ToListAsync();

            var recentPurchases = await db.PurchaseInvoices.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.InvoiceDate)
                .Take(10)
                .Select(x => new ExecutiveTopRowDto { Label = x.Folio, Secondary = x.Supplier != null ? x.Supplier.Name : string.Empty, Amount = x.Total })
                .ToListAsync();

            return Results.Ok(new ExecutiveSummaryDto
            {
                ActiveCustomers = activeCustomers,
                ActiveSuppliers = activeSuppliers,
                ActiveItems = activeItems,
                PurchaseTotal30Days = purchaseTotal,
                SalesTotal30Days = salesTotal,
                GrossMargin30Days = grossMargin,
                InventoryValue = inventoryValue,
                TreasuryAvailable = treasuryAvailable,
                PendingReconciliations = pendingReconciliations,
                RecentSales = recentSales,
                RecentPurchases = recentPurchases
            });
        });

        group.MapGet("/export/{reportKey}", async (string reportKey, NanchesoftDbContext db) =>
        {
            var csv = reportKey.ToLowerInvariant() switch
            {
                "purchases" => BuildCsv(await db.PurchaseInvoices.AsNoTracking()
                    .OrderByDescending(x => x.InvoiceDate)
                    .Take(500)
                    .Select(x => new ReportsPurchaseRowDto
                    {
                        Id = x.Id,
                        Folio = x.Folio,
                        Date = x.InvoiceDate,
                        Status = x.Status,
                        PartyName = x.Supplier != null ? x.Supplier.Name : string.Empty,
                        Subtotal = x.Subtotal,
                        TaxAmount = x.TaxAmount,
                        Total = x.Total,
                        Reference = x.SupplierInvoiceFolio ?? string.Empty
                    }).ToListAsync()),
                "sales" => BuildCsv(await db.SalesInvoices.AsNoTracking()
                    .OrderByDescending(x => x.InvoiceDate)
                    .Take(500)
                    .Select(x => new ReportsSalesRowDto
                    {
                        Id = x.Id,
                        Folio = x.Folio,
                        Date = x.InvoiceDate,
                        Status = x.Status,
                        PartyName = x.Customer != null ? x.Customer.Name : string.Empty,
                        Subtotal = x.Subtotal,
                        TaxAmount = x.TaxAmount,
                        Total = x.Total,
                        Reference = x.Notes ?? string.Empty
                    }).ToListAsync()),
                "inventory" => BuildCsv(await BuildInventoryRowsQuery(db)
                    .OrderByDescending(x => x.UpdatedAt)
                    .Take(500)
                    .Select(x => new ReportsInventoryRowDto
                    {
                        Id = x.Id,
                        WarehouseName = x.WarehouseName,
                        ItemCode = x.ItemCode,
                        ItemName = x.ItemName,
                        QuantityOnHand = x.QuantityOnHand,
                        QuantityAvailable = x.QuantityAvailable,
                        AverageCost = x.AverageCost,
                        LastCost = x.LastCost,
                        ExtendedValue = x.QuantityOnHand * x.LastCost
                    }).ToListAsync()),
                "treasury" => BuildCsv((await db.CashAccounts.AsNoTracking().Select(x => new ReportsTreasuryRowDto
                    {
                        Id = x.Id,
                        AccountType = "Caja",
                        Code = x.Code,
                        Name = x.Name,
                        Status = x.Status,
                        CurrencyId = x.CurrencyId,
                        Balance = x.CurrentBalance,
                        CompanyId = x.CompanyId,
                        BranchId = x.BranchId,
                        Extra = string.Empty
                    }).ToListAsync()).Concat(await db.BankAccounts.AsNoTracking().Select(x => new ReportsTreasuryRowDto
                    {
                        Id = x.Id,
                        AccountType = "Banco",
                        Code = x.Code,
                        Name = x.Name,
                        Status = x.Status,
                        CurrencyId = x.CurrencyId,
                        Balance = x.CurrentBalance,
                        CompanyId = x.CompanyId,
                        BranchId = null,
                        Extra = x.AccountNumber
                    }).ToListAsync()).ToList()),
                _ => null
            };

            return csv is null
                ? Results.NotFound(new { message = $"No existe el reporte '{reportKey}'." })
                : Results.Text(csv, "text/csv", Encoding.UTF8);
        });

        return app;
    }

    private static IQueryable<InventoryProjectionRow> BuildInventoryRowsQuery(NanchesoftDbContext db)
        => from stock in db.StockBalances.AsNoTracking()
           join warehouse in db.Warehouses.AsNoTracking() on stock.WarehouseId equals warehouse.Id into warehouseJoin
           from warehouse in warehouseJoin.DefaultIfEmpty()
           join item in db.Items.AsNoTracking() on stock.ItemId equals item.Id into itemJoin
           from item in itemJoin.DefaultIfEmpty()
           select new InventoryProjectionRow
           {
               Id = stock.Id,
               UpdatedAt = stock.UpdatedAt,
               WarehouseName = warehouse != null ? warehouse.Name : string.Empty,
               ItemCode = item != null ? item.Code : string.Empty,
               ItemName = item != null ? item.Name : string.Empty,
               QuantityOnHand = stock.QuantityOnHand,
               QuantityAvailable = stock.QuantityAvailable,
               AverageCost = stock.AverageCost,
               LastCost = stock.LastCost
           };

    private static string BuildCsv<T>(IReadOnlyCollection<T> rows)
    {
        if (rows.Count == 0)
        {
            return "Sin datos";
        }

        var properties = typeof(T).GetProperties();
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', properties.Select(x => Escape(x.Name))));

        foreach (var row in rows)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(row);
                return Escape(value switch
                {
                    null => string.Empty,
                    DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    decimal d => d.ToString("0.00", CultureInfo.InvariantCulture),
                    Guid g => g.ToString(),
                    _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
                });
            });
            sb.AppendLine(string.Join(',', values));
        }

        return sb.ToString();
    }

    private static string Escape(string value)
        => $"\"{value.Replace("\"", "\"\"")}\"";

    private sealed class InventoryProjectionRow
    {
        public Guid Id { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public decimal QuantityOnHand { get; set; }
        public decimal QuantityAvailable { get; set; }
        public decimal AverageCost { get; set; }
        public decimal LastCost { get; set; }
    }
}

public sealed class ReportsOperationalSummaryDto
{
    public int PurchaseOrdersOpen { get; set; }
    public decimal PurchaseInvoicesPeriod { get; set; }
    public int SalesOrdersOpen { get; set; }
    public decimal SalesInvoicesPeriod { get; set; }
    public int StockRows { get; set; }
    public decimal StockOnHand { get; set; }
    public decimal StockValue { get; set; }
    public decimal CashBalance { get; set; }
    public decimal BankBalance { get; set; }
    public decimal CombinedTreasuryBalance { get; set; }
}

public class ReportsPurchaseRowDto
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string Reference { get; set; } = string.Empty;
}

public sealed class ReportsSalesRowDto : ReportsPurchaseRowDto
{
}

public sealed class ReportsInventoryRowDto
{
    public Guid Id { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityAvailable { get; set; }
    public decimal AverageCost { get; set; }
    public decimal LastCost { get; set; }
    public decimal ExtendedValue { get; set; }
}

public sealed class ReportsTreasuryRowDto
{
    public Guid Id { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Extra { get; set; } = string.Empty;
}

public sealed class ExecutiveSummaryDto
{
    public int ActiveCustomers { get; set; }
    public int ActiveSuppliers { get; set; }
    public int ActiveItems { get; set; }
    public decimal PurchaseTotal30Days { get; set; }
    public decimal SalesTotal30Days { get; set; }
    public decimal GrossMargin30Days { get; set; }
    public decimal InventoryValue { get; set; }
    public decimal TreasuryAvailable { get; set; }
    public int PendingReconciliations { get; set; }
    public List<ExecutiveTopRowDto> RecentSales { get; set; } = new();
    public List<ExecutiveTopRowDto> RecentPurchases { get; set; } = new();
}

public sealed class ExecutiveTopRowDto
{
    public string Label { get; set; } = string.Empty;
    public string Secondary { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

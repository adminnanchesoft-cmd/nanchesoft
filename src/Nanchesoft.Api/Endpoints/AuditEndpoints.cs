using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/audit").WithTags("Audit");

        group.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.AuditLogs
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Take(100)
                .Select(x => new { x.Id, x.CreatedAt, x.Module, x.EntityName, x.Action })
                .ToListAsync()));

        group.MapGet("/change-log", async (NanchesoftDbContext db) =>
            Results.Ok(await db.AuditLogs
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Take(500)
                .Select(x => new AuditChangeLogRowDto
                {
                    AuditLogId = x.Id,
                    CreatedAt = x.CreatedAt,
                    Module = x.Module,
                    EntityName = x.EntityName,
                    EntityId = x.EntityId,
                    Action = x.Action,
                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,
                    UserId = x.UserId,
                    IpAddress = x.IpAddress,
                    OldValues = x.OldValues,
                    NewValues = x.NewValues
                })
                .ToListAsync()));

        group.MapGet("/document-log", async (NanchesoftDbContext db) =>
        {
            var rows = new List<DocumentLogRowDto>();

            rows.AddRange(await db.PurchaseInvoices.AsNoTracking()
                .OrderByDescending(x => x.InvoiceDate)
                .Take(100)
                .Select(x => new DocumentLogRowDto
                {
                    DocumentType = "Factura proveedor",
                    DocumentKey = x.Id,
                    Folio = x.Folio,
                    DocumentDate = x.InvoiceDate,
                    Status = x.Status,
                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,
                    PartyName = x.Supplier != null ? x.Supplier.Name : string.Empty,
                    Amount = x.Total,
                    Notes = x.SupplierInvoiceFolio ?? string.Empty,
                    SourceModule = "Compras"
                }).ToListAsync());

            rows.AddRange(await db.SalesInvoices.AsNoTracking()
                .OrderByDescending(x => x.InvoiceDate)
                .Take(100)
                .Select(x => new DocumentLogRowDto
                {
                    DocumentType = "Factura venta",
                    DocumentKey = x.Id,
                    Folio = x.Folio,
                    DocumentDate = x.InvoiceDate,
                    Status = x.Status,
                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,
                    PartyName = x.Customer != null ? x.Customer.Name : string.Empty,
                    Amount = x.Total,
                    Notes = x.Notes ?? string.Empty,
                    SourceModule = "Ventas"
                }).ToListAsync());

            rows.AddRange(await (
                from entry in db.InventoryEntries.AsNoTracking()
                join warehouse in db.Warehouses.AsNoTracking() on entry.WarehouseId equals warehouse.Id into warehouseJoin
                from warehouse in warehouseJoin.DefaultIfEmpty()
                orderby entry.EntryDate descending
                select new DocumentLogRowDto
                {
                    DocumentType = "Entrada inventario",
                    DocumentKey = entry.Id,
                    Folio = entry.Folio,
                    DocumentDate = entry.EntryDate,
                    Status = entry.Status,
                    CompanyId = entry.CompanyId,
                    BranchId = entry.BranchId,
                    PartyName = warehouse != null ? warehouse.Name : string.Empty,
                    Amount = 0m,
                    Notes = entry.Reason ?? string.Empty,
                    SourceModule = "Inventario"
                })
                .Take(100)
                .ToListAsync());

            rows.AddRange(await db.TreasuryIncomes.AsNoTracking()
                .OrderByDescending(x => x.IncomeDate)
                .Take(100)
                .Select(x => new DocumentLogRowDto
                {
                    DocumentType = "Ingreso",
                    DocumentKey = x.Id,
                    Folio = x.Folio,
                    DocumentDate = x.IncomeDate,
                    Status = x.Status,
                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,
                    PartyName = x.Reference ?? string.Empty,
                    Amount = x.Total,
                    Notes = x.Notes ?? string.Empty,
                    SourceModule = "Tesorería"
                }).ToListAsync());

            rows.AddRange(await db.TreasuryExpenses.AsNoTracking()
                .OrderByDescending(x => x.ExpenseDate)
                .Take(100)
                .Select(x => new DocumentLogRowDto
                {
                    DocumentType = "Egreso",
                    DocumentKey = x.Id,
                    Folio = x.Folio,
                    DocumentDate = x.ExpenseDate,
                    Status = x.Status,
                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,
                    PartyName = x.Reference ?? string.Empty,
                    Amount = x.Total,
                    Notes = x.Notes ?? string.Empty,
                    SourceModule = "Tesorería"
                }).ToListAsync());

            return Results.Ok(rows.OrderByDescending(x => x.DocumentDate).Take(500).ToList());
        });

        return app;
    }
}

public sealed class AuditChangeLogRowDto
{
    public Guid AuditLogId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}

public sealed class DocumentLogRowDto
{
    public Guid DocumentKey { get; set; }
    public string SourceModule { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
}

using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class InventorySeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        var company = await dbContext.Companies.OrderBy(x => x.CreatedAt).FirstAsync();
        var branch = await dbContext.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstAsync();
        var warehouse = await dbContext.Warehouses.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstAsync();
        var item = await dbContext.Items.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstAsync();
        var unit = await dbContext.Units.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();

        if (!await dbContext.StockBalances.AnyAsync(x => x.CompanyId == company.Id && x.WarehouseId == warehouse.Id && x.ItemId == item.Id))
        {
            dbContext.StockBalances.Add(new StockBalance
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                WarehouseId = warehouse.Id,
                ItemId = item.Id,
                QuantityOnHand = 95m,
                QuantityReserved = 5m,
                QuantityAvailable = 90m,
                AverageCost = 850m,
                LastCost = 900m,
                CreatedBy = "seed"
            });
        }

        if (!await dbContext.ItemLots.AnyAsync(x => x.ItemId == item.Id && x.LotNumber == "LOT-ERP-001"))
        {
            dbContext.ItemLots.Add(new ItemLot
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                ItemId = item.Id,
                WarehouseId = warehouse.Id,
                LotNumber = "LOT-ERP-001",
                QuantityOnHand = 40m,
                CreatedBy = "seed"
            });
        }

        if (!await dbContext.ItemSerials.AnyAsync(x => x.ItemId == item.Id && x.SerialNumber == "SER-ERP-0001"))
        {
            dbContext.ItemSerials.Add(new ItemSerial
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                ItemId = item.Id,
                WarehouseId = warehouse.Id,
                SerialNumber = "SER-ERP-0001",
                DocumentType = "inventory_entry",
                CreatedBy = "seed"
            });
        }

        if (!await dbContext.InventoryEntries.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "ENT-0001"))
        {
            var entry = new InventoryEntry
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                WarehouseId = warehouse.Id,
                Folio = "ENT-0001",
                EntryDate = DateTime.UtcNow.Date,
                Status = "posted",
                Reason = "Entrada inicial demo",
                PostedAt = DateTime.UtcNow,
                CreatedBy = "seed"
            };
            entry.Lines.Add(new InventoryEntryLine
            {
                LineNumber = 1,
                ItemId = item.Id,
                Description = item.Name,
                Quantity = 100m,
                UnitId = unit?.Id,
                UnitCost = 900m,
                LineTotal = 90000m
            });
            dbContext.InventoryEntries.Add(entry);
        }

        if (!await dbContext.InventoryExits.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "SAL-0001"))
        {
            var exit = new InventoryExit
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                WarehouseId = warehouse.Id,
                Folio = "SAL-0001",
                ExitDate = DateTime.UtcNow.Date,
                Status = "posted",
                Reason = "Salida demo",
                PostedAt = DateTime.UtcNow,
                CreatedBy = "seed"
            };
            exit.Lines.Add(new InventoryExitLine
            {
                LineNumber = 1,
                ItemId = item.Id,
                Description = item.Name,
                Quantity = 10m,
                UnitId = unit?.Id,
                UnitCost = 900m,
                LineTotal = 9000m
            });
            dbContext.InventoryExits.Add(exit);
        }

        if (!await dbContext.InventoryTransfers.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "TRA-0001"))
        {
            var transfer = new InventoryTransfer
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                SourceWarehouseId = warehouse.Id,
                TargetWarehouseId = warehouse.Id,
                Folio = "TRA-0001",
                TransferDate = DateTime.UtcNow.Date,
                Status = "draft",
                Reason = "Demo de estructura",
                CreatedBy = "seed"
            };
            transfer.Lines.Add(new InventoryTransferLine
            {
                LineNumber = 1,
                ItemId = item.Id,
                Description = item.Name,
                Quantity = 5m,
                UnitId = unit?.Id,
                UnitCost = 900m,
                LineTotal = 4500m
            });
            dbContext.InventoryTransfers.Add(transfer);
        }

        if (!await dbContext.InventoryAdjustments.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "AJU-0001"))
        {
            var adjustment = new InventoryAdjustment
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                WarehouseId = warehouse.Id,
                Folio = "AJU-0001",
                AdjustmentDate = DateTime.UtcNow.Date,
                AdjustmentType = "negative",
                Status = "approved",
                Reason = "Ajuste demo",
                ApprovedAt = DateTime.UtcNow,
                CreatedBy = "seed"
            };
            adjustment.Lines.Add(new InventoryAdjustmentLine
            {
                LineNumber = 1,
                ItemId = item.Id,
                Description = item.Name,
                Quantity = 2m,
                UnitId = unit?.Id,
                UnitCost = 900m,
                LineTotal = 1800m
            });
            dbContext.InventoryAdjustments.Add(adjustment);
        }

        if (!await dbContext.PhysicalCounts.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "CFI-0001"))
        {
            var count = new PhysicalCount
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                WarehouseId = warehouse.Id,
                Folio = "CFI-0001",
                CountDate = DateTime.UtcNow.Date,
                Status = "closed",
                ClosedAt = DateTime.UtcNow,
                CreatedBy = "seed"
            };
            count.Lines.Add(new PhysicalCountLine
            {
                LineNumber = 1,
                ItemId = item.Id,
                SystemQuantity = 95m,
                CountedQuantity = 94m,
                DifferenceQuantity = -1m,
                UnitId = unit?.Id
            });
            dbContext.PhysicalCounts.Add(count);
        }

        if (!await dbContext.InventoryMovements.AnyAsync(x => x.CompanyId == company.Id && x.DocumentType == "inventory_entry"))
        {
            dbContext.InventoryMovements.AddRange(
                new InventoryMovement
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    BranchId = branch.Id,
                    WarehouseId = warehouse.Id,
                    ItemId = item.Id,
                    MovementType = "entry",
                    DocumentType = "inventory_entry",
                    DocumentId = Guid.NewGuid(),
                    MovementDate = DateTime.UtcNow.Date.AddDays(-2),
                    QuantityIn = 100m,
                    BalanceAfter = 100m,
                    UnitCost = 900m,
                    TotalCost = 90000m,
                    Reference = "Entrada inicial",
                    CreatedBy = "seed"
                },
                new InventoryMovement
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    BranchId = branch.Id,
                    WarehouseId = warehouse.Id,
                    ItemId = item.Id,
                    MovementType = "exit",
                    DocumentType = "inventory_exit",
                    DocumentId = Guid.NewGuid(),
                    MovementDate = DateTime.UtcNow.Date.AddDays(-1),
                    QuantityOut = 10m,
                    BalanceAfter = 90m,
                    UnitCost = 900m,
                    TotalCost = 9000m,
                    Reference = "Salida demo",
                    CreatedBy = "seed"
                });
        }

        await dbContext.SaveChangesAsync();
    }
}

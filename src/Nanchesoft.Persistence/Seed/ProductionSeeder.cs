using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class ProductionSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        const string seedUser = "seed-production-01";

        var tenant = await dbContext.Tenants.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var company = await dbContext.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var branch = await dbContext.Branches.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (tenant is null || company is null || branch is null) return;

        // ─── Production phases ───────────────────────────────────────────────
        var phaseSeeds = new[]
        {
            (Code: "CORTE",    Name: "Corte",             Seq: 10),
            (Code: "PREP",     Name: "Preparado",         Seq: 20),
            (Code: "COSTURA",  Name: "Costura",           Seq: 30),
            (Code: "ARMADO",   Name: "Armado/Montado",    Seq: 40),
            (Code: "SUELA",    Name: "Ensuelado",         Seq: 50),
            (Code: "ACABADO",  Name: "Acabado",           Seq: 60),
            (Code: "EMPAQUE",  Name: "Empaque",           Seq: 70),
        };

        var existingPhases = await dbContext.ProductionPhases
            .Where(x => x.TenantId == tenant.Id)
            .ToDictionaryAsync(x => x.Code, x => x);

        foreach (var (code, name, seq) in phaseSeeds)
        {
            if (existingPhases.ContainsKey(code)) continue;

            var phase = new ProductionPhase
            {
                TenantId = tenant.Id,
                Code = code,
                Name = name,
                Description = name,
                Sequence = seq,
                CreatedBy = seedUser
            };
            dbContext.ProductionPhases.Add(phase);
            existingPhases[code] = phase;
        }

        await dbContext.SaveChangesAsync();

        existingPhases = await dbContext.ProductionPhases
            .Where(x => x.TenantId == tenant.Id)
            .ToDictionaryAsync(x => x.Code, x => x);

        // ─── Production cells ────────────────────────────────────────────────
        var cellSeeds = new[]
        {
            (PhaseCode: "CORTE",   CellCode: "CORTE-1",   Name: "Línea Corte 1",      CapDay: 200, CapWeek: 1000),
            (PhaseCode: "PREP",    CellCode: "PREP-1",    Name: "Línea Preparado 1",  CapDay: 180, CapWeek: 900),
            (PhaseCode: "COSTURA", CellCode: "COST-1",    Name: "Línea Costura 1",    CapDay: 150, CapWeek: 750),
            (PhaseCode: "ARMADO",  CellCode: "ARMA-1",    Name: "Línea Armado 1",     CapDay: 120, CapWeek: 600),
            (PhaseCode: "SUELA",   CellCode: "SUEL-1",    Name: "Línea Ensuelado 1",  CapDay: 120, CapWeek: 600),
            (PhaseCode: "ACABADO", CellCode: "ACAB-1",    Name: "Línea Acabado 1",    CapDay: 200, CapWeek: 1000),
            (PhaseCode: "EMPAQUE", CellCode: "EMPA-1",    Name: "Línea Empaque 1",    CapDay: 300, CapWeek: 1500),
        };

        var existingCellCodes = await dbContext.ProductionCells
            .Where(x => x.CompanyId == company.Id)
            .Select(x => x.Code)
            .ToHashSetAsync();

        foreach (var (phaseCode, cellCode, cellName, capDay, capWeek) in cellSeeds)
        {
            if (existingCellCodes.Contains(cellCode)) continue;
            if (!existingPhases.TryGetValue(phaseCode, out var phase)) continue;

            dbContext.ProductionCells.Add(new ProductionCell
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                ProductionPhaseId = phase.Id,
                Code = cellCode,
                Name = cellName,
                CapacityPerDay = capDay,
                CapacityPerWeek = capWeek,
                CreatedBy = seedUser
            });
        }

        await dbContext.SaveChangesAsync();

        // ─── Piece work rates (effective 2026-01-01) ─────────────────────────
        var rateDate = new DateOnly(2026, 1, 1);
        var rateSeeds = new[]
        {
            (PhaseCode: "CORTE",   Price: 4.50m),
            (PhaseCode: "PREP",    Price: 3.00m),
            (PhaseCode: "COSTURA", Price: 6.00m),
            (PhaseCode: "ARMADO",  Price: 8.00m),
            (PhaseCode: "SUELA",   Price: 5.50m),
            (PhaseCode: "ACABADO", Price: 3.50m),
            (PhaseCode: "EMPAQUE", Price: 2.00m),
        };

        foreach (var (phaseCode, price) in rateSeeds)
        {
            if (!existingPhases.TryGetValue(phaseCode, out var phase)) continue;

            var alreadyExists = await dbContext.PieceWorkRates.AnyAsync(x =>
                x.CompanyId == company.Id &&
                x.ProductionPhaseId == phase.Id &&
                x.EffectiveDate == rateDate);

            if (alreadyExists) continue;

            dbContext.PieceWorkRates.Add(new PieceWorkRate
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                ProductionPhaseId = phase.Id,
                EffectiveDate = rateDate,
                PricePerUnit = price,
                Notes = "Tarifa inicial 2026",
                CreatedBy = seedUser
            });
        }

        // ─── Document series for production orders ───────────────────────────
        if (!await dbContext.DocumentSeries.AnyAsync(x => x.CompanyId == company.Id && x.DocumentType == "PRODUCTION_ORDER" && x.Code == "OP"))
        {
            dbContext.DocumentSeries.Add(new DocumentSeries
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                CompanyId = company.Id,
                DocumentType = "PRODUCTION_ORDER",
                Code = "OP",
                Name = "Orden de Producción",
                Prefix = "OP",
                CurrentNumber = 1,
                NumberLength = 6,
                IsDefault = true,
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.DocumentSeries.AnyAsync(x => x.CompanyId == company.Id && x.DocumentType == "PRODUCTION_VOUCHER" && x.Code == "VALE"))
        {
            dbContext.DocumentSeries.Add(new DocumentSeries
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                CompanyId = company.Id,
                DocumentType = "PRODUCTION_VOUCHER",
                Code = "VALE",
                Name = "Vale de Producción",
                Prefix = "VALE",
                CurrentNumber = 1,
                NumberLength = 6,
                IsDefault = true,
                CreatedBy = seedUser
            });
        }

        await dbContext.SaveChangesAsync();
    }
}

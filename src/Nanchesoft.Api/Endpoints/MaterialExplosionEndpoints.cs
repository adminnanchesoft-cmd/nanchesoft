using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class MaterialExplosionEndpoints
{
    public static void MapMaterialExplosionEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/material-explosion").WithTags("MaterialExplosion");

        // Returns the size run sizes for a product (for quantity input matrix)
        g.MapGet("/{productId:guid}/sizes", async (Guid productId, NanchesoftDbContext db) =>
        {
            var product = await db.FinishedProducts.AsNoTracking()
                .Select(x => new { x.Id, x.Code, x.Name, x.ProductSizeRunId, x.ProductStyleId })
                .FirstOrDefaultAsync(x => x.Id == productId);
            if (product is null) return Results.NotFound(new { message = "Producto no encontrado." });
            if (!product.ProductSizeRunId.HasValue)
                return Results.BadRequest(new { message = "El producto no tiene corrida asignada." });

            var sizes = await db.ProductSizeRunSizes.AsNoTracking()
                .Where(x => x.ProductSizeRunId == product.ProductSizeRunId.Value && x.IsActive)
                .OrderBy(x => x.Sequence)
                .Select(x => new SizeOptionDto { SizeRunSizeId = x.Id, SizeCode = x.SizeCode, DisplayLabel = x.DisplayLabel, Sequence = x.Sequence })
                .ToListAsync();

            return Results.Ok(new { product.Id, product.Code, product.Name, SizeRunId = product.ProductSizeRunId, Sizes = sizes });
        });

        // Calculates material explosion
        g.MapPost("/calculate", async (ExplosionRequest request, NanchesoftDbContext db) =>
        {
            if (request.FinishedProductId == Guid.Empty)
                return Results.BadRequest(new { message = "ProductId es obligatorio." });
            if (request.QuantitiesPerSize == null || request.QuantitiesPerSize.Count == 0)
                return Results.BadRequest(new { message = "Debe capturar al menos una cantidad." });

            // Load product
            var product = await db.FinishedProducts.AsNoTracking()
                .Include(x => x.ProductStyle)
                .Include(x => x.ProductSizeRun)
                .FirstOrDefaultAsync(x => x.Id == request.FinishedProductId);
            if (product is null) return Results.NotFound(new { message = "Producto no encontrado." });
            if (!product.ProductStyleId.HasValue || !product.ProductSizeRunId.HasValue)
                return Results.BadRequest(new { message = "El producto debe tener estilo y corrida asignados." });

            // Load authorized consumption template for style+run
            var template = await db.ConsumptionTemplates.AsNoTracking()
                .Include(x => x.Details).ThenInclude(d => d.Sizes)
                .Include(x => x.Details).ThenInclude(d => d.ProductComponent).ThenInclude(c => c!.ConsumptionUnit)
                .FirstOrDefaultAsync(x => x.CompanyId == product.CompanyId
                    && x.ProductStyleId == product.ProductStyleId.Value
                    && x.ProductSizeRunId == product.ProductSizeRunId.Value
                    && x.IsActive && x.IsAuthorized);
            if (template is null)
                return Results.BadRequest(new { message = "No hay plantilla de consumo autorizada para el estilo y corrida de este producto." });

            // Load authorized supplies (material per component per size)
            var supplies = await db.FinishedProductSupplies.AsNoTracking()
                .Include(x => x.ProductComponent)
                .Include(x => x.Sizes).ThenInclude(s => s.MaterialItem).ThenInclude(m => m!.IssueUnit)
                .Include(x => x.Sizes).ThenInclude(s => s.MaterialItem).ThenInclude(m => m!.Supplier)
                .Where(x => x.FinishedProductId == request.FinishedProductId && x.IsActive && x.IsAuthorized)
                .ToListAsync();
            if (!supplies.Any())
                return Results.BadRequest(new { message = "El producto no tiene insumos autorizados. Inicialice y autorice los insumos primero." });

            // Load sizes in the run (for ordering)
            var sizeRunSizes = await db.ProductSizeRunSizes.AsNoTracking()
                .Where(x => x.ProductSizeRunId == product.ProductSizeRunId.Value && x.IsActive)
                .OrderBy(x => x.Sequence)
                .ToListAsync();

            // Load unit conversions for cost adjustment (component unit → issue unit)
            var unitConversions = await db.UnitConversions.AsNoTracking()
                .Include(x => x.FromUnit)
                .Include(x => x.ToUnit)
                .ToListAsync();

            // Build explosion lines
            var lines = new List<ExplosionLineDto>();
            var errors = new List<string>();

            foreach (var templateDetail in template.Details.Where(d => d.IsActive))
            {
                var component = templateDetail.ProductComponent;
                if (component is null) continue;

                var supply = supplies.FirstOrDefault(s => s.ProductComponentId == templateDetail.ProductComponentId);
                if (supply is null)
                {
                    errors.Add($"Componente '{component.Code}' no tiene insumos asignados.");
                    continue;
                }

                // Build per-size breakdown
                var consumptionBySize = new Dictionary<Guid, decimal>();
                var quantityBySize = new Dictionary<Guid, decimal>();
                var totalBySize = new Dictionary<Guid, decimal>();

                foreach (var size in sizeRunSizes)
                {
                    if (!request.QuantitiesPerSize.TryGetValue(size.Id, out var qty) || qty <= 0)
                        continue;

                    var templateSize = templateDetail.Sizes.FirstOrDefault(s => s.ProductSizeRunSizeId == size.Id);
                    if (templateSize is null) continue;

                    var consumption = templateSize.Consumption;
                    var supplySize = supply.Sizes.FirstOrDefault(s => s.ProductSizeRunSizeId == size.Id);
                    if (supplySize?.MaterialItem is null) continue;

                    // Apply unit conversion if needed
                    var convertedConsumption = ApplyUnitConversion(
                        consumption,
                        component.ConsumptionUnitId,
                        supplySize.MaterialItem.IssueUnitId,
                        unitConversions);

                    var totalForSize = convertedConsumption * qty;
                    consumptionBySize[size.Id] = convertedConsumption;
                    quantityBySize[size.Id] = qty;
                    totalBySize[size.Id] = totalForSize;
                }

                if (!totalBySize.Any()) continue;

                // Get the material for this component (should be same across sizes or per-size)
                // Use the material from the first size with assignment as representative
                var repSupplySize = supply.Sizes
                    .FirstOrDefault(s => s.MaterialItem != null && totalBySize.ContainsKey(s.ProductSizeRunSizeId));

                var material = repSupplySize?.MaterialItem;

                var totalConsumption = totalBySize.Values.Sum();
                var unitCost = material?.AuthorizedCost ?? 0m;
                var totalCost = totalConsumption * unitCost;

                lines.Add(new ExplosionLineDto
                {
                    ComponentCode = component.Code,
                    ComponentName = component.Name,
                    MaterialCode = material?.Code ?? string.Empty,
                    MaterialName = material?.Name ?? string.Empty,
                    SupplierName = material?.Supplier?.Name ?? string.Empty,
                    ConsumptionUnit = component.ConsumptionUnit?.Code ?? string.Empty,
                    IssueUnit = material?.IssueUnit?.Code ?? string.Empty,
                    ConsumptionBySize = consumptionBySize,
                    QuantityBySize = quantityBySize,
                    TotalBySize = totalBySize,
                    TotalConsumption = totalConsumption,
                    UnitCost = unitCost,
                    TotalCost = totalCost
                });
            }

            // Consolidate by material
            var consolidated = lines
                .Where(l => !string.IsNullOrEmpty(l.MaterialCode))
                .GroupBy(l => l.MaterialCode)
                .Select(g2 => new ConsolidatedMaterialDto
                {
                    MaterialCode = g2.Key,
                    MaterialName = g2.First().MaterialName,
                    SupplierName = g2.First().SupplierName,
                    IssueUnit = g2.First().IssueUnit,
                    TotalConsumption = g2.Sum(x => x.TotalConsumption),
                    UnitCost = g2.First().UnitCost,
                    TotalCost = g2.Sum(x => x.TotalCost)
                })
                .OrderBy(x => x.MaterialCode)
                .ToList();

            var grandTotalCost = consolidated.Sum(x => x.TotalCost);
            var sizes2 = sizeRunSizes.Select(s => new SizeOptionDto
            {
                SizeRunSizeId = s.Id,
                SizeCode = s.SizeCode,
                DisplayLabel = s.DisplayLabel,
                Sequence = s.Sequence
            }).ToList();

            return Results.Ok(new ExplosionResultDto
            {
                ProductCode = product.Code,
                ProductName = product.Name ?? string.Empty,
                StyleCode = product.ProductStyle?.Code ?? string.Empty,
                SizeRunName = product.ProductSizeRun?.Name ?? string.Empty,
                GeneratedAt = DateTime.UtcNow,
                Sizes = sizes2,
                Lines = lines,
                Consolidated = consolidated,
                GrandTotalCost = grandTotalCost,
                Errors = errors
            });
        });
    }

    private static decimal ApplyUnitConversion(
        decimal value,
        Guid? fromUnitId,
        Guid? toUnitId,
        List<UnitConversion> conversions)
    {
        if (fromUnitId is null || toUnitId is null || fromUnitId == toUnitId) return value;
        var conv = conversions.FirstOrDefault(c =>
            (c.FromUnitId == fromUnitId && c.ToUnitId == toUnitId) ||
            (c.IsBidirectional && c.FromUnitId == toUnitId && c.ToUnitId == fromUnitId));
        if (conv is null) return value;
        if (conv.FromUnitId == fromUnitId) return value * conv.ConversionFactor;
        return conv.ConversionFactor != 0 ? value / conv.ConversionFactor : value;
    }
}

public sealed class SizeOptionDto
{
    public Guid SizeRunSizeId { get; set; }
    public string SizeCode { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
    public int Sequence { get; set; }
}

public sealed class ExplosionRequest
{
    public Guid FinishedProductId { get; set; }
    public Dictionary<Guid, decimal> QuantitiesPerSize { get; set; } = new();
}

public sealed class ExplosionLineDto
{
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string ConsumptionUnit { get; set; } = string.Empty;
    public string IssueUnit { get; set; } = string.Empty;
    public Dictionary<Guid, decimal> ConsumptionBySize { get; set; } = new();
    public Dictionary<Guid, decimal> QuantityBySize { get; set; } = new();
    public Dictionary<Guid, decimal> TotalBySize { get; set; } = new();
    public decimal TotalConsumption { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}

public sealed class ConsolidatedMaterialDto
{
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string IssueUnit { get; set; } = string.Empty;
    public decimal TotalConsumption { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}

public sealed class ExplosionResultDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string StyleCode { get; set; } = string.Empty;
    public string SizeRunName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public List<SizeOptionDto> Sizes { get; set; } = new();
    public List<ExplosionLineDto> Lines { get; set; } = new();
    public List<ConsolidatedMaterialDto> Consolidated { get; set; } = new();
    public decimal GrandTotalCost { get; set; }
    public List<string> Errors { get; set; } = new();
}

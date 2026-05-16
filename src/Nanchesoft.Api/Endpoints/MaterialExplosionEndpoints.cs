using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

        // Generates a PDF report from an already-calculated explosion result
        g.MapPost("/report", (ExplosionReportRequest request) =>
        {
            if (request.Result is null)
                return Results.BadRequest(new { message = "Result is required." });
            try
            {
                var pdfBytes = BuildExplosionPdf(request);
                var base64 = Convert.ToBase64String(pdfBytes);
                var fileName = $"explosion_{request.Result.ProductCode}_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf";
                return Results.Ok(new { base64, fileName });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
    }

    private static byte[] BuildExplosionPdf(ExplosionReportRequest request)
    {
        var result = request.Result;
        var activeSizes = result.Sizes
            .Where(s => request.QuantitiesPerSize.ContainsKey(s.SizeRunSizeId) && request.QuantitiesPerSize[s.SizeRunSizeId] > 0)
            .OrderBy(s => s.Sequence)
            .ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20, QuestPDF.Infrastructure.Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().PaddingBottom(6).BorderBottom(1).BorderColor(Colors.Blue.Darken2).Row(row =>
                {
                    row.RelativeItem(2).Column(col =>
                    {
                        col.Item().Text(string.IsNullOrWhiteSpace(request.CompanyName) ? "Nanchesoft ERP" : request.CompanyName)
                            .FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().Text("Sistema ERP Nanchesoft").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                    row.RelativeItem(3).AlignRight().Column(col =>
                    {
                        col.Item().Text("EXPLOSIÓN DE MATERIALES").FontSize(13).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"Fecha: {result.GeneratedAt.ToLocalTime():yyyy-MM-dd HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                        if (!string.IsNullOrWhiteSpace(request.GeneratedBy))
                            col.Item().Text($"Generado por: {request.GeneratedBy}").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Spacing(10);

                    // Info block
                    col.Item().Background(Colors.Blue.Lighten5).Border(1).BorderColor(Colors.Blue.Lighten3).Padding(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("PRODUCTO").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                            c.Item().Text($"{result.ProductCode} · {result.ProductName}").FontSize(9).Bold();
                        });
                        row.ConstantItem(1).Background(Colors.Blue.Lighten3);
                        row.RelativeItem().PaddingLeft(8).Column(c =>
                        {
                            c.Item().Text("ESTILO").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                            c.Item().Text(result.StyleCode).FontSize(9);
                        });
                        row.ConstantItem(1).Background(Colors.Blue.Lighten3);
                        row.RelativeItem().PaddingLeft(8).Column(c =>
                        {
                            c.Item().Text("CORRIDA").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                            c.Item().Text(result.SizeRunName).FontSize(9);
                        });
                    });

                    // Quantities table
                    if (activeSizes.Count > 0)
                    {
                        col.Item().Column(qcol =>
                        {
                            qcol.Item().Text("Cantidades a producir").FontSize(8).Bold().FontColor(Colors.Blue.Darken2);
                            qcol.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    foreach (var _ in activeSizes) cols.RelativeColumn();
                                    cols.RelativeColumn();
                                });
                                table.Header(header =>
                                {
                                    foreach (var s in activeSizes)
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(4).AlignCenter().Text(s.DisplayLabel).FontSize(8).Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignCenter().Text("TOTAL").FontSize(8).Bold().FontColor(Colors.White);
                                });
                                foreach (var s in activeSizes)
                                    table.Cell().Padding(4).AlignCenter().Text(request.QuantitiesPerSize[s.SizeRunSizeId].ToString("N0")).FontSize(9);
                                table.Cell().Background(Colors.Blue.Lighten4).Padding(4).AlignCenter().Text(request.QuantitiesPerSize.Where(kv => activeSizes.Any(s => s.SizeRunSizeId == kv.Key)).Sum(kv => kv.Value).ToString("N0")).FontSize(9).Bold();
                            });
                        });
                    }

                    // Detail by component
                    if (result.Lines.Count > 0)
                    {
                        col.Item().Column(dcol =>
                        {
                            dcol.Item().Text("Detalle por componente").FontSize(8).Bold().FontColor(Colors.Blue.Darken2);
                            dcol.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.ConstantColumn(55);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(1.5f);
                                    cols.ConstantColumn(28);
                                    foreach (var _ in activeSizes) cols.ConstantColumn(26);
                                    cols.ConstantColumn(38);
                                    cols.ConstantColumn(40);
                                    cols.ConstantColumn(45);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Componente").FontSize(7).Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Material").FontSize(7).Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Proveedor").FontSize(7).Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).AlignCenter().Text("Unidad").FontSize(7).Bold().FontColor(Colors.White);
                                    foreach (var s in activeSizes)
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(4).AlignRight().Text(s.DisplayLabel).FontSize(7).Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).AlignRight().Text("Total").FontSize(7).Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).AlignRight().Text("Costo U.").FontSize(7).Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).AlignRight().Text("Costo Total").FontSize(7).Bold().FontColor(Colors.White);
                                });

                                var rowAlt = false;
                                foreach (var line in result.Lines)
                                {
                                    var bg = rowAlt ? Colors.Grey.Lighten4 : Colors.White;
                                    rowAlt = !rowAlt;
                                    table.Cell().Background(bg).Padding(3).Column(c =>
                                    {
                                        c.Item().Text(line.ComponentCode).FontSize(7).Bold().FontColor(Colors.Blue.Darken2);
                                        c.Item().Text(line.ComponentName).FontSize(6).FontColor(Colors.Grey.Darken1);
                                    });
                                    table.Cell().Background(bg).Padding(3).Column(c =>
                                    {
                                        c.Item().Text(line.MaterialCode).FontSize(7).Bold();
                                        c.Item().Text(line.MaterialName).FontSize(6).FontColor(Colors.Grey.Darken1);
                                    });
                                    table.Cell().Background(bg).Padding(3).Text(string.IsNullOrWhiteSpace(line.SupplierName) ? "—" : line.SupplierName).FontSize(7).FontColor(Colors.Grey.Darken1);
                                    table.Cell().Background(bg).Padding(3).AlignCenter().Text(line.IssueUnit).FontSize(7);
                                    foreach (var s in activeSizes)
                                    {
                                        var v = line.TotalBySize.TryGetValue(s.SizeRunSizeId, out var tv) ? tv : 0;
                                        table.Cell().Background(bg).Padding(3).AlignRight().Text(v > 0 ? v.ToString("F2") : "—").FontSize(7).FontColor(v > 0 ? Colors.Black : Colors.Grey.Lighten1);
                                    }
                                    table.Cell().Background(bg).Padding(3).AlignRight().Text(line.TotalConsumption.ToString("F4")).FontSize(7).Bold();
                                    table.Cell().Background(bg).Padding(3).AlignRight().Text(line.UnitCost.ToString("C4")).FontSize(7).FontColor(Colors.Grey.Darken1);
                                    table.Cell().Background(bg).Padding(3).AlignRight().Text(line.TotalCost.ToString("C2")).FontSize(8).Bold().FontColor(Colors.Blue.Darken2);
                                }
                            });
                        });
                    }

                    // Consolidated by material
                    if (result.Consolidated.Count > 0)
                    {
                        col.Item().Column(ccol =>
                        {
                            ccol.Item().Text("Resumen consolidado por material").FontSize(8).Bold().FontColor(Colors.Blue.Darken2);
                            ccol.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.ConstantColumn(50);
                                    cols.RelativeColumn(3);
                                    cols.RelativeColumn(2);
                                    cols.ConstantColumn(35);
                                    cols.ConstantColumn(55);
                                    cols.ConstantColumn(55);
                                    cols.ConstantColumn(60);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Código").FontSize(8).Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Material").FontSize(8).Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Proveedor").FontSize(8).Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Unidad").FontSize(8).Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Total Consumo").FontSize(8).Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Costo U.").FontSize(8).Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Costo Total").FontSize(8).Bold().FontColor(Colors.White);
                                });

                                var rowAlt = false;
                                foreach (var mat in result.Consolidated)
                                {
                                    var bg = rowAlt ? Colors.Grey.Lighten4 : Colors.White;
                                    rowAlt = !rowAlt;
                                    table.Cell().Background(bg).Padding(5).Text(mat.MaterialCode).FontSize(8).Bold().FontColor(Colors.Blue.Darken2);
                                    table.Cell().Background(bg).Padding(5).Text(mat.MaterialName).FontSize(8);
                                    table.Cell().Background(bg).Padding(5).Text(string.IsNullOrWhiteSpace(mat.SupplierName) ? "—" : mat.SupplierName).FontSize(8).FontColor(Colors.Grey.Darken1);
                                    table.Cell().Background(bg).Padding(5).AlignRight().Text(mat.IssueUnit).FontSize(8);
                                    table.Cell().Background(bg).Padding(5).AlignRight().Text(mat.TotalConsumption.ToString("F4")).FontSize(8).Bold();
                                    table.Cell().Background(bg).Padding(5).AlignRight().Text(mat.UnitCost.ToString("C4")).FontSize(8).FontColor(Colors.Grey.Darken1);
                                    table.Cell().Background(bg).Padding(5).AlignRight().Text(mat.TotalCost.ToString("C2")).FontSize(9).Bold().FontColor(Colors.Blue.Darken2);
                                }

                                table.Cell().ColumnSpan(6).Background(Colors.Blue.Darken2).Padding(6)
                                    .AlignRight().Text("COSTO TOTAL GENERAL").FontSize(9).Bold().FontColor(Colors.White);
                                table.Cell().Background(Colors.Blue.Darken1).Padding(6)
                                    .AlignRight().Text(result.GrandTotalCost.ToString("C2")).FontSize(12).Bold().FontColor(Colors.White);
                            });
                        });
                    }

                    if (result.Errors?.Count > 0)
                    {
                        col.Item().Background(Colors.Yellow.Lighten3).Border(1).BorderColor(Colors.Yellow.Darken1).Padding(8).Column(c =>
                        {
                            c.Item().Text("Advertencias:").FontSize(8).Bold().FontColor(Colors.Orange.Darken3);
                            foreach (var err in result.Errors)
                                c.Item().Text($"• {err}").FontSize(8).FontColor(Colors.Orange.Darken2);
                        });
                    }
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text("Nanchesoft ERP · Explosión de materiales").FontSize(7).FontColor(Colors.Grey.Darken1);
                    row.ConstantItem(100).AlignRight().Text(x =>
                    {
                        x.Span("Página ").FontSize(7).FontColor(Colors.Grey.Darken1);
                        x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Darken1);
                        x.Span(" de ").FontSize(7).FontColor(Colors.Grey.Darken1);
                        x.TotalPages().FontSize(7).FontColor(Colors.Grey.Darken1);
                    });
                });
            });
        }).GeneratePdf();
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

public sealed class ExplosionReportRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string GeneratedBy { get; set; } = string.Empty;
    public ExplosionResultDto Result { get; set; } = new();
    public Dictionary<Guid, decimal> QuantitiesPerSize { get; set; } = new();
}

using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Nanchesoft.Api.Endpoints;

public static class ProductionVoucherEndpoints
{
    public static void MapProductionVoucherEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/production/vouchers").WithTags("ProductionVouchers");

        // ─── List ────────────────────────────────────────────────────────────
        g.MapGet("/", async (Guid? orderId, Guid? phaseId, string? status, int page = 1, int pageSize = 20, NanchesoftDbContext db = default!) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

            var query = db.ProductionVouchers.AsNoTracking()
                .Include(x => x.ProductionPhase)
                .AsQueryable();

            if (orderId.HasValue) query = query.Where(x => x.ProductionOrderId == orderId.Value);
            if (phaseId.HasValue) query = query.Where(x => x.ProductionPhaseId == phaseId.Value);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToLower());

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new ProductionVoucherSummaryDto
                {
                    ProductionVoucherId = x.Id,
                    ProductionOrderId = x.ProductionOrderId,
                    Folio = x.Folio,
                    LotNumber = x.LotNumber,
                    PhaseName = x.ProductionPhase != null ? x.ProductionPhase.Name : string.Empty,
                    BatchSize = x.BatchSize,
                    Status = x.Status,
                    IssuedDate = x.IssuedDate,
                    IssuedBy = x.IssuedBy,
                    CompletedDate = x.CompletedDate,
                    Printed = x.Printed,
                    PrintCount = x.PrintCount
                })
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        // ─── Get detail ──────────────────────────────────────────────────────
        g.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var voucher = await db.ProductionVouchers.AsNoTracking()
                .Include(x => x.ProductionPhase)
                .Include(x => x.ProductionCell)
                .Include(x => x.Details).ThenInclude(d => d.SizeRunSize)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (voucher is null) return Results.NotFound(new { message = "Vale no encontrado." });

            var order = await db.ProductionOrders.AsNoTracking()
                .Select(x => new { x.Id, x.Folio })
                .FirstOrDefaultAsync(x => x.Id == voucher.ProductionOrderId);

            return Results.Ok(new ProductionVoucherDetailDto
            {
                ProductionVoucherId = voucher.Id,
                ProductionOrderId = voucher.ProductionOrderId,
                OrderFolio = order?.Folio ?? string.Empty,
                ProductionOrderLineId = voucher.ProductionOrderLineId,
                PhaseName = voucher.ProductionPhase?.Name ?? string.Empty,
                CellName = voucher.ProductionCell?.Name ?? string.Empty,
                Folio = voucher.Folio,
                LotNumber = voucher.LotNumber,
                BatchSize = voucher.BatchSize,
                Status = voucher.Status,
                IssuedDate = voucher.IssuedDate,
                IssuedBy = voucher.IssuedBy,
                CompletedDate = voucher.CompletedDate,
                CompletedBy = voucher.CompletedBy,
                CancelledDate = voucher.CancelledDate,
                CancelledReason = voucher.CancelledReason,
                Printed = voucher.Printed,
                PrintedAt = voucher.PrintedAt,
                PrintCount = voucher.PrintCount,
                Notes = voucher.Notes,
                Details = voucher.Details.Select(d => new ProductionVoucherDetailLineDto
                {
                    ProductionVoucherDetailId = d.Id,
                    SizeCode = d.SizeRunSize?.SizeCode ?? string.Empty,
                    SizeLabel = d.SizeRunSize?.DisplayLabel ?? string.Empty,
                    QuantityAssigned = d.QuantityAssigned,
                    QuantityProduced = d.QuantityProduced,
                    QuantityRejected = d.QuantityRejected,
                    OperationCode = d.OperationCode
                }).ToList()
            });
        });

        // ─── Issue voucher ───────────────────────────────────────────────────
        g.MapPost("/", async (IssueVoucherRequest request, NanchesoftDbContext db) =>
        {
            if (request.ProductionOrderId == Guid.Empty)
                return Results.BadRequest(new { message = "ProductionOrderId es obligatorio." });
            if (request.ProductionOrderLineId == Guid.Empty)
                return Results.BadRequest(new { message = "ProductionOrderLineId es obligatorio." });
            if (request.ProductionPhaseId == Guid.Empty)
                return Results.BadRequest(new { message = "ProductionPhaseId es obligatorio." });
            if (request.BatchSize <= 0)
                return Results.BadRequest(new { message = "BatchSize debe ser mayor a cero." });

            var order = await db.ProductionOrders.AsNoTracking()
                .Select(x => new { x.Id, x.TenantId, x.CompanyId, x.Status })
                .FirstOrDefaultAsync(x => x.Id == request.ProductionOrderId);

            if (order is null) return Results.BadRequest(new { message = "Orden de producción no encontrada." });
            if (order.Status != "in_progress")
                return Results.BadRequest(new { message = "Solo se pueden emitir vales para órdenes en estado 'in_progress'." });

            var by = request.UserId ?? "api";
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var folio = await GenerateVoucherFolioAsync(db, order.TenantId, order.CompanyId, by);

            var voucher = new ProductionVoucher
            {
                TenantId = order.TenantId,
                CompanyId = order.CompanyId,
                ProductionOrderId = request.ProductionOrderId,
                ProductionOrderLineId = request.ProductionOrderLineId,
                ProductionPhaseId = request.ProductionPhaseId,
                ProductionCellId = request.ProductionCellId,
                Folio = folio,
                LotNumber = request.LotNumber ?? $"LOTE-{today:yyyyMMdd}",
                BatchSize = request.BatchSize,
                Status = "issued",
                IssuedDate = today,
                IssuedBy = by,
                Notes = request.Notes ?? string.Empty,
                CreatedBy = by
            };

            // Create size detail lines
            if (request.SizeDetails?.Count > 0)
            {
                foreach (var detail in request.SizeDetails)
                {
                    voucher.Details.Add(new ProductionVoucherDetail
                    {
                        SizeRunSizeId = detail.SizeRunSizeId,
                        QuantityAssigned = detail.QuantityAssigned,
                        OperationCode = detail.OperationCode ?? string.Empty,
                        CreatedBy = by
                    });
                }
            }

            db.ProductionVouchers.Add(voucher);
            await db.SaveChangesAsync();

            return Results.Created($"/api/production/vouchers/{voucher.Id}", new { productionVoucherId = voucher.Id, folio = voucher.Folio });
        });

        // ─── Complete voucher ────────────────────────────────────────────────
        g.MapPost("/{id:guid}/complete", async (Guid id, CompleteVoucherRequest request, NanchesoftDbContext db) =>
        {
            var voucher = await db.ProductionVouchers.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id);
            if (voucher is null) return Results.NotFound(new { message = "Vale no encontrado." });
            if (voucher.Status != "issued" && voucher.Status != "in_progress")
                return Results.BadRequest(new { message = $"No se puede completar un vale en estado '{voucher.Status}'." });

            var by = request.UserId ?? "api";
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var now = DateTime.UtcNow;

            // Update quantities produced per size
            if (request.SizeResults?.Count > 0)
            {
                foreach (var result in request.SizeResults)
                {
                    var detail = voucher.Details.FirstOrDefault(d => d.SizeRunSizeId == result.SizeRunSizeId);
                    if (detail is null) continue;
                    detail.QuantityProduced = result.QuantityProduced;
                    detail.QuantityRejected = result.QuantityRejected;
                    detail.UpdatedAt = now;
                    detail.UpdatedBy = by;
                }
            }

            var totalProduced = voucher.Details.Sum(d => d.QuantityProduced);
            var totalRejected = voucher.Details.Sum(d => d.QuantityRejected);

            voucher.Status = "completed";
            voucher.CompletedDate = today;
            voucher.CompletedBy = by;
            voucher.UpdatedAt = now;
            voucher.UpdatedBy = by;

            // Update phase progress
            var progress = await db.ProductionPhaseProgress
                .FirstOrDefaultAsync(x => x.ProductionOrderId == voucher.ProductionOrderId
                    && x.ProductionOrderLineId == voucher.ProductionOrderLineId
                    && x.ProductionPhaseId == voucher.ProductionPhaseId);

            if (progress is not null)
            {
                progress.UnitsCompleted += totalProduced;
                progress.UnitsRejected += totalRejected;
                progress.UnitsPending = Math.Max(0, progress.UnitsPlanned - progress.UnitsCompleted);
                progress.LastUpdatedAt = now;
                progress.UpdatedAt = now;
                progress.UpdatedBy = by;

                if (progress.UnitsPending == 0 && progress.UnitsCompleted >= progress.UnitsPlanned)
                {
                    progress.Status = "completed";
                    progress.CompletedAt = now;
                }
                else if (progress.UnitsCompleted > 0)
                {
                    progress.Status = "in_progress";
                    if (progress.StartedAt is null) progress.StartedAt = now;
                }
            }

            // Update order line units produced
            var line = await db.ProductionOrderLines.FirstOrDefaultAsync(x => x.Id == voucher.ProductionOrderLineId);
            if (line is not null)
            {
                line.TotalUnitsProduced += totalProduced;
                line.TotalUnitsPending = Math.Max(0, line.TotalUnitsPlanned - line.TotalUnitsProduced);
                line.UpdatedAt = now;
                line.UpdatedBy = by;
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Vale completado.", folio = voucher.Folio, totalProduced, totalRejected });
        });

        // ─── Cancel voucher ──────────────────────────────────────────────────
        g.MapPost("/{id:guid}/cancel", async (Guid id, ProductionOrderActionRequest request, NanchesoftDbContext db) =>
        {
            var voucher = await db.ProductionVouchers.FirstOrDefaultAsync(x => x.Id == id);
            if (voucher is null) return Results.NotFound(new { message = "Vale no encontrado." });
            if (voucher.Status == "completed" || voucher.Status == "cancelled")
                return Results.BadRequest(new { message = $"No se puede cancelar un vale en estado '{voucher.Status}'." });

            voucher.Status = "cancelled";
            voucher.CancelledDate = DateOnly.FromDateTime(DateTime.UtcNow);
            voucher.CancelledReason = request.Reason;
            voucher.UpdatedAt = DateTime.UtcNow;
            voucher.UpdatedBy = request.UserId ?? "api";

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Vale cancelado." });
        });

        // ─── Print / PDF ─────────────────────────────────────────────────────
        g.MapPost("/{id:guid}/print", async (Guid id, PrintVoucherRequest request, NanchesoftDbContext db) =>
        {
            var voucher = await db.ProductionVouchers.AsNoTracking()
                .Include(x => x.ProductionPhase)
                .Include(x => x.ProductionCell)
                .Include(x => x.Details).ThenInclude(d => d.SizeRunSize)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (voucher is null) return Results.NotFound(new { message = "Vale no encontrado." });

            var order = await db.ProductionOrders.AsNoTracking()
                .Select(x => new { x.Id, x.Folio, x.WeekCode, x.DeliveryDate })
                .FirstOrDefaultAsync(x => x.Id == voucher.ProductionOrderId);

            var line = await db.ProductionOrderLines.AsNoTracking()
                .Include(x => x.FinishedProduct)
                .FirstOrDefaultAsync(x => x.Id == voucher.ProductionOrderLineId);

            try
            {
                var pdfBytes = BuildVoucherPdf(voucher, order?.Folio ?? string.Empty, order?.WeekCode ?? string.Empty,
                    order?.DeliveryDate, line?.FinishedProduct?.Code ?? string.Empty,
                    line?.FinishedProduct?.Name ?? string.Empty, request.CompanyName ?? "Nanchesoft ERP");

                // Mark as printed
                var tracked = await db.ProductionVouchers.FirstOrDefaultAsync(x => x.Id == id);
                if (tracked is not null)
                {
                    tracked.Printed = true;
                    tracked.PrintedAt = DateTime.UtcNow;
                    tracked.PrintCount++;
                    await db.SaveChangesAsync();
                }

                return Results.Ok(new { base64 = Convert.ToBase64String(pdfBytes), fileName = $"VALE_{voucher.Folio}.pdf" });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
    }

    private static async Task<string> GenerateVoucherFolioAsync(
        NanchesoftDbContext db, Guid tenantId, Guid companyId, string by)
    {
        var series = await db.DocumentSeries
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.DocumentType == "PRODUCTION_VOUCHER" && x.IsDefault && x.IsActive);

        if (series is null)
            return $"VALE-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var folio = await db.DocumentFolios
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.DocumentType == "PRODUCTION_VOUCHER" && x.SeriesId == series.Id);

        var number = folio?.CurrentNumber ?? series.CurrentNumber;
        var formatted = $"{series.Prefix}{number.ToString().PadLeft(series.NumberLength, '0')}";

        if (folio is not null)
        {
            folio.CurrentNumber = number + 1;
            folio.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            series.CurrentNumber = number + 1;
            series.UpdatedAt = DateTime.UtcNow;
        }

        return formatted;
    }

    private static byte[] BuildVoucherPdf(
        ProductionVoucher voucher,
        string orderFolio,
        string weekCode,
        DateOnly? deliveryDate,
        string productCode,
        string productName,
        string companyName)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(15, QuestPDF.Infrastructure.Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().PaddingBottom(6).BorderBottom(2).BorderColor(Colors.Blue.Darken2).Row(row =>
                {
                    row.RelativeItem(2).Column(col =>
                    {
                        col.Item().Text(companyName).FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().Text("TARJETA DE PRODUCCIÓN").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                    row.RelativeItem(1).AlignRight().Column(col =>
                    {
                        col.Item().Text(voucher.Folio).FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text(voucher.IssuedDate.ToString("dd/MM/yyyy")).FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Spacing(6);

                    // Info grid
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });

                        void InfoRow(string label, string value)
                        {
                            table.Cell().Padding(3).Column(c =>
                            {
                                c.Item().Text(label).FontSize(7).FontColor(Colors.Grey.Darken2);
                                c.Item().Text(value).FontSize(9).Bold();
                            });
                        }

                        InfoRow("ORDEN DE PRODUCCIÓN", orderFolio);
                        InfoRow("SEMANA", weekCode);
                        InfoRow("FASE", voucher.ProductionPhase?.Name ?? string.Empty);
                        InfoRow("CÉLULA", voucher.ProductionCell?.Name ?? "Sin asignar");
                        InfoRow("PRODUCTO", $"{productCode}");
                        InfoRow("LOTE", voucher.LotNumber);
                        InfoRow("DESCRIPCIÓN", productName);
                        InfoRow("FECHA ENTREGA", deliveryDate?.ToString("dd/MM/yyyy") ?? "—");
                    });

                    // Size quantities table
                    if (voucher.Details.Any())
                    {
                        col.Item().Column(sc =>
                        {
                            sc.Item().PaddingTop(4).Text("Cantidades por talla").FontSize(8).Bold().FontColor(Colors.Blue.Darken2);
                            sc.Item().Table(table =>
                            {
                                var details = voucher.Details.OrderBy(d => d.SizeRunSize?.Sequence ?? 0).ToList();
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1.5f);
                                    foreach (var _ in details) c.RelativeColumn();
                                    c.RelativeColumn();
                                });

                                // Header row
                                table.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("").FontSize(8);
                                foreach (var d in details)
                                    table.Cell().Background(Colors.Blue.Darken2).Padding(4).AlignCenter()
                                        .Text(d.SizeRunSize?.DisplayLabel ?? "?").FontSize(8).Bold().FontColor(Colors.White);
                                table.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignCenter()
                                    .Text("TOTAL").FontSize(8).Bold().FontColor(Colors.White);

                                // Assigned row
                                table.Cell().Background(Colors.Blue.Lighten5).Padding(4).Text("ASIGNADO").FontSize(7).Bold();
                                foreach (var d in details)
                                    table.Cell().Background(Colors.Blue.Lighten5).Padding(4).AlignCenter()
                                        .Text(d.QuantityAssigned.ToString()).FontSize(9).Bold();
                                table.Cell().Background(Colors.Blue.Lighten4).Padding(4).AlignCenter()
                                    .Text(details.Sum(d => d.QuantityAssigned).ToString()).FontSize(9).Bold();

                                // Produced row (blank — filled by worker)
                                table.Cell().Padding(4).Text("PRODUCIDO").FontSize(7).FontColor(Colors.Grey.Darken1);
                                foreach (var _ in details)
                                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8);
                                table.Cell().Background(Colors.Grey.Lighten4).Padding(8);

                                // Rejected row (blank — filled by worker)
                                table.Cell().Padding(4).Text("RECHAZADO").FontSize(7).FontColor(Colors.Grey.Darken1);
                                foreach (var _ in details)
                                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8);
                                table.Cell().Background(Colors.Grey.Lighten4).Padding(8);
                            });
                        });
                    }

                    // Signature boxes
                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(20);
                            c.Item().PaddingTop(4).AlignCenter().Text("Operador").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(20);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(20);
                            c.Item().PaddingTop(4).AlignCenter().Text("Supervisor").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(20);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(20);
                            c.Item().PaddingTop(4).AlignCenter().Text("Calidad").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });

                page.Footer().AlignCenter()
                    .Text($"Nanchesoft ERP · {voucher.Folio} · Impreso: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();
    }
}

public sealed class ProductionVoucherSummaryDto
{
    public Guid ProductionVoucherId { get; set; }
    public Guid ProductionOrderId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public string PhaseName { get; set; } = string.Empty;
    public int BatchSize { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly IssuedDate { get; set; }
    public string? IssuedBy { get; set; }
    public DateOnly? CompletedDate { get; set; }
    public bool Printed { get; set; }
    public int PrintCount { get; set; }
}

public sealed class ProductionVoucherDetailDto
{
    public Guid ProductionVoucherId { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid ProductionOrderLineId { get; set; }
    public string OrderFolio { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public string PhaseName { get; set; } = string.Empty;
    public string CellName { get; set; } = string.Empty;
    public int BatchSize { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly IssuedDate { get; set; }
    public string? IssuedBy { get; set; }
    public DateOnly? CompletedDate { get; set; }
    public string? CompletedBy { get; set; }
    public DateOnly? CancelledDate { get; set; }
    public string? CancelledReason { get; set; }
    public bool Printed { get; set; }
    public DateTime? PrintedAt { get; set; }
    public int PrintCount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<ProductionVoucherDetailLineDto> Details { get; set; } = new();
}

public sealed class ProductionVoucherDetailLineDto
{
    public Guid ProductionVoucherDetailId { get; set; }
    public string SizeCode { get; set; } = string.Empty;
    public string SizeLabel { get; set; } = string.Empty;
    public int QuantityAssigned { get; set; }
    public int QuantityProduced { get; set; }
    public int QuantityRejected { get; set; }
    public string OperationCode { get; set; } = string.Empty;
}

public sealed class IssueVoucherRequest
{
    public Guid ProductionOrderId { get; set; }
    public Guid ProductionOrderLineId { get; set; }
    public Guid ProductionPhaseId { get; set; }
    public Guid? ProductionCellId { get; set; }
    public string? LotNumber { get; set; }
    public int BatchSize { get; set; }
    public string? Notes { get; set; }
    public string? UserId { get; set; }
    public List<VoucherSizeDetailRequest>? SizeDetails { get; set; }
}

public sealed class VoucherSizeDetailRequest
{
    public Guid? SizeRunSizeId { get; set; }
    public int QuantityAssigned { get; set; }
    public string? OperationCode { get; set; }
}

public sealed class CompleteVoucherRequest
{
    public string? UserId { get; set; }
    public List<VoucherSizeResultRequest>? SizeResults { get; set; }
}

public sealed class VoucherSizeResultRequest
{
    public Guid? SizeRunSizeId { get; set; }
    public int QuantityProduced { get; set; }
    public int QuantityRejected { get; set; }
}

public sealed class PrintVoucherRequest
{
    public string? CompanyName { get; set; }
}

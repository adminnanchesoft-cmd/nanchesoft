using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ConsumptionTemplateEndpoints
{
    public static void MapConsumptionTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/consumption-templates").WithTags("ConsumptionTemplates");

        g.MapGet("/", async (Guid? productStyleId, Guid? productSizeRunId, NanchesoftDbContext db) =>
        {
            var query = db.ConsumptionTemplates
                .AsNoTracking()
                .Include(x => x.ProductStyle)
                .Include(x => x.ProductSizeRun)
                .Include(x => x.Details).ThenInclude(d => d.ProductComponent).ThenInclude(c => c!.ConsumptionUnit)
                .Include(x => x.Details).ThenInclude(d => d.Sizes).ThenInclude(s => s.ProductSizeRunSize)
                .AsQueryable();

            if (productStyleId.HasValue)
                query = query.Where(x => x.ProductStyleId == productStyleId.Value);
            if (productSizeRunId.HasValue)
                query = query.Where(x => x.ProductSizeRunId == productSizeRunId.Value);

            var templates = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
            return Results.Ok(templates.Select(MapToDto).ToList());
        });

        g.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var template = await db.ConsumptionTemplates
                .AsNoTracking()
                .Include(x => x.ProductStyle)
                .Include(x => x.ProductSizeRun).ThenInclude(r => r.Sizes.OrderBy(s => s.Sequence))
                .Include(x => x.Details).ThenInclude(d => d.ProductComponent).ThenInclude(c => c!.ConsumptionUnit)
                .Include(x => x.Details).ThenInclude(d => d.Sizes).ThenInclude(s => s.ProductSizeRunSize)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (template is null) return Results.NotFound(new { message = "Plantilla no encontrada." });
            return Results.Ok(MapToDto(template));
        });

        g.MapPost("/initialize", async (ConsumptionTemplateInitializeRequest request, NanchesoftDbContext db) =>
        {
            if (request.ProductStyleId == Guid.Empty || request.ProductSizeRunId == Guid.Empty)
                return Results.BadRequest(new { message = "Estilo y corrida son obligatorios." });

            var company = await db.Companies.OrderBy(x => x.CreatedAt)
                .Select(x => new { x.Id }).FirstOrDefaultAsync();
            if (company is null) return Results.BadRequest(new { message = "No hay empresa configurada." });

            var existing = await db.ConsumptionTemplates
                .AnyAsync(x => x.CompanyId == company.Id
                            && x.ProductStyleId == request.ProductStyleId
                            && x.ProductSizeRunId == request.ProductSizeRunId
                            && x.IsActive);
            if (existing)
                return Results.BadRequest(new { message = "Ya existe una plantilla activa para este estilo y corrida." });

            var sizeRun = await db.ProductSizeRuns
                .Include(x => x.Sizes.OrderBy(s => s.Sequence))
                .FirstOrDefaultAsync(x => x.Id == request.ProductSizeRunId);
            if (sizeRun is null) return Results.BadRequest(new { message = "Corrida no encontrada." });

            var components = await db.ProductComponents
                .Where(x => x.CompanyId == company.Id && x.IsActive)
                .OrderBy(x => x.Code)
                .ToListAsync();

            var template = new ConsumptionTemplate
            {
                CompanyId = company.Id,
                ProductStyleId = request.ProductStyleId,
                ProductSizeRunId = request.ProductSizeRunId,
                Notes = request.Notes ?? string.Empty,
                CreatedBy = "web-api"
            };

            foreach (var component in components)
            {
                var detail = new ConsumptionTemplateDetail
                {
                    ProductComponentId = component.Id,
                    Pieces = 1,
                    DispersionMode = "paired",
                    CreatedBy = "web-api"
                };
                foreach (var size in sizeRun.Sizes)
                {
                    detail.Sizes.Add(new ConsumptionTemplateSize
                    {
                        ProductSizeRunSizeId = size.Id,
                        Consumption = component.DefaultConsumption,
                        CreatedBy = "web-api"
                    });
                }
                template.Details.Add(detail);
            }

            db.ConsumptionTemplates.Add(template);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = template.Id });
        });

        g.MapPut("/{id:guid}", async (Guid id, ConsumptionTemplateUpdateRequest request, NanchesoftDbContext db) =>
        {
            var template = await db.ConsumptionTemplates
                .Include(x => x.Details).ThenInclude(d => d.ProductComponent).ThenInclude(c => c!.ConsumptionUnit)
                .Include(x => x.Details).ThenInclude(d => d.Sizes).ThenInclude(s => s.ProductSizeRunSize)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (template is null) return Results.NotFound(new { message = "Plantilla no encontrada." });
            if (template.IsAuthorized)
                return Results.BadRequest(new { message = "No puedes modificar una plantilla ya autorizada." });

            template.Notes = request.Notes ?? string.Empty;
            template.UpdatedAt = DateTime.UtcNow;
            template.UpdatedBy = "web-api";

            foreach (var detailUpdate in request.Details)
            {
                var detail = template.Details.FirstOrDefault(d => d.Id == detailUpdate.DetailId);
                if (detail is null) continue;

                detail.Pieces = detailUpdate.Pieces;
                detail.IsActive = detailUpdate.IsActive;
                detail.Notes = detailUpdate.Notes ?? string.Empty;
                detail.DispersionMode = detailUpdate.DispersionMode ?? "paired";
                detail.UpdatedAt = DateTime.UtcNow;
                detail.UpdatedBy = "web-api";

                foreach (var sizeUpdate in detailUpdate.Sizes)
                {
                    var size = detail.Sizes.FirstOrDefault(s => s.Id == sizeUpdate.SizeId);
                    if (size is null) continue;
                    size.Consumption = sizeUpdate.Consumption;
                    size.UpdatedAt = DateTime.UtcNow;
                    size.UpdatedBy = "web-api";
                }

                // Re-apply DCM2 spread server-side to guarantee consistency
                bool isDcm2 = detail.ProductComponent?.ConsumptionUnit?.Code
                    ?.Equals("DCM2", StringComparison.OrdinalIgnoreCase) == true;
                if (isDcm2 && detail.IsActive)
                {
                    var sorted = detail.Sizes.OrderBy(s => s.ProductSizeRunSize?.Sequence ?? 0).ToList();
                    ApplyConsumptionSpread(sorted, detail.DispersionMode);
                }
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        g.MapPost("/{id:guid}/authorize", async (Guid id, NanchesoftDbContext db) =>
        {
            var template = await db.ConsumptionTemplates
                .Include(x => x.Details).ThenInclude(d => d.Sizes)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (template is null) return Results.NotFound(new { message = "Plantilla no encontrada." });
            if (template.IsAuthorized)
                return Results.BadRequest(new { message = "La plantilla ya está autorizada." });

            var activeDetails = template.Details.Where(d => d.IsActive).ToList();
            if (activeDetails.Count == 0)
                return Results.BadRequest(new { message = "No hay componentes activos para autorizar." });

            foreach (var detail in activeDetails)
            {
                if (detail.Pieces <= 0)
                    return Results.BadRequest(new { message = $"El componente tiene piezas inválidas." });

                var invalidSizes = detail.Sizes.Where(s => s.IsActive && s.Consumption <= 0).ToList();
                if (invalidSizes.Count > 0)
                    return Results.BadRequest(new { message = $"Existen tallas sin consumo en un componente activo." });
            }

            template.IsAuthorized = true;
            template.AuthorizedAt = DateTime.UtcNow;
            template.AuthorizedBy = "web-api";
            template.UpdatedAt = DateTime.UtcNow;
            template.UpdatedBy = "web-api";

            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        g.MapPost("/{id:guid}/copy-from", async (Guid id, ConsumptionTemplateCopyFromRequest request, NanchesoftDbContext db) =>
        {
            var target = await db.ConsumptionTemplates
                .Include(x => x.Details).ThenInclude(d => d.Sizes)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (target is null) return Results.NotFound(new { message = "Plantilla destino no encontrada." });
            if (target.IsAuthorized)
                return Results.BadRequest(new { message = "No puedes copiar sobre una plantilla ya autorizada." });

            var source = await db.ConsumptionTemplates
                .AsNoTracking()
                .Include(x => x.Details).ThenInclude(d => d.Sizes)
                .FirstOrDefaultAsync(x => x.Id == request.SourceTemplateId && x.IsActive);
            if (source is null) return Results.NotFound(new { message = "Plantilla origen no encontrada o inactiva." });

            var targetSizes = await db.ProductSizeRunSizes
                .AsNoTracking()
                .Where(x => x.ProductSizeRunId == target.ProductSizeRunId)
                .OrderBy(x => x.Sequence)
                .ToListAsync();

            var sourceSizesBySequence = source.Details
                .SelectMany(d => d.Sizes.Select(s => new { d.ProductComponentId, s.ProductSizeRunSize?.Sequence, s.Consumption }))
                .Where(x => x.Sequence.HasValue)
                .GroupBy(x => x.ProductComponentId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.Sequence!.Value, x => x.Consumption));

            var sourceDetailPieces = source.Details
                .ToDictionary(d => d.ProductComponentId, d => d.Pieces);
            var sourceDetailModes = source.Details
                .ToDictionary(d => d.ProductComponentId, d => d.DispersionMode);

            foreach (var detail in target.Details)
            {
                if (sourceDetailPieces.TryGetValue(detail.ProductComponentId, out var pieces))
                    detail.Pieces = pieces;
                if (sourceDetailModes.TryGetValue(detail.ProductComponentId, out var mode))
                    detail.DispersionMode = mode;

                if (!sourceSizesBySequence.TryGetValue(detail.ProductComponentId, out var sizeMap)) continue;

                foreach (var size in detail.Sizes)
                {
                    var seq = targetSizes.FirstOrDefault(s => s.Id == size.ProductSizeRunSizeId)?.Sequence;
                    if (seq.HasValue && sizeMap.TryGetValue(seq.Value, out var consumption))
                        size.Consumption = consumption;
                }
            }

            target.UpdatedAt = DateTime.UtcNow;
            target.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });
    }

    // Applies DCM2 dispersion in-place. Sizes must be sorted by sequence ascending.
    private static void ApplyConsumptionSpread(List<ConsumptionTemplateSize> sizes, string mode)
    {
        if (sizes.Count < 2) return;
        decimal first = sizes[0].Consumption;
        decimal last = sizes[^1].Consumption;

        if (mode == "paired")
        {
            // Group sizes into pairs. Each pair gets the same linearly-interpolated value.
            // Pair 0 = tallas[0,1], pair 1 = tallas[2,3], etc.
            // Number of "steps" = (ceil(n/2) - 1)
            int pairCount = (sizes.Count + 1) / 2;
            if (pairCount < 2) return;
            decimal z = (last - first) / (pairCount - 1);
            for (int i = 0; i < sizes.Count; i++)
                sizes[i].Consumption = Math.Round(first + z * (i / 2), 4, MidpointRounding.AwayFromZero);
            sizes[^1].Consumption = last; // preserve exact user-captured last value
        }
        else // linear
        {
            decimal z = (last - first) / (sizes.Count - 1);
            for (int i = 0; i < sizes.Count; i++)
                sizes[i].Consumption = Math.Round(first + z * i, 4, MidpointRounding.AwayFromZero);
        }
    }

    private static ConsumptionTemplateDto MapToDto(ConsumptionTemplate t) => new()
    {
        Id = t.Id,
        CompanyId = t.CompanyId,
        ProductStyleId = t.ProductStyleId,
        ProductStyleCode = t.ProductStyle?.Code ?? string.Empty,
        ProductSizeRunId = t.ProductSizeRunId,
        ProductSizeRunName = t.ProductSizeRun?.Name ?? string.Empty,
        IsActive = t.IsActive,
        IsAuthorized = t.IsAuthorized,
        AuthorizedAt = t.AuthorizedAt,
        AuthorizedBy = t.AuthorizedBy,
        Notes = t.Notes,
        CreatedAt = t.CreatedAt,
        CreatedBy = t.CreatedBy,
        Status = t.IsAuthorized ? "authorized"
            : t.Details.Any(d => d.IsActive && d.Sizes.Any(s => s.IsActive && s.Consumption > 0))
                ? "incomplete" : "pending",
        Details = t.Details.OrderBy(d => d.ProductComponent?.Code).Select(d => new ConsumptionTemplateDetailDto
        {
            Id = d.Id,
            ConsumptionTemplateId = d.ConsumptionTemplateId,
            ProductComponentId = d.ProductComponentId,
            ProductComponentCode = d.ProductComponent?.Code ?? string.Empty,
            ProductComponentName = d.ProductComponent?.Name ?? string.Empty,
            UnitCode = d.ProductComponent?.ConsumptionUnit?.Code ?? string.Empty,
            UnitAbbreviation = d.ProductComponent?.ConsumptionUnit?.Abbreviation ?? string.Empty,
            Pieces = d.Pieces,
            IsActive = d.IsActive,
            DispersionMode = d.DispersionMode,
            Notes = d.Notes,
            Sizes = d.Sizes.OrderBy(s => s.ProductSizeRunSize?.Sequence).Select(s => new ConsumptionTemplateSizeDto
            {
                Id = s.Id,
                ConsumptionTemplateDetailId = s.ConsumptionTemplateDetailId,
                ProductSizeRunSizeId = s.ProductSizeRunSizeId,
                SizeCode = s.ProductSizeRunSize?.SizeCode ?? string.Empty,
                DisplayLabel = s.ProductSizeRunSize?.DisplayLabel ?? string.Empty,
                Sequence = s.ProductSizeRunSize?.Sequence ?? 0,
                Consumption = s.Consumption,
                IsActive = s.IsActive
            }).ToList()
        }).ToList()
    };
}

public sealed class ConsumptionTemplateDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProductStyleId { get; set; }
    public string ProductStyleCode { get; set; } = string.Empty;
    public Guid ProductSizeRunId { get; set; }
    public string ProductSizeRunName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsAuthorized { get; set; }
    public DateTime? AuthorizedAt { get; set; }
    public string? AuthorizedBy { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string Status { get; set; } = "pending";
    public List<ConsumptionTemplateDetailDto> Details { get; set; } = [];
}

public sealed class ConsumptionTemplateDetailDto
{
    public Guid Id { get; set; }
    public Guid ConsumptionTemplateId { get; set; }
    public Guid ProductComponentId { get; set; }
    public string ProductComponentCode { get; set; } = string.Empty;
    public string ProductComponentName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public string UnitAbbreviation { get; set; } = string.Empty;
    public int Pieces { get; set; }
    public bool IsActive { get; set; }
    public string DispersionMode { get; set; } = "paired";
    public string Notes { get; set; } = string.Empty;
    public List<ConsumptionTemplateSizeDto> Sizes { get; set; } = [];
}

public sealed class ConsumptionTemplateSizeDto
{
    public Guid Id { get; set; }
    public Guid ConsumptionTemplateDetailId { get; set; }
    public Guid ProductSizeRunSizeId { get; set; }
    public string SizeCode { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public decimal Consumption { get; set; }
    public bool IsActive { get; set; }
}

public sealed class ConsumptionTemplateInitializeRequest
{
    public Guid ProductStyleId { get; set; }
    public Guid ProductSizeRunId { get; set; }
    public string? Notes { get; set; }
}

public sealed class ConsumptionTemplateUpdateRequest
{
    public string? Notes { get; set; }
    public List<ConsumptionTemplateDetailUpdateRequest> Details { get; set; } = [];
}

public sealed class ConsumptionTemplateDetailUpdateRequest
{
    public Guid DetailId { get; set; }
    public int Pieces { get; set; }
    public bool IsActive { get; set; }
    public string? DispersionMode { get; set; }
    public string? Notes { get; set; }
    public List<ConsumptionTemplateSizeUpdateRequest> Sizes { get; set; } = [];
}

public sealed class ConsumptionTemplateSizeUpdateRequest
{
    public Guid SizeId { get; set; }
    public decimal Consumption { get; set; }
}

public sealed class ConsumptionTemplateCopyFromRequest
{
    public Guid SourceTemplateId { get; set; }
}

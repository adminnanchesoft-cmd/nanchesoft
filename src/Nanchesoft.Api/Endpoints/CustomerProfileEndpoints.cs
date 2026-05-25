using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class CustomerProfileEndpoints
{
    private static readonly string PdfUploadDir = "/opt/nanchesoft/uploads/fiscal-pdfs";

    public static void MapCustomerProfileEndpoints(this IEndpointRouteBuilder app)
    {
        // ─── Customer general ────────────────────────────────────────────────
        // (list/create/update already in ThirdPartiesAndProductsEndpoints)
        // We add enriched detail endpoint here
        app.MapGet("/api/third-parties/customers/{customerId:guid}/profile", async (Guid customerId, NanchesoftDbContext db) =>
        {
            var customer = await db.Customers.AsNoTracking()
                .Include(x => x.PriceList)
                .Include(x => x.Currency)
                .Where(x => x.Id == customerId)
                .Select(x => new
                {
                    CustomerId = x.Id,
                    x.TenantId,
                    x.CompanyId,
                    x.Code,
                    x.Name,
                    x.LegalName,
                    x.TaxId,
                    x.Email,
                    x.Phone,
                    x.CreditLimit,
                    x.PaymentTermDays,
                    x.IsActive,
                    PriceListId = x.PriceListId,
                    PriceListName = x.PriceList != null ? x.PriceList.Name : string.Empty,
                    CurrencyId = x.CurrencyId,
                    CurrencyCode = x.Currency != null ? x.Currency.Code : string.Empty
                })
                .FirstOrDefaultAsync();

            if (customer is null) return Results.NotFound();
            return Results.Ok(customer);
        }).WithTags("CustomerProfile");

        // ─── Customer legal entities (razones sociales) ──────────────────────
        app.MapGet("/api/third-parties/customers/{customerId:guid}/legal-entities", async (Guid customerId, NanchesoftDbContext db) =>
        {
            var list = await db.CustomerLegalEntities.AsNoTracking()
                .Where(x => x.CustomerId == customerId && x.IsActive)
                .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.LegalName)
                .Select(x => new CustomerLegalEntityDto
                {
                    LegalEntityId = x.Id,
                    CustomerId = x.CustomerId,
                    LegalName = x.LegalName,
                    TaxId = x.TaxId,
                    FiscalRegime = x.FiscalRegime,
                    ZipCode = x.ZipCode,
                    CfdiUse = x.CfdiUse,
                    FiscalSituationPdfPath = x.FiscalSituationPdfPath,
                    HasPdf = !string.IsNullOrEmpty(x.FiscalSituationPdfPath),
                    IsPrimary = x.IsPrimary,
                    IsActive = x.IsActive
                })
                .ToListAsync();
            return Results.Ok(list);
        }).WithTags("CustomerProfile");

        app.MapPost("/api/third-parties/customers/{customerId:guid}/legal-entities", async (
            Guid customerId, CustomerLegalEntityRequest req, NanchesoftDbContext db) =>
        {
            var customer = await db.Customers.AsNoTracking()
                .Where(x => x.Id == customerId)
                .Select(x => new { x.TenantId, x.CompanyId })
                .FirstOrDefaultAsync();
            if (customer is null) return Results.NotFound(new { message = "Cliente no encontrado." });

            if (req.IsPrimary)
            {
                var existing = await db.CustomerLegalEntities
                    .Where(x => x.CustomerId == customerId && x.IsPrimary).ToListAsync();
                foreach (var e in existing) e.IsPrimary = false;
            }

            var entity = new CustomerLegalEntity
            {
                TenantId = customer.TenantId,
                CompanyId = customer.CompanyId,
                CustomerId = customerId,
                LegalName = req.LegalName.Trim(),
                TaxId = req.TaxId.Trim().ToUpper(),
                FiscalRegime = req.FiscalRegime?.Trim() ?? string.Empty,
                ZipCode = req.ZipCode?.Trim() ?? string.Empty,
                CfdiUse = req.CfdiUse?.Trim() ?? string.Empty,
                IsPrimary = req.IsPrimary,
                CreatedBy = req.UserId ?? "api"
            };

            db.CustomerLegalEntities.Add(entity);
            await db.SaveChangesAsync();
            return Results.Created($"/api/third-parties/customers/{customerId}/legal-entities/{entity.Id}",
                new { LegalEntityId = entity.Id, entity.LegalName, entity.TaxId, entity.IsPrimary });
        }).WithTags("CustomerProfile");

        app.MapPut("/api/third-parties/customers/{customerId:guid}/legal-entities/{id:guid}", async (
            Guid customerId, Guid id, CustomerLegalEntityRequest req, NanchesoftDbContext db) =>
        {
            var entity = await db.CustomerLegalEntities
                .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId);
            if (entity is null) return Results.NotFound();

            if (req.IsPrimary && !entity.IsPrimary)
            {
                var others = await db.CustomerLegalEntities
                    .Where(x => x.CustomerId == customerId && x.IsPrimary && x.Id != id).ToListAsync();
                foreach (var e in others) e.IsPrimary = false;
            }

            entity.LegalName = req.LegalName.Trim();
            entity.TaxId = req.TaxId.Trim().ToUpper();
            entity.FiscalRegime = req.FiscalRegime?.Trim() ?? entity.FiscalRegime;
            entity.ZipCode = req.ZipCode?.Trim() ?? entity.ZipCode;
            entity.CfdiUse = req.CfdiUse?.Trim() ?? entity.CfdiUse;
            entity.IsPrimary = req.IsPrimary;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = req.UserId ?? "api";

            await db.SaveChangesAsync();
            return Results.Ok(new { entity.Id, entity.LegalName, entity.TaxId, entity.IsPrimary });
        }).WithTags("CustomerProfile");

        app.MapDelete("/api/third-parties/customers/{customerId:guid}/legal-entities/{id:guid}", async (
            Guid customerId, Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.CustomerLegalEntities
                .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId);
            if (entity is null) return Results.NotFound();
            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Razón social eliminada." });
        }).WithTags("CustomerProfile");

        // ─── PDF upload for fiscal situation ─────────────────────────────────
        app.MapPost("/api/third-parties/customers/{customerId:guid}/legal-entities/{id:guid}/fiscal-pdf",
            async (Guid customerId, Guid id, IFormFile file, NanchesoftDbContext db) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { message = "No se proporcionó archivo." });
            if (!file.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { message = "Solo se aceptan archivos PDF." });
            if (file.Length > 5 * 1024 * 1024)
                return Results.BadRequest(new { message = "El archivo no debe superar 5 MB." });

            var entity = await db.CustomerLegalEntities
                .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId);
            if (entity is null) return Results.NotFound();

            Directory.CreateDirectory(PdfUploadDir);

            // Delete old file if exists
            if (!string.IsNullOrEmpty(entity.FiscalSituationPdfPath))
            {
                var oldPath = Path.Combine(PdfUploadDir, entity.FiscalSituationPdfPath);
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }

            var fileName = $"{id:N}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            var fullPath = Path.Combine(PdfUploadDir, fileName);
            await using var stream = File.Create(fullPath);
            await file.CopyToAsync(stream);

            entity.FiscalSituationPdfPath = fileName;
            entity.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new { fileName, message = "PDF subido correctamente." });
        }).WithTags("CustomerProfile").DisableAntiforgery();

        // ─── Download PDF ─────────────────────────────────────────────────────
        app.MapGet("/api/third-parties/fiscal-pdf/{fileName}", (string fileName) =>
        {
            // Prevent path traversal
            if (fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains(".."))
                return Results.BadRequest();

            var fullPath = Path.Combine(PdfUploadDir, fileName);
            if (!File.Exists(fullPath)) return Results.NotFound();

            return Results.File(fullPath, "application/pdf", fileName);
        }).WithTags("CustomerProfile");

        // ─── Customer locations (sucursales / direcciones) ───────────────────
        app.MapGet("/api/third-parties/customers/{customerId:guid}/locations", async (Guid customerId, NanchesoftDbContext db) =>
        {
            var list = await db.ThirdPartyAddresses.AsNoTracking()
                .Where(x => x.ThirdPartyType == "customer" && x.ThirdPartyId == customerId && x.IsActive)
                .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.LocationName).ThenBy(x => x.Street)
                .Select(x => new CustomerLocationDto
                {
                    LocationId = x.Id,
                    LocationName = x.LocationName,
                    AddressType = x.AddressType,
                    Street = x.Street,
                    ExteriorNumber = x.ExteriorNumber,
                    InteriorNumber = x.InteriorNumber,
                    Neighborhood = x.Neighborhood,
                    ZipCode = x.ZipCode,
                    Reference = x.Reference,
                    IsPrimary = x.IsPrimary,
                    IsActive = x.IsActive,
                    FullAddress = (x.LocationName != "" ? x.LocationName + " — " : "") +
                                  string.Join(", ", new[] { (x.Street + " " + x.ExteriorNumber).Trim(), x.Neighborhood, x.ZipCode }
                                      .Where(s => s != ""))
                })
                .ToListAsync();
            return Results.Ok(list);
        }).WithTags("CustomerProfile");

        app.MapPost("/api/third-parties/customers/{customerId:guid}/locations", async (
            Guid customerId, CustomerLocationRequest req, NanchesoftDbContext db) =>
        {
            var customer = await db.Customers.AsNoTracking()
                .Where(x => x.Id == customerId)
                .Select(x => new { x.TenantId, x.CompanyId })
                .FirstOrDefaultAsync();
            if (customer is null) return Results.NotFound(new { message = "Cliente no encontrado." });

            if (req.IsPrimary)
            {
                var others = await db.ThirdPartyAddresses
                    .Where(x => x.ThirdPartyType == "customer" && x.ThirdPartyId == customerId && x.IsPrimary)
                    .ToListAsync();
                foreach (var o in others) o.IsPrimary = false;
            }

            var address = new ThirdPartyAddress
            {
                TenantId = customer.TenantId,
                CompanyId = customer.CompanyId,
                ThirdPartyType = "customer",
                ThirdPartyId = customerId,
                LocationName = req.LocationName?.Trim() ?? string.Empty,
                AddressType = req.AddressType?.Trim() ?? "Principal",
                Street = req.Street?.Trim() ?? string.Empty,
                ExteriorNumber = req.ExteriorNumber?.Trim() ?? string.Empty,
                InteriorNumber = req.InteriorNumber?.Trim() ?? string.Empty,
                Neighborhood = req.Neighborhood?.Trim() ?? string.Empty,
                ZipCode = req.ZipCode?.Trim() ?? string.Empty,
                Reference = req.Reference?.Trim() ?? string.Empty,
                IsPrimary = req.IsPrimary,
                CreatedBy = req.UserId ?? "api"
            };
            db.ThirdPartyAddresses.Add(address);
            await db.SaveChangesAsync();
            return Results.Created($"/api/third-parties/customers/{customerId}/locations/{address.Id}",
                new { LocationId = address.Id, address.LocationName, address.IsPrimary });
        }).WithTags("CustomerProfile");

        app.MapPut("/api/third-parties/customers/{customerId:guid}/locations/{id:guid}", async (
            Guid customerId, Guid id, CustomerLocationRequest req, NanchesoftDbContext db) =>
        {
            var address = await db.ThirdPartyAddresses
                .FirstOrDefaultAsync(x => x.Id == id && x.ThirdPartyType == "customer" && x.ThirdPartyId == customerId);
            if (address is null) return Results.NotFound();

            if (req.IsPrimary && !address.IsPrimary)
            {
                var others = await db.ThirdPartyAddresses
                    .Where(x => x.ThirdPartyType == "customer" && x.ThirdPartyId == customerId && x.IsPrimary && x.Id != id)
                    .ToListAsync();
                foreach (var o in others) o.IsPrimary = false;
            }

            address.LocationName = req.LocationName?.Trim() ?? address.LocationName;
            address.AddressType = req.AddressType?.Trim() ?? address.AddressType;
            address.Street = req.Street?.Trim() ?? address.Street;
            address.ExteriorNumber = req.ExteriorNumber?.Trim() ?? address.ExteriorNumber;
            address.InteriorNumber = req.InteriorNumber?.Trim() ?? address.InteriorNumber;
            address.Neighborhood = req.Neighborhood?.Trim() ?? address.Neighborhood;
            address.ZipCode = req.ZipCode?.Trim() ?? address.ZipCode;
            address.Reference = req.Reference?.Trim() ?? address.Reference;
            address.IsPrimary = req.IsPrimary;
            address.UpdatedAt = DateTime.UtcNow;
            address.UpdatedBy = req.UserId ?? "api";

            await db.SaveChangesAsync();
            return Results.Ok(new { address.Id, address.LocationName, address.IsPrimary });
        }).WithTags("CustomerProfile");

        app.MapDelete("/api/third-parties/customers/{customerId:guid}/locations/{id:guid}", async (
            Guid customerId, Guid id, NanchesoftDbContext db) =>
        {
            var address = await db.ThirdPartyAddresses
                .FirstOrDefaultAsync(x => x.Id == id && x.ThirdPartyType == "customer" && x.ThirdPartyId == customerId);
            if (address is null) return Results.NotFound();
            address.IsActive = false;
            address.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Ubicación eliminada." });
        }).WithTags("CustomerProfile");

        // ─── Quick-add endpoints (from production order) ─────────────────────
        app.MapPost("/api/third-parties/customers/{customerId:guid}/quick-add-legal-entity", async (
            Guid customerId, CustomerLegalEntityRequest req, NanchesoftDbContext db) =>
        {
            var customer = await db.Customers.AsNoTracking()
                .Where(x => x.Id == customerId)
                .Select(x => new { x.TenantId, x.CompanyId })
                .FirstOrDefaultAsync();
            if (customer is null) return Results.NotFound(new { message = "Cliente no encontrado." });

            var entity = new CustomerLegalEntity
            {
                TenantId = customer.TenantId,
                CompanyId = customer.CompanyId,
                CustomerId = customerId,
                LegalName = req.LegalName.Trim(),
                TaxId = req.TaxId.Trim().ToUpper(),
                FiscalRegime = req.FiscalRegime?.Trim() ?? string.Empty,
                ZipCode = req.ZipCode?.Trim() ?? string.Empty,
                CfdiUse = req.CfdiUse?.Trim() ?? string.Empty,
                IsPrimary = false,
                CreatedBy = req.UserId ?? "web"
            };
            db.CustomerLegalEntities.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new CustomerLegalEntityDto
            {
                LegalEntityId = entity.Id,
                CustomerId = entity.CustomerId,
                LegalName = entity.LegalName,
                TaxId = entity.TaxId,
                FiscalRegime = entity.FiscalRegime,
                ZipCode = entity.ZipCode,
                IsPrimary = entity.IsPrimary
            });
        }).WithTags("CustomerProfile");

        app.MapPost("/api/third-parties/customers/{customerId:guid}/quick-add-location", async (
            Guid customerId, CustomerLocationRequest req, NanchesoftDbContext db) =>
        {
            var customer = await db.Customers.AsNoTracking()
                .Where(x => x.Id == customerId)
                .Select(x => new { x.TenantId, x.CompanyId })
                .FirstOrDefaultAsync();
            if (customer is null) return Results.NotFound(new { message = "Cliente no encontrado." });

            var address = new ThirdPartyAddress
            {
                TenantId = customer.TenantId,
                CompanyId = customer.CompanyId,
                ThirdPartyType = "customer",
                ThirdPartyId = customerId,
                LocationName = req.LocationName?.Trim() ?? string.Empty,
                AddressType = req.AddressType?.Trim() ?? "Principal",
                Street = req.Street?.Trim() ?? string.Empty,
                ExteriorNumber = req.ExteriorNumber?.Trim() ?? string.Empty,
                Neighborhood = req.Neighborhood?.Trim() ?? string.Empty,
                ZipCode = req.ZipCode?.Trim() ?? string.Empty,
                IsPrimary = false,
                CreatedBy = req.UserId ?? "web"
            };
            db.ThirdPartyAddresses.Add(address);
            await db.SaveChangesAsync();

            var fullAddress = (string.IsNullOrEmpty(address.LocationName) ? "" : address.LocationName + " — ") +
                string.Join(", ", new[] { (address.Street + " " + address.ExteriorNumber).Trim(), address.Neighborhood, address.ZipCode }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

            return Results.Ok(new CustomerLocationDto
            {
                LocationId = address.Id,
                LocationName = address.LocationName,
                AddressType = address.AddressType,
                Street = address.Street,
                ExteriorNumber = address.ExteriorNumber,
                Neighborhood = address.Neighborhood,
                ZipCode = address.ZipCode,
                IsPrimary = address.IsPrimary,
                FullAddress = fullAddress
            });
        }).WithTags("CustomerProfile");
    }
}

// ─── DTOs ────────────────────────────────────────────────────────────────────

public sealed class CustomerLegalEntityDto
{
    public Guid LegalEntityId { get; set; }
    public Guid CustomerId { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string FiscalRegime { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string CfdiUse { get; set; } = string.Empty;
    public string FiscalSituationPdfPath { get; set; } = string.Empty;
    public bool HasPdf { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CustomerLegalEntityRequest
{
    public string LegalName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string? FiscalRegime { get; set; }
    public string? ZipCode { get; set; }
    public string? CfdiUse { get; set; }
    public bool IsPrimary { get; set; }
    public string? UserId { get; set; }
}

public sealed class CustomerLocationDto
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string AddressType { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string ExteriorNumber { get; set; } = string.Empty;
    public string InteriorNumber { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public string FullAddress { get; set; } = string.Empty;
}

public sealed class CustomerLocationRequest
{
    public string? LocationName { get; set; }
    public string? AddressType { get; set; }
    public string? Street { get; set; }
    public string? ExteriorNumber { get; set; }
    public string? InteriorNumber { get; set; }
    public string? Neighborhood { get; set; }
    public string? ZipCode { get; set; }
    public string? Reference { get; set; }
    public bool IsPrimary { get; set; }
    public string? UserId { get; set; }
}

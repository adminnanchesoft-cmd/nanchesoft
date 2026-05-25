using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ConstanciaImportEndpoints
{
    public static void MapConstanciaImportEndpoints(this IEndpointRouteBuilder app)
    {
        // ─── Parse PDF ────────────────────────────────────────────────────────
        app.MapPost("/api/third-parties/parse-constancia", async (IFormFile? file) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { message = "No se proporcionó archivo." });
            if (file.Length > 10 * 1024 * 1024)
                return Results.BadRequest(new { message = "El archivo no debe superar 10 MB." });

            var tmpPath = Path.GetTempFileName() + ".pdf";
            try
            {
                await using (var fs = File.Create(tmpPath))
                    await file.CopyToAsync(fs);

                var psi = new ProcessStartInfo("pdftotext")
                {
                    Arguments = $"-layout \"{tmpPath}\" -",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                using var proc = Process.Start(psi);
                if (proc is null)
                    return Results.StatusCode(500);

                var text = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();

                if (string.IsNullOrWhiteSpace(text))
                    return Results.BadRequest(new { message = "No se pudo extraer texto del PDF. Asegúrate de que sea una Constancia de Situación Fiscal." });

                var parsed = ConstanciaParser.Parse(text);
                if (!parsed.ParsedSuccessfully)
                    return Results.BadRequest(new { message = "No se encontró RFC válido en el PDF. Verifica que sea una Constancia de Situación Fiscal del SAT." });

                return Results.Ok(parsed);
            }
            finally
            {
                try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
            }
        }).WithTags("Constancia").DisableAntiforgery();

        // ─── Parse QR text ────────────────────────────────────────────────────
        app.MapPost("/api/third-parties/parse-constancia-qr", (ConstanciaQrRequest req) =>
        {
            if (string.IsNullOrWhiteSpace(req.QrText))
                return Results.BadRequest(new { message = "Texto del QR vacío." });

            var rfc = ConstanciaParser.ParseRfcFromQr(req.QrText);
            return rfc is not null
                ? Results.Ok(new { rfc, found = true })
                : Results.Ok(new { rfc = string.Empty, found = false });
        }).WithTags("Constancia");

        // ─── Import / Upsert supplier from Constancia ────────────────────────
        app.MapPost("/api/suppliers/import-constancia", async (
            Guid tenantId, Guid companyId,
            ImportConstanciaRequest req,
            NanchesoftDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Rfc))
                return Results.BadRequest(new { message = "RFC es obligatorio." });

            var rfc = req.Rfc.Trim().ToUpper();
            var incomingName = req.LegalName?.Trim() ?? string.Empty;

            var supplier = await db.Suppliers.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.CompanyId == companyId
                && x.TaxId == rfc);

            var isNew = supplier is null;

            if (isNew)
            {
                var code = await GenerateSupplierCodeAsync(tenantId, companyId, rfc, db);
                var displayName = string.IsNullOrWhiteSpace(incomingName) ? rfc : incomingName;
                supplier = new Supplier
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    Code = code,
                    Name = displayName,
                    LegalName = incomingName,
                    TaxId = rfc,
                    CreatedBy = req.UserId ?? "import"
                };
                db.Suppliers.Add(supplier);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(incomingName))
                {
                    supplier!.LegalName = incomingName;
                    if (supplier.Name == supplier.TaxId || string.IsNullOrWhiteSpace(supplier.Name))
                        supplier.Name = incomingName;
                }
                supplier!.TaxId = rfc;
                supplier.UpdatedAt = DateTime.UtcNow;
                supplier.UpdatedBy = req.UserId ?? "import";
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                supplierId = supplier.Id,
                isNew,
                supplierName = supplier.Name,
                supplierCode = supplier.Code,
                rfc,
                message = isNew
                    ? "Proveedor creado desde Constancia de Situación Fiscal."
                    : "Proveedor actualizado desde Constancia de Situación Fiscal."
            });
        }).WithTags("Constancia");

        // ─── Import / Upsert customer from Constancia ─────────────────────────
        app.MapPost("/api/third-parties/import-constancia", async (
            Guid tenantId, Guid companyId,
            ImportConstanciaRequest req,
            NanchesoftDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Rfc))
                return Results.BadRequest(new { message = "RFC es obligatorio." });

            var rfc = req.Rfc.Trim().ToUpper();
            var isNew = false;

            // Generic RFC codes used for "Público en General" — cannot be used as a
            // unique key. For these, also match by legal name to avoid false positives.
            var isGenericRfc = rfc is "XAXX010101000" or "XEXX010101000";
            var incomingName = req.LegalName?.Trim() ?? string.Empty;

            Customer? customer = null;

            if (isGenericRfc && !string.IsNullOrWhiteSpace(incomingName))
            {
                // Match by RFC + exact legal name (case-insensitive)
                customer = await db.Customers.FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId && x.CompanyId == companyId && x.IsActive
                    && x.TaxId == rfc
                    && x.LegalName.ToLower() == incomingName.ToLower());
            }
            else if (!isGenericRfc)
            {
                // Real RFC — deduplicate by RFC on the customer record
                customer = await db.Customers.FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId && x.CompanyId == companyId && x.IsActive
                    && x.TaxId == rfc);

                // Also check legal entities if not found on the customer itself
                if (customer is null)
                {
                    var leCustomerId = await db.CustomerLegalEntities
                        .Where(x => x.TenantId == tenantId && x.CompanyId == companyId
                                    && x.TaxId == rfc && x.IsActive)
                        .Select(x => (Guid?)x.CustomerId)
                        .FirstOrDefaultAsync();

                    if (leCustomerId.HasValue)
                        customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == leCustomerId.Value);
                }
            }

            if (customer is null)
            {
                isNew = true;
                var code = await GenerateCustomerCodeAsync(tenantId, companyId, rfc, db);
                var customerName = string.IsNullOrWhiteSpace(incomingName) ? rfc : incomingName;

                customer = new Customer
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    Code = code,
                    Name = customerName,
                    LegalName = incomingName,
                    TaxId = rfc,
                    IsActive = true,
                    CreatedBy = req.UserId ?? "import"
                };
                db.Customers.Add(customer);
                await db.SaveChangesAsync();
            }
            else
            {
                // Update with fresh data from the constancia (SAT data always wins)
                if (!string.IsNullOrWhiteSpace(incomingName))
                {
                    customer.LegalName = incomingName;
                    // Sync display name if it was previously defaulted to the RFC string
                    if (customer.Name == customer.TaxId || string.IsNullOrWhiteSpace(customer.Name))
                        customer.Name = incomingName;
                }
                customer.TaxId = rfc;
                customer.UpdatedAt = DateTime.UtcNow;
                customer.UpdatedBy = req.UserId ?? "import";
            }

            // Upsert legal entity
            var legalEntity = await db.CustomerLegalEntities
                .FirstOrDefaultAsync(x => x.CustomerId == customer.Id && x.TaxId == rfc && x.IsActive);

            if (legalEntity is null)
            {
                var otherPrimaries = await db.CustomerLegalEntities
                    .Where(x => x.CustomerId == customer.Id && x.IsPrimary && x.IsActive).ToListAsync();
                foreach (var o in otherPrimaries) o.IsPrimary = false;

                legalEntity = new CustomerLegalEntity
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    CustomerId = customer.Id,
                    LegalName = req.LegalName?.Trim() ?? string.Empty,
                    TaxId = rfc,
                    FiscalRegime = req.FiscalRegimeCode ?? string.Empty,
                    ZipCode = req.ZipCode ?? string.Empty,
                    IsPrimary = true,
                    CreatedBy = req.UserId ?? "import"
                };
                db.CustomerLegalEntities.Add(legalEntity);
            }
            else
            {
                if (!string.IsNullOrEmpty(req.LegalName)) legalEntity.LegalName = req.LegalName.Trim();
                if (!string.IsNullOrEmpty(req.FiscalRegimeCode)) legalEntity.FiscalRegime = req.FiscalRegimeCode;
                if (!string.IsNullOrEmpty(req.ZipCode)) legalEntity.ZipCode = req.ZipCode;
                legalEntity.UpdatedAt = DateTime.UtcNow;
                legalEntity.UpdatedBy = req.UserId ?? "import";
            }

            // Upsert fiscal address
            if (!string.IsNullOrEmpty(req.Street) || !string.IsNullOrEmpty(req.ZipCode))
            {
                var address = await db.ThirdPartyAddresses
                    .FirstOrDefaultAsync(x => x.ThirdPartyType == "customer" && x.ThirdPartyId == customer.Id
                                               && x.AddressType == "Fiscal" && x.IsActive);

                var refNote = new[] { req.Municipality, req.State }.Where(s => !string.IsNullOrEmpty(s));
                var refText = string.Join(", ", refNote);

                if (address is null)
                {
                    address = new ThirdPartyAddress
                    {
                        TenantId = tenantId,
                        CompanyId = companyId,
                        ThirdPartyType = "customer",
                        ThirdPartyId = customer.Id,
                        AddressType = "Fiscal",
                        LocationName = "Domicilio fiscal (CSF)",
                        Street = req.Street ?? string.Empty,
                        ExteriorNumber = req.ExteriorNumber ?? string.Empty,
                        InteriorNumber = req.InteriorNumber ?? string.Empty,
                        Neighborhood = req.Neighborhood ?? string.Empty,
                        ZipCode = req.ZipCode ?? string.Empty,
                        Reference = refText,
                        IsPrimary = isNew,
                        CreatedBy = req.UserId ?? "import"
                    };
                    db.ThirdPartyAddresses.Add(address);
                }
                else
                {
                    if (!string.IsNullOrEmpty(req.Street)) address.Street = req.Street;
                    if (!string.IsNullOrEmpty(req.ExteriorNumber)) address.ExteriorNumber = req.ExteriorNumber;
                    if (req.InteriorNumber is not null) address.InteriorNumber = req.InteriorNumber;
                    if (!string.IsNullOrEmpty(req.Neighborhood)) address.Neighborhood = req.Neighborhood;
                    if (!string.IsNullOrEmpty(req.ZipCode)) address.ZipCode = req.ZipCode;
                    if (!string.IsNullOrEmpty(refText)) address.Reference = refText;
                    address.UpdatedAt = DateTime.UtcNow;
                    address.UpdatedBy = req.UserId ?? "import";
                }
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                customerId = customer.Id,
                isNew,
                customerName = customer.Name,
                customerCode = customer.Code,
                rfc,
                message = isNew ? "Cliente creado desde Constancia de Situación Fiscal." : "Cliente actualizado desde Constancia de Situación Fiscal."
            });
        }).WithTags("Constancia");
    }

    private static async Task<string> GenerateSupplierCodeAsync(Guid tenantId, Guid companyId, string rfc, NanchesoftDbContext db)
    {
        var prefix = rfc.Length >= 7 ? $"PRV-{rfc[..4]}" : $"PRV-{rfc}";
        if (!await db.Suppliers.AnyAsync(x => x.TenantId == tenantId && x.CompanyId == companyId && x.Code == prefix))
            return prefix;
        for (int i = 2; i <= 99; i++)
        {
            var candidate = $"{prefix}-{i}";
            if (!await db.Suppliers.AnyAsync(x => x.TenantId == tenantId && x.CompanyId == companyId && x.Code == candidate))
                return candidate;
        }
        return $"PRV-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    private static async Task<string> GenerateCustomerCodeAsync(Guid tenantId, Guid companyId, string rfc, NanchesoftDbContext db)
    {
        var prefix = rfc.Length >= 7 ? $"CLI-{rfc[..4]}" : $"CLI-{rfc}";
        if (!await db.Customers.AnyAsync(x => x.TenantId == tenantId && x.CompanyId == companyId && x.Code == prefix))
            return prefix;

        for (int i = 2; i <= 99; i++)
        {
            var candidate = $"{prefix}-{i}";
            if (!await db.Customers.AnyAsync(x => x.TenantId == tenantId && x.CompanyId == companyId && x.Code == candidate))
                return candidate;
        }

        return $"CLI-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}

// ─── ConstanciaParser ─────────────────────────────────────────────────────────

public static class ConstanciaParser
{
    private static readonly Regex RfcLabeled = new(@"RFC:\s*([A-Z&Ñ]{3,4}\d{6}[A-Z0-9]{3})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RfcFallback = new(@"\b([A-Z&Ñ]{3,4}\d{6}[A-Z0-9]{3})\b", RegexOptions.Compiled);

    public static ConstanciaData Parse(string text)
    {
        var data = new ConstanciaData();

        // RFC
        var m = RfcLabeled.Match(text);
        data.Rfc = m.Success
            ? m.Groups[1].Value.ToUpper()
            : (RfcFallback.Match(text) is { Success: true } fb ? fb.Groups[1].Value.ToUpper() : string.Empty);

        // Names
        data.FirstName = Field(text, @"Nombre\s*\(\s*[Ss]\s*\)") ?? string.Empty;
        data.LastName1 = Field(text, @"Primer\s+Apellido") ?? string.Empty;
        data.LastName2 = Field(text, @"Segundo\s+Apellido") ?? string.Empty;
        var businessName = Field(text, @"Denominaci[oó]n\s*/\s*Raz[oó]n\s*Social")
                         ?? Field(text, @"Raz[oó]n\s*Social");

        data.LegalName = !string.IsNullOrEmpty(businessName)
            ? businessName
            : string.Join(" ", new[] { data.LastName1, data.LastName2, data.FirstName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        // Address — Código Postal may have no space after colon
        var cpMatch = Regex.Match(text, @"[Cc][oó]digo\s+[Pp]ostal:?\s*(\d{5})", RegexOptions.IgnoreCase);
        data.ZipCode = cpMatch.Success ? cpMatch.Groups[1].Value : string.Empty;

        data.StreetType = Field(text, @"Tipo de Vialidad") ?? string.Empty;
        data.Street = Field(text, @"Nombre de Vialidad") ?? string.Empty;
        data.ExteriorNumber = Field(text, @"N[uú]mero\s+Exterior") ?? string.Empty;
        data.InteriorNumber = Field(text, @"N[uú]mero\s+Interior") ?? string.Empty;
        data.Neighborhood = Field(text, @"Nombre de la Colonia") ?? Field(text, @"Colonia\b") ?? string.Empty;
        data.Municipality = Field(text, @"Nombre del Municipio[^:]*") ?? Field(text, @"Municipio[^:]*") ?? string.Empty;
        data.State = Field(text, @"Nombre de la Entidad Federativa") ?? Field(text, @"Entidad Federativa") ?? string.Empty;

        // Fiscal regime
        data.PrimaryFiscalRegime = DetermineRegimeCode(text);

        data.ParsedSuccessfully = !string.IsNullOrEmpty(data.Rfc);
        return data;
    }

    private static string? Field(string text, string labelPattern)
    {
        var labelRegex = new Regex(labelPattern + @":", RegexOptions.IgnoreCase);

        foreach (var line in text.Split('\n'))
        {
            var m = labelRegex.Match(line);
            if (!m.Success) continue;

            // Take everything after the label+colon, strip leading spaces
            var rest = line[(m.Index + m.Length)..].TrimStart();
            if (string.IsNullOrEmpty(rest)) return null;

            // Extract up to the first 3+ space gap (two-column layout) or end of line
            var valueMatch = Regex.Match(rest, @"^([^\n]+?)(?:\s{3,}|$)");
            if (!valueMatch.Success) return null;

            var v = valueMatch.Groups[1].Value.Trim();
            // If the extracted segment contains ":" it is the next label, not a value
            if (string.IsNullOrEmpty(v) || v.Contains(':')) return null;
            return v;
        }

        return null;
    }

    private static string DetermineRegimeCode(string text)
    {
        // Explicit code format: "612 - Name"
        var coded = Regex.Match(text, @"\b(6\d{2})\s*[-–]");
        if (coded.Success) return coded.Groups[1].Value;

        // Regime section for name-based matching
        var sectionMatch = Regex.Match(text, @"Reg[ií]menes?[:\s]+(.*?)(?:Obligaciones|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var s = sectionMatch.Success ? sectionMatch.Groups[1].Value : text;

        if (Regex.IsMatch(s, @"Actividades\s+Empresariales\s+y\s+Profesionales", RegexOptions.IgnoreCase)) return "612";
        if (Regex.IsMatch(s, @"Simplificado\s+de\s+Confianza|RESICO", RegexOptions.IgnoreCase)) return "626";
        if (Regex.IsMatch(s, @"Incorporaci[oó]n\s+Fiscal", RegexOptions.IgnoreCase)) return "621";
        if (Regex.IsMatch(s, @"General\s+de\s+Ley\s+Personas\s+Morales", RegexOptions.IgnoreCase)) return "601";
        if (Regex.IsMatch(s, @"Sueldos\s+y\s+Salarios", RegexOptions.IgnoreCase)) return "605";
        if (Regex.IsMatch(s, @"Arrendamiento", RegexOptions.IgnoreCase)) return "606";
        if (Regex.IsMatch(s, @"Sin\s+obligaciones\s+fiscales", RegexOptions.IgnoreCase)) return "616";
        if (Regex.IsMatch(s, @"Plataformas\s+Tecnol[oó]gicas", RegexOptions.IgnoreCase)) return "625";
        if (Regex.IsMatch(s, @"Personas\s+F[ií]sicas", RegexOptions.IgnoreCase)) return "612";

        return string.Empty;
    }

    public static string? ParseRfcFromQr(string qrText)
    {
        // Cadena original: ||date|RFC|CONSTANCIA...|
        if (qrText.Contains("||"))
        {
            foreach (var part in qrText.Split('|'))
            {
                var t = part.Trim();
                if (Regex.IsMatch(t, @"^[A-Z&Ñ]{3,4}\d{6}[A-Z0-9]{3}$", RegexOptions.IgnoreCase))
                    return t.ToUpper();
            }
        }

        var m = RfcFallback.Match(qrText.ToUpper());
        return m.Success ? m.Groups[1].Value : null;
    }
}

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public sealed class ConstanciaData
{
    public bool ParsedSuccessfully { get; set; }
    public string Rfc { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName1 { get; set; } = string.Empty;
    public string LastName2 { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string StreetType { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string ExteriorNumber { get; set; } = string.Empty;
    public string InteriorNumber { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string Municipality { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PrimaryFiscalRegime { get; set; } = string.Empty;
}

public sealed class ConstanciaQrRequest
{
    public string QrText { get; set; } = string.Empty;
}

public sealed class ImportConstanciaRequest
{
    public string Rfc { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? ZipCode { get; set; }
    public string? Street { get; set; }
    public string? ExteriorNumber { get; set; }
    public string? InteriorNumber { get; set; }
    public string? Neighborhood { get; set; }
    public string? Municipality { get; set; }
    public string? State { get; set; }
    public string? FiscalRegimeCode { get; set; }
    public string? UserId { get; set; }
}

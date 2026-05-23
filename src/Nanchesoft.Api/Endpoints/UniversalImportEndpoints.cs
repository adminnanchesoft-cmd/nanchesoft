using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

// ═══════════════════════════════════════════════════════════════════
//  Importador universal — detecta entidad, mapea columnas, auto-crea FKs
// ═══════════════════════════════════════════════════════════════════
public static class UniversalImportEndpoints
{
    public static IEndpointRouteBuilder MapUniversalImportEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/import").WithTags("Import");
        g.MapPost("/analyze",  AnalyzeAsync);
        g.MapPost("/execute",  ExecuteAsync);
        g.MapGet ("/entities", GetEntitiesAsync);
        return app;
    }

    // ── GET /api/import/entities ─────────────────────────────────────
    private static IResult GetEntitiesAsync()
        => Results.Ok(Schema.Entities.Select(e => new { e.Key, e.Name, e.Group }));

    // ── POST /api/import/analyze ─────────────────────────────────────
    private static IResult AnalyzeAsync(AnalyzeRequest req)
    {
        var rawHeaders = req.Headers ?? [];
        var headers = rawHeaders.Select(Normalize).ToList();

        // Score each entity by how many of its field aliases appear in the headers
        EntityDef? best = null;
        int bestScore = -1;
        foreach (var entity in Schema.Entities)
        {
            int score = 0;
            foreach (var field in entity.Fields)
                if (field.Aliases.Any(a => headers.Contains(a)))
                    score++;
            // Extra weight for detective keywords
            foreach (var kw in entity.DetectKeywords)
                if (headers.Any(h => h.Contains(kw)))
                    score += 2;
            if (score > bestScore) { bestScore = score; best = entity; }
        }

        if (best is null || bestScore == 0)
            return Results.Ok(new AnalyzeResult(null, null, [], 0));

        var mappings = BuildMappings(best, rawHeaders);
        return Results.Ok(new AnalyzeResult(best.Key, best.Name, mappings, bestScore));
    }

    // ── POST /api/import/execute ─────────────────────────────────────
    private static async Task<IResult> ExecuteAsync(
        HttpContext httpContext,
        ExecuteRequest req,
        NanchesoftDbContext db)
    {
        var entity = Schema.Entities.FirstOrDefault(e => e.Key == req.EntityKey);
        if (entity is null)
            return Results.BadRequest(new { message = $"Entidad '{req.EntityKey}' no reconocida." });

        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);
        var tenantId  = ApiTenantScope.ResolveTenantId(httpContext);

        // Nunca caer por default a la primera empresa; el importador debe respetar el contexto activo.
        if (!companyId.HasValue)
            return Results.BadRequest(new { message = "No se pudo resolver la empresa activa. Selecciona el tenant/empresa antes de importar." });

        if (!tenantId.HasValue)
        {
            tenantId = await db.Companies
                .Where(x => x.Id == companyId.Value)
                .Select(x => (Guid?)x.TenantId)
                .FirstOrDefaultAsync();
        }

        if (!companyId.HasValue || !tenantId.HasValue)
            return Results.BadRequest(new { message = "No se pudo resolver el tenant/empresa activos para la importación." });

        // Build confirmed mapping: csvHeader → FieldDef
        var colMap = new Dictionary<int, FieldDef>();
        foreach (var m in req.Mappings ?? [])
        {
            var field = entity.Fields.FirstOrDefault(f => f.Key == m.FieldKey);
            if (field is not null && m.ColumnIndex >= 0)
                colMap[m.ColumnIndex] = field;
        }

        // Completar en servidor los encabezados obvios del CSV aunque el cliente no haya
        // confirmado el mapeo correctamente. Esto evita perder campos como Sueldo semanal.
        var mappedFieldKeys = colMap.Values
            .Select(x => x.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var inferred in BuildMappings(entity, req.Headers ?? []))
        {
            if (string.IsNullOrWhiteSpace(inferred.FieldKey))
                continue;

            if (mappedFieldKeys.Contains(inferred.FieldKey))
                continue;

            var field = entity.Fields.FirstOrDefault(f => f.Key == inferred.FieldKey);
            if (field is null)
                continue;

            var inferredIndex = inferred.ColumnIndex;
            if (inferredIndex < 0 || colMap.ContainsKey(inferredIndex))
                continue;

            colMap[inferredIndex] = field;
            mappedFieldKeys.Add(field.Key);
        }

        // ── 1. Pre-resolve / auto-create FK catalogs ─────────────────
        var fkCache = new Dictionary<string, Dictionary<string, Guid>>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in entity.Fields.Where(f => f.FkEntity is not null))
        {
            // Find which csv column maps to this field
            var colEntry = colMap.FirstOrDefault(kv => kv.Value.Key == field.Key);
            if (colEntry.Value is null) continue;

            // Collect all unique raw values in this column from the CSV
            var colIdx = colEntry.Key;
            if (colIdx < 0) continue;

            var rawValues = (req.Rows ?? [])
                .Select(r => colIdx < r.Count ? r[colIdx]?.Trim() ?? "" : "")
                .Where(v => !string.IsNullOrWhiteSpace(v) && v != "ND")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            fkCache[field.Key] = await ResolveOrCreateFkAsync(
                field.FkEntity!, rawValues, companyId.Value, tenantId.Value, db);
        }

        // ── 2. Import rows ────────────────────────────────────────────
        var results = new List<RowResult>();
        int created = 0, updated = 0, dup = 0, failed = 0;

        // Existing codes/names for dup check
        var existingCodes = await entity.GetExistingCodesAsync(db, companyId.Value);
        var existingNames = await entity.GetExistingNamesAsync(db, companyId.Value);

        foreach (var (row, idx) in (req.Rows ?? []).Select((r, i) => (r, i)))
        {
            try
            {
                var values = MapRow(entity, req.Headers ?? [], row, colMap, fkCache);

                // Inject company / tenant
                if (!values.ContainsKey("CompanyId")) values["CompanyId"] = companyId.Value;
                if (!values.ContainsKey("TenantId"))  values["TenantId"]  = tenantId.Value;
                values["IsActive"] = true;

                // Auto-generate Code from Name if missing
                if (!values.TryGetValue("Code", out var codeObj) || string.IsNullOrWhiteSpace(codeObj?.ToString()))
                {
                    if (values.TryGetValue("Name", out var nameObj) && nameObj is not null)
                        values["Code"] = Slug(nameObj.ToString()!);
                }
                if (!values.TryGetValue("EmployeeNumber", out var enObj) || string.IsNullOrWhiteSpace(enObj?.ToString()))
                {
                    if (values.TryGetValue("Code", out var codeObj2))
                        values["EmployeeNumber"] = codeObj2?.ToString() ?? "";
                }

                var code = values.TryGetValue("Code", out var c) ? c?.ToString()?.Trim() ?? "" : "";
                var name = values.TryGetValue("Name", out var n) ? n?.ToString()?.Trim() ?? "" : "";

                if (entity.UpdateAsync is not null)
                {
                    var updatedExisting = await entity.UpdateAsync(db, values);
                    if (updatedExisting)
                    {
                        var updateMessage = values.TryGetValue("__ImportMessage", out var updateMsg) ? updateMsg?.ToString() ?? "Actualizado" : "Actualizado";
                        results.Add(new RowResult(idx + 1, "updated", updateMessage));
                        updated++;
                        continue;
                    }
                }

                if ((!string.IsNullOrEmpty(code) && existingCodes.Contains(code)) ||
                    (!string.IsNullOrEmpty(name)  && existingNames.Contains(name)))
                {
                    results.Add(new RowResult(idx + 1, "duplicado", "Ya existe en el catálogo — omitido"));
                    dup++;
                    continue;
                }

                var inserted = await entity.InsertAsync(db, values);
                if (inserted)
                {
                    if (!string.IsNullOrEmpty(code)) existingCodes.Add(code);
                    if (!string.IsNullOrEmpty(name))  existingNames.Add(name);
                    var createMessage = values.TryGetValue("__ImportMessage", out var createMsg) ? createMsg?.ToString() ?? "Importado" : "Importado";
                    results.Add(new RowResult(idx + 1, "created", createMessage));
                    created++;
                }
                else
                {
                    results.Add(new RowResult(idx + 1, "error", "No se pudo insertar"));
                    failed++;
                }
            }
            catch (Exception ex)
            {
                results.Add(new RowResult(idx + 1, "error", ex.Message));
                failed++;
            }
        }

        return Results.Ok(new
        {
            total = results.Count,
            imported = created + updated,
            created,
            updated,
            duplicates = dup,
            failed,
            rows = results
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static List<ColumnMapping> BuildMappings(EntityDef entity, List<string> rawHeaders)
    {
        var mappings = new List<ColumnMapping>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rawHeaders.Count; i++)
        {
            var csvHeader = rawHeaders[i];
            var normalizedCsvHeader = Normalize(csvHeader);
            var matched = entity.Fields.FirstOrDefault(f => f.Aliases.Any(a => Normalize(a) == normalizedCsvHeader));
            mappings.Add(new ColumnMapping(
                ColumnIndex: i,
                CsvColumn: csvHeader,
                FieldKey: matched?.Key,
                FieldName: matched?.Name,
                DataType: matched?.DataType,
                IsFk: matched?.FkEntity is not null,
                FkEntity: matched?.FkEntity,
                IsRequired: matched?.Required ?? false
            ));
            if (matched is not null) used.Add(matched.Key);
        }

        return mappings;
    }

    private static Dictionary<string, object?> MapRow(
        EntityDef entity,
        List<string> headers,
        List<string?> cells,
        Dictionary<int, FieldDef> colMap,
        Dictionary<string, Dictionary<string, Guid>> fkCache)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headers.Count; i++)
        {
            if (!colMap.TryGetValue(i, out var field)) continue;
            var raw = (i < cells.Count ? cells[i] : null)?.Trim() ?? "";

            if (raw == "ND" || raw == "N/D" || raw == "N.D." || raw == "-")
                raw = "";

            object? value = field.DataType switch
            {
                "fk"      => fkCache.TryGetValue(field.Key, out var cache) && cache.TryGetValue(raw, out var fkId)
                               ? fkId : (object?)null,
                "decimal" => ParseDecimal(raw),
                "date"    => ParseDate(raw),
                "bool"    => ParseBool(raw),
                _         => Normalize(field, raw)
            };

            values[field.Key] = value;
        }

        return values;
    }

    private static async Task<Dictionary<string, Guid>> ResolveOrCreateFkAsync(
        string fkEntityKey, List<string> rawValues, Guid companyId, Guid tenantId, NanchesoftDbContext db)
    {
        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        if (rawValues.Count == 0) return result;

        switch (fkEntityKey)
        {
            case "branches":
                var branches = await db.Branches.Where(x => x.CompanyId == companyId).ToListAsync();
                foreach (var v in rawValues)
                {
                    var existing = branches.FirstOrDefault(b =>
                        string.Equals(b.Name, v, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(b.Code, v, StringComparison.OrdinalIgnoreCase));
                    if (existing is not null) { result[v] = existing.Id; continue; }
                    var created = new Branch { TenantId = tenantId, CompanyId = companyId, Code = Slug(v), Name = v.ToUpperInvariant(), IsActive = true, CreatedBy = "import" };
                    db.Branches.Add(created);
                    await db.SaveChangesAsync();
                    branches.Add(created);
                    result[v] = created.Id;
                }
                break;

            case "hr-departments":
                var depts = await db.Departments.Where(x => x.CompanyId == companyId).ToListAsync();
                foreach (var v in rawValues)
                {
                    var existing = depts.FirstOrDefault(d =>
                        string.Equals(d.Name, v, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(d.Code, v, StringComparison.OrdinalIgnoreCase));
                    if (existing is not null) { result[v] = existing.Id; continue; }
                    var created = new Department { TenantId = tenantId, CompanyId = companyId, Code = Slug(v), Name = v.ToUpperInvariant(), IsActive = true, CreatedBy = "import" };
                    db.Departments.Add(created);
                    await db.SaveChangesAsync();
                    depts.Add(created);
                    result[v] = created.Id;
                }
                break;

            case "hr-positions":
                var positions = await db.Positions.Where(x => x.CompanyId == companyId).ToListAsync();
                foreach (var v in rawValues)
                {
                    var existing = positions.FirstOrDefault(p =>
                        string.Equals(p.Name, v, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.Code, v, StringComparison.OrdinalIgnoreCase));
                    if (existing is not null) { result[v] = existing.Id; continue; }
                    var created = new Position { TenantId = tenantId, CompanyId = companyId, Code = Slug(v), Name = v.ToUpperInvariant(), IsActive = true, CreatedBy = "import" };
                    db.Positions.Add(created);
                    await db.SaveChangesAsync();
                    positions.Add(created);
                    result[v] = created.Id;
                }
                break;

            case "hr-work-schedules":
                var scheds = await db.WorkSchedules.Where(x => x.CompanyId == companyId).ToListAsync();
                foreach (var v in rawValues)
                {
                    var existing = scheds.FirstOrDefault(s =>
                        string.Equals(s.Name, v, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(s.Code, v, StringComparison.OrdinalIgnoreCase));
                    if (existing is not null) { result[v] = existing.Id; continue; }
                    var created = new WorkSchedule { TenantId = tenantId, CompanyId = companyId, Code = Slug(v), Name = v, Monday = true, Tuesday = true, Wednesday = true, Thursday = true, Friday = true, IsActive = true, CreatedBy = "import" };
                    db.WorkSchedules.Add(created);
                    await db.SaveChangesAsync();
                    scheds.Add(created);
                    result[v] = created.Id;
                }
                break;

            case "hr-shifts":
                var shifts = await db.WorkShifts.Where(x => x.CompanyId == companyId).ToListAsync();
                foreach (var v in rawValues)
                {
                    var existing = shifts.FirstOrDefault(s =>
                        string.Equals(s.Name, v, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(s.Code, v, StringComparison.OrdinalIgnoreCase));
                    if (existing is not null) { result[v] = existing.Id; continue; }
                    var created = new WorkShift { TenantId = tenantId, CompanyId = companyId, Code = Slug(v), Name = v, StartTime = "08:00", EndTime = "17:00", IsActive = true, CreatedBy = "import" };
                    db.WorkShifts.Add(created);
                    await db.SaveChangesAsync();
                    shifts.Add(created);
                    result[v] = created.Id;
                }
                break;

            case "payroll-concepts":
                var concepts = await db.PayrollConcepts.Where(x => x.CompanyId == companyId).ToListAsync();
                foreach (var v in rawValues)
                {
                    var existing = concepts.FirstOrDefault(c =>
                        string.Equals(c.Name, v, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(c.Code, v, StringComparison.OrdinalIgnoreCase));
                    if (existing is not null) { result[v] = existing.Id; continue; }
                    var created = new PayrollConcept { TenantId = tenantId, CompanyId = companyId, Code = Slug(v), Name = v, ConceptType = "percepcion", CalculationType = "fixed", IsActive = true, CreatedBy = "import" };
                    db.PayrollConcepts.Add(created);
                    await db.SaveChangesAsync();
                    concepts.Add(created);
                    result[v] = created.Id;
                }
                break;

            case "hr-employees":
                // No auto-create — employees deben existir antes. Buscar por Code, EmployeeNumber o ClockKey.
                var employeesList = await db.Employees
                    .Where(x => x.CompanyId == companyId && x.IsActive)
                    .Select(x => new { x.Id, x.Code, x.EmployeeNumber, x.ClockKey })
                    .ToListAsync();
                foreach (var v in rawValues)
                {
                    var vTrim = v.Trim();
                    var existing = employeesList.FirstOrDefault(e =>
                        string.Equals(e.Code, vTrim, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(e.EmployeeNumber, vTrim, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(e.ClockKey, vTrim, StringComparison.OrdinalIgnoreCase));
                    if (existing is not null) result[v] = existing.Id;
                }
                break;
        }

        return result;
    }

    // ── Value normalizers ─────────────────────────────────────────────
    private static string? Normalize(FieldDef f, string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return f.Key switch
        {
            "Gender"        => raw.ToUpperInvariant() switch { "MASCULINO" => "M", "FEMENINO" => "F", "HOMBRE" => "M", "MUJER" => "F", "MALE" => "M", "FEMALE" => "F", _ => raw },
            "MaritalStatus" => raw.ToLowerInvariant() switch
            {
                var s when s.StartsWith("casad")  => "casado",
                var s when s.StartsWith("solter") => "soltero",
                var s when s.StartsWith("divorc") => "divorciado",
                var s when s.StartsWith("viud")   => "viudo",
                var s when s.StartsWith("uni")    => "union",
                _ => raw.ToLowerInvariant()
            },
            "ContractType"  => raw.ToLowerInvariant() switch
            {
                var s when s.Contains("indeterm") || s.Contains("indefin") || s.Contains("base") => "indefinite",
                var s when s.Contains("determ") || s.Contains("plazo")                           => "determined",
                var s when s.Contains("tempo")  || s.Contains("eventu")                          => "field_temp",
                var s when s.Contains("prueba")                                                   => "trial",
                var s when s.Contains("capac")                                                    => "training",
                _ => "indefinite"
            },
            "PaymentForm"   => raw.ToLowerInvariant() switch
            {
                var s when s.Contains("tarjet") || s.Contains("card") => "tarjeta",
                var s when s.Contains("transf")                        => "transferencia",
                var s when s.Contains("efect") || s.Contains("cash")  => "efectivo",
                var s when s.Contains("chequ") || s.Contains("check") => "cheque",
                _ => "tarjeta"
            },
            "AddressZipCode" => raw.Replace("CP ", "").Replace("C.P.", "").Trim(),
            "AddressColony"  => raw.StartsWith("COL.", StringComparison.OrdinalIgnoreCase)
                                   ? raw[4..].Trim() : raw,
            "Code"           => raw.ToUpperInvariant().Trim(),
            _                => raw.Trim()
        };
    }

    private static decimal? ParseDecimal(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw == "ND") return null;
        var clean = raw
            .Replace("$", "")
            .Replace("MXN", "", StringComparison.OrdinalIgnoreCase)
            .Replace("USD", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        clean = new string(clean.Where(c => !char.IsWhiteSpace(c)).ToArray());

        if (string.IsNullOrWhiteSpace(clean))
            return null;

        // 1,234.56 -> 1234.56
        if (clean.Contains(',') && clean.Contains('.'))
        {
            var lastComma = clean.LastIndexOf(',');
            var lastDot = clean.LastIndexOf('.');

            if (lastComma > lastDot)
            {
                // 1.234,56 -> 1234.56
                clean = clean.Replace(".", "").Replace(",", ".");
            }
            else
            {
                // 1,234.56 -> 1234.56
                clean = clean.Replace(",", "");
            }
        }
        else if (clean.Contains(','))
        {
            var commaIndex = clean.LastIndexOf(',');
            var decimals = clean.Length - commaIndex - 1;

            clean = decimals is 1 or 2
                ? clean.Replace(",", ".")
                : clean.Replace(",", "");
        }

        return decimal.TryParse(
            clean,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var d)
            ? d
            : null;
    }

    private static DateTime? ParseDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw == "ND") return null;
        string[] formats = ["dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "d/M/yyyy", "dd-MM-yyyy"];
        foreach (var fmt in formats)
            if (DateTime.TryParseExact(raw, fmt, null, System.Globalization.DateTimeStyles.None, out var d))
                return d;
        return DateTime.TryParse(raw, out var dt) ? dt : null;
    }

    private static bool? ParseBool(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return raw.ToLowerInvariant() is "sí" or "si" or "yes" or "true" or "1" or "activo";
    }

    private static string Normalize(string header)
    {
        // lower + remove accents + remove spaces
        var s = header.ToLowerInvariant().Trim();
        s = s.Replace("á","a").Replace("é","e").Replace("í","i").Replace("ó","o").Replace("ú","u").Replace("ñ","n");
        s = new string(s.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        return s;
    }

    private static string Slug(string name)
    {
        var s = Normalize(name).ToUpperInvariant();
        return s.Length > 20 ? s[..20] : s;
    }

    // DTOs
    private sealed record AnalyzeRequest(List<string>? Headers, List<List<string?>>? FirstRows);
    private sealed record AnalyzeResult(string? EntityKey, string? EntityName, List<ColumnMapping> Mappings, int Score);
    private sealed record ColumnMapping(int ColumnIndex, string CsvColumn, string? FieldKey, string? FieldName, string? DataType, bool IsFk, string? FkEntity, bool IsRequired);
    private sealed record ExecuteRequest(string EntityKey, List<string>? Headers, List<List<string?>>? Rows, List<MappingEntry>? Mappings);
    private sealed record MappingEntry(int ColumnIndex, string CsvColumn, string FieldKey);
    private sealed record RowResult(int Row, string Status, string Message);
}

// ═══════════════════════════════════════════════════════════════════
//  Schema — entidades, campos y aliases
// ═══════════════════════════════════════════════════════════════════
file static class Schema
{
    private static string[] A(params string[] a) => a;

    public static readonly List<EntityDef> Entities =
    [
        // ── Departamentos ──────────────────────────────────────────
        new EntityDef
        {
            Key = "hr-departments", Name = "Departamentos", Group = "RH",
            DetectKeywords = ["departamento","depto","area","dept"],
            Fields =
            [
                new() { Key="Code",        Name="Código",      DataType="text",    Required=false, Aliases=A("codigo","clave","code","clave dept","num") },
                new() { Key="Name",        Name="Nombre",      DataType="text",    Required=true,  Aliases=A("nombre","departamento","area","depto","dept","name","descripcion") },
                new() { Key="Description", Name="Descripción", DataType="text",    Required=false, Aliases=A("descripcion","description","desc","notas","notes") },
            ],
            GetExistingCodesAsync = async (db, cid) => (await db.Departments.Where(x => x.CompanyId == cid).Select(x => x.Code).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            GetExistingNamesAsync = async (db, cid) => (await db.Departments.Where(x => x.CompanyId == cid).Select(x => x.Name).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            InsertAsync = async (db, v) =>
            {
                var e = new Department
                {
                    TenantId  = (Guid)v["TenantId"]!,
                    CompanyId = (Guid)v["CompanyId"]!,
                    Code      = v.TryGetValue("Code", out var c) && c is not null ? c.ToString()! : Slug(v["Name"]?.ToString() ?? ""),
                    Name      = v["Name"]?.ToString() ?? "",
                    Description = v.TryGetValue("Description", out var d) ? d?.ToString() : null,
                    IsActive  = true, CreatedBy = "import"
                };
                db.Departments.Add(e);
                await db.SaveChangesAsync();
                return true;
            }
        },

        // ── Puestos ───────────────────────────────────────────────
        new EntityDef
        {
            Key = "hr-positions", Name = "Puestos", Group = "RH",
            DetectKeywords = ["puesto","cargo","posicion","job"],
            Fields =
            [
                new() { Key="Code",         Name="Código",       DataType="text",    Required=false, Aliases=A("codigo","clave","code") },
                new() { Key="Name",         Name="Puesto",       DataType="text",    Required=true,  Aliases=A("nombre","puesto","cargo","posicion","name","job","rol","title") },
                new() { Key="DepartmentId", Name="Departamento", DataType="fk",      Required=false, FkEntity="hr-departments", Aliases=A("departamento","area","depto","dept","department") },
                new() { Key="PayrollGroup", Name="Grupo nómina", DataType="text",    Required=false, Aliases=A("grupo","gruponomina","grupo nomina","payroll group","nomina") },
                new() { Key="BaseSalary",   Name="Salario base", DataType="decimal", Required=false, Aliases=A("salario","sueldo","salariobase","sueldo base","base salary","salario base") },
                new() { Key="Description",  Name="Descripción",  DataType="text",    Required=false, Aliases=A("descripcion","description","desc") },
            ],
            GetExistingCodesAsync = async (db, cid) => (await db.Positions.Where(x => x.CompanyId == cid).Select(x => x.Code).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            GetExistingNamesAsync = async (db, cid) => (await db.Positions.Where(x => x.CompanyId == cid).Select(x => x.Name).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            InsertAsync = async (db, v) =>
            {
                var e = new Position
                {
                    TenantId     = (Guid)v["TenantId"]!,
                    CompanyId    = (Guid)v["CompanyId"]!,
                    Code         = v.TryGetValue("Code", out var c) && c is not null ? c.ToString()! : Slug(v["Name"]?.ToString() ?? ""),
                    Name         = v["Name"]?.ToString() ?? "",
                    DepartmentId = v.TryGetValue("DepartmentId", out var d) && d is Guid g ? g : null,
                    PayrollGroup = v.TryGetValue("PayrollGroup", out var pg) ? pg?.ToString() : null,
                    BaseSalary   = v.TryGetValue("BaseSalary", out var bs) && bs is decimal dec ? dec : 0,
                    IsActive     = true, CreatedBy = "import"
                };
                db.Positions.Add(e);
                await db.SaveChangesAsync();
                return true;
            }
        },

        // ── Colaboradores (Empleados) ─────────────────────────────
        new EntityDef
        {
            Key = "hr-employees", Name = "Colaboradores", Group = "RH",
            DetectKeywords = ["empleado","colaborador","paterno","materno","nombresolo","claveempleado","nss","curp"],
            Fields =
            [
                new() { Key="Code",                 Name="Código / Clave",    DataType="text",    Required=true,  Aliases=A("claveempleado","clave empleado","codigo","empleado","code","num empleado","numero empleado","noemplead","clave","id empleado") },
                new() { Key="EmployeeNumber",       Name="Número empleado",   DataType="text",    Required=false, Aliases=A("numeroempleado","numero empleado","no empleado","employee number","folio") },
                new() { Key="ClockKey",             Name="Clave reloj",       DataType="text",    Required=false, Aliases=A("clavereloj","clave reloj","reloj","clock","clockkey","clock key") },
                new() { Key="NoiKey",               Name="Clave NOI",         DataType="text",    Required=false, Aliases=A("clavei","noi","noikey","noi key","clave noi") },
                new() { Key="FirstName",            Name="Nombre",            DataType="text",    Required=true,  Aliases=A("nombre","nombresolo","nombre solo","nombres","first name","firstname","name") },
                new() { Key="LastName",             Name="Apellido paterno",  DataType="text",    Required=true,  Aliases=A("paterno","apellido paterno","primer apellido","apellido1","last name","lastname","apellido") },
                new() { Key="SecondLastName",       Name="Apellido materno",  DataType="text",    Required=false, Aliases=A("materno","apellido materno","segundo apellido","apellido2","second last name") },
                new() { Key="MiddleName",           Name="Segundo nombre",    DataType="text",    Required=false, Aliases=A("segundonombre","segundo nombre","middle name","middlename") },
                new() { Key="BirthDate",            Name="Fecha nacimiento",  DataType="date",    Required=false, Aliases=A("nacimiento","fechanacimiento","fecha nacimiento","fecha de nacimiento","fnac","birthdate","birth date","dateofbirth") },
                new() { Key="HireDate",             Name="Fecha alta",        DataType="date",    Required=true,  Aliases=A("alta","fechaalta","fecha alta","fecha de alta","ingreso","fechaingreso","hiredate","hire date","startdate") },
                new() { Key="BranchId",             Name="Sucursal",          DataType="fk",      Required=false, FkEntity="branches", Aliases=A("sucursal","branch","branchid","suc","sucursalcodigo","sucursal codigo","codigo sucursal") },
                new() { Key="DepartmentId",         Name="Departamento",      DataType="fk",      Required=false, FkEntity="hr-departments", Aliases=A("departamento","area","depto","dept","department") },
                new() { Key="PositionId",           Name="Puesto",            DataType="fk",      Required=false, FkEntity="hr-positions",   Aliases=A("puesto","cargo","posicion","position","job title","jobtitle","rol") },
                new() { Key="WorkScheduleId",       Name="Horario",           DataType="fk",      Required=false, FkEntity="hr-work-schedules", Aliases=A("horario","turno","jornada","schedule","horario laboral","workschedule") },
                new() { Key="Gender",               Name="Sexo",              DataType="text",    Required=false, Aliases=A("sexo","genero","gender","sex") },
                new() { Key="BloodType",            Name="Tipo de sangre",    DataType="text",    Required=false, Aliases=A("tiposangre","tipo sangre","tipo de sangre","bloodtype","blood type","sangre") },
                new() { Key="MaritalStatus",        Name="Estado civil",      DataType="text",    Required=false, Aliases=A("estadocivil","estado civil","civil","marital","marital status") },
                new() { Key="PlaceOfBirth",         Name="Lugar nacimiento",  DataType="text",    Required=false, Aliases=A("lugardenacimiento","lugar nacimiento","lugar de nacimiento","birthplace","lugar") },
                new() { Key="Phone",                Name="Teléfono",          DataType="text",    Required=false, Aliases=A("telefono","cel","celular","movil","phone","tel","fono") },
                new() { Key="EmergencyPhone",       Name="Teléfono emergencia",DataType="text",   Required=false, Aliases=A("telefonoemergencia","telefono emergencia","tel emergencia","emergencyphone","emergency phone","tel emerg") },
                new() { Key="Email",                Name="Email",             DataType="text",    Required=false, Aliases=A("email","correo","correo electronico","mail","e-mail") },
                new() { Key="TaxId",                Name="RFC",               DataType="text",    Required=false, Aliases=A("rfc","tax id","taxid","registro fiscal") },
                new() { Key="NationalId",           Name="CURP/ID",           DataType="text",    Required=false, Aliases=A("curpid","curp id","identificacion","national id","nationalid") },
                new() { Key="Curp",                 Name="CURP",              DataType="text",    Required=false, Aliases=A("curp","clave unica") },
                new() { Key="Nss",                  Name="NSS",               DataType="text",    Required=false, Aliases=A("nss","numero seguro","seguro social","imss numero","no seguridad") },
                new() { Key="PeriodSalary",         Name="Sueldo del periodo",DataType="decimal", Required=false, Aliases=A("sueldo del periodo","sueldo periodo","sueldodelperiodo","sueldo semanal","sueldo_semanal","sueldosemanal","salario semanal","salario_semanal","salariosemanal","sueldo semana","sueldo","salario","salarioquincenal","salario quincenal","salariomensual","salario mensual","salary","pay","pago") },
                new() { Key="DailySalary",          Name="Salario diario",    DataType="decimal", Required=false, Aliases=A("salariodiario","salario diario","daily salary","sueldo diario") },
                new() { Key="IntegratedDailySalary",Name="Salario diario integrado",DataType="decimal", Required=false, Aliases=A("salariodiariointegrado","salario diario integrado","sdi","integrated daily salary") },
                new() { Key="SbcFija",              Name="SBC fija",          DataType="decimal", Required=false, Aliases=A("sbcfija","sbc fija","sbc","sbc_fija") },
                new() { Key="PaymentForm",          Name="Forma de pago",     DataType="text",    Required=false, Aliases=A("formadepago","forma pago","forma de pago","payment","payment form","tipo pago") },
                new() { Key="BankAccount",          Name="Cuenta bancaria",   DataType="text",    Required=false, Aliases=A("cuenta","cuentadeposito","cuenta deposito","cuenta bancaria","bank account","account","bankaccount") },
                new() { Key="Clabe",                Name="CLABE",             DataType="text",    Required=false, Aliases=A("clabe","interbancaria","cuenta clabe","clabe interbancaria") },
                new() { Key="BankCode",             Name="Banco",             DataType="text",    Required=false, Aliases=A("banco","bank","bankcode","clave banco") },
                new() { Key="BankBranch",           Name="Sucursal banco",    DataType="text",    Required=false, Aliases=A("sucursal banco","sucursalbanco","bank branch","bankbranch","plaza bancaria") },
                new() { Key="AddressStreet",        Name="Calle y número",    DataType="text",    Required=false, Aliases=A("calle","calleynumero","calle y numero","calleinumero","street","address","domicilio","direccion") },
                new() { Key="AddressColony",        Name="Colonia",           DataType="text",    Required=false, Aliases=A("colonia","colony","neighborhood","asentamiento") },
                new() { Key="AddressCity",          Name="Ciudad / Municipio",DataType="text",    Required=false, Aliases=A("ciudad","municipio","poblacion","city","municipality","localidad") },
                new() { Key="AddressState",         Name="Estado",            DataType="text",    Required=false, Aliases=A("estado","state","entidad","entidadfederativa") },
                new() { Key="AddressZipCode",       Name="Código Postal",     DataType="text",    Required=false, Aliases=A("cp","codigopostal","codigo postal","zip","postal code","zipcode","codigopostal") },
                new() { Key="FatherName",           Name="Nombre del padre",  DataType="text",    Required=false, Aliases=A("padre","nombr padre","father","fathername","nombre padre") },
                new() { Key="MotherName",           Name="Nombre de la madre",DataType="text",    Required=false, Aliases=A("madre","nombre madre","mother","mothername") },
                new() { Key="ContractType",         Name="Tipo de contrato",  DataType="text",    Required=false, Aliases=A("contrato","tipodecontrato","tipo contrato","tipo de contrato","contract","contracttype") },
                new() { Key="ImssRegId",            Name="Registro patronal", DataType="text",    Required=false, Aliases=A("regpatronal","registro patronal","imssregid","registro imss") },
                new() { Key="ImssRegistrationDate", Name="Fecha alta IMSS",   DataType="date",    Required=false, Aliases=A("fechaimss","fecha imss","alta imss","imss","imss alta","imss date") },
                new() { Key="IsImssRegistered",     Name="IMSS registrado",   DataType="bool",    Required=false, Aliases=A("imss registrado","isimss","tieneimss","con imss") },
                new() { Key="PayrollPeriodType",    Name="Tipo nómina",       DataType="text",    Required=false, Aliases=A("periodonomina","periodo nomina","tipo nomina","payroll type","frecuencia") },
                new() { Key="Afore",                Name="AFORE",             DataType="text",    Required=false, Aliases=A("afore") },
                new() { Key="Fonacot",              Name="FONACOT",           DataType="text",    Required=false, Aliases=A("fonacot") },
                new() { Key="Infonavit",            Name="INFONAVIT",         DataType="text",    Required=false, Aliases=A("infonavit") },
                new() { Key="ImmediateSupervisor",  Name="Jefe directo",      DataType="text",    Required=false, Aliases=A("jefe directo","jefedirecto","supervisor","immediate supervisor") },
                new() { Key="Category",             Name="Categoría",         DataType="text",    Required=false, Aliases=A("categoria","categoría","category") },
                new() { Key="Notes",                Name="Notas",             DataType="text",    Required=false, Aliases=A("notas","notes","observaciones","comentarios","expediente","obs") },
            ],
            GetExistingCodesAsync = async (db, cid) => (await db.Employees.Where(x => x.CompanyId == cid).Select(x => x.Code).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            GetExistingNamesAsync = async (db, cid) => new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            InsertAsync = async (db, v) =>
            {
                string? Str(string k) => v.TryGetValue(k, out var x) ? x?.ToString() : null;
                Guid? FkGuid(string k) => v.TryGetValue(k, out var x) && x is Guid g ? g : null;
                decimal Dec(string k) => v.TryGetValue(k, out var x) && x is decimal d ? d : 0;
                DateTime? Dt(string k) => v.TryGetValue(k, out var x) && x is DateTime dt ? dt : null;
                bool Bool(string k) => v.TryGetValue(k, out var x) && x is bool b && b;

                var firstName    = Str("FirstName")?.Trim() ?? "";
                var lastName     = Str("LastName")?.Trim() ?? "";
                var secondLast   = Str("SecondLastName")?.Trim() ?? "";

                if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName) && string.IsNullOrEmpty(secondLast))
                    throw new InvalidOperationException("Sin nombre ni apellidos — fila omitida");

                var autoCode = EmployeeCodeFromName(lastName, secondLast, firstName);

                var e = new Employee
                {
                    TenantId     = (Guid)v["TenantId"]!,
                    CompanyId    = (Guid)v["CompanyId"]!,
                    Code         = Str("Code") is { Length: > 0 } c ? c : autoCode,
                    EmployeeNumber = Str("EmployeeNumber") ?? Str("Code") ?? "",
                    ClockKey     = Str("ClockKey"),
                    NoiKey       = Str("NoiKey"),
                    FirstName    = firstName,
                    LastName     = lastName,
                    SecondLastName = secondLast.Length > 0 ? secondLast : null,
                    MiddleName   = Str("MiddleName") ?? "",
                    Email        = Str("Email") ?? "",
                    Phone        = Str("Phone") ?? "",
                    EmergencyPhone = Str("EmergencyPhone"),
                    TaxId        = Str("TaxId") ?? "",
                    NationalId   = Str("NationalId") ?? Str("Curp") ?? "",
                    Curp         = Str("Curp") ?? "",
                    Nss          = Str("Nss") ?? "",
                    ImssRegId    = Str("ImssRegId") ?? "",
                    Gender       = Str("Gender"),
                    BloodType    = Str("BloodType"),
                    MaritalStatus = Str("MaritalStatus"),
                    PlaceOfBirth = Str("PlaceOfBirth"),
                    Nationality  = Str("Nationality"),
                    FatherName   = Str("FatherName"),
                    MotherName   = Str("MotherName"),
                    AddressStreet  = Str("AddressStreet"),
                    AddressColony  = Str("AddressColony"),
                    AddressCity    = Str("AddressCity"),
                    AddressState   = Str("AddressState"),
                    AddressZipCode = Str("AddressZipCode"),
                    BranchId       = FkGuid("BranchId"),
                    DepartmentId   = FkGuid("DepartmentId"),
                    PositionId     = FkGuid("PositionId"),
                    WorkScheduleId = FkGuid("WorkScheduleId"),
                    PeriodSalary   = Dec("PeriodSalary"),
                    DailySalary    = Dec("DailySalary"),
                    IntegratedDailySalary = Dec("IntegratedDailySalary"),
                    SbcFija        = Dec("SbcFija"),
                    HireDate       = Dt("HireDate") ?? DateTime.UtcNow,
                    BirthDate      = Dt("BirthDate"),
                    ImssRegistrationDate = Dt("ImssRegistrationDate"),
                    IsImssRegistered = Bool("IsImssRegistered") || v.TryGetValue("ImssRegistrationDate", out var iDate) && iDate is DateTime,
                    ContractType   = Str("ContractType") ?? "indefinite",
                    CotizationBase = "fixed",
                    TaxRegime      = "sueldos_salarios",
                    EmployeeType   = "base",
                    SalaryZone     = "A",
                    PaymentForm    = Str("PaymentForm") ?? "tarjeta",
                    PayrollPeriodType = Str("PayrollPeriodType") ?? "semanal",
                    BankCode       = Str("BankCode") ?? "",
                    BankAccount    = Str("BankAccount") ?? "",
                    Clabe          = Str("Clabe") ?? "",
                    BankBranch     = Str("BankBranch") ?? "",
                    Afore          = Str("Afore") ?? "",
                    Fonacot        = Str("Fonacot") ?? "",
                    Infonavit      = Str("Infonavit") ?? "",
                    ImmediateSupervisor = Str("ImmediateSupervisor"),
                    Category       = Str("Category"),
                    Notes          = Str("Notes"),
                    Status         = "active",
                    PrintReceipt   = true,
                    IsActive       = true,
                    CreatedBy      = "import"
                };
                v["__ImportMessage"] = $"Creado {e.Code} · {e.FirstName} {e.LastName}".Trim();
                db.Employees.Add(e);
                await db.SaveChangesAsync();
                return true;
            },
            UpdateAsync = async (db, v) =>
            {
                string? Str(string k) => v.TryGetValue(k, out var x) ? x?.ToString() : null;
                Guid? FkGuid(string k) => v.TryGetValue(k, out var x) && x is Guid g ? g : null;
                decimal? Dec(string k) => v.TryGetValue(k, out var x) && x is decimal d ? d : null;
                DateTime? Dt(string k) => v.TryGetValue(k, out var x) && x is DateTime dt ? dt : null;
                bool? Bool(string k) => v.TryGetValue(k, out var x) && x is bool b ? b : null;
                var changes = new List<string>();

                void TrackString(string label, string? current, string? incoming, Action<string?> apply)
                {
                    if (string.IsNullOrWhiteSpace(incoming) || string.Equals(current ?? "", incoming ?? "", StringComparison.Ordinal))
                        return;
                    changes.Add($"{label}: '{current ?? ""}' -> '{incoming}'");
                    apply(incoming);
                }

                void TrackGuid(string label, Guid? current, Guid? incoming, Action<Guid?> apply)
                {
                    if (!incoming.HasValue || current == incoming)
                        return;
                    changes.Add($"{label}: {current?.ToString("D") ?? "(vacío)"} -> {incoming.Value:D}");
                    apply(incoming);
                }

                void TrackDecimal(string label, decimal? current, decimal? incoming, Action<decimal> apply)
                {
                    if (!incoming.HasValue || current == incoming.Value)
                        return;
                    changes.Add($"{label}: {current?.ToString("0.##") ?? "0"} -> {incoming.Value:0.##}");
                    apply(incoming.Value);
                }

                void TrackDate(string label, DateTime? current, DateTime? incoming, Action<DateTime?> apply)
                {
                    if (!incoming.HasValue || current == incoming.Value)
                        return;
                    changes.Add($"{label}: {current?.ToString("yyyy-MM-dd") ?? "(vacío)"} -> {incoming.Value:yyyy-MM-dd}");
                    apply(incoming);
                }

                void TrackBool(string label, bool current, bool? incoming, Action<bool> apply)
                {
                    if (!incoming.HasValue || current == incoming.Value)
                        return;
                    changes.Add($"{label}: {(current ? "Sí" : "No")} -> {(incoming.Value ? "Sí" : "No")}");
                    apply(incoming.Value);
                }

                var companyId = (Guid)v["CompanyId"]!;
                var code = Str("Code")?.Trim();
                var employeeNumber = Str("EmployeeNumber")?.Trim();
                var clockKey = Str("ClockKey")?.Trim();

                var existing = await db.Employees.FirstOrDefaultAsync(x =>
                    x.CompanyId == companyId &&
                    ((!string.IsNullOrWhiteSpace(code) && x.Code == code) ||
                     (!string.IsNullOrWhiteSpace(employeeNumber) && x.EmployeeNumber == employeeNumber) ||
                     (!string.IsNullOrWhiteSpace(clockKey) && x.ClockKey == clockKey)));

                if (existing is null)
                    return false;

                TrackString("Número empleado", existing.EmployeeNumber, employeeNumber, x => existing.EmployeeNumber = x ?? existing.EmployeeNumber);
                TrackString("Clave reloj", existing.ClockKey, clockKey, x => existing.ClockKey = x);
                TrackString("Clave NOI", existing.NoiKey, Str("NoiKey"), x => existing.NoiKey = x);
                TrackString("Nombre", existing.FirstName, Str("FirstName"), x => existing.FirstName = x ?? existing.FirstName);
                TrackString("Apellido paterno", existing.LastName, Str("LastName"), x => existing.LastName = x ?? existing.LastName);
                TrackString("Apellido materno", existing.SecondLastName, Str("SecondLastName"), x => existing.SecondLastName = x);
                TrackString("Segundo nombre", existing.MiddleName, Str("MiddleName"), x => existing.MiddleName = x ?? existing.MiddleName);
                TrackString("Email", existing.Email, Str("Email"), x => existing.Email = x ?? existing.Email);
                TrackString("Teléfono", existing.Phone, Str("Phone"), x => existing.Phone = x ?? existing.Phone);
                TrackString("Tel. emergencia", existing.EmergencyPhone, Str("EmergencyPhone"), x => existing.EmergencyPhone = x);
                TrackString("RFC", existing.TaxId, Str("TaxId"), x => existing.TaxId = x ?? existing.TaxId);
                TrackString("CURP/ID", existing.NationalId, Str("NationalId"), x => existing.NationalId = x ?? existing.NationalId);
                TrackString("CURP", existing.Curp, Str("Curp"), x => existing.Curp = x ?? existing.Curp);
                TrackString("NSS", existing.Nss, Str("Nss"), x => existing.Nss = x ?? existing.Nss);
                TrackString("Registro patronal", existing.ImssRegId, Str("ImssRegId"), x => existing.ImssRegId = x ?? existing.ImssRegId);
                TrackString("Sexo", existing.Gender, Str("Gender"), x => existing.Gender = x);
                TrackString("Tipo de sangre", existing.BloodType, Str("BloodType"), x => existing.BloodType = x);
                TrackString("Estado civil", existing.MaritalStatus, Str("MaritalStatus"), x => existing.MaritalStatus = x);
                TrackString("Lugar nacimiento", existing.PlaceOfBirth, Str("PlaceOfBirth"), x => existing.PlaceOfBirth = x);
                TrackString("Nacionalidad", existing.Nationality, Str("Nationality"), x => existing.Nationality = x);
                TrackString("Nombre del padre", existing.FatherName, Str("FatherName"), x => existing.FatherName = x);
                TrackString("Nombre de la madre", existing.MotherName, Str("MotherName"), x => existing.MotherName = x);
                TrackString("Calle y número", existing.AddressStreet, Str("AddressStreet"), x => existing.AddressStreet = x);
                TrackString("Colonia", existing.AddressColony, Str("AddressColony"), x => existing.AddressColony = x);
                TrackString("Ciudad", existing.AddressCity, Str("AddressCity"), x => existing.AddressCity = x);
                TrackString("Estado", existing.AddressState, Str("AddressState"), x => existing.AddressState = x);
                TrackString("Código postal", existing.AddressZipCode, Str("AddressZipCode"), x => existing.AddressZipCode = x);
                TrackGuid("Sucursal", existing.BranchId, FkGuid("BranchId"), x => existing.BranchId = x);
                TrackGuid("Departamento", existing.DepartmentId, FkGuid("DepartmentId"), x => existing.DepartmentId = x);
                TrackGuid("Puesto", existing.PositionId, FkGuid("PositionId"), x => existing.PositionId = x);
                TrackGuid("Horario", existing.WorkScheduleId, FkGuid("WorkScheduleId"), x => existing.WorkScheduleId = x);
                TrackDecimal("Sueldo del periodo", existing.PeriodSalary, Dec("PeriodSalary"), x => existing.PeriodSalary = x);
                TrackDecimal("Salario diario", existing.DailySalary, Dec("DailySalary"), x => existing.DailySalary = x);
                TrackDecimal("Salario diario integrado", existing.IntegratedDailySalary, Dec("IntegratedDailySalary"), x => existing.IntegratedDailySalary = x);
                TrackDecimal("SBC fija", existing.SbcFija, Dec("SbcFija"), x => existing.SbcFija = x);
                TrackDate("Fecha alta", existing.HireDate, Dt("HireDate"), x => existing.HireDate = x ?? existing.HireDate);
                TrackDate("Fecha nacimiento", existing.BirthDate, Dt("BirthDate"), x => existing.BirthDate = x);
                TrackDate("Fecha alta IMSS", existing.ImssRegistrationDate, Dt("ImssRegistrationDate"), x => existing.ImssRegistrationDate = x);
                TrackBool("IMSS registrado", existing.IsImssRegistered, Bool("IsImssRegistered"), x => existing.IsImssRegistered = x);
                TrackString("Tipo contrato", existing.ContractType, Str("ContractType"), x => existing.ContractType = x);
                TrackString("Forma de pago", existing.PaymentForm, Str("PaymentForm"), x => existing.PaymentForm = x);
                TrackString("Periodo nómina", existing.PayrollPeriodType, Str("PayrollPeriodType"), x => existing.PayrollPeriodType = x);
                TrackString("Banco", existing.BankCode, Str("BankCode"), x => existing.BankCode = x ?? existing.BankCode);
                TrackString("Cuenta bancaria", existing.BankAccount, Str("BankAccount"), x => existing.BankAccount = x ?? existing.BankAccount);
                TrackString("CLABE", existing.Clabe, Str("Clabe"), x => existing.Clabe = x ?? existing.Clabe);
                TrackString("Sucursal banco", existing.BankBranch, Str("BankBranch"), x => existing.BankBranch = x);
                TrackString("AFORE", existing.Afore, Str("Afore"), x => existing.Afore = x);
                TrackString("FONACOT", existing.Fonacot, Str("Fonacot"), x => existing.Fonacot = x);
                TrackString("INFONAVIT", existing.Infonavit, Str("Infonavit"), x => existing.Infonavit = x);
                TrackString("Jefe directo", existing.ImmediateSupervisor, Str("ImmediateSupervisor"), x => existing.ImmediateSupervisor = x);
                TrackString("Categoría", existing.Category, Str("Category"), x => existing.Category = x);
                TrackString("Notas", existing.Notes, Str("Notes"), x => existing.Notes = x);

                if (changes.Count == 0)
                {
                    v["__ImportMessage"] = $"Sin cambios en {existing.Code} · {existing.FirstName} {existing.LastName}".Trim();
                    return true;
                }

                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = "import";
                await db.SaveChangesAsync();
                v["__ImportMessage"] = $"Actualizado {existing.Code} · {existing.FirstName} {existing.LastName}. Cambios: {string.Join("; ", changes)}";
                return true;
            }
        },

        // ── Turnos ───────────────────────────────────────────────
        new EntityDef
        {
            Key = "hr-shifts", Name = "Turnos", Group = "RH",
            DetectKeywords = ["turno","shift","entrada","salida","horario turno"],
            Fields =
            [
                new() { Key="Code",             Name="Código",     DataType="text",    Required=false, Aliases=A("codigo","clave","code","shift code") },
                new() { Key="Name",             Name="Turno",      DataType="text",    Required=true,  Aliases=A("nombre","turno","shift","name") },
                new() { Key="StartTime",        Name="Entrada",    DataType="text",    Required=false, Aliases=A("entrada","hora entrada","start","starttime","inicio","entry") },
                new() { Key="EndTime",          Name="Salida",     DataType="text",    Required=false, Aliases=A("salida","hora salida","end","endtime","salida","exit") },
                new() { Key="BreakMinutes",     Name="Descanso",   DataType="decimal", Required=false, Aliases=A("descanso","comida","break","breakminutes","minutos descanso") },
                new() { Key="ToleranceMinutes", Name="Tolerancia", DataType="decimal", Required=false, Aliases=A("tolerancia","tolerance","tol") },
            ],
            GetExistingCodesAsync = async (db, cid) => (await db.WorkShifts.Where(x => x.CompanyId == cid).Select(x => x.Code).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            GetExistingNamesAsync = async (db, cid) => (await db.WorkShifts.Where(x => x.CompanyId == cid).Select(x => x.Name).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            InsertAsync = async (db, v) =>
            {
                string? Str(string k) => v.TryGetValue(k, out var x) ? x?.ToString() : null;
                decimal Dec(string k) => v.TryGetValue(k, out var x) && x is decimal d ? d : 0;
                var e = new WorkShift { TenantId=(Guid)v["TenantId"]!, CompanyId=(Guid)v["CompanyId"]!, Code=Str("Code")??Slug(Str("Name")??""), Name=Str("Name")??"", StartTime=Str("StartTime")??"08:00", EndTime=Str("EndTime")??"17:00", BreakMinutes=(int)Dec("BreakMinutes"), ToleranceMinutes=(int)Dec("ToleranceMinutes"), IsActive=true, CreatedBy="import" };
                db.WorkShifts.Add(e);
                await db.SaveChangesAsync();
                return true;
            }
        },

        // ── Conceptos de nómina ───────────────────────────────────
        new EntityDef
        {
            Key = "payroll-concepts", Name = "Conceptos de nómina", Group = "Nómina",
            DetectKeywords = ["concepto","percepcion","deduccion","sat code","satcode"],
            Fields =
            [
                new() { Key="Code",            Name="Código",      DataType="text", Required=true,  Aliases=A("codigo","clave","code","concepto codigo") },
                new() { Key="Name",            Name="Concepto",    DataType="text", Required=true,  Aliases=A("nombre","concepto","name","concept") },
                new() { Key="ConceptType",     Name="Tipo",        DataType="text", Required=true,  Aliases=A("tipo","tipo concepto","concepttype","type","clase") },
                new() { Key="CalculationType", Name="Cálculo",     DataType="text", Required=false, Aliases=A("calculo","calculotype","calculation","tipo calculo") },
                new() { Key="SatCode",         Name="Código SAT",  DataType="text", Required=false, Aliases=A("sat","satcode","codigosat","codigo sat","clave sat") },
                new() { Key="SatAgrupador",    Name="Agrupador",   DataType="text", Required=false, Aliases=A("agrupador","satgroup","agrupadorsat") },
            ],
            GetExistingCodesAsync = async (db, cid) => (await db.PayrollConcepts.Where(x => x.CompanyId == cid).Select(x => x.Code).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            GetExistingNamesAsync = async (db, cid) => (await db.PayrollConcepts.Where(x => x.CompanyId == cid).Select(x => x.Name).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            InsertAsync = async (db, v) =>
            {
                string? Str(string k) => v.TryGetValue(k, out var x) ? x?.ToString() : null;
                var e = new PayrollConcept { TenantId=(Guid)v["TenantId"]!, CompanyId=(Guid)v["CompanyId"]!, Code=Str("Code")??"", Name=Str("Name")??"", ConceptType=Str("ConceptType")??"percepcion", CalculationType=Str("CalculationType")??"fixed", SatCode=Str("SatCode"), SatAgrupador=Str("SatAgrupador"), IsActive=true, CreatedBy="import" };
                db.PayrollConcepts.Add(e);
                await db.SaveChangesAsync();
                return true;
            }
        },

        // ── Tipos de periodo de nómina ────────────────────────────
        new EntityDef
        {
            Key = "payroll-period-types", Name = "Tipos de nómina", Group = "Nómina",
            DetectKeywords = ["periodo","period type","tipo nomina","diasperiodo"],
            Fields =
            [
                new() { Key="Code",           Name="Código",      DataType="text",    Required=false, Aliases=A("codigo","clave","code") },
                new() { Key="Name",           Name="Nombre",      DataType="text",    Required=true,  Aliases=A("nombre","tipo","name","tipo nomina","period type") },
                new() { Key="DaysPerPeriod",  Name="Días/Periodo",DataType="decimal", Required=false, Aliases=A("dias","diasperiodo","dias periodo","days","days per period") },
                new() { Key="PeriodsPerYear", Name="Periodos/año",DataType="decimal", Required=false, Aliases=A("periodos","periodosaño","periodos año","periods per year","periodos año") },
            ],
            GetExistingCodesAsync = async (db, cid) => (await db.PayrollPeriodTypes.Where(x => x.CompanyId == cid).Select(x => x.Code).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            GetExistingNamesAsync = async (db, cid) => (await db.PayrollPeriodTypes.Where(x => x.CompanyId == cid).Select(x => x.Name).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            InsertAsync = async (db, v) =>
            {
                string? Str(string k) => v.TryGetValue(k, out var x) ? x?.ToString() : null;
                decimal Dec(string k) => v.TryGetValue(k, out var x) && x is decimal d ? d : 0;
                var e = new PayrollPeriodType { TenantId=(Guid)v["TenantId"]!, CompanyId=(Guid)v["CompanyId"]!, Code=Str("Code")??Slug(Str("Name")??""), Name=Str("Name")??"", DaysPerPeriod=(int)Dec("DaysPerPeriod"), PeriodsPerYear=(int)Dec("PeriodsPerYear"), IsActive=true, CreatedBy="import" };
                db.PayrollPeriodTypes.Add(e);
                await db.SaveChangesAsync();
                return true;
            }
        },

        // ── Tipos de ausencia ─────────────────────────────────────
        new EntityDef
        {
            Key = "hr-leave-types", Name = "Tipos de ausencia", Group = "RH",
            DetectKeywords = ["ausencia","permiso","vacacion","leave","incapacidad"],
            Fields =
            [
                new() { Key="Code",     Name="Código",    DataType="text", Required=false, Aliases=A("codigo","clave","code") },
                new() { Key="Name",     Name="Tipo",      DataType="text", Required=true,  Aliases=A("nombre","tipo","ausencia","leave","name","permiso") },
                new() { Key="Category", Name="Categoría", DataType="text", Required=false, Aliases=A("categoria","category","clase") },
                new() { Key="WithPay",  Name="Con goce",  DataType="bool", Required=false, Aliases=A("goce","con goce","con sueldo","withpay","paid") },
                new() { Key="DefaultDays", Name="Días",   DataType="decimal", Required=false, Aliases=A("dias","dias default","default days","dias maximos") },
            ],
            GetExistingCodesAsync = async (db, cid) => (await db.LeaveTypes.Where(x => x.CompanyId == cid).Select(x => x.Code).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            GetExistingNamesAsync = async (db, cid) => (await db.LeaveTypes.Where(x => x.CompanyId == cid).Select(x => x.Name).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            InsertAsync = async (db, v) =>
            {
                string? Str(string k) => v.TryGetValue(k, out var x) ? x?.ToString() : null;
                decimal Dec(string k) => v.TryGetValue(k, out var x) && x is decimal d ? d : 0;
                bool Bl(string k) => v.TryGetValue(k, out var x) && x is true;
                var e = new LeaveType { TenantId=(Guid)v["TenantId"]!, CompanyId=(Guid)v["CompanyId"]!, Code=Str("Code")??Slug(Str("Name")??""), Name=Str("Name")??"", Category=Str("Category"), WithPay=Bl("WithPay"), DefaultDays=(int)Dec("DefaultDays"), ImpactsPayroll=false, IsActive=true, CreatedBy="import" };
                db.LeaveTypes.Add(e);
                await db.SaveChangesAsync();
                return true;
            }
        },

        // ── Checadas (Reloj checador) ─────────────────────────────
        // Transaccional: cada fila genera 1 o 2 AttendancePunch.
        // Soporta formato "primera/última perforación" (Silvasoft) y
        // formato matricial (Fecha + HoraEntrada/HoraSalida) y simple (FechaHora + Tipo).
        new EntityDef
        {
            Key = "hr-time-clock", Name = "Checadas", Group = "RH",
            DetectKeywords = ["perforacion","checada","punch","primeraperforacion","ultimaperforacion","horasreales","horareal"],
            Fields =
            [
                new() { Key="EmployeeId",  Name="Empleado",     DataType="fk",   Required=true,  FkEntity="hr-employees", Aliases=A("iddepersona","id de persona","clave reloj","clavereloj","reloj","clockkey","clave empleado","claveempleado","codigo","empleado","noempleado","numeroempleado","numero empleado","employee","employee id","id empleado") },
                new() { Key="WorkDate",    Name="Fecha",        DataType="date", Required=true,  Aliases=A("fecha","dia","date","workdate","work date","fecha checada") },
                new() { Key="EntryTime",   Name="Entrada",      DataType="text", Required=false, Aliases=A("horaentrada","hora entrada","entrada","primeraperforacion","primera perforacion","first punch","entry","check in","checkin","horaentry") },
                new() { Key="ExitTime",    Name="Salida",       DataType="text", Required=false, Aliases=A("horasalida","hora salida","salida","ultimaperforacion","ultima perforacion","last punch","exit","check out","checkout","horaexit") },
                new() { Key="FechaHora",   Name="Fecha y hora", DataType="text", Required=false, Aliases=A("fechahora","fecha y hora","fecha hora","datetime","timestamp","punchdatetime","fechachecada") },
                new() { Key="PunchType",   Name="Tipo",         DataType="text", Required=false, Aliases=A("tipo","punchtype","punch type","type","entradasalida") },
            ],
            GetExistingCodesAsync = (_, _) => Task.FromResult(new HashSet<string>()),
            GetExistingNamesAsync = (_, _) => Task.FromResult(new HashSet<string>()),
            InsertAsync = async (db, v) =>
            {
                Guid? Fk(string k) => v.TryGetValue(k, out var x) && x is Guid g ? g : null;
                string? Str(string k) => v.TryGetValue(k, out var x) ? x?.ToString() : null;
                DateTime? Dt(string k) => v.TryGetValue(k, out var x) && x is DateTime dt ? dt : null;

                var employeeId = Fk("EmployeeId");
                if (employeeId is null)
                    throw new InvalidOperationException("Empleado no encontrado para esa clave");

                var companyId = (Guid)v["CompanyId"]!;
                var tenantId  = (Guid)v["TenantId"]!;

                var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId.Value);
                if (employee is null)
                    throw new InvalidOperationException("Empleado no existe");

                var branchId = await db.Branches.Where(x => x.CompanyId == companyId)
                    .OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

                var fechaHoraStr = Str("FechaHora");
                var entryStr     = Str("EntryTime");
                var exitStr      = Str("ExitTime");
                var workDate     = Dt("WorkDate");
                var punchType    = (Str("PunchType") ?? "").Trim().ToLowerInvariant();

                AttendancePunch BuildOne(DateTime dt, string type) => new()
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    BranchId = branchId,
                    EmployeeId = employee.Id,
                    WorkDate = dt.Date,
                    PunchDateTime = dt,
                    PunchType = type,
                    Source = "universal-import",
                    Status = "captured",
                    IsActive = true,
                    CreatedBy = "import"
                };

                int added = 0;

                if (!string.IsNullOrWhiteSpace(fechaHoraStr) && DateTime.TryParse(fechaHoraStr, out var fh))
                {
                    var t = punchType is "exit" or "salida" or "s" ? "exit" : "entry";
                    db.AttendancePunches.Add(BuildOne(fh, t));
                    added++;
                }
                else if (workDate is not null)
                {
                    if (!string.IsNullOrWhiteSpace(entryStr) && TimeSpan.TryParse(entryStr, out var entryTs))
                    {
                        db.AttendancePunches.Add(BuildOne(workDate.Value.Date.Add(entryTs), "entry"));
                        added++;
                    }
                    if (!string.IsNullOrWhiteSpace(exitStr) && TimeSpan.TryParse(exitStr, out var exitTs)
                        && (!TimeSpan.TryParse(entryStr ?? "", out var entryCheck) || exitTs != entryCheck))
                    {
                        db.AttendancePunches.Add(BuildOne(workDate.Value.Date.Add(exitTs), "exit"));
                        added++;
                    }
                }

                if (added == 0)
                    throw new InvalidOperationException("Fila sin fecha/hora válida");

                await db.SaveChangesAsync();
                return true;
            }
        },
    ];

    private static string Slug(string s)
    {
        s = s.ToUpperInvariant().Trim();
        s = s.Replace("Á","A").Replace("É","E").Replace("Í","I").Replace("Ó","O").Replace("Ú","U").Replace("Ñ","N");
        s = new string(s.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return s.Length > 20 ? s[..20] : s;
    }

    private static string EmployeeCodeFromName(string paterno, string materno, string nombre)
    {
        static string Clean(string s)
        {
            s = s.ToUpperInvariant().Trim();
            s = s.Replace("Á","A").Replace("É","E").Replace("Í","I").Replace("Ó","O").Replace("Ú","U").Replace("Ñ","N");
            return new string(s.Where(char.IsLetter).ToArray());
        }
        var p = Clean(paterno);
        var m = Clean(materno);
        var n = Clean(nombre);
        var part1 = p.Length > 0 ? p[..Math.Min(4, p.Length)] : "_";
        var part2 = m.Length > 0 ? m[..Math.Min(2, m.Length)] : "";
        var part3 = n.Length > 0 ? n[..Math.Min(2, n.Length)] : "";
        return part1 + part2 + part3;
    }
}

// Model
internal sealed class EntityDef
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Group { get; set; } = "";
    public List<string> DetectKeywords { get; set; } = [];
    public List<FieldDef> Fields { get; set; } = [];
    public Func<NanchesoftDbContext, Guid, Task<HashSet<string>>> GetExistingCodesAsync { get; set; } = (_, _) => Task.FromResult(new HashSet<string>());
    public Func<NanchesoftDbContext, Guid, Task<HashSet<string>>> GetExistingNamesAsync  { get; set; } = (_, _) => Task.FromResult(new HashSet<string>());
    public Func<NanchesoftDbContext, Dictionary<string, object?>, Task<bool>> InsertAsync { get; set; } = (_, _) => Task.FromResult(false);
    public Func<NanchesoftDbContext, Dictionary<string, object?>, Task<bool>>? UpdateAsync { get; set; }
}

internal sealed class FieldDef
{
    public string Key      { get; set; } = "";
    public string Name     { get; set; } = "";
    public string DataType { get; set; } = "text";  // text | decimal | date | bool | fk
    public bool   Required { get; set; }
    public string? FkEntity { get; set; }
    public string[] Aliases { get; set; } = [];
}

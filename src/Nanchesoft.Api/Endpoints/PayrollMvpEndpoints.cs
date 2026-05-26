using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PayrollMvpEndpoints
{
    private const string UnidentifiedCatalogCode = "POR IDENTIFICAR";

    public static IEndpointRouteBuilder MapPayrollMvpEndpoints(this IEndpointRouteBuilder app)
    {
        var employees = app.MapGroup("/api/hr/employees").WithTags("HrEmployees");
        employees.MapGet("/import/template", DownloadImportTemplateAsync);
        employees.MapPost("/import/preview", PreviewImportFromExcelAsync).DisableAntiforgery();
        employees.MapPost("/import", ImportEmployeesFromExcelAsync).DisableAntiforgery();

        var clock = app.MapGroup("/api/hr/time-clock").WithTags("PayrollTimeClock");
        clock.MapPost("/import", ImportPunchesFromExcelAsync).DisableAntiforgery();
        clock.MapPost("/import/preview", PreviewPunchesFromFileAsync).DisableAntiforgery();
        clock.MapPost("/import/csv", ImportPunchesFromCsvAsync).DisableAntiforgery();
        clock.MapGet("/import-history", GetClockImportHistoryAsync);

        var clockMappings = app.MapGroup("/api/hr/clock-import-mappings").WithTags("ClockImportMappings");
        clockMappings.MapGet("/", GetClockImportMappingsAsync);
        clockMappings.MapPost("/", CreateClockImportMappingAsync);
        clockMappings.MapPut("/{id:guid}", UpdateClockImportMappingAsync);
        clockMappings.MapDelete("/{id:guid}", DeleteClockImportMappingAsync);

        var periods = app.MapGroup("/api/payroll/periods").WithTags("PayrollPeriods");
        periods.MapGet("/{periodId:guid}/generation-preview", GetPayrollGenerationPreviewAsync);
        periods.MapPost("/{periodId:guid}/generate-summaries", GenerateAttendanceDailySummariesAsync);
        periods.MapPost("/{periodId:guid}/generate-incidents", GenerateIncidentsFromSummariesAsync);
        periods.MapPost("/{periodId:guid}/generate-incidents-policy", GenerateIncidentsFromPolicyAsync);
        periods.MapGet("/{periodId:guid}/operational-prepayroll", GetOperationalPrePayrollAsync);
        periods.MapPost("/{periodId:guid}/generate-operational-prepayroll", GenerateOperationalPrePayrollAsync);

        var runs = app.MapGroup("/api/payroll/runs").WithTags("PayrollRuns");
        runs.MapPost("/{runId:guid}/calculate", CalculatePayrollRunAsync);
        runs.MapGet("/{runId:guid}/receipts/{lineId:guid}", GetPayrollReceiptHtmlAsync);

        app.MapPost("/api/payroll/seed-default-concepts", SeedDefaultConceptsAsync).WithTags("PayrollConcepts");

        return app;
    }

    // ──────────────────────────────────────────────────────────────
    // 1. IMPORTAR EMPLEADOS DESDE EXCEL
    //
    // Pipeline: ParseFile → DetectDuplicates → ValidateAgainstDb →
    //           ApplyConflictMode → SaveTransactionally → PersistLog.
    //
    // Modos de conflicto (query string ?conflict_mode=update|skip|error):
    //   * update  (default) — si el empleado existe se actualiza.
    //   * skip               — si existe se omite (no se modifica nada).
    //   * error              — si existe se rechaza la fila.
    //
    // Validaciones obligatorias:
    //   * Duplicados intra-archivo por NoEmpleado, RFC, CURP, NSS.
    //   * Conflictos cruzados contra BD: RFC/CURP/NSS pertenecientes a otro empleado.
    //   * Catálogos requeridos (Departamento/Puesto) si vienen códigos.
    //
    // Si CUALQUIER fila tiene status=error y conflict_mode!=skip, se aborta
    // la importación completa (todo o nada). La bitácora se guarda siempre.
    // ──────────────────────────────────────────────────────────────
    private static async Task<IResult> ImportEmployeesFromExcelAsync(HttpContext httpContext, IFormFile file, NanchesoftDbContext db)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { message = "El archivo está vacío." });

        var conflictMode = ResolveConflictMode(httpContext);

        var (companyOrError, branchId) = await ResolveImportScopeAsync(httpContext, db);
        if (companyOrError is not Company company)
            return Results.BadRequest(new { message = companyOrError as string ?? "No se pudo determinar la empresa." });

        var sw = System.Diagnostics.Stopwatch.StartNew();
        EmployeeImportBundle? bundle;
        try
        {
            bundle = await BuildEmployeeImportBundleAsync(file, db, company, branchId, conflictMode);
        }
        catch (Exception ex)
        {
            return Results.Problem($"No se pudo leer el archivo: {ex.Message}", statusCode: 400);
        }

        var rows = bundle.Rows;
        var summary = SummarizeRows(rows);
        var executedBy = ResolveUserName(httpContext);

        var fatalError = summary.ErrorCount > 0 && conflictMode != "skip";
        var log = new HrEmployeeImportLog
        {
            TenantId = company.TenantId,
            CompanyId = company.Id,
            BranchId = branchId,
            FileName = file.FileName ?? string.Empty,
            FileSizeBytes = file.Length,
            ConflictMode = conflictMode,
            TotalRows = summary.Total,
            CreatedCount = summary.NewCount,
            UpdatedCount = summary.UpdateCount,
            SkippedCount = summary.SkipCount,
            DuplicateCount = summary.DuplicateCount,
            ErrorCount = summary.ErrorCount,
            ExecutedBy = executedBy,
            ExecutedAt = DateTime.UtcNow
        };

        // Errores fatales: no se aplica nada, sólo se persiste bitácora.
        if (fatalError)
        {
            log.Success = false;
            log.RolledBack = true;
            log.Errors = System.Text.Json.JsonSerializer.Serialize(rows.Where(r => r.Status == "error").Take(200));
            log.Duplicates = System.Text.Json.JsonSerializer.Serialize(rows.Where(r => r.Status == "duplicate").Take(200));
            log.DurationMs = (int)sw.ElapsedMilliseconds;
            db.HrEmployeeImportLogs.Add(log);
            await db.SaveChangesAsync();

            return Results.BadRequest(new
            {
                success = false,
                message = "La importación se canceló porque hay filas con error. Corrige el archivo o usa conflict_mode=skip.",
                rolledBack = true,
                summary = new
                {
                    total = summary.Total,
                    created = 0,
                    updated = 0,
                    skipped = summary.SkipCount,
                    duplicates = summary.DuplicateCount,
                    errors = summary.ErrorCount,
                },
                errorRows = rows.Where(r => r.Status == "error").Take(50).Select(r => new { r.RowNumber, r.EmployeeNumber, r.Message }),
                logId = log.Id
            });
        }

        // Aplicar las filas válidas en una sola transacción.
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            await EnsureImportCatalogsAsync(db, company, rows);

            foreach (var row in rows)
            {
                if (row.Status == "new")
                {
                    db.Employees.Add(BuildNewEmployee(row, company, branchId));
                }
                else if (row.Status == "update")
                {
                    ApplyUpdate(row, bundle.ExistingByEmployeeNumber[row.EmployeeNumber]);
                }
            }

            await db.SaveChangesAsync();

            log.Success = true;
            log.RolledBack = false;
            log.Errors = System.Text.Json.JsonSerializer.Serialize(rows.Where(r => r.Status == "error").Take(200));
            log.Duplicates = System.Text.Json.JsonSerializer.Serialize(rows.Where(r => r.Status == "duplicate").Take(200));
            log.DurationMs = (int)sw.ElapsedMilliseconds;
            db.HrEmployeeImportLogs.Add(log);
            await db.SaveChangesAsync();

            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();

            // Persistir log fuera de la transacción para que quede registro del fallo.
            log.Success = false;
            log.RolledBack = true;
            log.ErrorCount = Math.Max(log.ErrorCount, 1);
            log.Errors = System.Text.Json.JsonSerializer.Serialize(new[] { new { row = 0, message = $"Excepción al guardar: {ex.Message}" } });
            log.DurationMs = (int)sw.ElapsedMilliseconds;
            db.ChangeTracker.Clear();
            db.HrEmployeeImportLogs.Add(log);
            await db.SaveChangesAsync();

            return Results.Problem($"Importación abortada y revertida. {ex.Message}", statusCode: 500);
        }

        return Results.Ok(new
        {
            success = true,
            created = summary.NewCount,
            updated = summary.UpdateCount,
            skipped = summary.SkipCount,
            duplicates = summary.DuplicateCount,
            errors = summary.ErrorCount,
            logId = log.Id,
            durationMs = log.DurationMs
        });
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers compartidos entre Preview e Import
    // ──────────────────────────────────────────────────────────────
    private static string ResolveConflictMode(HttpContext httpContext)
    {
        var mode = httpContext.Request.Query["conflict_mode"].ToString().Trim().ToLowerInvariant();
        return mode is "update" or "skip" or "error" ? mode : "update";
    }

    private static string ResolveUserName(HttpContext httpContext)
    {
        var headerUser = httpContext.Request.Headers["X-User-Email"].ToString();
        if (!string.IsNullOrWhiteSpace(headerUser)) return headerUser.Trim();
        var headerName = httpContext.Request.Headers["X-User-Name"].ToString();
        if (!string.IsNullOrWhiteSpace(headerName)) return headerName.Trim();
        return httpContext.User?.Identity?.Name ?? "excel-import";
    }

    private static async Task<(object companyOrError, Guid? branchId)> ResolveImportScopeAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var ctxTenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var ctxCompanyId = ApiTenantScope.ResolveCompanyId(httpContext);
        var ctxBranchId = ApiTenantScope.ResolveBranchId(httpContext);

        Company? company = null;
        if (ctxCompanyId.HasValue)
            company = await db.Companies.FirstOrDefaultAsync(x => x.Id == ctxCompanyId.Value);
        else if (ctxTenantId.HasValue)
            company = await db.Companies.Where(x => x.TenantId == ctxTenantId.Value && x.IsActive).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();

        if (company is null)
            return ("No se pudo determinar la empresa para importar. Selecciona empresa activa en el contexto del usuario.", null);

        var branchId = ctxBranchId
            ?? await db.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

        return (company, branchId);
    }

    private sealed class EmployeeImportBundle
    {
        public List<EmployeeImportPreviewRow> Rows { get; set; } = [];
        public Dictionary<string, Employee> ExistingByEmployeeNumber { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<EmployeeImportFieldMapping> Mappings { get; set; } = [];
    }

    private sealed class ImportSummary
    {
        public int Total { get; set; }
        public int NewCount { get; set; }
        public int UpdateCount { get; set; }
        public int SkipCount { get; set; }
        public int DuplicateCount { get; set; }
        public int ErrorCount { get; set; }
    }

    private static ImportSummary SummarizeRows(List<EmployeeImportPreviewRow> rows) => new()
    {
        Total = rows.Count,
        NewCount = rows.Count(r => r.Status == "new"),
        UpdateCount = rows.Count(r => r.Status == "update"),
        SkipCount = rows.Count(r => r.Status == "skip"),
        DuplicateCount = rows.Count(r => r.Status == "duplicate"),
        ErrorCount = rows.Count(r => r.Status == "error")
    };

    private static async Task<EmployeeImportBundle> BuildEmployeeImportBundleAsync(
        IFormFile file, NanchesoftDbContext db, Company company, Guid? branchIdHint, string conflictMode)
    {
        var bundle = new EmployeeImportBundle();

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet(1);

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in ws.Row(1).CellsUsed())
            headers[cell.GetString().Trim()] = cell.Address.ColumnNumber;
        bundle.Mappings = BuildEmployeeImportMappings(headers);

        var branches = await db.Branches.Where(x => x.CompanyId == company.Id)
            .ToDictionaryAsync(x => x.Code.ToUpperInvariant(), x => new { x.Id, x.Name });
        var departments = await db.Departments.Where(x => x.CompanyId == company.Id)
            .ToDictionaryAsync(x => x.Code.ToUpperInvariant(), x => new { x.Id, x.Name });
        var positions = await db.Positions.Where(x => x.CompanyId == company.Id)
            .ToDictionaryAsync(x => x.Code.ToUpperInvariant(), x => new { x.Id, x.Name });

        var existing = await db.Employees.Where(x => x.CompanyId == company.Id).ToListAsync();
        bundle.ExistingByEmployeeNumber = existing.ToDictionary(x => x.EmployeeNumber.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);
        var existingByTaxId = existing.Where(x => !string.IsNullOrWhiteSpace(x.TaxId))
            .GroupBy(x => x.TaxId.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First());
        var existingByCurp = existing.Where(x => !string.IsNullOrWhiteSpace(x.Curp))
            .GroupBy(x => x.Curp.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First());
        var existingByNss = existing.Where(x => !string.IsNullOrWhiteSpace(x.Nss))
            .GroupBy(x => x.Nss.Trim())
            .ToDictionary(g => g.Key, g => g.First());

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        // 1) Lectura inicial — todas las filas a estructura uniforme.
        var rawRows = new List<EmployeeImportPreviewRow>();
        for (int r = 2; r <= lastRow; r++)
        {
            var empNum = GetCell(ws, r, headers, "NoEmpleado").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(empNum))
            {
                rawRows.Add(new EmployeeImportPreviewRow { RowNumber = r, Status = "skip", Message = "Sin número de empleado." });
                continue;
            }

            var firstName = GetCell(ws, r, headers, "Nombre").Trim();
            var lastName = GetCell(ws, r, headers, "ApellidoPaterno").Trim();
            var secondLastName = GetCell(ws, r, headers, "ApellidoMaterno").Trim();
            var taxId = GetCell(ws, r, headers, "RFC").Trim().ToUpperInvariant();
            var curp = GetCell(ws, r, headers, "CURP").Trim().ToUpperInvariant();
            var nss = GetCell(ws, r, headers, "NSS").Trim();
            var email = GetCell(ws, r, headers, "Email").Trim();
            var phone = GetCell(ws, r, headers, "Telefono").Trim();
            var hireDateStr = GetCell(ws, r, headers, "FechaIngreso").Trim();
            var periodSalaryStr = GetCellAny(ws, r, headers, "SueldoPeriodo", "SueldoDelPeriodo", "Sueldo del periodo", "Sueldo periodo", "Sueldo semanal").Trim();
            var salaryStr = GetCell(ws, r, headers, "SalarioDiario").Trim();
            var intSalaryStr = GetCell(ws, r, headers, "SalarioDiarioIntegrado").Trim();
            var branchCode = NormalizeCatalogCode(GetCell(ws, r, headers, "SucursalCodigo"));
            var deptCode = NormalizeCatalogCode(GetCell(ws, r, headers, "DepartamentoCodigo"));
            var posCode = NormalizeCatalogCode(GetCell(ws, r, headers, "PuestoCodigo"));

            _ = DateTime.TryParse(hireDateStr, out var hireDate);
            TryParseDecimalValue(periodSalaryStr, out var periodSalary);
            TryParseDecimalValue(salaryStr, out var dailySalary);
            TryParseDecimalValue(intSalaryStr, out var intDailySalary);
            if (intDailySalary == 0m && dailySalary != 0m) intDailySalary = dailySalary;

            string branchName = "", deptName = "", posName = "";
            Guid? branchId = null, deptId = null, posId = null;
            if (branches.TryGetValue(branchCode, out var b)) branchName = b.Name;
            if (branches.TryGetValue(branchCode, out b)) branchId = b.Id;
            if (departments.TryGetValue(deptCode, out var d)) deptName = d.Name;
            if (departments.TryGetValue(deptCode, out d)) deptId = d.Id;
            if (positions.TryGetValue(posCode, out var p)) posName = p.Name;
            if (positions.TryGetValue(posCode, out p)) posId = p.Id;

            rawRows.Add(new EmployeeImportPreviewRow
            {
                RowNumber = r,
                EmployeeNumber = empNum,
                FirstName = firstName,
                LastName = lastName,
                SecondLastName = secondLastName,
                FullName = string.Join(" ", new[] { firstName, lastName, secondLastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                TaxId = taxId,
                Curp = curp,
                Nss = nss,
                Email = email,
                Phone = phone,
                HireDate = hireDate == default ? null : hireDate.Date,
                PeriodSalary = periodSalary,
                DailySalary = dailySalary,
                IntegratedDailySalary = intDailySalary,
                BranchCode = branchCode,
                BranchName = branchName,
                BranchId = branchId,
                CreateBranch = branchId is null,
                DepartmentCode = deptCode,
                DepartmentName = deptName,
                DepartmentId = deptId,
                CreateDepartment = deptId is null,
                PositionCode = posCode,
                PositionName = posName,
                PositionId = posId,
                CreatePosition = posId is null,
                HireDateRaw = hireDateStr,
                PeriodSalaryRaw = periodSalaryStr,
                SalaryRaw = salaryStr
            });
        }

        // 2) Duplicados dentro del propio archivo.
        var dupEmpNum = rawRows.Where(r => !string.IsNullOrWhiteSpace(r.EmployeeNumber))
                               .GroupBy(r => r.EmployeeNumber).Where(g => g.Count() > 1)
                               .SelectMany(g => g.Skip(1)).Select(r => r.RowNumber).ToHashSet();
        var dupTaxId = rawRows.Where(r => !string.IsNullOrWhiteSpace(r.TaxId))
                              .GroupBy(r => r.TaxId).Where(g => g.Count() > 1)
                              .SelectMany(g => g.Skip(1)).Select(r => r.RowNumber).ToHashSet();
        var dupCurp = rawRows.Where(r => !string.IsNullOrWhiteSpace(r.Curp))
                             .GroupBy(r => r.Curp).Where(g => g.Count() > 1)
                             .SelectMany(g => g.Skip(1)).Select(r => r.RowNumber).ToHashSet();
        var dupNss = rawRows.Where(r => !string.IsNullOrWhiteSpace(r.Nss))
                            .GroupBy(r => r.Nss).Where(g => g.Count() > 1)
                            .SelectMany(g => g.Skip(1)).Select(r => r.RowNumber).ToHashSet();

        // 3) Asignar status final.
        foreach (var row in rawRows)
        {
            if (row.Status == "skip") continue; // ya skip por falta de NoEmpleado

            var rowErrors = new List<string>();

            if (dupEmpNum.Contains(row.RowNumber)) rowErrors.Add($"Número de empleado '{row.EmployeeNumber}' repetido en el archivo.");
            if (dupTaxId.Contains(row.RowNumber)) rowErrors.Add($"RFC '{row.TaxId}' repetido en el archivo.");
            if (dupCurp.Contains(row.RowNumber)) rowErrors.Add($"CURP '{row.Curp}' repetida en el archivo.");
            if (dupNss.Contains(row.RowNumber)) rowErrors.Add($"NSS '{row.Nss}' repetido en el archivo.");

            var isExisting = bundle.ExistingByEmployeeNumber.ContainsKey(row.EmployeeNumber);

            // Choque cruzado contra BD: el RFC/CURP/NSS pertenece a OTRO empleado distinto del que se está procesando.
            if (!string.IsNullOrWhiteSpace(row.TaxId) && existingByTaxId.TryGetValue(row.TaxId, out var owner1)
                && (!isExisting || !string.Equals(owner1.EmployeeNumber, row.EmployeeNumber, StringComparison.OrdinalIgnoreCase)))
                rowErrors.Add($"RFC '{row.TaxId}' ya pertenece al empleado {owner1.EmployeeNumber}.");
            if (!string.IsNullOrWhiteSpace(row.Curp) && existingByCurp.TryGetValue(row.Curp, out var owner2)
                && (!isExisting || !string.Equals(owner2.EmployeeNumber, row.EmployeeNumber, StringComparison.OrdinalIgnoreCase)))
                rowErrors.Add($"CURP '{row.Curp}' ya pertenece al empleado {owner2.EmployeeNumber}.");
            if (!string.IsNullOrWhiteSpace(row.Nss) && existingByNss.TryGetValue(row.Nss, out var owner3)
                && (!isExisting || !string.Equals(owner3.EmployeeNumber, row.EmployeeNumber, StringComparison.OrdinalIgnoreCase)))
                rowErrors.Add($"NSS '{row.Nss}' ya pertenece al empleado {owner3.EmployeeNumber}.");

            // Validaciones de campos básicos.
            if (!isExisting)
            {
                if (string.IsNullOrWhiteSpace(row.FirstName)) rowErrors.Add("Nombre vacío.");
                if (string.IsNullOrWhiteSpace(row.LastName)) rowErrors.Add("Apellido paterno vacío.");
            }
            if (!string.IsNullOrWhiteSpace(row.HireDateRaw) && !DateTime.TryParse(row.HireDateRaw, out _))
                rowErrors.Add($"Fecha ingreso inválida: '{row.HireDateRaw}'.");
            if (!string.IsNullOrWhiteSpace(row.SalaryRaw) && !TryParseDecimalValue(row.SalaryRaw, out _))
                rowErrors.Add($"Salario diario inválido: '{row.SalaryRaw}'.");
            if (!string.IsNullOrWhiteSpace(row.PeriodSalaryRaw) && !TryParseDecimalValue(row.PeriodSalaryRaw, out _))
                rowErrors.Add($"Sueldo del periodo inválido: '{row.PeriodSalaryRaw}'.");
            if (rowErrors.Count > 0)
            {
                row.Status = "error";
                row.Message = string.Join(" ", rowErrors);
                continue;
            }

            if (isExisting)
            {
                switch (conflictMode)
                {
                    case "skip":
                        row.Status = "skip";
                        row.Message = "Existe — omitido (modo skip).";
                        break;
                    case "error":
                        row.Status = "duplicate";
                        row.Message = "Ya existe en la base de datos (modo error).";
                        break;
                    default:
                        row.Status = "update";
                        row.Message = "Actualización.";
                        break;
                }
            }
            else
            {
                row.Status = "new";
                row.Message = "Nuevo.";
            }

            if (row.Status is "new" or "update")
            {
                if (row.CreateBranch)
                    row.Message = AppendMessage(row.Message, $"Se creará la sucursal '{row.BranchCode}'.");
                else if (string.Equals(row.BranchCode, UnidentifiedCatalogCode, StringComparison.OrdinalIgnoreCase))
                    row.Message = AppendMessage(row.Message, $"Se asignará la sucursal '{UnidentifiedCatalogCode}'.");
                if (row.CreateDepartment)
                    row.Message = AppendMessage(row.Message, $"Se creará el departamento '{row.DepartmentCode}'.");
                else if (string.Equals(row.DepartmentCode, UnidentifiedCatalogCode, StringComparison.OrdinalIgnoreCase))
                    row.Message = AppendMessage(row.Message, $"Se asignará el departamento '{UnidentifiedCatalogCode}'.");

                if (row.CreatePosition)
                    row.Message = AppendMessage(row.Message, $"Se creará el puesto '{row.PositionCode}'.");
                else if (string.Equals(row.PositionCode, UnidentifiedCatalogCode, StringComparison.OrdinalIgnoreCase))
                    row.Message = AppendMessage(row.Message, $"Se asignará el puesto '{UnidentifiedCatalogCode}'.");
            }
        }

        var nextEmployeeCodeNumber = GetNextEmployeeCodeNumber(existing);
        foreach (var row in rawRows.Where(x => x.Status == "new"))
        {
            row.Code = FormatEmployeeCode(nextEmployeeCodeNumber++);
            row.Message = AppendMessage($"Nuevo. Código asignado: {row.Code}.", row.Message.Replace("Nuevo.", string.Empty).Trim());
        }

        foreach (var row in rawRows.Where(x => x.Status == "update"))
        {
            if (bundle.ExistingByEmployeeNumber.TryGetValue(row.EmployeeNumber, out var existingEmployee))
                row.Code = existingEmployee.Code;
        }

        bundle.Rows = rawRows;
        return bundle;
    }

    private static Employee BuildNewEmployee(EmployeeImportPreviewRow row, Company company, Guid? branchId)
    {
        return new Employee
        {
            TenantId = company.TenantId,
            CompanyId = company.Id,
            BranchId = row.BranchId ?? branchId,
            DepartmentId = row.DepartmentId,
            PositionId = row.PositionId,
            Code = row.Code,
            EmployeeNumber = row.EmployeeNumber,
            FirstName = row.FirstName,
            LastName = row.LastName,
            SecondLastName = string.IsNullOrWhiteSpace(row.SecondLastName) ? null : row.SecondLastName,
            MiddleName = string.Empty,
            Email = row.Email,
            Phone = row.Phone,
            TaxId = row.TaxId,
            NationalId = row.Curp,
            Curp = row.Curp,
            Nss = row.Nss,
            HireDate = row.HireDate ?? DateTime.UtcNow.Date,
            PeriodSalary = row.PeriodSalary,
            DailySalary = row.DailySalary,
            IntegratedDailySalary = row.IntegratedDailySalary,
            Status = "active",
            IsActive = true,
            CreatedBy = "excel-import"
        };
    }

    private static void ApplyUpdate(EmployeeImportPreviewRow row, Employee existing)
    {
        if (!string.IsNullOrWhiteSpace(row.FirstName)) existing.FirstName = row.FirstName;
        if (!string.IsNullOrWhiteSpace(row.LastName)) existing.LastName = row.LastName;
        if (!string.IsNullOrWhiteSpace(row.SecondLastName)) existing.SecondLastName = row.SecondLastName;
        if (!string.IsNullOrWhiteSpace(row.TaxId)) existing.TaxId = row.TaxId;
        if (!string.IsNullOrWhiteSpace(row.Curp)) { existing.Curp = row.Curp; existing.NationalId = row.Curp; }
        if (!string.IsNullOrWhiteSpace(row.Nss)) existing.Nss = row.Nss;
        if (!string.IsNullOrWhiteSpace(row.Email)) existing.Email = row.Email;
        if (!string.IsNullOrWhiteSpace(row.Phone)) existing.Phone = row.Phone;
        if (row.HireDate.HasValue) existing.HireDate = row.HireDate.Value;
        if (row.BranchId.HasValue) existing.BranchId = row.BranchId;
        if (row.DepartmentId.HasValue) existing.DepartmentId = row.DepartmentId;
        if (row.PositionId.HasValue) existing.PositionId = row.PositionId;
        if (!string.IsNullOrWhiteSpace(row.PeriodSalaryRaw)) existing.PeriodSalary = row.PeriodSalary;
        if (!string.IsNullOrWhiteSpace(row.SalaryRaw))
        {
            existing.DailySalary = row.DailySalary;
            existing.IntegratedDailySalary = row.IntegratedDailySalary;
        }
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = "excel-import";
    }

    // ──────────────────────────────────────────────────────────────
    // 2. IMPORTAR CHECADAS DESDE EXCEL
    // Columnas esperadas: NoEmpleado, Fecha, HoraEntrada, HoraSalida
    // También acepta: NoEmpleado, FechaHora, Tipo (entry/exit)
    // ──────────────────────────────────────────────────────────────
    private static async Task<IResult> ImportPunchesFromExcelAsync(IFormFile file, NanchesoftDbContext db)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { message = "El archivo está vacío." });

        var company = await db.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (company is null)
            return Results.BadRequest(new { message = "No existe empresa configurada." });

        var branchId = await db.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

        var employees = await db.Employees.Where(x => x.CompanyId == company.Id && x.IsActive)
            .ToDictionaryAsync(x => x.EmployeeNumber.ToUpperInvariant());
        var existingPunches = await db.AttendancePunches
            .Where(x => x.CompanyId == company.Id && x.IsActive)
            .Select(x => new { x.EmployeeId, x.PunchDateTime, x.PunchType })
            .ToListAsync();
        var punchKeySet = existingPunches.Select(x => BuildPunchKey(x.EmployeeId, x.PunchDateTime, x.PunchType)).ToHashSet();

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet(1);

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in ws.Row(1).CellsUsed())
            headers[cell.GetString().Trim()] = cell.Address.ColumnNumber;

        bool isSimpleFormat = headers.ContainsKey("FechaHora") || headers.ContainsKey("fechahora");

        int created = 0, skipped = 0;
        var errors = new List<string>();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            try
            {
                var empNum = GetCell(ws, row, headers, "NoEmpleado").Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(empNum) || !employees.TryGetValue(empNum, out var employee))
                {
                    skipped++;
                    continue;
                }

                if (isSimpleFormat)
                {
                    var fechaHoraStr = GetCell(ws, row, headers, "FechaHora").Trim();
                    var tipo = GetCell(ws, row, headers, "Tipo").Trim().ToLowerInvariant();
                    if (!DateTime.TryParse(fechaHoraStr, out var punchDt))
                    {
                        errors.Add($"Fila {row}: fecha/hora inválida '{fechaHoraStr}'.");
                        continue;
                    }
                    var punchType = tipo == "exit" ? "exit" : "entry";
                    if (TryQueuePunch(company, branchId, employee.Id, punchDt, punchType, punchKeySet, db))
                        created++;
                    else
                        skipped++;
                }
                else
                {
                    var fechaStr = GetCell(ws, row, headers, "Fecha").Trim();
                    var entradaStr = GetCell(ws, row, headers, "HoraEntrada").Trim();
                    var salidaStr = GetCell(ws, row, headers, "HoraSalida").Trim();

                    if (!DateTime.TryParse(fechaStr, out var fecha))
                    {
                        errors.Add($"Fila {row}: fecha inválida '{fechaStr}'.");
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(entradaStr) && TimeSpan.TryParse(entradaStr, out var entrada))
                    {
                        var entryDt = fecha.Date.Add(entrada);
                        if (TryQueuePunch(company, branchId, employee.Id, entryDt, "entry", punchKeySet, db))
                            created++;
                        else
                            skipped++;
                    }

                    if (!string.IsNullOrWhiteSpace(salidaStr) && TimeSpan.TryParse(salidaStr, out var salida))
                    {
                        var exitDt = fecha.Date.Add(salida);
                        if (TryQueuePunch(company, branchId, employee.Id, exitDt, "exit", punchKeySet, db))
                            created++;
                        else
                            skipped++;
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Fila {row}: {ex.Message}");
            }
        }

        await db.SaveChangesAsync();
        db.ClockImports.Add(new ClockImport
        {
            TenantId = company.TenantId, CompanyId = company.Id,
            FileName = file.FileName ?? string.Empty,
            FileSizeBytes = file.Length,
            FileFormat = Path.GetExtension(file.FileName ?? string.Empty).TrimStart('.').ToUpperInvariant(),
            ImportedAt = DateTime.UtcNow, ImportedBy = "web-api",
            RowsRead = lastRow - 1, RowsCreated = created, RowsSkipped = skipped, RowsError = errors.Count,
            Status = errors.Count > 0 ? "Error" : "Done",
            ErrorSummary = string.Join("; ", errors.Take(5)),
            IsActive = true
        });
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, created, skipped, errors });
    }

    private static AttendancePunch BuildPunch(
        Company company, Guid? branchId, Guid employeeId, DateTime punchDt, string punchType,
        Guid? clockImportId = null, string? rawKey = null)
        => new()
        {
            TenantId = company.TenantId,
            CompanyId = company.Id,
            BranchId = branchId,
            EmployeeId = employeeId,
            WorkDate = punchDt.Date,
            PunchDateTime = punchDt,
            PunchType = punchType,
            Source = "file-import",
            Status = "captured",
            ClockImportId = clockImportId,
            RawEmployeeKey = rawKey,
            IsActive = true,
            CreatedBy = "file-import"
        };

    private static bool TryQueuePunch(
        Company company,
        Guid? branchId,
        Guid employeeId,
        DateTime punchDt,
        string punchType,
        HashSet<string> punchKeySet,
        NanchesoftDbContext db,
        Guid? clockImportId = null,
        string? rawKey = null)
    {
        var key = BuildPunchKey(employeeId, punchDt, punchType);
        if (!punchKeySet.Add(key))
            return false;

        db.AttendancePunches.Add(BuildPunch(company, branchId, employeeId, punchDt, punchType, clockImportId, rawKey));
        return true;
    }

    private static string BuildPunchKey(Guid employeeId, DateTime punchDateTime, string punchType)
        => employeeId.ToString("D") + "|" + punchDateTime.ToString("O") + "|" + punchType;

    // ──────────────────────────────────────────────────────────────
    // 2b. PREVIEW DE CHECADAS (Excel o CSV)
    // Lee el archivo y devuelve filas con estado (new/error/skip)
    // sin persistir nada en la base de datos.
    // ──────────────────────────────────────────────────────────────
    private static async Task<IResult> PreviewPunchesFromFileAsync(IFormFile file, NanchesoftDbContext db)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { message = "El archivo está vacío." });

        var company = await db.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (company is null)
            return Results.BadRequest(new { message = "No existe empresa configurada." });

        var employees = await db.Employees.Where(x => x.CompanyId == company.Id && x.IsActive)
            .Select(x => new { x.Id, x.EmployeeNumber, x.FirstName, x.LastName, x.SecondLastName })
            .ToListAsync();
        var employeeMap = employees.ToDictionary(
            x => x.EmployeeNumber.ToUpperInvariant(),
            x => new PunchEmployeeRef(x.Id, $"{x.FirstName} {x.LastName} {x.SecondLastName}".Trim()));

        var (headers, dataRows) = await ReadPunchFileAsync(file);
        if (headers.Count == 0)
            return Results.BadRequest(new { message = "El archivo no tiene encabezados válidos." });

        var headerMap = BuildPunchHeaderMap(headers);
        bool isSimpleFormat = headerMap.ContainsKey("fechahora");
        var rows = new List<PunchImportPreviewRow>();

        for (int i = 0; i < dataRows.Count; i++)
        {
            int rowNum = i + 2;
            var raw = dataRows[i];
            var empNum = GetCsvField(raw, headerMap, "noempleado").Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(empNum))
            {
                rows.Add(new PunchImportPreviewRow { RowNumber = rowNum, Status = "skip", Message = "Sin número de empleado" });
                continue;
            }
            if (!employeeMap.TryGetValue(empNum, out var emp))
            {
                rows.Add(new PunchImportPreviewRow { RowNumber = rowNum, EmployeeNumber = empNum, Status = "error", Message = "Empleado no encontrado" });
                continue;
            }

            if (isSimpleFormat)
            {
                var fechaHoraStr = GetCsvField(raw, headerMap, "fechahora").Trim();
                var tipo = GetCsvField(raw, headerMap, "tipo").Trim().ToLowerInvariant();
                if (!DateTime.TryParse(fechaHoraStr, out var punchDt))
                {
                    rows.Add(new PunchImportPreviewRow { RowNumber = rowNum, EmployeeNumber = empNum, EmployeeName = emp.FullName, Status = "error", Message = $"Fecha/hora inválida '{fechaHoraStr}'" });
                    continue;
                }
                var punchType = tipo is "exit" or "salida" or "s" ? "exit" : "entry";
                rows.Add(new PunchImportPreviewRow
                {
                    RowNumber = rowNum,
                    EmployeeNumber = empNum,
                    EmployeeName = emp.FullName,
                    PunchDate = punchDt.Date,
                    EntryTime = punchType == "entry" ? punchDt.TimeOfDay : null,
                    ExitTime = punchType == "exit" ? punchDt.TimeOfDay : null,
                    PunchType = punchType,
                    Status = "new",
                    Message = "Listo"
                });
            }
            else
            {
                var fechaStr = GetCsvField(raw, headerMap, "fecha").Trim();
                var entradaStr = GetCsvField(raw, headerMap, "horaentrada").Trim();
                var salidaStr = GetCsvField(raw, headerMap, "horasalida").Trim();
                if (!DateTime.TryParse(fechaStr, out var fecha))
                {
                    rows.Add(new PunchImportPreviewRow { RowNumber = rowNum, EmployeeNumber = empNum, EmployeeName = emp.FullName, Status = "error", Message = $"Fecha inválida '{fechaStr}'" });
                    continue;
                }
                TimeSpan? entry = TimeSpan.TryParse(entradaStr, out var et) ? et : null;
                TimeSpan? exit = TimeSpan.TryParse(salidaStr, out var st) ? st : null;
                if (entry is null && exit is null)
                {
                    rows.Add(new PunchImportPreviewRow { RowNumber = rowNum, EmployeeNumber = empNum, EmployeeName = emp.FullName, PunchDate = fecha.Date, Status = "error", Message = "Sin hora de entrada ni salida" });
                    continue;
                }
                rows.Add(new PunchImportPreviewRow
                {
                    RowNumber = rowNum,
                    EmployeeNumber = empNum,
                    EmployeeName = emp.FullName,
                    PunchDate = fecha.Date,
                    EntryTime = entry,
                    ExitTime = exit,
                    Status = "new",
                    Message = "Listo"
                });
            }
        }

        var summary = new
        {
            total = rows.Count,
            newCount = rows.Count(x => x.Status == "new"),
            errorCount = rows.Count(x => x.Status == "error"),
            skipCount = rows.Count(x => x.Status == "skip"),
            rows
        };
        return Results.Ok(summary);
    }

    // ──────────────────────────────────────────────────────────────
    // 2c. IMPORTAR CHECADAS DESDE CSV
    // Mismas columnas que el importador Excel:
    //   Formato simple: NoEmpleado, FechaHora, Tipo (entry/exit)
    //   Formato matricial: NoEmpleado, Fecha, HoraEntrada, HoraSalida
    // ──────────────────────────────────────────────────────────────
    private static async Task<IResult> ImportPunchesFromCsvAsync(IFormFile file, NanchesoftDbContext db)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { message = "El archivo está vacío." });

        var company = await db.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (company is null)
            return Results.BadRequest(new { message = "No existe empresa configurada." });

        var branchId = await db.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

        var employees = await db.Employees.Where(x => x.CompanyId == company.Id && x.IsActive)
            .ToDictionaryAsync(x => x.EmployeeNumber.ToUpperInvariant());
        var existingPunches = await db.AttendancePunches
            .Where(x => x.CompanyId == company.Id && x.IsActive)
            .Select(x => new { x.EmployeeId, x.PunchDateTime, x.PunchType })
            .ToListAsync();
        var punchKeySet = existingPunches.Select(x => BuildPunchKey(x.EmployeeId, x.PunchDateTime, x.PunchType)).ToHashSet();

        var (headers, dataRows) = await ReadPunchFileAsync(file);
        if (headers.Count == 0)
            return Results.BadRequest(new { message = "El archivo no tiene encabezados válidos." });

        // Crear registro ClockImport primero para linkar los punches
        var clockImport = new ClockImport
        {
            TenantId = company.TenantId, CompanyId = company.Id,
            FileName = file.FileName ?? string.Empty,
            FileSizeBytes = file.Length,
            FileFormat = Path.GetExtension(file.FileName ?? string.Empty).TrimStart('.').ToUpperInvariant(),
            ImportedAt = DateTime.UtcNow, ImportedBy = "web-api",
            RowsRead = dataRows.Count,
            Status = "Processing",
            IsActive = true
        };
        db.ClockImports.Add(clockImport);
        await db.SaveChangesAsync();

        var headerMap = BuildPunchHeaderMap(headers);
        bool isSimpleFormat = headerMap.ContainsKey("fechahora");

        int created = 0, skipped = 0;
        var errors = new List<string>();

        for (int i = 0; i < dataRows.Count; i++)
        {
            int rowNum = i + 2;
            try
            {
                var raw = dataRows[i];
                var empNum = GetCsvField(raw, headerMap, "noempleado").Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(empNum) || !employees.TryGetValue(empNum, out var employee))
                {
                    skipped++;
                    // Punch con error de cruce registrado para diagnóstico
                    if (!string.IsNullOrWhiteSpace(empNum))
                    {
                        db.AttendancePunches.Add(new AttendancePunch
                        {
                            TenantId = company.TenantId, CompanyId = company.Id,
                            EmployeeId = Guid.Empty,
                            WorkDate = DateTime.UtcNow.Date,
                            PunchDateTime = DateTime.UtcNow,
                            PunchType = "unknown",
                            Source = "file-import",
                            Status = "error",
                            ClockImportId = clockImport.Id,
                            RawEmployeeKey = empNum,
                            ReadError = "Empleado no encontrado",
                            IsActive = false,
                            CreatedBy = "file-import"
                        });
                    }
                    continue;
                }

                if (isSimpleFormat)
                {
                    var fechaHoraStr = GetCsvField(raw, headerMap, "fechahora").Trim();
                    var tipo = GetCsvField(raw, headerMap, "tipo").Trim().ToLowerInvariant();
                    if (!DateTime.TryParse(fechaHoraStr, out var punchDt))
                    {
                        errors.Add($"Fila {rowNum}: fecha/hora inválida '{fechaHoraStr}'.");
                        continue;
                    }
                    var punchType = tipo is "exit" or "salida" or "s" ? "exit" : "entry";
                    if (TryQueuePunch(company, branchId, employee.Id, punchDt, punchType, punchKeySet, db, clockImport.Id, empNum))
                        created++;
                    else
                        skipped++;
                }
                else
                {
                    var fechaStr = GetCsvField(raw, headerMap, "fecha").Trim();
                    var entradaStr = GetCsvField(raw, headerMap, "horaentrada").Trim();
                    var salidaStr = GetCsvField(raw, headerMap, "horasalida").Trim();
                    if (!DateTime.TryParse(fechaStr, out var fecha))
                    {
                        errors.Add($"Fila {rowNum}: fecha inválida '{fechaStr}'.");
                        continue;
                    }
                    TimeSpan? entrada = !string.IsNullOrWhiteSpace(entradaStr) && TimeSpan.TryParse(entradaStr, out var et) ? et : null;
                    TimeSpan? salida = !string.IsNullOrWhiteSpace(salidaStr) && TimeSpan.TryParse(salidaStr, out var st) ? st : null;

                    if (entrada.HasValue)
                    {
                        if (TryQueuePunch(company, branchId, employee.Id, fecha.Date.Add(entrada.Value), "entry", punchKeySet, db, clockImport.Id, empNum))
                            created++;
                        else
                            skipped++;
                    }
                    if (salida.HasValue && (!entrada.HasValue || salida.Value != entrada.Value))
                    {
                        if (TryQueuePunch(company, branchId, employee.Id, fecha.Date.Add(salida.Value), "exit", punchKeySet, db, clockImport.Id, empNum))
                            created++;
                        else
                            skipped++;
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Fila {rowNum}: {ex.Message}");
            }
        }

        // Actualizar el registro ClockImport con los resultados finales
        clockImport.RowsCreated = created;
        clockImport.RowsSkipped = skipped;
        clockImport.RowsError = errors.Count;
        clockImport.Status = errors.Count > 0 ? "Error" : "Done";
        clockImport.ErrorSummary = string.Join("; ", errors.Take(5));

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, created, skipped, errors, clockImportId = clockImport.Id });
    }

    private static async Task<IResult> GetClockImportHistoryAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var rows = await db.ClockImports.AsNoTracking()
            .Where(x => tenantId.HasValue && x.TenantId == tenantId.Value
                     && (!companyId.HasValue || x.CompanyId == companyId.Value))
            .OrderByDescending(x => x.ImportedAt)
            .Take(200)
            .Select(x => new
            {
                ClockImportId = x.Id, x.CompanyId,
                x.FileName, x.FileSizeBytes, x.FileFormat,
                x.ImportedAt, x.ImportedBy,
                x.RowsRead, x.RowsCreated, x.RowsSkipped, x.RowsError,
                x.Status, x.ErrorSummary, x.Notes
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> GetClockImportMappingsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var rows = await db.ClockImportMappings.AsNoTracking()
            .Where(x => tenantId.HasValue && x.TenantId == tenantId.Value
                     && (!companyId.HasValue || x.CompanyId == companyId.Value))
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new
            {
                ClockImportMappingId = x.Id, x.TenantId, x.CompanyId,
                x.Code, x.Name, x.DeviceCode,
                x.EmployeeNumberColumn, x.DateTimeColumn,
                x.DateColumn, x.TimeInColumn, x.TimeOutColumn,
                x.PunchTypeColumn, x.DefaultPunchType,
                x.DateFormat, x.TimeFormat, x.Delimiter,
                x.IsDefault, x.Notes, x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateClockImportMappingAsync(HttpContext httpContext, ClockImportMappingRequest request, NanchesoftDbContext db)
    {
        var company = await db.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (company is null)
            return Results.BadRequest(new { message = "No existe empresa configurada." });

        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        var entity = new ClockImportMapping
        {
            TenantId = company.TenantId, CompanyId = company.Id,
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            DeviceCode = request.DeviceCode?.Trim() ?? string.Empty,
            EmployeeNumberColumn = request.EmployeeNumberColumn?.Trim() ?? "NoEmpleado",
            DateTimeColumn = request.DateTimeColumn?.Trim() ?? string.Empty,
            DateColumn = request.DateColumn?.Trim() ?? string.Empty,
            TimeInColumn = request.TimeInColumn?.Trim() ?? string.Empty,
            TimeOutColumn = request.TimeOutColumn?.Trim() ?? string.Empty,
            PunchTypeColumn = request.PunchTypeColumn?.Trim() ?? string.Empty,
            DefaultPunchType = request.DefaultPunchType?.Trim() ?? "entry",
            DateFormat = request.DateFormat?.Trim() ?? "yyyy-MM-dd",
            TimeFormat = request.TimeFormat?.Trim() ?? "HH:mm:ss",
            Delimiter = request.Delimiter?.Trim() ?? ",",
            IsDefault = request.IsDefault,
            Notes = request.Notes?.Trim() ?? string.Empty,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.ClockImportMappings.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateClockImportMappingAsync(Guid id, ClockImportMappingRequest request, NanchesoftDbContext db)
    {
        var entity = await db.ClockImportMappings.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el mapeo." });
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();
        entity.DeviceCode = request.DeviceCode?.Trim() ?? string.Empty;
        entity.EmployeeNumberColumn = request.EmployeeNumberColumn?.Trim() ?? "NoEmpleado";
        entity.DateTimeColumn = request.DateTimeColumn?.Trim() ?? string.Empty;
        entity.DateColumn = request.DateColumn?.Trim() ?? string.Empty;
        entity.TimeInColumn = request.TimeInColumn?.Trim() ?? string.Empty;
        entity.TimeOutColumn = request.TimeOutColumn?.Trim() ?? string.Empty;
        entity.PunchTypeColumn = request.PunchTypeColumn?.Trim() ?? string.Empty;
        entity.DefaultPunchType = request.DefaultPunchType?.Trim() ?? "entry";
        entity.DateFormat = request.DateFormat?.Trim() ?? "yyyy-MM-dd";
        entity.TimeFormat = request.TimeFormat?.Trim() ?? "HH:mm:ss";
        entity.Delimiter = request.Delimiter?.Trim() ?? ",";
        entity.IsDefault = request.IsDefault;
        entity.Notes = request.Notes?.Trim() ?? string.Empty;
        entity.IsActive = request.IsActive;
        entity.UpdatedBy = "web-api";
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteClockImportMappingAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.ClockImportMappings.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el mapeo." });

        db.ClockImportMappings.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<(List<string> headers, List<List<string>> rows)> ReadPunchFileAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
        if (ext is ".csv" or ".txt")
            return await ReadDelimitedFileAsync(file);
        return ReadExcelRows(file);
    }

    private static async Task<(List<string>, List<List<string>>)> ReadDelimitedFileAsync(IFormFile file)
    {
        var parsedRows = new List<List<string>>();
        using var reader = new StreamReader(file.OpenReadStream(), System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        string? line;
        char? delim = null;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            if (line.Length == 0) continue;
            delim ??= DetectCsvDelimiter(line);
            var fields = ParseCsvLine(line, delim.Value);
            if (fields.Any(x => !string.IsNullOrWhiteSpace(x)))
                parsedRows.Add(fields);
        }

        if (parsedRows.Count == 0)
            return ([], []);

        var headerIndex = parsedRows.FindIndex(IsLikelyPunchHeader);
        if (headerIndex < 0) headerIndex = 0;

        var headers = parsedRows[headerIndex].Select(x => x.Trim()).ToList();
        var rows = parsedRows.Skip(headerIndex + 1).ToList();
        return (headers, rows);
    }

    private static bool IsLikelyPunchHeader(List<string> fields)
    {
        var keys = fields.Select(NormalizePunchHeader).ToHashSet();
        var hasEmployee = keys.Contains("noempleado") || keys.Contains("iddepersona") || keys.Contains("numeroempleado") || keys.Contains("empleado");
        var hasDate = keys.Contains("fecha") || keys.Contains("fechahora");
        var hasPunch = keys.Contains("horaentrada") || keys.Contains("horasalida")
            || keys.Contains("primeraperforacion") || keys.Contains("primeraperforacin")
            || keys.Contains("ultimaperforacion") || keys.Contains("ultimaperforacin")
            || keys.Contains("ltimaperforacion") || keys.Contains("ltimaperforacin")
            || keys.Contains("tipo");
        return hasEmployee && hasDate && hasPunch;
    }

    private static (List<string>, List<List<string>>) ReadExcelRows(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheet(1);
        var headers = new List<string>();
        foreach (var cell in ws.Row(1).CellsUsed())
            headers.Add(cell.GetString().Trim());
        var rows = new List<List<string>>();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (int r = 2; r <= lastRow; r++)
        {
            var rowData = new List<string>();
            for (int c = 1; c <= headers.Count; c++)
            {
                var cell = ws.Cell(r, c);
                rowData.Add(cell.IsEmpty() ? string.Empty : cell.GetString());
            }
            rows.Add(rowData);
        }
        return (headers, rows);
    }

    private static char DetectCsvDelimiter(string headerLine)
    {
        int commas = headerLine.Count(c => c == ',');
        int semis = headerLine.Count(c => c == ';');
        int tabs = headerLine.Count(c => c == '\t');
        if (tabs > 0 && tabs >= semis && tabs >= commas) return '\t';
        if (semis > commas) return ';';
        return ',';
    }

    private static List<string> ParseCsvLine(string line, char delim)
    {
        var fields = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == delim) { fields.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
        }
        fields.Add(sb.ToString());
        return fields;
    }

    private static Dictionary<string, int> BuildPunchHeaderMap(List<string> headers)
    {
        var map = new Dictionary<string, int>();
        for (int i = 0; i < headers.Count; i++)
        {
            var key = NormalizePunchHeader(headers[i] ?? string.Empty);
            if (!map.ContainsKey(key)) map[key] = i;
            foreach (var alias in PunchHeaderAliases(key))
                if (!map.ContainsKey(alias)) map[alias] = i;
        }
        return map;
    }

    private static string NormalizePunchHeader(string value)
    {
        var key = value.Trim().ToLowerInvariant()
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u")
            .Replace("ñ", "n");
        return new string(key.Where(char.IsLetterOrDigit).ToArray());
    }

    private static IEnumerable<string> PunchHeaderAliases(string key)
        => key switch
        {
            "iddepersona" or "idpersona" or "numeroempleado" or "claveempleado" or "codigoempleado" => ["noempleado"],
            "primeraperforacion" or "primeraperforacin" or "primermarcaje" or "entrada" => ["horaentrada"],
            "ultimaperforacion" or "ultimaperforacin" or "ltimaperforacion" or "ltimaperforacin" or "ultimomarcaje" or "salida" => ["horasalida"],
            "fechayhora" or "timestamp" => ["fechahora"],
            _ => []
        };

    private static string GetCsvField(List<string> row, Dictionary<string, int> headerMap, string key)
        => headerMap.TryGetValue(key, out var idx) && idx < row.Count ? row[idx] ?? string.Empty : string.Empty;

    private static bool IsPayrollRunEditable(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized)
            || normalized is "draft" or "borrador" or "open" or "abierto" or "captura" or "pending" or "pendiente";
    }

    private sealed record PunchEmployeeRef(Guid Id, string FullName);

    public sealed class PunchImportPreviewRow
    {
        public int RowNumber { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime? PunchDate { get; set; }
        public TimeSpan? EntryTime { get; set; }
        public TimeSpan? ExitTime { get; set; }
        public string PunchType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class PayrollGenerationPreviewDto
    {
        public Guid PayrollPeriodId { get; set; }
        public string PeriodName { get; set; } = string.Empty;
        public int ActiveEmployees { get; set; }
        public int EmployeesWithoutSalary { get; set; }
        public int AttendanceSummaries { get; set; }
        public int Incidents { get; set; }
        public int PrePayrollAdjustments { get; set; }
        public int RecurringMovements { get; set; }
        public int ActiveLoans { get; set; }
        public int TotalEmployeesInCompany { get; set; }
        public int DiscardedInactive { get; set; }
        public int DiscardedByStatus { get; set; }
        public int WithoutDepartment { get; set; }
        public int WithoutPosition { get; set; }
        public List<string> MissingConceptCodes { get; set; } = [];
        public List<string> Warnings { get; set; } = [];
    }

    private static async Task<IResult> GetPayrollGenerationPreviewAsync(Guid periodId, NanchesoftDbContext db)
    {
        var period = await db.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == periodId);
        if (period is null)
            return Results.NotFound(new { message = "No se encontró el periodo." });

        // MVP nómina básica: empleado elegible si IsActive + Status="active" + CompanyId del periodo.
        // No filtrar por DepartmentId/PositionId/WorkScheduleId/PayrollScheduleId (son opcionales).
        var companyEmployees = await db.Employees.AsNoTracking()
            .Where(x => x.CompanyId == period.CompanyId)
            .Select(x => new
            {
                x.Id,
                x.EmployeeNumber,
                x.IsActive,
                x.Status,
                x.DailySalary,
                x.PeriodSalary,
                HasDepartment = x.DepartmentId.HasValue,
                HasPosition = x.PositionId.HasValue
            })
            .ToListAsync();

        // Comparación tolerante: status puede venir "Active", "ACTIVE", "Activo"...
        bool IsActiveStatus(string? s)
            => string.IsNullOrWhiteSpace(s) || s.Equals("active", StringComparison.OrdinalIgnoreCase) || s.Equals("activo", StringComparison.OrdinalIgnoreCase);

        var employeeIds = companyEmployees
            .Where(x => x.IsActive && IsActiveStatus(x.Status))
            .Select(x => x.Id)
            .ToList();

        var activeEmployees = employeeIds.Count;
        var totalInCompany = companyEmployees.Count;
        var discardedInactive = companyEmployees.Count(x => !x.IsActive);
        var discardedStatus = companyEmployees.Count(x => x.IsActive && !IsActiveStatus(x.Status));
        var employeesWithoutSalary = companyEmployees.Count(x => x.IsActive && IsActiveStatus(x.Status) && x.DailySalary <= 0m && x.PeriodSalary <= 0m);
        var employeesWithoutDepartment = companyEmployees.Count(x => x.IsActive && IsActiveStatus(x.Status) && !x.HasDepartment);
        var employeesWithoutPosition = companyEmployees.Count(x => x.IsActive && IsActiveStatus(x.Status) && !x.HasPosition);
        var attendanceSummaries = await db.AttendanceDailySummaries.AsNoTracking()
            .CountAsync(x => x.CompanyId == period.CompanyId && x.PayrollPeriodId == period.Id && x.IsActive);
        var incidents = await db.EmployeeIncidents.AsNoTracking()
            .CountAsync(x => x.CompanyId == period.CompanyId && x.PayrollPeriodId == period.Id && x.IsActive);
        var prePayrollAdjustments = await db.PrePayrollAdjustments.AsNoTracking()
            .CountAsync(x => x.CompanyId == period.CompanyId && x.PayrollPeriodId == period.Id && x.IsActive && x.Status != "cancelled");
        var activeLoans = employeeIds.Count == 0
            ? 0
            : await db.EmployeeLoans.AsNoTracking()
                .CountAsync(x => x.CompanyId == period.CompanyId && employeeIds.Contains(x.EmployeeId) && x.Status == "active" && x.IsActive && x.BalanceAmount > 0m);
        var recurringMovements = employeeIds.Count == 0
            ? 0
            : await db.PayrollRecurringMovements.AsNoTracking()
                .CountAsync(x => x.CompanyId == period.CompanyId && x.IsActive && employeeIds.Contains(x.EmployeeId)
                    && x.EffectiveStartDate.Date <= period.PaymentDate.Date
                    && (x.EffectiveEndDate == null || x.EffectiveEndDate.Value.Date >= period.PaymentDate.Date));

        var requiredConcepts = new[] { "SAL", "ISR", "IMSS", "DESCTO", "SUBSE" };
        var existingConceptCodes = await db.PayrollConcepts.AsNoTracking()
            .Where(x => x.CompanyId == period.CompanyId && x.IsActive && requiredConcepts.Contains(x.Code))
            .Select(x => x.Code)
            .ToListAsync();
        var missingConcepts = requiredConcepts.Except(existingConceptCodes).ToList();

        var warnings = new List<string>();
        if (totalInCompany == 0)
            warnings.Add($"La empresa del periodo no tiene colaboradores registrados. Importa empleados con la empresa actual seleccionada.");
        else if (activeEmployees == 0)
            warnings.Add($"Hay {totalInCompany} colaborador(es) en la empresa, pero ninguno elegible: {discardedInactive} inactivos, {discardedStatus} con status distinto a 'active'.");
        if (employeesWithoutSalary > 0)
            warnings.Add($"{employeesWithoutSalary} colaborador(es) elegibles sin sueldo del periodo ni sueldo diario — quedarán en 0 al calcular.");
        if (employeesWithoutDepartment > 0)
            warnings.Add($"{employeesWithoutDepartment} colaborador(es) sin departamento — pueden procesarse igual, sólo informativo.");
        if (employeesWithoutPosition > 0)
            warnings.Add($"{employeesWithoutPosition} colaborador(es) sin puesto — pueden procesarse igual, sólo informativo.");
        if (attendanceSummaries == 0)
            warnings.Add("No hay resumen diario de asistencia para el periodo (es opcional; sin esto se procesa con sueldo completo).");
        if (missingConcepts.Count > 0)
            warnings.Add("Faltan conceptos base; se crearán automáticamente al calcular: " + string.Join(", ", missingConcepts) + ".");

        return Results.Ok(new PayrollGenerationPreviewDto
        {
            PayrollPeriodId = period.Id,
            PeriodName = period.Name,
            ActiveEmployees = activeEmployees,
            EmployeesWithoutSalary = employeesWithoutSalary,
            AttendanceSummaries = attendanceSummaries,
            Incidents = incidents,
            PrePayrollAdjustments = prePayrollAdjustments,
            RecurringMovements = recurringMovements,
            ActiveLoans = activeLoans,
            TotalEmployeesInCompany = totalInCompany,
            DiscardedInactive = discardedInactive,
            DiscardedByStatus = discardedStatus,
            WithoutDepartment = employeesWithoutDepartment,
            WithoutPosition = employeesWithoutPosition,
            MissingConceptCodes = missingConcepts,
            Warnings = warnings
        });
    }

    // ──────────────────────────────────────────────────────────────
    // 3. GENERAR RESUMEN DIARIO DE ASISTENCIA POR PERIODO
    // Procesa todos los empleados activos y agrupa sus checadas
    // Hora entrada programada por defecto: 09:00, salida: 18:00
    // ──────────────────────────────────────────────────────────────
    private static async Task<IResult> GenerateAttendanceDailySummariesAsync(
        Guid periodId,
        int? defaultToleranceMinutes,
        int? earlyLeaveToleranceMinutes,
        decimal? halfAbsenceUnderHours,
        decimal? fullAbsenceUnderHours,
        NanchesoftDbContext db)
    {
        var period = await db.PayrollPeriods.FirstOrDefaultAsync(x => x.Id == periodId);
        if (period is null)
            return Results.NotFound(new { message = "No se encontró el periodo." });

        var company = await db.Companies.Where(x => x.Id == period.CompanyId).FirstOrDefaultAsync();
        if (company is null)
            return Results.BadRequest(new { message = "No existe empresa para el periodo." });

        var branchId = await db.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

        var employees = await db.Employees
            .Include(x => x.WorkSchedule)
            .Where(x => x.CompanyId == company.Id && x.IsActive && x.Status == "active")
            .ToListAsync();

        var startDate = period.StartDate.Date;
        var endDate = period.EndDate.Date;

        var employeeIds = employees.Select(x => x.Id).ToList();

        var allPunches = await db.AttendancePunches
            .Where(x => x.CompanyId == company.Id && employeeIds.Contains(x.EmployeeId)
                && x.WorkDate >= startDate && x.WorkDate <= endDate && x.IsActive)
            .ToListAsync();

        var punchesByEmpDate = allPunches
            .GroupBy(x => (x.EmployeeId, x.WorkDate.Date))
            .ToDictionary(g => g.Key, g => g.ToList());

        var existing = await db.AttendanceDailySummaries
            .Where(x => x.CompanyId == company.Id && x.PayrollPeriodId == periodId)
            .ToListAsync();
        db.AttendanceDailySummaries.RemoveRange(existing);

        var defaultEntry = new TimeSpan(9, 0, 0);
        var defaultExit = new TimeSpan(18, 0, 0);
        var fallbackToleranceMinutes = Math.Clamp(defaultToleranceMinutes ?? 5, 0, 240);
        var exitToleranceMinutes = Math.Clamp(earlyLeaveToleranceMinutes ?? 5, 0, 240);
        var fullAbsenceLimit = Math.Clamp(fullAbsenceUnderHours ?? 4m, 0m, 24m);
        var halfAbsenceLimit = Math.Clamp(halfAbsenceUnderHours ?? 6m, fullAbsenceLimit, 24m);
        int created = 0;

        foreach (var employee in employees)
        {
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                punchesByEmpDate.TryGetValue((employee.Id, date), out var dayPunches);

                // Resolve scheduled times from WorkSchedule or defaults
                TimeSpan entryTs, exitTs;
                int toleranceMin;
                bool isRestDay;

                if (employee.WorkSchedule is { } sched)
                {
                    var (isWork, e, x2, tol) = GetScheduleForDay(sched, date.DayOfWeek);
                    isRestDay = !isWork;
                    entryTs = e;
                    exitTs = x2;
                    toleranceMin = tol > 0 ? tol : fallbackToleranceMinutes;
                }
                else
                {
                    isRestDay = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                    entryTs = defaultEntry;
                    exitTs = defaultExit;
                    toleranceMin = fallbackToleranceMinutes;
                }

                // Skip rest days with no punches — they are not absences
                if (isRestDay && dayPunches is null) continue;

                var scheduledEntry = date.Add(entryTs);
                var scheduledExit = date.Add(exitTs);

                var entryPunch = dayPunches?.Where(x => x.PunchType == "entry").OrderBy(x => x.PunchDateTime).FirstOrDefault()
                    ?? dayPunches?.OrderBy(x => x.PunchDateTime).FirstOrDefault();
                var exitPunch = dayPunches?.Where(x => x.PunchType == "exit").OrderByDescending(x => x.PunchDateTime).FirstOrDefault()
                    ?? (dayPunches?.Count > 1 ? dayPunches.OrderByDescending(x => x.PunchDateTime).First() : null);

                decimal workedHours = 0m;
                int delayMinutes = 0;
                int earlyLeaveMinutes = 0;
                decimal overtimeHours = 0m;
                decimal absenceUnits = 0m;

                if (entryPunch is not null)
                {
                    var actualEntry = entryPunch.PunchDateTime;
                    var actualExit = exitPunch?.PunchDateTime ?? actualEntry;

                    if (exitPunch is not null && exitPunch.PunchDateTime > actualEntry)
                    {
                        var totalHours = (exitPunch.PunchDateTime - actualEntry).TotalHours;
                        workedHours = (decimal)totalHours;
                        var scheduledHours = (scheduledExit - scheduledEntry).TotalHours;
                        absenceUnits = workedHours < fullAbsenceLimit ? 1m : workedHours < halfAbsenceLimit ? 0.5m : 0m;
                        overtimeHours = workedHours > (decimal)scheduledHours ? Math.Round(workedHours - (decimal)scheduledHours, 2) : 0m;

                        var delayTs = actualEntry - scheduledEntry;
                        if (delayTs.TotalMinutes > toleranceMin) delayMinutes = (int)delayTs.TotalMinutes;

                        var earlyTs = scheduledExit - actualExit;
                        if (earlyTs.TotalMinutes > exitToleranceMinutes && actualExit < scheduledExit) earlyLeaveMinutes = (int)earlyTs.TotalMinutes;
                    }
                    else
                    {
                        absenceUnits = 0.5m;
                    }
                }
                else
                {
                    absenceUnits = isRestDay ? 0m : 1m;
                }

                db.AttendanceDailySummaries.Add(new AttendanceDailySummary
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    BranchId = branchId,
                    EmployeeId = employee.Id,
                    PayrollPeriodId = periodId,
                    WorkDate = date,
                    ScheduledEntryTime = scheduledEntry,
                    ScheduledExitTime = scheduledExit,
                    FirstPunchDateTime = entryPunch?.PunchDateTime,
                    LastPunchDateTime = exitPunch?.PunchDateTime,
                    WorkedHours = Math.Round(workedHours, 2),
                    DelayMinutes = delayMinutes,
                    EarlyLeaveMinutes = earlyLeaveMinutes,
                    OvertimeHours = Math.Round(overtimeHours, 2),
                    AbsenceUnits = absenceUnits,
                    DayType = isRestDay ? "restday" : "workday",
                    Status = "calculated",
                    Source = dayPunches is not null ? "time-clock" : "no-punch",
                    Notes = dayPunches is null && !isRestDay ? "Sin checadas" : string.Empty,
                    IsActive = true,
                    CreatedBy = "mvp-engine"
                });
                created++;
            }
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, created, employees = employees.Count, days = (endDate - startDate).Days + 1 });
    }

    // ──────────────────────────────────────────────────────────────
    // 4. GENERAR INCIDENCIAS AUTOMÁTICAS DESDE RESUMEN DIARIO
    // Genera: falta, retardo, hora_extra
    // ──────────────────────────────────────────────────────────────
    private static async Task<IResult> GenerateIncidentsFromSummariesAsync(Guid periodId, int? delayIncidentThresholdMinutes, decimal? overtimeMultiplier, NanchesoftDbContext db)
    {
        var period = await db.PayrollPeriods.FirstOrDefaultAsync(x => x.Id == periodId);
        if (period is null)
            return Results.NotFound(new { message = "No se encontró el periodo." });

        var summaries = await db.AttendanceDailySummaries
            .Where(x => x.PayrollPeriodId == periodId && x.IsActive)
            .ToListAsync();

        if (summaries.Count == 0)
            return Results.BadRequest(new { message = "No hay resúmenes de asistencia. Genera primero el resumen diario." });

        var autoIncidents = await db.EmployeeIncidents
            .Where(x => x.PayrollPeriodId == periodId && x.Origin == "clock" && !x.ManuallyEdited)
            .ToListAsync();
        foreach (var incident in autoIncidents)
        {
            incident.IsActive = false;
            incident.IsDeleted = true;
            incident.DeletedAt = DateTime.UtcNow;
            incident.DeletedBy = "mvp-engine";
        }

        // Importación más reciente cuyos punches cubren este periodo (para trazabilidad)
        var latestImportId = await db.AttendancePunches
            .Where(x => x.CompanyId == period.CompanyId && x.ClockImportId != null
                     && x.WorkDate >= period.StartDate && x.WorkDate <= period.EndDate)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.ClockImportId)
            .FirstOrDefaultAsync();

        int created = 0;
        var delayThreshold = Math.Clamp(delayIncidentThresholdMinutes ?? 15, 0, 240);
        var overtimeRate = Math.Clamp(overtimeMultiplier ?? 1.5m, 1m, 5m);
        var incidentTypes = await db.NomPayrollIncidentTypes
            .Where(x => x.CompanyId == period.CompanyId && x.IsActive && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Code);
        var grouped = summaries
            .GroupBy(x => x.EmployeeId)
            .ToList();

        foreach (var empGroup in grouped)
        {
            var empId = empGroup.Key;
            var employee = await db.Employees.FindAsync(empId);
            if (employee is null) continue;

            var absenceSum = empGroup.Sum(x => x.AbsenceUnits);
            var delaySum = empGroup.Sum(x => x.DelayMinutes);
            var overtimeSum = empGroup.Sum(x => x.OvertimeHours);

            if (absenceSum > 0m)
            {
                db.EmployeeIncidents.Add(new EmployeeIncident
                {
                    TenantId = period.TenantId,
                    CompanyId = period.CompanyId,
                    BranchId = employee.BranchId,
                    EmployeeId = empId,
                    PayrollPeriodId = periodId,
                    NomPayrollIncidentTypeId = ResolveIncidentTypeId(incidentTypes, "FINJ"),
                    IncidentDate = period.StartDate,
                    IncidentType = "FINJ",
                    Quantity = absenceSum,
                    Amount = employee.DailySalary * absenceSum,
                    Status = "draft",
                    Origin = "clock",
                    ClockImportId = latestImportId,
                    IsActive = true,
                    CreatedBy = "mvp-engine"
                });
                created++;
            }

            if (delaySum > delayThreshold)
            {
                var delayHours = delaySum / 60m;
                db.EmployeeIncidents.Add(new EmployeeIncident
                {
                    TenantId = period.TenantId,
                    CompanyId = period.CompanyId,
                    BranchId = employee.BranchId,
                    EmployeeId = empId,
                    PayrollPeriodId = periodId,
                    NomPayrollIncidentTypeId = ResolveIncidentTypeId(incidentTypes, "RET"),
                    IncidentDate = period.StartDate,
                    IncidentType = "RET",
                    Quantity = Math.Round(delayHours, 2),
                    Amount = Math.Round(employee.DailySalary / 8m * delayHours, 2),
                    Status = "draft",
                    Origin = "clock",
                    ClockImportId = latestImportId,
                    IsActive = true,
                    CreatedBy = "mvp-engine"
                });
                created++;
            }

            if (overtimeSum > 0m)
            {
                db.EmployeeIncidents.Add(new EmployeeIncident
                {
                    TenantId = period.TenantId,
                    CompanyId = period.CompanyId,
                    BranchId = employee.BranchId,
                    EmployeeId = empId,
                    PayrollPeriodId = periodId,
                    NomPayrollIncidentTypeId = ResolveIncidentTypeId(incidentTypes, "HE2"),
                    IncidentDate = period.StartDate,
                    IncidentType = "HE2",
                    Quantity = Math.Round(overtimeSum, 2),
                    Amount = Math.Round(employee.DailySalary / 8m * overtimeRate * overtimeSum, 2),
                    Status = "draft",
                    Origin = "clock",
                    ClockImportId = latestImportId,
                    IsActive = true,
                    CreatedBy = "mvp-engine"
                });
                created++;
            }
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, created, employees = grouped.Count });
    }

    // ──────────────────────────────────────────────────────────────
    // 4b. GENERAR INCIDENCIAS DESDE POLÍTICA DE ASISTENCIA (Fase 6)
    // Convierte AttendanceDailySummary → EmployeeIncident usando
    // las reglas de AttendancePolicy configuradas por el usuario.
    // ──────────────────────────────────────────────────────────────
    private static async Task<IResult> GenerateIncidentsFromPolicyAsync(Guid periodId, Guid? policyId, NanchesoftDbContext db)
    {
        var period = await db.PayrollPeriods.FirstOrDefaultAsync(x => x.Id == periodId);
        if (period is null)
            return Results.NotFound(new { message = "No se encontró el periodo." });

        // Resolve policy: explicit policyId → default for company → error
        AttendancePolicy? policy;
        if (policyId.HasValue && policyId.Value != Guid.Empty)
        {
            policy = await db.AttendancePolicies
                .Include(x => x.Rules)
                .FirstOrDefaultAsync(x => x.Id == policyId.Value && !x.IsDeleted && x.IsActive);
        }
        else
        {
            policy = await db.AttendancePolicies
                .Include(x => x.Rules)
                .FirstOrDefaultAsync(x => x.CompanyId == period.CompanyId && x.IsDefault && !x.IsDeleted && x.IsActive);
        }

        if (policy is null)
            return Results.BadRequest(new { message = "No se encontró política de asistencia activa. Configura una política default o proporciona policyId." });

        var rules = policy.Rules.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToList();
        if (rules.Count == 0)
            return Results.BadRequest(new { message = "La política no tiene reglas activas." });

        var summaries = await db.AttendanceDailySummaries
            .Where(x => x.PayrollPeriodId == periodId && x.IsActive)
            .ToListAsync();

        if (summaries.Count == 0)
            return Results.BadRequest(new { message = "No hay resúmenes de asistencia. Genera primero el resumen diario." });

        // Remove previously policy-generated incidents for this period
        var prevIncidents = await db.EmployeeIncidents
            .Where(x => x.PayrollPeriodId == periodId && x.Notes == "policy-auto")
            .ToListAsync();
        foreach (var inc in prevIncidents)
        {
            inc.IsActive = false;
            inc.IsDeleted = true;
            inc.DeletedAt = DateTime.UtcNow;
            inc.DeletedBy = "policy-engine";
        }

        var incidentTypes = await db.NomPayrollIncidentTypes
            .Where(x => x.CompanyId == period.CompanyId && x.IsActive && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Code);

        int created = 0;
        var grouped = summaries.GroupBy(x => x.EmployeeId).ToList();

        foreach (var empGroup in grouped)
        {
            var empId = empGroup.Key;
            var employee = await db.Employees.FindAsync(empId);
            if (employee is null) continue;

            // Aggregate summaries for this employee
            var totalAbsenceDays = empGroup.Sum(x => x.AbsenceUnits);
            var totalDelayMinutes = empGroup.Sum(x => x.DelayMinutes);
            var totalEarlyLeaveMinutes = empGroup.Sum(x => x.EarlyLeaveMinutes);
            var totalOvertimeHours = empGroup.Sum(x => x.OvertimeHours);
            var missingPunchDays = empGroup.Count(x => x.FirstPunchDateTime is null && x.WorkDate.DayOfWeek != DayOfWeek.Saturday && x.WorkDate.DayOfWeek != DayOfWeek.Sunday);

            foreach (var rule in rules)
            {
                if (rule.ActionType != "CreateIncident") continue;
                if (string.IsNullOrWhiteSpace(rule.IncidentTypeCode)) continue;
                if (!incidentTypes.TryGetValue(rule.IncidentTypeCode, out var incType)) continue;

                decimal metricValue = rule.RuleType switch
                {
                    "Absence" => (decimal)totalAbsenceDays,
                    "Lateness" => totalDelayMinutes,
                    "EarlyLeave" => totalEarlyLeaveMinutes,
                    "Overtime" => (decimal)(totalOvertimeHours * 60),
                    "MissingPunch" => missingPunchDays,
                    _ => 0m
                };

                decimal threshold = rule.ThresholdMinutes.HasValue
                    ? rule.ThresholdMinutes.Value
                    : (rule.ThresholdDays.HasValue ? rule.ThresholdDays.Value * (rule.RuleType == "Absence" ? 1m : 480m) : 0m);

                bool conditionMet = rule.ConditionType switch
                {
                    "GreaterThan" => metricValue > threshold,
                    "GreaterThanOrEqual" => metricValue >= threshold,
                    "Equal" => metricValue == threshold,
                    "Always" => metricValue > 0m,
                    _ => metricValue > threshold
                };

                if (!conditionMet) continue;

                // Compute quantity: for time-based rules use hours, for day-based use days
                decimal quantity = rule.RuleType switch
                {
                    "Lateness" or "EarlyLeave" => Math.Round(metricValue / 60m, 2),
                    "Overtime" => Math.Round(totalOvertimeHours, 2),
                    "MissingPunch" => missingPunchDays,
                    _ => totalAbsenceDays
                };

                decimal amount = rule.ActionValue > 0m
                    ? rule.ActionValue * quantity
                    : employee.DailySalary * quantity;

                db.EmployeeIncidents.Add(new EmployeeIncident
                {
                    TenantId = period.TenantId,
                    CompanyId = period.CompanyId,
                    BranchId = employee.BranchId,
                    EmployeeId = empId,
                    PayrollPeriodId = periodId,
                    NomPayrollIncidentTypeId = incType.Id,
                    IncidentDate = period.StartDate,
                    IncidentType = rule.IncidentTypeCode,
                    Quantity = quantity,
                    Amount = Math.Round(amount, 2),
                    Status = "draft",
                    Notes = "policy-auto",
                    IsActive = true,
                    CreatedBy = "policy-engine"
                });
                created++;
            }
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, created, employees = grouped.Count, policy = policy.Name });
    }

    // ── Fase 8: Prenómina operativa ─────────────────────────────────────────

    private static async Task<IResult> GetOperationalPrePayrollAsync(Guid periodId, NanchesoftDbContext db)
    {
        var period = await db.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == periodId);
        if (period is null)
            return Results.NotFound(new { message = "No se encontró el periodo." });

        // Active employees for this company
        var employees = await db.Employees.AsNoTracking()
            .Where(x => x.CompanyId == period.CompanyId && x.IsActive)
            .Select(x => new { x.Id, x.EmployeeNumber, x.PeriodSalary, x.DailySalary,
                               FullName = (x.FirstName + " " + x.LastName).Trim() })
            .ToListAsync();
        var empIds = employees.Select(e => e.Id).ToList();

        // Attendance summaries aggregated per employee
        var attendanceRaw = await db.AttendanceDailySummaries.AsNoTracking()
            .Where(x => x.PayrollPeriodId == periodId && x.IsActive && empIds.Contains(x.EmployeeId))
            .GroupBy(x => x.EmployeeId)
            .Select(g => new
            {
                EmployeeId = g.Key,
                WorkedHours = g.Sum(x => x.WorkedHours),
                DelayMinutes = g.Sum(x => x.DelayMinutes),
                OvertimeHours = g.Sum(x => x.OvertimeHours),
                AbsenceUnits = g.Sum(x => x.AbsenceUnits),
                Days = g.Count()
            })
            .ToDictionaryAsync(x => x.EmployeeId);

        // Incidents aggregated per employee
        var incidentTypes = await db.NomPayrollIncidentTypes.AsNoTracking()
            .Where(x => x.CompanyId == period.CompanyId && x.IsActive && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id);

        var incidents = await db.EmployeeIncidents.AsNoTracking()
            .Where(x => x.PayrollPeriodId == periodId && !x.IsDeleted && x.IsActive && empIds.Contains(x.EmployeeId))
            .Select(x => new { x.EmployeeId, x.PayrollIncidentTypeId, x.Quantity, x.Amount })
            .ToListAsync();

        var incByEmp = incidents
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // PrePayrollAdjustments
        var adjustments = await db.PrePayrollAdjustments.AsNoTracking()
            .Where(x => x.PayrollPeriodId == periodId && x.IsActive && x.Status != "cancelled" && empIds.Contains(x.EmployeeId))
            .Select(x => new { x.EmployeeId, x.Amount, x.AdjustmentType })
            .ToListAsync();
        var adjByEmp = adjustments.GroupBy(x => x.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        var rows = employees.Select(emp =>
        {
            var att = attendanceRaw.GetValueOrDefault(emp.Id);
            var incs = incByEmp.GetValueOrDefault(emp.Id) ?? [];
            var adjs = adjByEmp.GetValueOrDefault(emp.Id) ?? [];

            decimal incPerceptions = 0m, incDeductions = 0m;
            foreach (var inc in incs)
            {
                if (!incidentTypes.TryGetValue(inc.PayrollIncidentTypeId, out var itype)) continue;
                var cat = itype.IncidentCategory;
                var aff = itype.AffectType;
                if (cat is "AUSENCIA" or "DEDUCCION" || aff is "RESTA") incDeductions += inc.Amount > 0 ? inc.Amount : 0;
                else incPerceptions += inc.Amount > 0 ? inc.Amount : 0;
            }
            decimal adjPerceptions = adjs.Where(a => a.AdjustmentType != "deduction").Sum(a => a.Amount);
            decimal adjDeductions  = adjs.Where(a => a.AdjustmentType == "deduction").Sum(a => a.Amount);

            return new OperationalPrePayrollEmployeeRow
            {
                EmployeeId = emp.Id,
                EmployeeNumber = emp.EmployeeNumber ?? string.Empty,
                EmployeeName = emp.FullName,
                PeriodSalary = emp.PeriodSalary,
                DailySalary = emp.DailySalary,
                WorkedHours = att?.WorkedHours ?? 0,
                DelayMinutes = att?.DelayMinutes ?? 0,
                OvertimeHours = att?.OvertimeHours ?? 0,
                AbsenceUnits = att?.AbsenceUnits ?? 0,
                AttendanceDays = att?.Days ?? 0,
                IncidentCount = incs.Count,
                IncidentPerceptionsTotal = Math.Round(incPerceptions, 2),
                IncidentDeductionsTotal = Math.Round(incDeductions, 2),
                AdjustmentCount = adjs.Count,
                AdjustmentPerceptionsTotal = Math.Round(adjPerceptions, 2),
                AdjustmentDeductionsTotal = Math.Round(adjDeductions, 2)
            };
        }).OrderBy(r => r.EmployeeNumber).ToList();

        return Results.Ok(new OperationalPrePayrollSummary
        {
            PayrollPeriodId = period.Id,
            PeriodName = period.Name,
            IsClosed = period.IsClosed,
            Rows = rows
        });
    }

    private static async Task<IResult> GenerateOperationalPrePayrollAsync(Guid periodId, NanchesoftDbContext db)
    {
        var period = await db.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == periodId);
        if (period is null)
            return Results.NotFound(new { message = "No se encontró el periodo." });
        if (period.IsClosed)
            return Results.BadRequest(new { message = "El periodo está cerrado." });

        // Load incidents with concept mapping
        var incidents = await db.EmployeeIncidents
            .Where(x => x.PayrollPeriodId == periodId && !x.IsDeleted && x.IsActive)
            .ToListAsync();

        var typeIds = incidents.Select(x => x.PayrollIncidentTypeId).Distinct().ToList();
        var incidentTypes = await db.NomPayrollIncidentTypes
            .Where(x => typeIds.Contains(x.Id) && x.PayrollConceptId.HasValue)
            .ToDictionaryAsync(x => x.Id);

        if (incidentTypes.Count == 0)
            return Results.Ok(new { success = true, created = 0, message = "Ningún tipo de incidencia tiene concepto de nómina asignado. Configura PayrollConceptId en los tipos de incidencia." });

        // Delete previous operational-auto adjustments for this period
        var prev = await db.PrePayrollAdjustments
            .Where(x => x.PayrollPeriodId == periodId && x.CaptureSource == "operational-auto")
            .ToListAsync();
        db.PrePayrollAdjustments.RemoveRange(prev);

        int created = 0;
        foreach (var inc in incidents)
        {
            if (!incidentTypes.TryGetValue(inc.PayrollIncidentTypeId, out var itype)) continue;
            var concept = await db.PayrollConcepts.FirstOrDefaultAsync(c => c.Id == itype.PayrollConceptId!.Value);
            if (concept is null) continue;

            var isDeduction = concept.ConceptType == "deduction" || itype.IncidentCategory is "DEDUCCION" or "AUSENCIA";
            var amount = inc.Amount > 0 ? inc.Amount : 0m;
            if (amount <= 0) continue;

            db.PrePayrollAdjustments.Add(new PrePayrollAdjustment
            {
                TenantId = period.TenantId,
                CompanyId = period.CompanyId,
                EmployeeId = inc.EmployeeId,
                PayrollPeriodId = periodId,
                PayrollConceptId = concept.Id,
                AdjustmentCode = $"OP-{itype.Code}",
                AdjustmentName = itype.Name,
                AdjustmentType = isDeduction ? "deduction" : "perception",
                CaptureSource = "operational-auto",
                ReferenceDate = inc.IncidentDate,
                Quantity = inc.Quantity <= 0 ? 1m : inc.Quantity,
                Amount = Math.Round(amount, 2),
                TaxableAmount = isDeduction ? 0m : Math.Round(amount, 2),
                ExemptAmount = 0m,
                Status = "captured",
                Notes = $"Generado de incidencia {inc.Id:D}",
                IsActive = true,
                CreatedBy = "operational-prepayroll"
            });
            created++;
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, created, deleted = prev.Count });
    }

    // ──────────────────────────────────────────────────────────────
    // 5. CALCULAR CORRIDA DE NÓMINA
    // Fórmula: gross = daily_salary * days_paid + horas_extra + bonos
    //          deductions = faltas + retardos + préstamos + deducciones
    //          net = gross - deductions
    // ──────────────────────────────────────────────────────────────
    private static async Task<IResult> CalculatePayrollRunAsync(Guid runId, NanchesoftDbContext db)
    {
        var run = await db.PayrollRuns.Include(x => x.PayrollPeriod).FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null)
            return Results.NotFound(new { message = "No se encontró la corrida de nómina." });

        if (!IsPayrollRunEditable(run.Status))
            return Results.BadRequest(new { message = "La corrida ya fue calculada/autorizada/cerrada. Genera una nueva corrida o cancélala formalmente para hacer ajustes." });

        var period = run.PayrollPeriod!;
        int periodDays = (period.EndDate.Date - period.StartDate.Date).Days + 1;

        // MVP: empleado elegible si IsActive y status "active"/"activo"/vacío (case-insensitive).
        // No exigir DepartmentId/PositionId/WorkScheduleId/PayrollScheduleId.
        var employees = await db.Employees
            .Where(x => x.CompanyId == run.CompanyId && x.IsActive
                && (x.Status == null
                    || x.Status == ""
                    || EF.Functions.ILike(x.Status, "active")
                    || EF.Functions.ILike(x.Status, "activo")))
            .ToListAsync();

        if (employees.Count == 0)
        {
            var totalInCompany = await db.Employees.CountAsync(x => x.CompanyId == run.CompanyId);
            return Results.BadRequest(new
            {
                message = totalInCompany == 0
                    ? "La empresa del periodo no tiene empleados. Importa colaboradores con la empresa actual seleccionada."
                    : $"Hay {totalInCompany} empleado(s) en la empresa pero ninguno elegible (IsActive=true y status 'active'). Revisa los colaboradores."
            });
        }

        await EnsureDefaultConceptsAsync(db, run.CompanyId);

        var concepts = await db.PayrollConcepts.Where(x => x.CompanyId == run.CompanyId && x.IsActive).ToListAsync();
        var conceptSal   = concepts.FirstOrDefault(IsWeeklySalaryConcept)
                        ?? concepts.FirstOrDefault(x => x.Code == "SAL")
                        ?? concepts.FirstOrDefault(x => x.SatCode == "P-001" && x.ConceptType == "perception")
                        ?? concepts.FirstOrDefault(x => x.ConceptType == "perception");
        var conceptBon   = concepts.FirstOrDefault(x => x.Code == "BON")
                        ?? concepts.FirstOrDefault(x => x.ConceptType == "perception" && x.Code != (conceptSal?.Code ?? ""));
        var conceptSubse  = concepts.FirstOrDefault(x => x.Code == "SUBSE")
                         ?? concepts.FirstOrDefault(x => x.SatCode == "P-017");
        var conceptImss   = concepts.FirstOrDefault(x => x.Code == "IMSS")
                         ?? concepts.FirstOrDefault(x => x.SatCode == "D-001");
        var conceptIsr    = concepts.FirstOrDefault(x => x.Code == "ISR")
                         ?? concepts.FirstOrDefault(x => x.SatCode == "D-002")
                         ?? concepts.FirstOrDefault(x => x.ConceptType == "deduction");
        var conceptDescto = concepts.FirstOrDefault(x => x.Code == "DESCTO")
                         ?? concepts.FirstOrDefault(x => x.ConceptType == "deduction"
                             && x.Code != "PRESTA"
                             && x.Code != "PRES")
                         ?? conceptIsr;
        var conceptLoan = concepts.FirstOrDefault(x => x.Code == "PRESTA")
                       ?? concepts.FirstOrDefault(x => x.Code == "PRES")
                       ?? concepts.FirstOrDefault(x => x.Code == "DESCTO");

        if (conceptSal is null)
            return Results.BadRequest(new { message = "No existe concepto de percepción (SAL) configurado." });

        // Step 1: Delete auto-generated details — safe, no external FK references point to details
        var oldDetails = await db.PayrollRunLineDetails
            .Where(x => x.PayrollRunId == runId && x.IsGenerated)
            .ToListAsync();
        db.PayrollRunLineDetails.RemoveRange(oldDetails);

        // Step 2: Reverse and delete loan deductions from any previous run of this same corrida
        var oldLoanDeductions = await db.EmployeeLoanDeductions
            .Where(x => x.PayrollRunId == runId)
            .ToListAsync();
        if (oldLoanDeductions.Count > 0)
        {
            var affectedLoanIds = oldLoanDeductions.Select(x => x.EmployeeLoanId).Distinct().ToList();
            var affectedLoans = await db.EmployeeLoans.Where(x => affectedLoanIds.Contains(x.Id)).ToListAsync();
            var loansById = affectedLoans.ToDictionary(x => x.Id);
            foreach (var ld in oldLoanDeductions)
            {
                if (loansById.TryGetValue(ld.EmployeeLoanId, out var loanToReverse))
                {
                    loanToReverse.BalanceAmount += ld.Amount;
                    loanToReverse.InstallmentsPaid = Math.Max(0, loanToReverse.InstallmentsPaid - 1);
                    if (loanToReverse.Status == "paid" && loanToReverse.BalanceAmount > 0)
                        loanToReverse.Status = "active";
                    loanToReverse.UpdatedAt = DateTime.UtcNow;
                    loanToReverse.UpdatedBy = "mvp-recalculate";
                }
            }
            db.EmployeeLoanDeductions.RemoveRange(oldLoanDeductions);
        }

        await db.SaveChangesAsync();

        // Step 3: Load existing lines for upsert — we UPDATE amounts in-place to preserve FK references
        // (e.g., payroll_dispersion_lines references payroll_run_lines by ID)
        var existingLines = await db.PayrollRunLines
            .Where(x => x.PayrollRunId == runId)
            .ToDictionaryAsync(x => x.EmployeeId);

        var employeeIds = employees.Select(x => x.Id).ToList();

        var summaryByEmployee = await db.AttendanceDailySummaries
            .Where(x => x.CompanyId == run.CompanyId && x.PayrollPeriodId == run.PayrollPeriodId && employeeIds.Contains(x.EmployeeId) && x.IsActive)
            .GroupBy(x => x.EmployeeId)
            .Select(g => new
            {
                EmployeeId = g.Key,
                TotalAbsenceUnits = g.Sum(x => x.AbsenceUnits),
                TotalOvertimeHours = g.Sum(x => x.OvertimeHours)
            })
            .ToDictionaryAsync(x => x.EmployeeId);

        var incidentRows = await db.EmployeeIncidents
            .Include(x => x.PayrollIncidentType)
            .Where(x => x.CompanyId == run.CompanyId && x.PayrollPeriodId == run.PayrollPeriodId && employeeIds.Contains(x.EmployeeId) && x.IsActive && !x.IsDeleted)
            .ToListAsync();
        var incidentsByEmployee = incidentRows.GroupBy(x => x.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        var prePayrollAdjustments = await db.PrePayrollAdjustments
            .Where(x => x.CompanyId == run.CompanyId && x.PayrollPeriodId == run.PayrollPeriodId && employeeIds.Contains(x.EmployeeId) && x.IsActive && x.Status != "cancelled")
            .ToListAsync();
        var prePayrollByEmployee = prePayrollAdjustments.GroupBy(x => x.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        var loansByEmployee = await db.EmployeeLoans
            .Where(x => x.CompanyId == run.CompanyId && employeeIds.Contains(x.EmployeeId) && x.Status == "active" && x.IsActive && x.BalanceAmount > 0m)
            .ToListAsync();
        var loansGrouped = loansByEmployee.GroupBy(x => x.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        var loanDeductionsToCreate = new List<EmployeeLoanDeduction>();
        var createdLines = 0;

        // Pre-load installments already applied outside this run to avoid unique key conflicts.
        // Some legacy loan records can have InstallmentsPaid out of sync with existing deductions,
        // so we derive the next available installment number from the actual deduction rows.
        var activeLoanIds = loansByEmployee.Select(x => x.Id).ToList();
        var paidInstallments = await db.EmployeeLoanDeductions
            .Where(x => activeLoanIds.Contains(x.EmployeeLoanId) && x.PayrollRunId != runId)
            .Select(x => new { x.EmployeeLoanId, x.InstallmentNumber })
            .ToListAsync();
        var paidInstallmentsByLoan = paidInstallments
            .GroupBy(x => x.EmployeeLoanId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.InstallmentNumber).ToHashSet());

        // Load active recurring movements for this run date
        var allRecurring = await db.PayrollRecurringMovements
            .Where(x => x.CompanyId == run.CompanyId && x.IsActive && employeeIds.Contains(x.EmployeeId)
                && x.EffectiveStartDate.Date <= run.RunDate.Date
                && (x.EffectiveEndDate == null || x.EffectiveEndDate.Value.Date >= run.RunDate.Date))
            .ToListAsync();
        var recurringByEmployee = allRecurring.GroupBy(x => x.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());
        var recurringConceptIds = allRecurring.Select(x => x.PayrollConceptId).Distinct().ToList();
        var recurringConceptsMap = recurringConceptIds.Count > 0
            ? await db.PayrollConcepts.Where(x => recurringConceptIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id)
            : new Dictionary<Guid, PayrollConcept>();

        foreach (var employee in employees)
        {
            summaryByEmployee.TryGetValue(employee.Id, out var summary);
            var absenceDays = summary?.TotalAbsenceUnits ?? 0m;
            var daysPaid = Math.Max(0m, periodDays - absenceDays);
            var appliesFiscalConcepts = period.IsImssInsured && employee.IsImssRegistered;

            // SUELDO SEMANAL comes strictly from the employee's Sueldo del periodo.
            var baseSalary = employee.PeriodSalary > 0m
                ? employee.PeriodSalary
                : 0m;

            // ── Incidencias ──
            decimal extraPerceptions = 0m;
            decimal extraDeductions = 0m;
            decimal incidentPerceptions = 0m;
            decimal incidentDeductions = 0m;
            var incidentDetails = new List<(EmployeeIncident Incident, PayrollConcept Concept, decimal Qty, decimal Amt, bool IsDeduction, int Sort)>();

            incidentsByEmployee.TryGetValue(employee.Id, out var incidents);
            if (incidents is not null)
            {
                foreach (var inc in incidents)
                {
                    var conceptType = ResolveIncidentConceptType(inc);
                    var category = inc.PayrollIncidentType?.IncidentCategory ?? string.Empty;
                    var affectType = inc.PayrollIncidentType?.AffectType ?? string.Empty;

                    switch (conceptType)
                    {
                        case "HORAS_EXTRA":
                            var overtimeAmount = Math.Round(employee.DailySalary / 8m * 1.5m * inc.Quantity, 2);
                            extraPerceptions += overtimeAmount;
                            incidentPerceptions += overtimeAmount;
                            if (conceptBon is not null)
                                incidentDetails.Add((inc, conceptBon, inc.Quantity <= 0m ? 1m : inc.Quantity, overtimeAmount, false, 40));
                            break;
                        case "BONO":
                        case "COMISION":
                            var perceptionAmount = Math.Round(inc.Amount > 0 ? inc.Amount : employee.DailySalary * inc.Quantity, 2);
                            extraPerceptions += perceptionAmount;
                            incidentPerceptions += perceptionAmount;
                            if (conceptBon is not null)
                                incidentDetails.Add((inc, conceptBon, inc.Quantity <= 0m ? 1m : inc.Quantity, perceptionAmount, false, 40));
                            break;
                        case "FALTA":
                            var absenceAmount = Math.Round(inc.Amount > 0 ? inc.Amount : employee.DailySalary * inc.Quantity, 2);
                            extraDeductions += absenceAmount;
                            incidentDeductions += absenceAmount;
                            if (conceptDescto is not null)
                                incidentDetails.Add((inc, conceptDescto, inc.Quantity <= 0m ? 1m : inc.Quantity, absenceAmount, true, 85));
                            break;
                        case "RETARDO":
                            var delayAmount = Math.Round(inc.Amount > 0 ? inc.Amount : employee.DailySalary / 8m * inc.Quantity, 2);
                            extraDeductions += delayAmount;
                            incidentDeductions += delayAmount;
                            if (conceptDescto is not null)
                                incidentDetails.Add((inc, conceptDescto, inc.Quantity <= 0m ? 1m : inc.Quantity, delayAmount, true, 85));
                            break;
                        default:
                            if (category == "PERCEPCION" || affectType == "SUMA")
                            {
                                var genericPerceptionAmount = Math.Round(inc.Amount > 0 ? inc.Amount : employee.DailySalary * inc.Quantity, 2);
                                extraPerceptions += genericPerceptionAmount;
                                incidentPerceptions += genericPerceptionAmount;
                                if (conceptBon is not null)
                                    incidentDetails.Add((inc, conceptBon, inc.Quantity <= 0m ? 1m : inc.Quantity, genericPerceptionAmount, false, 40));
                            }
                            else if (category == "DEDUCCION" || affectType == "RESTA")
                            {
                                var genericDeductionAmount = Math.Round(inc.Amount > 0 ? inc.Amount : employee.DailySalary * inc.Quantity, 2);
                                extraDeductions += genericDeductionAmount;
                                incidentDeductions += genericDeductionAmount;
                                if (conceptDescto is not null)
                                    incidentDetails.Add((inc, conceptDescto, inc.Quantity <= 0m ? 1m : inc.Quantity, genericDeductionAmount, true, 85));
                            }
                            break;
                    }
                }
            }

            // ── Prenómina capturada ──
            var prePayrollDetails = new List<(PayrollConcept Concept, decimal Qty, decimal Amt, bool IsDeduction, int Sort)>();
            prePayrollByEmployee.TryGetValue(employee.Id, out var employeePrePayroll);
            if (employeePrePayroll is not null)
            {
                foreach (var adj in employeePrePayroll)
                {
                    if (!adj.PayrollConceptId.HasValue) continue;
                    var concept = concepts.FirstOrDefault(x => x.Id == adj.PayrollConceptId.Value);
                    if (concept is null) continue;
                    var amount = Math.Round(adj.Amount, 2);
                    if (amount <= 0m) continue;
                    var isDeduction = concept.ConceptType == "deduction" || adj.AdjustmentType == "deduction";
                    if (isDeduction) extraDeductions += amount;
                    else extraPerceptions += amount;
                    prePayrollDetails.Add((concept, adj.Quantity <= 0m ? 1m : adj.Quantity, amount, isDeduction, isDeduction ? 84 : 44));
                }
            }

            // ── Movimientos periódicos ──
            var recurringDetails = new List<(PayrollConcept Concept, decimal Qty, decimal Amt, bool IsDeduction, int Sort)>();
            recurringByEmployee.TryGetValue(employee.Id, out var employeeRecurring);
            if (employeeRecurring is not null)
            {
                foreach (var mov in employeeRecurring)
                {
                    if (!recurringConceptsMap.TryGetValue(mov.PayrollConceptId, out var movConcept)) continue;
                    var movAmt = mov.CalculationMode == "percent_of_salary"
                        ? Math.Round(baseSalary * (mov.Percentage / 100m), 2)
                        : Math.Round(mov.Amount * Math.Max(mov.Quantity, 1m), 2);
                    if (movAmt <= 0m) continue;
                    var isDeduction = mov.MovementType == "deduction";
                    if (isDeduction) extraDeductions += movAmt;
                    else extraPerceptions += movAmt;
                    recurringDetails.Add((movConcept, mov.Quantity <= 0m ? 1m : mov.Quantity, movAmt, isDeduction, isDeduction ? 82 : 42));
                }
            }

            // ── Préstamos ──
            decimal loanDeductionTotal = 0m;
            loansGrouped.TryGetValue(employee.Id, out var activeLoans);
            if (activeLoans is not null)
            {
                foreach (var loan in activeLoans)
                {
                    var installment = Math.Min(loan.InstallmentAmount, loan.BalanceAmount);
                    if (installment <= 0m) continue;

                    var nextInstallmentNo = loan.InstallmentsPaid + 1;
                    if (!paidInstallmentsByLoan.TryGetValue(loan.Id, out var paidNumbers))
                    {
                        paidNumbers = [];
                        paidInstallmentsByLoan[loan.Id] = paidNumbers;
                    }

                    while (paidNumbers.Contains(nextInstallmentNo))
                    {
                        nextInstallmentNo++;
                    }

                    loanDeductionTotal += installment;
                    paidNumbers.Add(nextInstallmentNo);
                    loanDeductionsToCreate.Add(new EmployeeLoanDeduction
                    {
                        TenantId = run.TenantId,
                        CompanyId = run.CompanyId,
                        EmployeeLoanId = loan.Id,
                        EmployeeId = employee.Id,
                        PayrollPeriodId = run.PayrollPeriodId,
                        PayrollRunId = runId,
                        DeductionDate = run.RunDate,
                        InstallmentNumber = nextInstallmentNo,
                        Amount = installment,
                        PrincipalApplied = installment,
                        InterestApplied = 0m,
                        RemainingBalance = Math.Max(0m, loan.BalanceAmount - installment),
                        Status = "applied",
                        Notes = $"Corrida {run.Folio}",
                        IsActive = true,
                        CreatedBy = "mvp-engine"
                    });

                    loan.BalanceAmount = Math.Max(0m, loan.BalanceAmount - installment);
                    loan.InstallmentsPaid = Math.Max(loan.InstallmentsPaid + 1, nextInstallmentNo);
                    if (loan.BalanceAmount <= 0m) loan.Status = "paid";
                    loan.UpdatedAt = DateTime.UtcNow;
                    loan.UpdatedBy = "mvp-engine";
                }
            }

            // ── ISR 2024 (Art. 96 LISR + Subsidio al Empleo) ──
            // SUELDO SEMANAL is used for new employees not yet registered with IMSS,
            // so it is paid on the receipt but does not feed the ISR taxable base.
            decimal grossTaxable = appliesFiscalConcepts ? extraPerceptions : 0m;
            var (netIsrPeriod, subsidioPerception) = appliesFiscalConcepts
                ? CalculateIsrAndSubsidio(grossTaxable, periodDays)
                : (0m, 0m);

            // ── IMSS cuota obrera ──
            // SUELDO SEMANAL is for new employees not yet registered with IMSS.
            var imssAmount = appliesFiscalConcepts ? 0m : 0m;

            decimal grossAmount = baseSalary + extraPerceptions + subsidioPerception;
            decimal deductionsAmount = extraDeductions + imssAmount + netIsrPeriod + loanDeductionTotal;
            decimal netAmount = Math.Max(0m, grossAmount - deductionsAmount);

            PayrollRunLine line;
            if (existingLines.TryGetValue(employee.Id, out var existingLine))
            {
                existingLine.DepartmentId = employee.DepartmentId;
                existingLine.PositionId = employee.PositionId;
                existingLine.DaysPaid = daysPaid;
                existingLine.GrossAmount = grossAmount;
                existingLine.DeductionsAmount = deductionsAmount;
                existingLine.IncidentsAmount = extraPerceptions;
                existingLine.NetAmount = netAmount;
                existingLine.UpdatedAt = DateTime.UtcNow;
                existingLine.UpdatedBy = "mvp-engine";
                line = existingLine;
            }
            else
            {
                line = new PayrollRunLine
                {
                    TenantId = run.TenantId,
                    CompanyId = run.CompanyId,
                    PayrollRunId = runId,
                    EmployeeId = employee.Id,
                    DepartmentId = employee.DepartmentId,
                    PositionId = employee.PositionId,
                    DaysPaid = daysPaid,
                    GrossAmount = grossAmount,
                    DeductionsAmount = deductionsAmount,
                    IncidentsAmount = extraPerceptions,
                    NetAmount = netAmount,
                    Notes = string.Empty,
                    IsActive = true,
                    CreatedBy = "mvp-engine"
                };
                db.PayrollRunLines.Add(line);
            }
            createdLines++;

            // ── Detalles por concepto ──
            int sortOrder = 10;

            // Percepciones
            // SUELDO SEMANAL siempre primero (sortOrder=10).
            // El importe sale directo del campo Employee.PeriodSalary (Sueldo del periodo).
            if (baseSalary > 0m)
            {
                var salDetail = BuildDetail(run, line, employee, conceptSal, daysPaid, baseSalary, sortOrder);
                salDetail.ConceptCode = string.IsNullOrWhiteSpace(conceptSal.Code) ? "SAL" : conceptSal.Code;
                salDetail.ConceptName = "SUELDO SEMANAL";
                salDetail.ConceptType = "perception";
                db.PayrollRunLineDetails.Add(salDetail);
                sortOrder += 10;
            }

            foreach (var (inc, incConcept, incQty, incAmt, _, _) in incidentDetails.Where(x => !x.IsDeduction))
            {
                db.PayrollRunLineDetails.Add(BuildIncidentDetail(run, line, employee, incConcept, inc, incQty, incAmt, sortOrder, isDeduction: false));
                sortOrder += 10;
            }

            if (subsidioPerception > 0m && conceptSubse is not null)
            {
                db.PayrollRunLineDetails.Add(BuildDetail(run, line, employee, conceptSubse, 1m, subsidioPerception, sortOrder, isDeduction: false));
                sortOrder += 10;
            }

            // Deducciones
            var incidentDeductionSort = 85;
            foreach (var (inc, incConcept, incQty, incAmt, _, _) in incidentDetails.Where(x => x.IsDeduction))
            {
                db.PayrollRunLineDetails.Add(BuildIncidentDetail(run, line, employee, incConcept, inc, incQty, incAmt, incidentDeductionSort, isDeduction: true));
                incidentDeductionSort++;
            }

            foreach (var (ppConcept, ppQty, ppAmt, ppIsDeduction, ppSort) in prePayrollDetails)
                db.PayrollRunLineDetails.Add(BuildDetail(run, line, employee, ppConcept, ppQty, ppAmt, ppSort, isDeduction: ppIsDeduction));

            if (imssAmount > 0m && conceptImss is not null)
            {
                db.PayrollRunLineDetails.Add(BuildDetail(run, line, employee, conceptImss, periodDays, imssAmount, 90, isDeduction: true));
            }

            if (netIsrPeriod > 0m && conceptIsr is not null)
            {
                db.PayrollRunLineDetails.Add(BuildDetail(run, line, employee, conceptIsr, 1m, netIsrPeriod, 95, isDeduction: true));
            }

            if (loanDeductionTotal > 0m && activeLoans is not null)
            {
                foreach (var loan in activeLoans)
                {
                    var loanConcept = concepts.FirstOrDefault(x => x.Id == loan.PayrollConceptId) ?? conceptLoan ?? conceptDescto;
                    if (loanConcept is null) continue;
                    var installment = loanDeductionsToCreate.FirstOrDefault(x => x.EmployeeLoanId == loan.Id)?.Amount ?? 0m;
                    if (installment > 0m)
                        db.PayrollRunLineDetails.Add(BuildDetail(run, line, employee, loanConcept, 1m, installment, 100, isDeduction: true));
                }
            }

            // Movimientos periódicos (detalles por concepto individual)
            foreach (var (mvConcept, mvQty, mvAmt, mvIsDeduction, mvSort) in recurringDetails)
                db.PayrollRunLineDetails.Add(BuildDetail(run, line, employee, mvConcept, mvQty, mvAmt, mvSort, isDeduction: mvIsDeduction));
        }

        if (loanDeductionsToCreate.Count > 0)
        {
            var loanIdsToCreate = loanDeductionsToCreate.Select(x => x.EmployeeLoanId).Distinct().ToList();
            var usedInstallments = (await db.EmployeeLoanDeductions
                    .AsNoTracking()
                    .Where(x => loanIdsToCreate.Contains(x.EmployeeLoanId))
                    .Select(x => new { x.EmployeeLoanId, x.InstallmentNumber })
                    .ToListAsync())
                .GroupBy(x => x.EmployeeLoanId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.InstallmentNumber).ToHashSet());

            foreach (var deduction in loanDeductionsToCreate)
            {
                if (!usedInstallments.TryGetValue(deduction.EmployeeLoanId, out var usedNumbers))
                {
                    usedNumbers = [];
                    usedInstallments[deduction.EmployeeLoanId] = usedNumbers;
                }

                while (usedNumbers.Contains(deduction.InstallmentNumber))
                    deduction.InstallmentNumber++;

                usedNumbers.Add(deduction.InstallmentNumber);
                if (deduction.RemainingBalance <= 0m)
                    deduction.Status = "applied";
            }
        }

        db.EmployeeLoanDeductions.AddRange(loanDeductionsToCreate);
        await db.SaveChangesAsync();

        // ── Recalcular totales de corrida ──
        var updatedLines = await db.PayrollRunLines.Where(x => x.PayrollRunId == runId).ToListAsync();
        run.EmployeeCount = updatedLines.Count;
        run.GrossAmount = updatedLines.Sum(x => x.GrossAmount);
        run.DeductionsAmount = updatedLines.Sum(x => x.DeductionsAmount);
        run.NetAmount = updatedLines.Sum(x => x.NetAmount);
        run.Status = "calculated";
        run.UpdatedAt = DateTime.UtcNow;
        run.UpdatedBy = "mvp-engine";
        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            success = true,
            employees = createdLines,
            grossAmount = run.GrossAmount,
            deductionsAmount = run.DeductionsAmount,
            netAmount = run.NetAmount
        });
    }

    // ──────────────────────────────────────────────────────────────
    // 6. RECIBO DE NÓMINA HTML
    // GET /api/payroll/runs/{runId}/receipts/{lineId}
    // ──────────────────────────────────────────────────────────────
    private static async Task<IResult> GetPayrollReceiptHtmlAsync(Guid runId, Guid lineId, NanchesoftDbContext db)
    {
        var line = await db.PayrollRunLines
            .Include(x => x.Employee)
            .Include(x => x.Department)
            .Include(x => x.Position)
            .FirstOrDefaultAsync(x => x.Id == lineId && x.PayrollRunId == runId);

        if (line is null)
            return Results.NotFound(new { message = "No se encontró el recibo." });

        var run = await db.PayrollRuns.Include(x => x.PayrollPeriod).Include(x => x.Company).FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null)
            return Results.NotFound(new { message = "No se encontró la corrida." });

        var details = await db.PayrollRunLineDetails
            .Where(x => x.PayrollRunLineId == lineId && x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.ConceptCode)
            .ToListAsync();

        var emp = line.Employee!;
        var period = run.PayrollPeriod!;
        var company = run.Company!;
        var perceptions = details.Where(x => x.ConceptType == "perception").ToList();
        var deductions = details.Where(x => x.ConceptType == "deduction").ToList();
        var totalPerceptions = perceptions.Sum(x => x.Amount);
        var totalDeductions = deductions.Sum(x => x.Amount);

        static string Row(PayrollRunLineDetail d)
            => $"<tr><td>{d.ConceptCode} – {d.ConceptName}</td><td class=\"amount\">${d.Amount:N2}</td></tr>";

        var percRows = string.Join("\n", perceptions.Select(Row));
        var dedRows  = string.Join("\n", deductions.Select(Row));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"UTF-8\">");
        sb.AppendLine("<title>Recibo de Nómina</title><style>");
        sb.AppendLine("body{font-family:Arial,sans-serif;font-size:12px;color:#333;margin:0;padding:20px}");
        sb.AppendLine(".hdr{background:#1e3a5f;color:#fff;padding:12px 16px;border-radius:6px 6px 0 0}");
        sb.AppendLine(".hdr h1{margin:0;font-size:16px}.hdr p{margin:2px 0;font-size:11px;opacity:.85}");
        sb.AppendLine(".sec{border:1px solid #ddd}");
        sb.AppendLine(".eg{display:grid;grid-template-columns:1fr 1fr 1fr;gap:8px;padding:12px 16px;background:#f8f9fa}");
        sb.AppendLine(".ef label{display:block;font-size:10px;color:#666;text-transform:uppercase;letter-spacing:.5px}");
        sb.AppendLine(".ef span{font-weight:600}");
        sb.AppendLine(".cv{display:grid;grid-template-columns:1fr 1fr}");
        sb.AppendLine(".ct{background:#e8ecf0;font-weight:700;padding:6px 12px;font-size:11px;text-transform:uppercase;border-bottom:1px solid #ddd}");
        sb.AppendLine(".ct.d{background:#fdecea}");
        sb.AppendLine("table.it{width:100%;border-collapse:collapse}");
        sb.AppendLine("table.it td{padding:5px 12px;border-bottom:1px solid #eee}");
        sb.AppendLine("table.it td.am{text-align:right;font-variant-numeric:tabular-nums}");
        sb.AppendLine(".tr td{font-weight:700;padding:6px 12px;background:#f0f0f0}");
        sb.AppendLine(".tr.d td{background:#fdf0ee}");
        sb.AppendLine(".nb{background:#1e3a5f;color:#fff;padding:10px 16px;display:flex;justify-content:space-between;align-items:center}");
        sb.AppendLine(".nb .lb{font-size:11px;opacity:.85}.nb .am{font-size:22px;font-weight:700}");
        sb.AppendLine(".ft{padding:8px 16px;font-size:10px;color:#888;text-align:center;border-top:1px solid #eee}");
        sb.AppendLine("@media print{body{padding:0}}</style></head><body><div class=\"sec\">");

        sb.AppendLine($"<div class=\"hdr\"><h1>{company.Name}</h1>");
        sb.AppendLine($"<p>Periodo: {period.Name} &nbsp;|&nbsp; {period.StartDate:dd/MM/yyyy} &ndash; {period.EndDate:dd/MM/yyyy} &nbsp;|&nbsp; Pago: {period.PaymentDate:dd/MM/yyyy} &nbsp;|&nbsp; Folio: {run.Folio}</p></div>");

        sb.AppendLine("<div class=\"eg\">");
        sb.AppendLine($"<div class=\"ef\"><label>Empleado</label><span>{emp.GetFullName()}</span></div>");
        sb.AppendLine($"<div class=\"ef\"><label>No. Empleado</label><span>{emp.EmployeeNumber}</span></div>");
        sb.AppendLine($"<div class=\"ef\"><label>RFC</label><span>{emp.TaxId}</span></div>");
        sb.AppendLine($"<div class=\"ef\"><label>CURP</label><span>{emp.Curp}</span></div>");
        sb.AppendLine($"<div class=\"ef\"><label>NSS</label><span>{emp.Nss}</span></div>");
        sb.AppendLine($"<div class=\"ef\"><label>Departamento</label><span>{line.Department?.Name ?? "—"}</span></div>");
        sb.AppendLine($"<div class=\"ef\"><label>Puesto</label><span>{line.Position?.Name ?? "—"}</span></div>");
        sb.AppendLine($"<div class=\"ef\"><label>Salario diario</label><span>${emp.DailySalary:N2}</span></div>");
        sb.AppendLine($"<div class=\"ef\"><label>Días pagados</label><span>{line.DaysPaid:N2}</span></div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"cv\">");
        sb.AppendLine($"<div><div class=\"ct\">Percepciones</div><table class=\"it\">{percRows}");
        sb.AppendLine($"<tr class=\"tr\"><td>Total Percepciones</td><td class=\"am\">${totalPerceptions:N2}</td></tr></table></div>");
        sb.AppendLine($"<div><div class=\"ct d\">Deducciones</div><table class=\"it\">{dedRows}");
        sb.AppendLine($"<tr class=\"tr d\"><td>Total Deducciones</td><td class=\"am\">${totalDeductions:N2}</td></tr></table></div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"nb\">");
        sb.AppendLine($"<div><div class=\"lb\">Percepciones</div><div>${totalPerceptions:N2}</div></div>");
        sb.AppendLine($"<div><div class=\"lb\">Deducciones</div><div>${totalDeductions:N2}</div></div>");
        sb.AppendLine($"<div><div class=\"lb\">NETO A PAGAR</div><div class=\"am\">${line.NetAmount:N2}</div></div>");
        sb.AppendLine("</div>");
        sb.AppendLine($"<div class=\"ft\">Recibo generado el {DateTime.Now:dd/MM/yyyy HH:mm} &nbsp;|&nbsp; Documento informativo, no es CFDI.</div>");
        sb.AppendLine("</div></body></html>");

        var html = sb.ToString();

        return Results.Content(html, "text/html; charset=utf-8");
    }

    // ──────────────────────────────────────────────────────────────
    // 7. SEMBRAR CONCEPTOS PREDETERMINADOS
    // ──────────────────────────────────────────────────────────────
    private static async Task<IResult> SeedDefaultConceptsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var ctxCompanyId = ApiTenantScope.ResolveCompanyId(httpContext);
        var ctxTenantId = ApiTenantScope.ResolveTenantId(httpContext);

        Company? company = null;
        if (ctxCompanyId.HasValue)
        {
            company = await db.Companies.FirstOrDefaultAsync(x => x.Id == ctxCompanyId.Value && x.IsActive);
            if (company is not null && ctxTenantId.HasValue && company.TenantId != ctxTenantId.Value)
                return Results.BadRequest(new { message = "La empresa seleccionada no pertenece al tenant actual." });
        }
        else if (ctxTenantId.HasValue)
        {
            company = await db.Companies
                .Where(x => x.TenantId == ctxTenantId.Value && x.IsActive)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        if (company is null)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para crear conceptos de nómina." });

        var (created, skipped) = await EnsureDefaultConceptsAsync(db, company.Id);
        return Results.Ok(new { success = true, companyId = company.Id, tenantId = company.TenantId, created, skipped });
    }

    private static async Task<(int Created, int Skipped)> EnsureDefaultConceptsAsync(NanchesoftDbContext db, Guid companyId)
    {
        var company = await db.Companies.FirstOrDefaultAsync(x => x.Id == companyId);
        if (company is null)
            return (0, 0);

        var seed = new[]
        {
            new { Code = "SAL",   Name = "SUELDO SEMANAL",       ConceptType = "perception", CalculationType = "period_salary", SatCode = "P-001", SatAgrupador = "Sueldos",     TaxableType = "taxable",     TaxablePercent = 100m, ExemptPercent = 0m,   IsRecurring = true,  IsAutomatic = true,  SortOrder = 10 },
            new { Code = "BON",   Name = "Bonificación",          ConceptType = "perception", CalculationType = "manual",        SatCode = "P-016", SatAgrupador = "OtrosPagos",  TaxableType = "taxable",     TaxablePercent = 100m, ExemptPercent = 0m,   IsRecurring = false, IsAutomatic = false, SortOrder = 20 },
            new { Code = "HEORD", Name = "Horas Extra Ordinarias",ConceptType = "perception", CalculationType = "hours",         SatCode = "P-019", SatAgrupador = "HorasExtra",  TaxableType = "mixed",       TaxablePercent = 50m,  ExemptPercent = 50m,  IsRecurring = false, IsAutomatic = false, SortOrder = 30 },
            new { Code = "PV",    Name = "Prima Vacacional",      ConceptType = "perception", CalculationType = "manual",        SatCode = "P-021", SatAgrupador = "Prima",       TaxableType = "mixed",       TaxablePercent = 60m,  ExemptPercent = 40m,  IsRecurring = false, IsAutomatic = false, SortOrder = 40 },
            new { Code = "AGUI",  Name = "Aguinaldo",             ConceptType = "perception", CalculationType = "manual",        SatCode = "P-002", SatAgrupador = "Prima",       TaxableType = "mixed",       TaxablePercent = 100m, ExemptPercent = 0m,   IsRecurring = false, IsAutomatic = false, SortOrder = 50 },
            new { Code = "SUBSE", Name = "Subsidio al Empleo",    ConceptType = "perception", CalculationType = "auto_subsidy",  SatCode = "P-017", SatAgrupador = "SubsidioEmpleo", TaxableType = "exempt",  TaxablePercent = 0m,   ExemptPercent = 100m, IsRecurring = true,  IsAutomatic = true,  SortOrder = 60 },
            new { Code = "IMSS",  Name = "Cuota IMSS Obrera",     ConceptType = "deduction",  CalculationType = "auto_imss",     SatCode = "D-001", SatAgrupador = "SeguridadSocial", TaxableType = "taxable",TaxablePercent = 100m, ExemptPercent = 0m,   IsRecurring = true,  IsAutomatic = true,  SortOrder = 90 },
            new { Code = "ISR",   Name = "Retención ISR",         ConceptType = "deduction",  CalculationType = "auto_isr",      SatCode = "D-002", SatAgrupador = "ISR",         TaxableType = "taxable",     TaxablePercent = 100m, ExemptPercent = 0m,   IsRecurring = true,  IsAutomatic = true,  SortOrder = 95 },
            new { Code = "PRESTA",Name = "Préstamo",              ConceptType = "deduction",  CalculationType = "fixed_amount",  SatCode = "D-004", SatAgrupador = "Deduccion",   TaxableType = "not_applicable", TaxablePercent = 0m,ExemptPercent = 0m,   IsRecurring = false, IsAutomatic = false, SortOrder = 100 },
            new { Code = "DESCTO",Name = "Descuento por ausencias",ConceptType = "deduction",  CalculationType = "auto_absent",   SatCode = "D-011", SatAgrupador = "Deduccion",   TaxableType = "not_applicable", TaxablePercent = 0m,ExemptPercent = 0m,   IsRecurring = false, IsAutomatic = true,  SortOrder = 88 },
        };

        int created = 0, skipped = 0;
        foreach (var s in seed)
        {
            var existingConcept = await db.PayrollConcepts.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == s.Code);
            if (existingConcept is not null)
            {
                // Mantener el concepto base alineado con la nómina semanal.
                if (s.Code == "SAL" && !IsWeeklySalaryConcept(existingConcept))
                {
                    existingConcept.Name = "SUELDO SEMANAL";
                    existingConcept.CalculationType = "period_salary";
                    existingConcept.SatAgrupador = "Sueldos";
                    existingConcept.UpdatedAt = DateTime.UtcNow;
                    existingConcept.UpdatedBy = "mvp-engine";
                }
                skipped++;
                continue;
            }
            db.PayrollConcepts.Add(new PayrollConcept
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                Code = s.Code,
                Name = s.Name,
                ConceptType = s.ConceptType,
                CalculationType = s.CalculationType,
                SatCode = s.SatCode,
                SatAgrupador = s.SatAgrupador,
                TaxableType = s.TaxableType,
                TaxablePercent = s.TaxablePercent,
                ExemptPercent = s.ExemptPercent,
                IsRecurring = s.IsRecurring,
                IsAutomatic = s.IsAutomatic,
                PrintOnReceipt = true,
                SortOrder = s.SortOrder,
                IsActive = true,
                CreatedBy = "seed"
            });
            created++;
        }
        await db.SaveChangesAsync();
        return (created, skipped);
    }

    private static bool IsWeeklySalaryConcept(PayrollConcept concept)
        => string.Equals(NormalizePayrollConceptText(concept.Name), "SUELDO SEMANAL", StringComparison.Ordinal)
        || string.Equals(NormalizePayrollConceptText(concept.Code), "SAL", StringComparison.Ordinal);

    private static string NormalizePayrollConceptText(string? value)
        => string.Join(' ', (value ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static Guid? ResolveIncidentTypeId(Dictionary<string, NomPayrollIncidentType> incidentTypes, string code)
        => incidentTypes.TryGetValue(code, out var incidentType) ? incidentType.Id : null;

    private static string ResolveIncidentConceptType(EmployeeIncident incident)
    {
        if (!string.IsNullOrWhiteSpace(incident.PayrollIncidentType?.PayrollConceptType))
            return incident.PayrollIncidentType.PayrollConceptType.ToUpperInvariant();

        return (incident.IncidentType ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "hora_extra" or "horas_extra" => "HORAS_EXTRA",
            "bono" or "bonus" or "percepcion" => "BONO",
            "falta" or "falta_injustificada" => "FALTA",
            "retardo" => "RETARDO",
            "deduccion" or "descuento" => "DESCUENTO_DANOS",
            _ => "OTRO"
        };
    }

    // ── ISR 2024 tarifa Art. 96 LISR (importes mensuales) ──
    private static readonly (decimal LI, decimal LS, decimal CF, decimal Rate)[] IsrTable2024 =
    [
        (      0.01m,     746.04m,      0.00m, 0.0192m),
        (    746.05m,   6_332.05m,     14.32m, 0.0640m),
        (  6_332.06m,  11_128.01m,    371.83m, 0.1088m),
        ( 11_128.02m,  12_935.82m,    893.63m, 0.1600m),
        ( 12_935.83m,  15_487.71m,  1_182.88m, 0.1792m),
        ( 15_487.72m,  31_236.49m,  1_640.18m, 0.2136m),
        ( 31_236.50m,  49_233.00m,  5_004.12m, 0.2352m),
        ( 49_233.01m,  93_993.90m,  9_236.89m, 0.3000m),
        ( 93_993.91m, 125_325.20m, 22_665.17m, 0.3200m),
        (125_325.21m, 375_975.61m, 32_691.18m, 0.3400m),
        (375_975.62m, decimal.MaxValue, 117_912.32m, 0.3500m),
    ];

    // ── Subsidio al Empleo 2024 (importes mensuales) ──
    private static readonly (decimal LI, decimal LS, decimal Subsidy)[] SubsidioTable2024 =
    [
        (    0.01m, 1_768.96m, 407.02m),
        (1_768.97m, 2_653.38m, 406.83m),
        (2_653.39m, 3_472.84m, 406.62m),
        (3_472.85m, 3_537.87m, 392.77m),
        (3_537.88m, 4_446.15m, 382.46m),
        (4_446.16m, 4_717.18m, 354.23m),
        (4_717.19m, 5_335.42m, 324.87m),
        (5_335.43m, 6_224.67m, 294.63m),
        (6_224.68m, 7_113.90m, 253.54m),
        (7_113.91m, 7_382.33m, 217.61m),
        (7_382.34m, decimal.MaxValue, 0.00m),
    ];

    private const decimal UmaDaily2024 = 108.57m;

    private static decimal IsrMonthly(decimal monthlyTaxable)
    {
        if (monthlyTaxable <= 0m) return 0m;
        foreach (var (li, ls, cf, rate) in IsrTable2024)
            if (monthlyTaxable >= li && monthlyTaxable <= ls)
                return Math.Round(cf + (monthlyTaxable - li) * rate, 2);
        return 0m;
    }

    private static decimal SubsidioMonthly(decimal monthlyTaxable)
    {
        if (monthlyTaxable <= 0m) return 0m;
        foreach (var (li, ls, subsidy) in SubsidioTable2024)
            if (monthlyTaxable >= li && monthlyTaxable <= ls)
                return subsidy;
        return 0m;
    }

    // Returns (netISR, subsidioPerception) — one of these will be zero.
    private static (decimal NetIsr, decimal SubsidioPerception) CalculateIsrAndSubsidio(decimal taxable, int periodDays)
    {
        if (taxable <= 0m || periodDays <= 0) return (0m, 0m);
        var factor = periodDays / 30.4m;
        var monthly = taxable / factor;
        var isr = IsrMonthly(monthly) * factor;
        var sub = SubsidioMonthly(monthly) * factor;
        if (sub >= isr) return (0m, Math.Round(sub - isr, 2));
        return (Math.Round(isr - sub, 2), 0m);
    }

    private static decimal CalculateImssCuotaObrera(decimal sbcDaily, int periodDays)
    {
        if (sbcDaily <= 0m || periodDays <= 0) return 0m;
        var excess    = Math.Max(0m, sbcDaily - 3m * UmaDaily2024);
        var emExcede  = excess   * 0.0040m;   // EM excedente obrera
        var emDinero  = sbcDaily * 0.0025m;   // EM prestaciones en dinero
        var iv        = sbcDaily * 0.00625m;  // Invalidez y vida
        var cv        = sbcDaily * 0.01125m;  // Cesantía en edad avanzada y vejez
        return Math.Round((emExcede + emDinero + iv + cv) * periodDays, 2);
    }

    private static PayrollRunLineDetail BuildDetail(
        PayrollRun run, PayrollRunLine line, Employee employee,
        PayrollConcept concept, decimal quantity, decimal amount,
        int sortOrder, bool isDeduction = false)
    {
        var (taxable, exempt) = SplitTaxable(concept.TaxableType, amount);
        return new PayrollRunLineDetail
        {
            TenantId = run.TenantId,
            CompanyId = run.CompanyId,
            PayrollRunId = run.Id,
            PayrollRunLineId = line.Id,
            EmployeeId = employee.Id,
            PayrollConceptId = concept.Id,
            ConceptCode = concept.Code,
            ConceptName = concept.Name,
            ConceptType = isDeduction ? "deduction" : concept.ConceptType,
            SatCode = concept.SatCode,
            TaxableType = concept.TaxableType,
            Quantity = quantity,
            Amount = amount,
            TaxableAmount = taxable,
            ExemptAmount = exempt,
            SortOrder = sortOrder,
            IsGenerated = true,
            Status = "applied",
            IsActive = true,
            CreatedBy = "mvp-engine"
        };
    }

    private static PayrollRunLineDetail BuildIncidentDetail(
        PayrollRun run, PayrollRunLine line, Employee employee,
        PayrollConcept baseConcept, EmployeeIncident incident,
        decimal quantity, decimal amount, int sortOrder, bool isDeduction)
    {
        var detail = BuildDetail(run, line, employee, baseConcept, quantity, amount, sortOrder, isDeduction);
        var incidentType = incident.PayrollIncidentType;
        if (incidentType is not null)
        {
            if (!string.IsNullOrWhiteSpace(incidentType.Code))
                detail.ConceptCode = TrimPayrollDetailText(incidentType.Code.ToUpperInvariant(), 32);
            if (!string.IsNullOrWhiteSpace(incidentType.Name))
                detail.ConceptName = TrimPayrollDetailText(incidentType.Name, 256);
            if (!string.IsNullOrWhiteSpace(incidentType.SatCode))
                detail.SatCode = incidentType.SatCode;
        }

        if (!string.IsNullOrWhiteSpace(incident.Notes))
            detail.Notes = TrimPayrollDetailText(incident.Notes, 1024);

        return detail;
    }

    private static string TrimPayrollDetailText(string value, int maxLength)
    {
        var text = value.Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static (decimal Taxable, decimal Exempt) SplitTaxable(string taxableType, decimal amount) =>
        taxableType?.ToLowerInvariant() switch
        {
            "exempt" => (0m, amount),
            "mixed" => (Math.Round(amount * 0.5m, 2), Math.Round(amount * 0.5m, 2)),
            _ => (amount, 0m)
        };

    private static (bool IsWorkDay, TimeSpan Entry, TimeSpan Exit, int Tolerance) GetScheduleForDay(WorkSchedule s, DayOfWeek dow)
    {
        static TimeSpan ParseTime(string t) =>
            TimeSpan.TryParse(t, out var ts) && ts > TimeSpan.Zero ? ts : new TimeSpan(9, 0, 0);

        return dow switch
        {
            DayOfWeek.Monday    => (s.Monday,    ParseTime(s.MonEntryTime), ParseTime(s.MonExitTime), s.MonToleranceMinutes),
            DayOfWeek.Tuesday   => (s.Tuesday,   ParseTime(s.TueEntryTime), ParseTime(s.TueExitTime), s.TueToleranceMinutes),
            DayOfWeek.Wednesday => (s.Wednesday, ParseTime(s.WedEntryTime), ParseTime(s.WedExitTime), s.WedToleranceMinutes),
            DayOfWeek.Thursday  => (s.Thursday,  ParseTime(s.ThuEntryTime), ParseTime(s.ThuExitTime), s.ThuToleranceMinutes),
            DayOfWeek.Friday    => (s.Friday,    ParseTime(s.FriEntryTime), ParseTime(s.FriExitTime), s.FriToleranceMinutes),
            DayOfWeek.Saturday  => (s.Saturday,  ParseTime(s.SatEntryTime), ParseTime(s.SatExitTime), s.SatToleranceMinutes),
            DayOfWeek.Sunday    => (s.Sunday,    ParseTime(s.SunEntryTime), ParseTime(s.SunExitTime), s.SunToleranceMinutes),
            _ => (true, new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), 5)
        };
    }

    private static string GetCell(IXLWorksheet ws, int row, Dictionary<string, int> headers, string name)
    {
        if (!headers.TryGetValue(name, out var col)) return string.Empty;
        var cell = ws.Cell(row, col);
        return cell.IsEmpty() ? string.Empty : cell.GetString();
    }

    private static string GetCellAny(IXLWorksheet ws, int row, Dictionary<string, int> headers, params string[] names)
    {
        foreach (var name in names)
        {
            var value = GetCell(ws, row, headers, name);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return string.Empty;
    }

    private static bool TryParseDecimalValue(string raw, out decimal value)
    {
        if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value))
            return true;
        return decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.GetCultureInfo("es-MX"), out value);
    }

    // ──────────────────────────────────────────────────────────────
    // PLANTILLA EXCEL PARA IMPORTAR EMPLEADOS
    // ──────────────────────────────────────────────────────────────
    private static IResult DownloadImportTemplateAsync()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Empleados");

        var headers = new[]
        {
            "NoEmpleado", "Nombre", "ApellidoPaterno", "ApellidoMaterno",
            "RFC", "CURP", "NSS",
            "Email", "Telefono", "TelefonoEmergencia",
            "FechaIngreso", "FechaNacimiento",
            "Sexo", "EstadoCivil", "TipoSangre",
            "SueldoPeriodo", "SalarioDiario", "SalarioDiarioIntegrado",
            "SucursalCodigo", "DepartamentoCodigo", "PuestoCodigo",
            "TipoContrato", "PeriodoNomina",
            "ClaveReloj", "ClaveNOI",
            "Curp_Domicilio_Calle", "Colonia", "Ciudad", "Estado", "CodigoPostal",
            "Banco", "Cuenta", "CLABE",
            "RegPatronal", "ZonaSalarial"
        };

        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Fila de ejemplo
        ws.Cell(2, 1).Value = "001";
        ws.Cell(2, 2).Value = "Juan";
        ws.Cell(2, 3).Value = "García";
        ws.Cell(2, 4).Value = "López";
        ws.Cell(2, 5).Value = "GALJ800101ABC";
        ws.Cell(2, 6).Value = "GALJ800101HDFXXX01";
        ws.Cell(2, 7).Value = "12345678901";
        ws.Cell(2, 8).Value = "juan.garcia@empresa.com";
        ws.Cell(2, 9).Value = "5512345678";
        ws.Cell(2, 10).Value = "";
        ws.Cell(2, 11).Value = DateTime.Today.ToString("yyyy-MM-dd");
        ws.Cell(2, 12).Value = "1980-01-01";
        ws.Cell(2, 13).Value = "M";
        ws.Cell(2, 14).Value = "soltero";
        ws.Cell(2, 15).Value = "O+";
        ws.Cell(2, 16).Value = 2100.00;
        ws.Cell(2, 17).Value = 300.00;
        ws.Cell(2, 18).Value = 320.00;
        ws.Cell(2, 19).Value = "MATRIZ";
        ws.Cell(2, 20).Value = "PROD";
        ws.Cell(2, 21).Value = "OPER";
        ws.Cell(2, 22).Value = "indefinite";
        ws.Cell(2, 23).Value = "semanal";
        ws.Cell(2, 24).Value = "001";
        ws.Cell(2, 25).Value = "";
        ws.Cell(2, 26).Value = "Av. Principal 123";
        ws.Cell(2, 27).Value = "Centro";
        ws.Cell(2, 28).Value = "Monterrey";
        ws.Cell(2, 29).Value = "NL";
        ws.Cell(2, 30).Value = "64000";
        ws.Cell(2, 31).Value = "BBVA";
        ws.Cell(2, 32).Value = "";
        ws.Cell(2, 33).Value = "";
        ws.Cell(2, 34).Value = "Y1234567890";
        ws.Cell(2, 35).Value = "A";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var bytes = ms.ToArray();

        return Results.File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "plantilla_importar_empleados.xlsx");
    }

    // ──────────────────────────────────────────────────────────────
    // PREVIEW DE IMPORTACIÓN (sin guardar en BD)
    // ──────────────────────────────────────────────────────────────
    private static async Task<IResult> PreviewImportFromExcelAsync(HttpContext httpContext, IFormFile file, NanchesoftDbContext db)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { message = "El archivo está vacío." });

        var conflictMode = ResolveConflictMode(httpContext);
        var (companyOrError, branchId) = await ResolveImportScopeAsync(httpContext, db);
        if (companyOrError is not Company company)
            return Results.BadRequest(new { message = companyOrError as string ?? "No se pudo determinar la empresa." });

        EmployeeImportBundle bundle;
        try
        {
            bundle = await BuildEmployeeImportBundleAsync(file, db, company, branchId, conflictMode);
        }
        catch (Exception ex)
        {
            return Results.Problem($"No se pudo leer el archivo: {ex.Message}", statusCode: 400);
        }

        var summary = SummarizeRows(bundle.Rows);

        return Results.Ok(new
        {
            total = summary.Total,
            newCount = summary.NewCount,
            updateCount = summary.UpdateCount,
            errorCount = summary.ErrorCount,
            skipCount = summary.SkipCount,
            duplicateCount = summary.DuplicateCount,
            conflictMode = conflictMode,
            mappings = bundle.Mappings,
            rows = bundle.Rows
        });
    }

    public sealed class EmployeeImportFieldMapping
    {
        public string TargetField { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string SourceColumn { get; set; } = string.Empty;
        public bool Required { get; set; }
        public bool Found { get; set; }
    }

    public sealed class EmployeeImportPreviewRow
    {
        public int RowNumber { get; set; }
        public string Code { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string SecondLastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string Curp { get; set; } = string.Empty;
        public string Nss { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime? HireDate { get; set; }
        public decimal PeriodSalary { get; set; }
        public decimal DailySalary { get; set; }
        public decimal IntegratedDailySalary { get; set; }
        public Guid? BranchId { get; set; }
        public bool CreateBranch { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public Guid? DepartmentId { get; set; }
        public bool CreateDepartment { get; set; }
        public string DepartmentCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public Guid? PositionId { get; set; }
        public bool CreatePosition { get; set; }
        public string PositionCode { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;     // new | update | skip | duplicate | error
        public string Message { get; set; } = string.Empty;

        // Internos: se usan para revalidar formatos en el momento de aplicar.
        public string HireDateRaw { get; set; } = string.Empty;
        public string PeriodSalaryRaw { get; set; } = string.Empty;
        public string SalaryRaw { get; set; } = string.Empty;
    }

    private static List<EmployeeImportFieldMapping> BuildEmployeeImportMappings(Dictionary<string, int> headers)
    {
        var specs = new (string TargetField, string Label, bool Required, string[] Aliases)[]
        {
            ("EmployeeNumber", "Número de empleado", true, ["NoEmpleado"]),
            ("FirstName", "Nombre", true, ["Nombre"]),
            ("LastName", "Apellido paterno", true, ["ApellidoPaterno"]),
            ("SecondLastName", "Apellido materno", false, ["ApellidoMaterno"]),
            ("TaxId", "RFC", false, ["RFC"]),
            ("Curp", "CURP", false, ["CURP"]),
            ("Nss", "NSS", false, ["NSS"]),
            ("Email", "Email", false, ["Email"]),
            ("Phone", "Teléfono", false, ["Telefono"]),
            ("HireDate", "Fecha ingreso", true, ["FechaIngreso"]),
            ("PeriodSalary", "Sueldo del periodo", false, ["SueldoPeriodo", "SueldoDelPeriodo", "Sueldo del periodo", "Sueldo periodo", "Sueldo semanal"]),
            ("DailySalary", "Salario diario", false, ["SalarioDiario"]),
            ("IntegratedDailySalary", "Salario diario integrado", false, ["SalarioDiarioIntegrado"]),
            ("BranchId", "Sucursal", false, ["SucursalCodigo"]),
            ("DepartmentId", "Departamento", false, ["DepartamentoCodigo"]),
            ("PositionId", "Puesto", false, ["PuestoCodigo"]),
            ("Code", "Código empleado", false, [])
        };

        var mappings = new List<EmployeeImportFieldMapping>();
        foreach (var spec in specs)
        {
            var source = spec.Aliases.FirstOrDefault(alias => headers.ContainsKey(alias)) ?? string.Empty;
            mappings.Add(new EmployeeImportFieldMapping
            {
                TargetField = spec.TargetField,
                Label = spec.Label,
                SourceColumn = string.IsNullOrWhiteSpace(source) && spec.TargetField == "Code" ? "Generado automáticamente" : source,
                Required = spec.Required,
                Found = spec.TargetField == "Code" || !string.IsNullOrWhiteSpace(source)
            });
        }
        return mappings;
    }

    private static int GetNextEmployeeCodeNumber(List<Employee> existing)
    {
        var max = 0;
        foreach (var employee in existing)
        {
            max = Math.Max(max, ExtractEmployeeCodeNumber(employee.Code));
            max = Math.Max(max, ExtractEmployeeCodeNumber(employee.EmployeeNumber));
        }
        return max + 1;
    }

    private static int ExtractEmployeeCodeNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var parsed) ? parsed : 0;
    }

    private static string FormatEmployeeCode(int number) => $"EMP{number:000}";

    private static string NormalizeCatalogCode(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? UnidentifiedCatalogCode : normalized;
    }

    private static string AppendMessage(string current, string extra)
        => string.IsNullOrWhiteSpace(current) ? extra : $"{current} {extra}";

    private static async Task EnsureImportCatalogsAsync(NanchesoftDbContext db, Company company, List<EmployeeImportPreviewRow> rows)
    {
        var placeholderBranch = await db.Branches
            .FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == UnidentifiedCatalogCode);
        if (placeholderBranch is null)
        {
            placeholderBranch = new Branch
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                Code = UnidentifiedCatalogCode,
                Name = UnidentifiedCatalogCode,
                IsActive = true,
                CreatedBy = "excel-import"
            };
            db.Branches.Add(placeholderBranch);
            await db.SaveChangesAsync();
        }

        var branchCodes = rows
            .Where(x => x.Status is "new" or "update")
            .Where(x => x.CreateBranch && !string.IsNullOrWhiteSpace(x.BranchCode))
            .Select(x => x.BranchCode.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (branchCodes.Count > 0)
        {
            var existingBranches = await db.Branches
                .Where(x => x.CompanyId == company.Id && branchCodes.Contains(x.Code))
                .ToDictionaryAsync(x => x.Code.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);

            foreach (var code in branchCodes.Where(code => !existingBranches.ContainsKey(code)))
            {
                var branch = new Branch
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    Code = code,
                    Name = code,
                    IsActive = true,
                    CreatedBy = "excel-import"
                };
                db.Branches.Add(branch);
                existingBranches[code] = branch;
            }

            await db.SaveChangesAsync();

            foreach (var row in rows.Where(x => x.CreateBranch && !string.IsNullOrWhiteSpace(x.BranchCode)))
            {
                if (existingBranches.TryGetValue(row.BranchCode.Trim().ToUpperInvariant(), out var branch))
                {
                    row.BranchId = branch.Id;
                    row.BranchName = branch.Name;
                    row.CreateBranch = false;
                }
            }
        }

        foreach (var row in rows.Where(x => x.Status is "new" or "update").Where(x => !x.BranchId.HasValue))
        {
            row.BranchCode = UnidentifiedCatalogCode;
            row.BranchName = placeholderBranch.Name;
            row.BranchId = placeholderBranch.Id;
            row.CreateBranch = false;
        }

        var placeholderDepartment = await db.Departments
            .FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == UnidentifiedCatalogCode);
        if (placeholderDepartment is null)
        {
            placeholderDepartment = new Department
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                Code = UnidentifiedCatalogCode,
                Name = UnidentifiedCatalogCode,
                Description = "Registro comodín para importación de empleados.",
                IsActive = true,
                CreatedBy = "excel-import"
            };
            db.Departments.Add(placeholderDepartment);
            await db.SaveChangesAsync();
        }

        var departmentCodes = rows
            .Where(x => x.Status is "new" or "update")
            .Where(x => x.CreateDepartment && !string.IsNullOrWhiteSpace(x.DepartmentCode))
            .Select(x => x.DepartmentCode.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (departmentCodes.Count > 0)
        {
            var existingDepartments = await db.Departments
                .Where(x => x.CompanyId == company.Id && departmentCodes.Contains(x.Code))
                .ToDictionaryAsync(x => x.Code.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);

            foreach (var code in departmentCodes.Where(code => !existingDepartments.ContainsKey(code)))
            {
                var department = new Department
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    Code = code,
                    Name = code,
                    Description = "Creado automáticamente por importación de empleados.",
                    IsActive = true,
                    CreatedBy = "excel-import"
                };
                db.Departments.Add(department);
                existingDepartments[code] = department;
            }

            await db.SaveChangesAsync();

            foreach (var row in rows.Where(x => x.CreateDepartment && !string.IsNullOrWhiteSpace(x.DepartmentCode)))
            {
                if (existingDepartments.TryGetValue(row.DepartmentCode.Trim().ToUpperInvariant(), out var department))
                {
                    row.DepartmentId = department.Id;
                    row.DepartmentName = department.Name;
                    row.CreateDepartment = false;
                }
            }
        }

        foreach (var row in rows.Where(x => x.Status is "new" or "update").Where(x => !x.DepartmentId.HasValue))
        {
            row.DepartmentCode = UnidentifiedCatalogCode;
            row.DepartmentName = placeholderDepartment.Name;
            row.DepartmentId = placeholderDepartment.Id;
            row.CreateDepartment = false;
        }

        var placeholderPosition = await db.Positions
            .FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == UnidentifiedCatalogCode);
        if (placeholderPosition is null)
        {
            placeholderPosition = new Position
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                DepartmentId = placeholderDepartment.Id,
                Code = UnidentifiedCatalogCode,
                Name = UnidentifiedCatalogCode,
                Description = "Registro comodín para importación de empleados.",
                PayrollGroup = string.Empty,
                BaseSalary = 0m,
                IsActive = true,
                CreatedBy = "excel-import"
            };
            db.Positions.Add(placeholderPosition);
            await db.SaveChangesAsync();
        }

        var positionCodes = rows
            .Where(x => x.Status is "new" or "update")
            .Where(x => x.CreatePosition && !string.IsNullOrWhiteSpace(x.PositionCode))
            .Select(x => x.PositionCode.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (positionCodes.Count > 0)
        {
            var existingPositions = await db.Positions
                .Where(x => x.CompanyId == company.Id && positionCodes.Contains(x.Code))
                .ToDictionaryAsync(x => x.Code.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);

            foreach (var code in positionCodes.Where(code => !existingPositions.ContainsKey(code)))
            {
                var position = new Position
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    DepartmentId = null,
                    Code = code,
                    Name = code,
                    Description = "Creado automáticamente por importación de empleados.",
                    PayrollGroup = string.Empty,
                    BaseSalary = 0m,
                    IsActive = true,
                    CreatedBy = "excel-import"
                };
                db.Positions.Add(position);
                existingPositions[code] = position;
            }

            await db.SaveChangesAsync();

            foreach (var row in rows.Where(x => x.CreatePosition && !string.IsNullOrWhiteSpace(x.PositionCode)))
            {
                if (existingPositions.TryGetValue(row.PositionCode.Trim().ToUpperInvariant(), out var position))
                {
                    row.PositionId = position.Id;
                    row.PositionName = position.Name;
                    row.CreatePosition = false;
                }
            }
        }

        foreach (var row in rows.Where(x => x.Status is "new" or "update").Where(x => !x.PositionId.HasValue))
        {
            row.PositionCode = UnidentifiedCatalogCode;
            row.PositionName = placeholderPosition.Name;
            row.PositionId = placeholderPosition.Id;
            row.CreatePosition = false;
        }
    }
}

public sealed class ClockImportMappingRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? DeviceCode { get; set; }
    public string? EmployeeNumberColumn { get; set; }
    public string? DateTimeColumn { get; set; }
    public string? DateColumn { get; set; }
    public string? TimeInColumn { get; set; }
    public string? TimeOutColumn { get; set; }
    public string? PunchTypeColumn { get; set; }
    public string? DefaultPunchType { get; set; }
    public string? DateFormat { get; set; }
    public string? TimeFormat { get; set; }
    public string? Delimiter { get; set; }
    public bool IsDefault { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

// ── Fase 8: Prenómina Operativa ─────────────────────────────────────────────

public sealed class OperationalPrePayrollEmployeeRow
{
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public decimal PeriodSalary { get; set; }
    public decimal DailySalary { get; set; }
    // Attendance
    public decimal WorkedHours { get; set; }
    public int DelayMinutes { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal AbsenceUnits { get; set; }
    public int AttendanceDays { get; set; }
    // Incidents
    public int IncidentCount { get; set; }
    public decimal IncidentPerceptionsTotal { get; set; }
    public decimal IncidentDeductionsTotal { get; set; }
    // PrePayroll adjustments
    public int AdjustmentCount { get; set; }
    public decimal AdjustmentPerceptionsTotal { get; set; }
    public decimal AdjustmentDeductionsTotal { get; set; }
}

public sealed class OperationalPrePayrollSummary
{
    public Guid PayrollPeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    public List<OperationalPrePayrollEmployeeRow> Rows { get; set; } = [];
}

using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PayrollMvpEndpoints
{
    public static IEndpointRouteBuilder MapPayrollMvpEndpoints(this IEndpointRouteBuilder app)
    {
        var employees = app.MapGroup("/api/hr/employees").WithTags("HrEmployees");
        employees.MapGet("/import/template", DownloadImportTemplateAsync);
        employees.MapPost("/import/preview", PreviewImportFromExcelAsync).DisableAntiforgery();
        employees.MapPost("/import", ImportEmployeesFromExcelAsync).DisableAntiforgery();

        var clock = app.MapGroup("/api/hr/time-clock").WithTags("PayrollTimeClock");
        clock.MapPost("/import", ImportPunchesFromExcelAsync).DisableAntiforgery();

        var periods = app.MapGroup("/api/payroll/periods").WithTags("PayrollPeriods");
        periods.MapPost("/{periodId:guid}/generate-summaries", GenerateAttendanceDailySummariesAsync);
        periods.MapPost("/{periodId:guid}/generate-incidents", GenerateIncidentsFromSummariesAsync);

        var runs = app.MapGroup("/api/payroll/runs").WithTags("PayrollRuns");
        runs.MapPost("/{runId:guid}/calculate", CalculatePayrollRunAsync);
        runs.MapGet("/{runId:guid}/receipts/{lineId:guid}", GetPayrollReceiptHtmlAsync);

        app.MapPost("/api/payroll/seed-default-concepts", SeedDefaultConceptsAsync).WithTags("PayrollConcepts");

        return app;
    }

    // ──────────────────────────────────────────────────────────────
    // 1. IMPORTAR EMPLEADOS DESDE EXCEL
    // Columnas esperadas: NoEmpleado, Nombre, ApellidoPaterno, ApellidoMaterno,
    //   RFC, CURP, Email, Telefono, FechaIngreso, SalarioDiario, SalarioDiarioIntegrado,
    //   DepartamentoCodigo, PuestoCodigo
    // ──────────────────────────────────────────────────────────────
    private static async Task<IResult> ImportEmployeesFromExcelAsync(IFormFile file, NanchesoftDbContext db)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { message = "El archivo está vacío." });

        var company = await db.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (company is null)
            return Results.BadRequest(new { message = "No existe empresa configurada." });

        var branchId = await db.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet(1);

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var headerRow = ws.Row(1);
        foreach (var cell in headerRow.CellsUsed())
            headers[cell.GetString().Trim()] = cell.Address.ColumnNumber;

        int created = 0, updated = 0, skipped = 0;
        var errors = new List<string>();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        var departments = await db.Departments.Where(x => x.CompanyId == company.Id).ToDictionaryAsync(x => x.Code.ToUpperInvariant(), x => x.Id);
        var positions = await db.Positions.Where(x => x.CompanyId == company.Id).ToDictionaryAsync(x => x.Code.ToUpperInvariant(), x => x.Id);
        var existingEmployees = await db.Employees.Where(x => x.CompanyId == company.Id).ToDictionaryAsync(x => x.EmployeeNumber.ToUpperInvariant());

        for (int row = 2; row <= lastRow; row++)
        {
            try
            {
                var empNum = GetCell(ws, row, headers, "NoEmpleado").Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(empNum))
                {
                    skipped++;
                    continue;
                }

                var firstName = GetCell(ws, row, headers, "Nombre").Trim();
                var lastName = GetCell(ws, row, headers, "ApellidoPaterno").Trim();
                var middleName = GetCell(ws, row, headers, "ApellidoMaterno").Trim();
                var taxId = GetCell(ws, row, headers, "RFC").Trim().ToUpperInvariant();
                var nationalId = GetCell(ws, row, headers, "CURP").Trim().ToUpperInvariant();
                var email = GetCell(ws, row, headers, "Email").Trim();
                var phone = GetCell(ws, row, headers, "Telefono").Trim();
                var hireDateStr = GetCell(ws, row, headers, "FechaIngreso").Trim();
                var salaryStr = GetCell(ws, row, headers, "SalarioDiario").Trim();
                var intSalaryStr = GetCell(ws, row, headers, "SalarioDiarioIntegrado").Trim();
                var deptCode = GetCell(ws, row, headers, "DepartamentoCodigo").Trim().ToUpperInvariant();
                var posCode = GetCell(ws, row, headers, "PuestoCodigo").Trim().ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    errors.Add($"Fila {row}: Nombre o apellido vacíos para No.{empNum}.");
                    continue;
                }

                _ = DateTime.TryParse(hireDateStr, out var hireDate);
                if (hireDate == default) hireDate = DateTime.UtcNow.Date;

                _ = decimal.TryParse(salaryStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var dailySalary);
                _ = decimal.TryParse(intSalaryStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var intDailySalary);
                if (intDailySalary == 0m) intDailySalary = dailySalary;

                Guid? deptId = !string.IsNullOrWhiteSpace(deptCode) && departments.TryGetValue(deptCode, out var did) ? did : null;
                Guid? posId = !string.IsNullOrWhiteSpace(posCode) && positions.TryGetValue(posCode, out var pid) ? pid : null;

                if (existingEmployees.TryGetValue(empNum, out var existing))
                {
                    existing.FirstName = firstName;
                    existing.LastName = lastName;
                    existing.MiddleName = middleName;
                    existing.TaxId = taxId;
                    existing.NationalId = nationalId;
                    existing.Email = email;
                    existing.Phone = phone;
                    existing.HireDate = hireDate;
                    existing.DailySalary = dailySalary;
                    existing.IntegratedDailySalary = intDailySalary;
                    if (deptId.HasValue) existing.DepartmentId = deptId;
                    if (posId.HasValue) existing.PositionId = posId;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = "excel-import";
                    updated++;
                }
                else
                {
                    var code = empNum;
                    if (await db.Employees.AnyAsync(x => x.CompanyId == company.Id && x.Code == code))
                        code = $"{code}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

                    db.Employees.Add(new Employee
                    {
                        TenantId = company.TenantId,
                        CompanyId = company.Id,
                        BranchId = branchId,
                        DepartmentId = deptId,
                        PositionId = posId,
                        Code = code,
                        EmployeeNumber = empNum,
                        FirstName = firstName,
                        LastName = lastName,
                        MiddleName = middleName,
                        TaxId = taxId,
                        NationalId = nationalId,
                        Email = email,
                        Phone = phone,
                        HireDate = hireDate,
                        DailySalary = dailySalary,
                        IntegratedDailySalary = intDailySalary,
                        Status = "active",
                        IsActive = true,
                        CreatedBy = "excel-import"
                    });
                    created++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Fila {row}: {ex.Message}");
            }
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, created, updated, skipped, errors });
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
                    db.AttendancePunches.Add(BuildPunch(company, branchId, employee.Id, punchDt, tipo == "exit" ? "exit" : "entry"));
                    created++;
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
                        db.AttendancePunches.Add(BuildPunch(company, branchId, employee.Id, entryDt, "entry"));
                        created++;
                    }

                    if (!string.IsNullOrWhiteSpace(salidaStr) && TimeSpan.TryParse(salidaStr, out var salida))
                    {
                        var exitDt = fecha.Date.Add(salida);
                        db.AttendancePunches.Add(BuildPunch(company, branchId, employee.Id, exitDt, "exit"));
                        created++;
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Fila {row}: {ex.Message}");
            }
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, created, skipped, errors });
    }

    private static AttendancePunch BuildPunch(Company company, Guid? branchId, Guid employeeId, DateTime punchDt, string punchType)
        => new()
        {
            TenantId = company.TenantId,
            CompanyId = company.Id,
            BranchId = branchId,
            EmployeeId = employeeId,
            WorkDate = punchDt.Date,
            PunchDateTime = punchDt,
            PunchType = punchType,
            Source = "excel-import",
            Status = "captured",
            IsActive = true,
            CreatedBy = "excel-import"
        };

    // ──────────────────────────────────────────────────────────────
    // 3. GENERAR RESUMEN DIARIO DE ASISTENCIA POR PERIODO
    // Procesa todos los empleados activos y agrupa sus checadas
    // Hora entrada programada por defecto: 09:00, salida: 18:00
    // ──────────────────────────────────────────────────────────────
    private static async Task<IResult> GenerateAttendanceDailySummariesAsync(Guid periodId, NanchesoftDbContext db)
    {
        var period = await db.PayrollPeriods.FirstOrDefaultAsync(x => x.Id == periodId);
        if (period is null)
            return Results.NotFound(new { message = "No se encontró el periodo." });

        var company = await db.Companies.Where(x => x.Id == period.CompanyId).FirstOrDefaultAsync();
        if (company is null)
            return Results.BadRequest(new { message = "No existe empresa para el periodo." });

        var branchId = await db.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

        var employees = await db.Employees.Where(x => x.CompanyId == company.Id && x.IsActive && x.Status == "active").ToListAsync();

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
        int created = 0;

        foreach (var employee in employees)
        {
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                punchesByEmpDate.TryGetValue((employee.Id, date), out var dayPunches);

                var entryPunch = dayPunches?.Where(x => x.PunchType == "entry").OrderBy(x => x.PunchDateTime).FirstOrDefault()
                    ?? dayPunches?.OrderBy(x => x.PunchDateTime).FirstOrDefault();
                var exitPunch = dayPunches?.Where(x => x.PunchType == "exit").OrderByDescending(x => x.PunchDateTime).FirstOrDefault()
                    ?? (dayPunches?.Count > 1 ? dayPunches.OrderByDescending(x => x.PunchDateTime).First() : null);

                decimal workedHours = 0m;
                int delayMinutes = 0;
                int earlyLeaveMinutes = 0;
                decimal overtimeHours = 0m;
                decimal absenceUnits = 0m;

                var scheduledEntry = date.Add(defaultEntry);
                var scheduledExit = date.Add(defaultExit);

                if (entryPunch is not null)
                {
                    var actualEntry = entryPunch.PunchDateTime;
                    var actualExit = exitPunch?.PunchDateTime ?? actualEntry;

                    if (exitPunch is not null && exitPunch.PunchDateTime > actualEntry)
                    {
                        workedHours = (decimal)(exitPunch.PunchDateTime - actualEntry).TotalHours;
                        if (workedHours < 4m)
                            absenceUnits = 1m;
                        else if (workedHours < 6m)
                            absenceUnits = 0.5m;
                        else
                            absenceUnits = 0m;

                        overtimeHours = workedHours > 8m ? workedHours - 8m : 0m;

                        var delayTs = actualEntry - scheduledEntry;
                        if (delayTs.TotalMinutes > 5) delayMinutes = (int)delayTs.TotalMinutes;

                        var earlyTs = scheduledExit - actualExit;
                        if (earlyTs.TotalMinutes > 5 && actualExit < scheduledExit) earlyLeaveMinutes = (int)earlyTs.TotalMinutes;
                    }
                    else
                    {
                        absenceUnits = 0.5m;
                        workedHours = 0m;
                    }
                }
                else
                {
                    absenceUnits = 1m;
                    workedHours = 0m;
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
                    DayType = "workday",
                    Status = "calculated",
                    Source = dayPunches is not null ? "time-clock" : "no-punch",
                    Notes = dayPunches is null ? "Sin checadas" : string.Empty,
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
    private static async Task<IResult> GenerateIncidentsFromSummariesAsync(Guid periodId, NanchesoftDbContext db)
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
            .Where(x => x.PayrollPeriodId == periodId && x.Notes == "auto-generado")
            .ToListAsync();
        db.EmployeeIncidents.RemoveRange(autoIncidents);

        int created = 0;
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
                    EmployeeId = empId,
                    PayrollPeriodId = periodId,
                    IncidentDate = period.StartDate,
                    IncidentType = "falta",
                    Quantity = absenceSum,
                    Amount = employee.DailySalary * absenceSum,
                    Status = "draft",
                    Notes = "auto-generado",
                    IsActive = true,
                    CreatedBy = "mvp-engine"
                });
                created++;
            }

            if (delaySum > 15)
            {
                var delayHours = delaySum / 60m;
                db.EmployeeIncidents.Add(new EmployeeIncident
                {
                    TenantId = period.TenantId,
                    CompanyId = period.CompanyId,
                    EmployeeId = empId,
                    PayrollPeriodId = periodId,
                    IncidentDate = period.StartDate,
                    IncidentType = "retardo",
                    Quantity = Math.Round(delayHours, 2),
                    Amount = Math.Round(employee.DailySalary / 8m * delayHours, 2),
                    Status = "draft",
                    Notes = "auto-generado",
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
                    EmployeeId = empId,
                    PayrollPeriodId = periodId,
                    IncidentDate = period.StartDate,
                    IncidentType = "hora_extra",
                    Quantity = Math.Round(overtimeSum, 2),
                    Amount = Math.Round(employee.DailySalary / 8m * 1.5m * overtimeSum, 2),
                    Status = "draft",
                    Notes = "auto-generado",
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

        if (run.Status == "closed")
            return Results.BadRequest(new { message = "La corrida ya está cerrada." });

        var period = run.PayrollPeriod!;
        int periodDays = (period.EndDate.Date - period.StartDate.Date).Days + 1;

        var employees = await db.Employees
            .Where(x => x.CompanyId == run.CompanyId && x.IsActive && x.Status == "active")
            .ToListAsync();

        if (employees.Count == 0)
            return Results.BadRequest(new { message = "No hay empleados activos para calcular." });

        var concepts = await db.PayrollConcepts.Where(x => x.CompanyId == run.CompanyId && x.IsActive).ToListAsync();
        var conceptSal   = concepts.FirstOrDefault(x => x.Code == "SAL")
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
                         ?? concepts.FirstOrDefault(x => x.Code == "PRES")
                         ?? conceptIsr;

        if (conceptSal is null)
            return Results.BadRequest(new { message = "No existe concepto de percepción (SAL) configurado. Use POST /api/payroll/concepts/seed-defaults para crearlos." });

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

        var incidentsByEmployee = await db.EmployeeIncidents
            .Where(x => x.CompanyId == run.CompanyId && x.PayrollPeriodId == run.PayrollPeriodId && employeeIds.Contains(x.EmployeeId) && x.IsActive)
            .GroupBy(x => x.EmployeeId)
            .ToDictionaryAsync(g => g.Key, g => g.ToList());

        var loansByEmployee = await db.EmployeeLoans
            .Where(x => x.CompanyId == run.CompanyId && employeeIds.Contains(x.EmployeeId) && x.Status == "active" && x.IsActive && x.BalanceAmount > 0m)
            .ToListAsync();
        var loansGrouped = loansByEmployee.GroupBy(x => x.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        var loanDeductionsToCreate = new List<EmployeeLoanDeduction>();
        var createdLines = 0;

        // Pre-load installments already applied outside this run to avoid unique key conflicts
        var paidInstallmentKeys = await db.EmployeeLoanDeductions
            .Where(x => x.CompanyId == run.CompanyId && x.PayrollRunId != runId)
            .Select(x => x.EmployeeLoanId.ToString() + ":" + x.InstallmentNumber.ToString())
            .ToListAsync();
        var paidInstallmentSet = paidInstallmentKeys.ToHashSet();

        foreach (var employee in employees)
        {
            summaryByEmployee.TryGetValue(employee.Id, out var summary);
            var absenceDays = summary?.TotalAbsenceUnits ?? 0m;
            var daysPaid = Math.Max(0m, periodDays - absenceDays);

            var baseSalary = Math.Round(employee.DailySalary * daysPaid, 2);

            // ── Incidencias ──
            decimal extraPerceptions = 0m;
            decimal extraDeductions = 0m;

            incidentsByEmployee.TryGetValue(employee.Id, out var incidents);
            if (incidents is not null)
            {
                foreach (var inc in incidents)
                {
                    switch (inc.IncidentType.ToLowerInvariant())
                    {
                        case "hora_extra":
                            extraPerceptions += Math.Round(employee.DailySalary / 8m * 1.5m * inc.Quantity, 2);
                            break;
                        case "bono":
                        case "percepcion":
                            extraPerceptions += Math.Round(inc.Amount > 0 ? inc.Amount : employee.DailySalary * inc.Quantity, 2);
                            break;
                        case "falta":
                            extraDeductions += Math.Round(employee.DailySalary * inc.Quantity, 2);
                            break;
                        case "retardo":
                            extraDeductions += Math.Round(employee.DailySalary / 8m * inc.Quantity, 2);
                            break;
                        case "deduccion":
                        case "descuento":
                            extraDeductions += Math.Round(inc.Amount > 0 ? inc.Amount : employee.DailySalary * inc.Quantity, 2);
                            break;
                    }
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
                    // Skip if this installment was already applied in another run (avoids unique key violation)
                    if (paidInstallmentSet.Contains(loan.Id.ToString() + ":" + nextInstallmentNo.ToString()))
                    {
                        // Advance counter without creating a deduction (cross-run consistency)
                        continue;
                    }

                    loanDeductionTotal += installment;
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
                    loan.InstallmentsPaid++;
                    if (loan.BalanceAmount <= 0m) loan.Status = "paid";
                    loan.UpdatedAt = DateTime.UtcNow;
                    loan.UpdatedBy = "mvp-engine";
                }
            }

            // ── ISR 2024 (Art. 96 LISR + Subsidio al Empleo) ──
            decimal grossTaxable = baseSalary + extraPerceptions;
            var (netIsrPeriod, subsidioPerception) = CalculateIsrAndSubsidio(grossTaxable, periodDays);

            // ── IMSS cuota obrera ──
            var sbc = employee.SbcFija > 0
                ? employee.SbcFija
                : (employee.IntegratedDailySalary > 0 ? employee.IntegratedDailySalary : employee.DailySalary);
            var imssAmount = conceptImss is not null ? CalculateImssCuotaObrera(sbc, periodDays) : 0m;

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
            if (baseSalary > 0m)
            {
                db.PayrollRunLineDetails.Add(BuildDetail(run, line, employee, conceptSal, daysPaid, baseSalary, sortOrder));
                sortOrder += 10;
            }

            if (extraPerceptions > 0m && conceptBon is not null)
            {
                db.PayrollRunLineDetails.Add(BuildDetail(run, line, employee, conceptBon, 1m, extraPerceptions, sortOrder));
                sortOrder += 10;
            }

            if (subsidioPerception > 0m && conceptSubse is not null)
            {
                db.PayrollRunLineDetails.Add(BuildDetail(run, line, employee, conceptSubse, 1m, subsidioPerception, sortOrder, isDeduction: false));
                sortOrder += 10;
            }

            // Deducciones
            if (extraDeductions > 0m && conceptDescto is not null)
            {
                db.PayrollRunLineDetails.Add(BuildDetail(run, line, employee, conceptDescto, 1m, extraDeductions, 85, isDeduction: true));
            }

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
                    var loanConcept = concepts.FirstOrDefault(x => x.Id == loan.PayrollConceptId) ?? conceptIsr;
                    if (loanConcept is null) continue;
                    var installment = loanDeductionsToCreate.FirstOrDefault(x => x.EmployeeLoanId == loan.Id)?.Amount ?? 0m;
                    if (installment > 0m)
                        db.PayrollRunLineDetails.Add(BuildDetail(run, line, employee, loanConcept, 1m, installment, 100, isDeduction: true));
                }
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
    private static async Task<IResult> SeedDefaultConceptsAsync(NanchesoftDbContext db)
    {
        var company = await db.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (company is null)
            return Results.BadRequest(new { message = "No existe empresa configurada." });

        var seed = new[]
        {
            new { Code = "SAL",   Name = "Sueldo",               ConceptType = "perception", CalculationType = "days_salary",   SatCode = "P-001", SatAgrupador = "HorasExtra",  TaxableType = "taxable",     TaxablePercent = 100m, ExemptPercent = 0m,   IsRecurring = true,  IsAutomatic = true,  SortOrder = 10 },
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
            if (await db.PayrollConcepts.AnyAsync(x => x.CompanyId == company.Id && x.Code == s.Code))
            {
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
        return Results.Ok(new { success = true, created, skipped });
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

    private static (decimal Taxable, decimal Exempt) SplitTaxable(string taxableType, decimal amount) =>
        taxableType?.ToLowerInvariant() switch
        {
            "exempt" => (0m, amount),
            "mixed" => (Math.Round(amount * 0.5m, 2), Math.Round(amount * 0.5m, 2)),
            _ => (amount, 0m)
        };

    private static string GetCell(IXLWorksheet ws, int row, Dictionary<string, int> headers, string name)
    {
        if (!headers.TryGetValue(name, out var col)) return string.Empty;
        var cell = ws.Cell(row, col);
        return cell.IsEmpty() ? string.Empty : cell.GetString();
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
            "SalarioDiario", "SalarioDiarioIntegrado",
            "DepartamentoCodigo", "PuestoCodigo",
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
        ws.Cell(2, 16).Value = 300.00;
        ws.Cell(2, 17).Value = 320.00;
        ws.Cell(2, 18).Value = "PROD";
        ws.Cell(2, 19).Value = "OPER";
        ws.Cell(2, 20).Value = "indefinite";
        ws.Cell(2, 21).Value = "semanal";
        ws.Cell(2, 22).Value = "001";
        ws.Cell(2, 23).Value = "";
        ws.Cell(2, 24).Value = "Av. Principal 123";
        ws.Cell(2, 25).Value = "Centro";
        ws.Cell(2, 26).Value = "Monterrey";
        ws.Cell(2, 27).Value = "NL";
        ws.Cell(2, 28).Value = "64000";
        ws.Cell(2, 29).Value = "BBVA";
        ws.Cell(2, 30).Value = "";
        ws.Cell(2, 31).Value = "";
        ws.Cell(2, 32).Value = "Y1234567890";
        ws.Cell(2, 33).Value = "A";

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
    private static async Task<IResult> PreviewImportFromExcelAsync(IFormFile file, NanchesoftDbContext db)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { message = "El archivo está vacío." });

        var company = await db.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (company is null)
            return Results.BadRequest(new { message = "No existe empresa configurada." });

        var departments = await db.Departments.Where(x => x.CompanyId == company.Id)
            .ToDictionaryAsync(x => x.Code.ToUpperInvariant(), x => x.Name);
        var positions = await db.Positions.Where(x => x.CompanyId == company.Id)
            .ToDictionaryAsync(x => x.Code.ToUpperInvariant(), x => x.Name);
        var existingNumbers = await db.Employees.Where(x => x.CompanyId == company.Id)
            .Select(x => x.EmployeeNumber.ToUpperInvariant())
            .ToHashSetAsync();

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet(1);

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in ws.Row(1).CellsUsed())
            headers[cell.GetString().Trim()] = cell.Address.ColumnNumber;

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        var rows = new List<EmployeeImportPreviewRow>();

        for (int r = 2; r <= lastRow; r++)
        {
            var empNum = GetCell(ws, r, headers, "NoEmpleado").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(empNum))
            {
                rows.Add(new EmployeeImportPreviewRow { RowNumber = r, Status = "skip", Message = "Sin número de empleado" });
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
            var salaryStr = GetCell(ws, r, headers, "SalarioDiario").Trim();
            var deptCode = GetCell(ws, r, headers, "DepartamentoCodigo").Trim().ToUpperInvariant();
            var posCode = GetCell(ws, r, headers, "PuestoCodigo").Trim().ToUpperInvariant();

            var rowErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(firstName)) rowErrors.Add("Nombre vacío");
            if (string.IsNullOrWhiteSpace(lastName)) rowErrors.Add("Apellido paterno vacío");

            _ = DateTime.TryParse(hireDateStr, out var hireDate);
            if (hireDate == default && !string.IsNullOrWhiteSpace(hireDateStr))
                rowErrors.Add($"Fecha ingreso inválida: '{hireDateStr}'");

            _ = decimal.TryParse(salaryStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var salary);

            var deptName = !string.IsNullOrWhiteSpace(deptCode) && departments.TryGetValue(deptCode, out var dn) ? dn : "";
            if (!string.IsNullOrWhiteSpace(deptCode) && string.IsNullOrWhiteSpace(deptName))
                rowErrors.Add($"Departamento '{deptCode}' no encontrado");

            var posName = !string.IsNullOrWhiteSpace(posCode) && positions.TryGetValue(posCode, out var pn) ? pn : "";
            if (!string.IsNullOrWhiteSpace(posCode) && string.IsNullOrWhiteSpace(posName))
                rowErrors.Add($"Puesto '{posCode}' no encontrado");

            var isUpdate = existingNumbers.Contains(empNum);
            var status = rowErrors.Count > 0 ? "error" : isUpdate ? "update" : "new";

            rows.Add(new EmployeeImportPreviewRow
            {
                RowNumber = r,
                EmployeeNumber = empNum,
                FullName = $"{firstName} {lastName} {secondLastName}".Trim(),
                TaxId = taxId,
                Curp = curp,
                Nss = nss,
                Email = email,
                Phone = phone,
                HireDate = hireDate == default ? null : hireDate.Date,
                DailySalary = salary,
                DepartmentCode = deptCode,
                DepartmentName = deptName,
                PositionCode = posCode,
                PositionName = posName,
                Status = status,
                Message = rowErrors.Count > 0 ? string.Join("; ", rowErrors) : isUpdate ? "Actualización" : "Nuevo"
            });
        }

        var summary = new
        {
            total = rows.Count,
            newCount = rows.Count(x => x.Status == "new"),
            updateCount = rows.Count(x => x.Status == "update"),
            errorCount = rows.Count(x => x.Status == "error"),
            skipCount = rows.Count(x => x.Status == "skip"),
            rows
        };

        return Results.Ok(summary);
    }

    public sealed class EmployeeImportPreviewRow
    {
        public int RowNumber { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string Curp { get; set; } = string.Empty;
        public string Nss { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime? HireDate { get; set; }
        public decimal DailySalary { get; set; }
        public string DepartmentCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string PositionCode { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}

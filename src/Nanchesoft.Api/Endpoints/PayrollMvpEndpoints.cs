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
        employees.MapPost("/import", ImportEmployeesFromExcelAsync).DisableAntiforgery();

        var clock = app.MapGroup("/api/hr/time-clock").WithTags("PayrollTimeClock");
        clock.MapPost("/import", ImportPunchesFromExcelAsync).DisableAntiforgery();

        var periods = app.MapGroup("/api/payroll/periods").WithTags("PayrollPeriods");
        periods.MapPost("/{periodId:guid}/generate-summaries", GenerateAttendanceDailySummariesAsync);
        periods.MapPost("/{periodId:guid}/generate-incidents", GenerateIncidentsFromSummariesAsync);

        var runs = app.MapGroup("/api/payroll/runs").WithTags("PayrollRuns");
        runs.MapPost("/{runId:guid}/calculate", CalculatePayrollRunAsync);

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
        var conceptSal = concepts.FirstOrDefault(x => x.Code == "SAL") ?? concepts.FirstOrDefault(x => x.ConceptType == "perception");
        var conceptBon = concepts.FirstOrDefault(x => x.Code == "BON") ?? concepts.FirstOrDefault(x => x.ConceptType == "perception" && x.Code != (conceptSal?.Code ?? ""));
        var conceptIsr = concepts.FirstOrDefault(x => x.Code == "ISR") ?? concepts.FirstOrDefault(x => x.ConceptType == "deduction");

        if (conceptSal is null)
            return Results.BadRequest(new { message = "No existe concepto de percepción (SAL) configurado." });

        if (conceptIsr is null)
            return Results.BadRequest(new { message = "No existe concepto de deducción (ISR) configurado." });

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
                        InstallmentNumber = loan.InstallmentsPaid + 1,
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

            decimal grossAmount = baseSalary + extraPerceptions;
            decimal deductionsAmount = extraDeductions + loanDeductionTotal;
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

            if (extraDeductions > 0m)
            {
                db.PayrollRunLineDetails.Add(BuildDetail(run, line, employee, conceptIsr, 1m, extraDeductions, 90, isDeduction: true));
            }

            if (loanDeductionTotal > 0m && activeLoans is not null)
            {
                foreach (var loan in activeLoans)
                {
                    var loanConcept = concepts.FirstOrDefault(x => x.Id == loan.PayrollConceptId) ?? conceptIsr;
                    var installment = Math.Min(loan.InstallmentAmount, loan.BalanceAmount + Math.Min(loan.InstallmentAmount, loan.BalanceAmount));
                    installment = loanDeductionsToCreate.FirstOrDefault(x => x.EmployeeLoanId == loan.Id)?.Amount ?? 0m;
                    if (installment > 0m)
                        db.PayrollRunLineDetails.Add(BuildDetail(run, line, employee, loanConcept, 1m, installment, 95, isDeduction: true));
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
}

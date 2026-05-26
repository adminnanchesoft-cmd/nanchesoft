using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PayrollReportEndpoints
{
    public static IEndpointRouteBuilder MapPayrollReportEndpoints(this IEndpointRouteBuilder app)
    {
        var reports = app.MapGroup("/api/payroll/reports").WithTags("PayrollReports");

        reports.MapGet("/attendance", GetAttendanceReportAsync);
        reports.MapGet("/lateness", GetLatenessReportAsync);
        reports.MapGet("/overtime", GetOvertimeReportAsync);

        return app;
    }

    // ── Reporte 1: Asistencia por periodo ─────────────────────────────────

    private static async Task<IResult> GetAttendanceReportAsync(
        HttpContext httpContext,
        NanchesoftDbContext db,
        [FromQuery] Guid? periodId = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? departmentId = null)
    {
        var tenantId  = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var query = db.AttendanceDailySummaries.AsNoTracking()
            .Where(x => (!tenantId.HasValue  || x.TenantId  == tenantId.Value)
                     && (!companyId.HasValue || x.CompanyId == companyId.Value)
                     && x.IsActive);

        if (periodId.HasValue)   query = query.Where(x => x.PayrollPeriodId == periodId.Value);
        if (employeeId.HasValue) query = query.Where(x => x.EmployeeId == employeeId.Value);

        var rows = await query
            .GroupBy(x => x.EmployeeId)
            .Select(g => new
            {
                EmployeeId       = g.Key,
                AttendanceDays   = g.Count(),
                WorkedHours      = g.Sum(x => x.WorkedHours),
                DelayMinutes     = g.Sum(x => x.DelayMinutes),
                OvertimeHours    = g.Sum(x => x.OvertimeHours),
                AbsenceUnits     = g.Sum(x => x.AbsenceUnits),
                EarlyLeaveMinutes = g.Sum(x => x.EarlyLeaveMinutes)
            })
            .ToListAsync();

        var empIds = rows.Select(r => r.EmployeeId).ToList();
        var employees = await db.Employees.AsNoTracking()
            .Where(x => empIds.Contains(x.Id))
            .Where(x => !departmentId.HasValue || x.DepartmentId == departmentId.Value)
            .Select(x => new
            {
                x.Id, x.EmployeeNumber,
                FullName = (x.FirstName + " " + x.LastName).Trim(),
                DeptId = x.DepartmentId
            })
            .ToListAsync();

        var deptIds = employees.Where(e => e.DeptId.HasValue).Select(e => e.DeptId!.Value).Distinct().ToList();
        var depts = await db.Departments.AsNoTracking()
            .Where(x => deptIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        var empMap = employees.ToDictionary(e => e.Id);

        var result = rows
            .Where(r => empMap.ContainsKey(r.EmployeeId))
            .Select(r =>
            {
                var emp = empMap[r.EmployeeId];
                return new AttendanceReportRow
                {
                    EmployeeId      = emp.Id,
                    EmployeeNumber  = emp.EmployeeNumber ?? string.Empty,
                    EmployeeName    = emp.FullName,
                    DepartmentName  = emp.DeptId.HasValue && depts.TryGetValue(emp.DeptId.Value, out var dn) ? dn : string.Empty,
                    AttendanceDays  = r.AttendanceDays,
                    WorkedHours     = Math.Round(r.WorkedHours, 2),
                    DelayMinutes    = r.DelayMinutes,
                    OvertimeHours   = Math.Round(r.OvertimeHours, 2),
                    AbsenceUnits    = Math.Round(r.AbsenceUnits, 2),
                    EarlyLeaveMinutes = r.EarlyLeaveMinutes
                };
            })
            .OrderBy(r => r.DepartmentName).ThenBy(r => r.EmployeeNumber)
            .ToList();

        return Results.Ok(result);
    }

    // ── Reporte 2: Retardos y faltas (detalle diario) ─────────────────────

    private static async Task<IResult> GetLatenessReportAsync(
        HttpContext httpContext,
        NanchesoftDbContext db,
        [FromQuery] Guid? periodId = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var tenantId  = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var query = db.AttendanceDailySummaries.AsNoTracking()
            .Where(x => (!tenantId.HasValue  || x.TenantId  == tenantId.Value)
                     && (!companyId.HasValue || x.CompanyId == companyId.Value)
                     && x.IsActive
                     && (x.DelayMinutes > 0 || x.AbsenceUnits > 0 || x.EarlyLeaveMinutes > 0));

        if (periodId.HasValue)   query = query.Where(x => x.PayrollPeriodId == periodId.Value);
        if (employeeId.HasValue) query = query.Where(x => x.EmployeeId == employeeId.Value);
        if (from.HasValue) query = query.Where(x => x.WorkDate >= from.Value.Date);
        if (to.HasValue)   query = query.Where(x => x.WorkDate <= to.Value.Date);

        var rows = await query
            .OrderBy(x => x.EmployeeId).ThenBy(x => x.WorkDate)
            .Select(x => new { x.EmployeeId, x.WorkDate, x.DelayMinutes, x.AbsenceUnits, x.EarlyLeaveMinutes, x.Notes })
            .ToListAsync();

        var empIds = rows.Select(r => r.EmployeeId).Distinct().ToList();
        var employees = await db.Employees.AsNoTracking()
            .Where(x => empIds.Contains(x.Id))
            .Where(x => !departmentId.HasValue || x.DepartmentId == departmentId.Value)
            .Select(x => new { x.Id, x.EmployeeNumber, FullName = (x.FirstName + " " + x.LastName).Trim(), DeptId = x.DepartmentId })
            .ToDictionaryAsync(x => x.Id);

        var deptIds = employees.Values.Where(e => e.DeptId.HasValue).Select(e => e.DeptId!.Value).Distinct().ToList();
        var depts = await db.Departments.AsNoTracking()
            .Where(x => deptIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        var result = rows
            .Where(r => employees.ContainsKey(r.EmployeeId))
            .Select(r =>
            {
                var emp = employees[r.EmployeeId];
                return new LatenessReportRow
                {
                    EmployeeId      = emp.Id,
                    EmployeeNumber  = emp.EmployeeNumber ?? string.Empty,
                    EmployeeName    = emp.FullName,
                    DepartmentName  = emp.DeptId.HasValue && depts.TryGetValue(emp.DeptId.Value, out var dn) ? dn : string.Empty,
                    WorkDate        = r.WorkDate,
                    DelayMinutes    = r.DelayMinutes,
                    AbsenceUnits    = Math.Round(r.AbsenceUnits, 2),
                    EarlyLeaveMinutes = r.EarlyLeaveMinutes,
                    Notes           = r.Notes
                };
            })
            .OrderBy(r => r.EmployeeNumber).ThenBy(r => r.WorkDate)
            .ToList();

        return Results.Ok(result);
    }

    // ── Reporte 3: Horas extra ────────────────────────��───────────────────

    private static async Task<IResult> GetOvertimeReportAsync(
        HttpContext httpContext,
        NanchesoftDbContext db,
        [FromQuery] Guid? periodId = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? departmentId = null)
    {
        var tenantId  = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var query = db.AttendanceDailySummaries.AsNoTracking()
            .Where(x => (!tenantId.HasValue  || x.TenantId  == tenantId.Value)
                     && (!companyId.HasValue || x.CompanyId == companyId.Value)
                     && x.IsActive && x.OvertimeHours > 0);

        if (periodId.HasValue)   query = query.Where(x => x.PayrollPeriodId == periodId.Value);
        if (employeeId.HasValue) query = query.Where(x => x.EmployeeId == employeeId.Value);

        var rows = await query
            .GroupBy(x => x.EmployeeId)
            .Select(g => new
            {
                EmployeeId    = g.Key,
                OvertimeDays  = g.Count(),
                TotalHours    = g.Sum(x => x.OvertimeHours)
            })
            .ToListAsync();

        var empIds = rows.Select(r => r.EmployeeId).ToList();
        var employees = await db.Employees.AsNoTracking()
            .Where(x => empIds.Contains(x.Id))
            .Where(x => !departmentId.HasValue || x.DepartmentId == departmentId.Value)
            .Select(x => new { x.Id, x.EmployeeNumber, FullName = (x.FirstName + " " + x.LastName).Trim(), DeptId = x.DepartmentId, x.DailySalary })
            .ToListAsync();

        var deptIds = employees.Where(e => e.DeptId.HasValue).Select(e => e.DeptId!.Value).Distinct().ToList();
        var depts = await db.Departments.AsNoTracking()
            .Where(x => deptIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        var empMap = employees.ToDictionary(e => e.Id);

        var result = rows
            .Where(r => empMap.ContainsKey(r.EmployeeId))
            .Select(r =>
            {
                var emp = empMap[r.EmployeeId];
                var hourlyRate = emp.DailySalary / 8m;
                var estimatedAmount = Math.Round(hourlyRate * (decimal)r.TotalHours * 2m, 2); // HE dobles
                return new OvertimeReportRow
                {
                    EmployeeId      = emp.Id,
                    EmployeeNumber  = emp.EmployeeNumber ?? string.Empty,
                    EmployeeName    = emp.FullName,
                    DepartmentName  = emp.DeptId.HasValue && depts.TryGetValue(emp.DeptId.Value, out var dn) ? dn : string.Empty,
                    OvertimeDays    = r.OvertimeDays,
                    TotalOvertimeHours = Math.Round((decimal)r.TotalHours, 2),
                    HourlyRate      = Math.Round(hourlyRate, 4),
                    EstimatedAmount = estimatedAmount
                };
            })
            .OrderBy(r => r.DepartmentName).ThenBy(r => r.EmployeeNumber)
            .ToList();

        return Results.Ok(result);
    }
}

// ── DTOs ────────���────────────────────────────���───────────────────────────────

public sealed class AttendanceReportRow
{
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int AttendanceDays { get; set; }
    public decimal WorkedHours { get; set; }
    public int DelayMinutes { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal AbsenceUnits { get; set; }
    public int EarlyLeaveMinutes { get; set; }
}

public sealed class LatenessReportRow
{
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public int DelayMinutes { get; set; }
    public decimal AbsenceUnits { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class OvertimeReportRow
{
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int OvertimeDays { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal EstimatedAmount { get; set; }
}

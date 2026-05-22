using Microsoft.EntityFrameworkCore;
using Nanchesoft.Api.Ai.Services;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Ai.Tools;

public sealed class PayrollSummaryRow
{
    public Guid PeriodId { get; set; }
    public string PeriodCode { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal Gross { get; set; }
    public decimal Deductions { get; set; }
    public decimal Net { get; set; }
    public bool HasRuns { get; set; }
}

public sealed class EmployeeIncidentsRow
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int IncidentCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime LastIncidentDate { get; set; }
}

public sealed class EmployeeStatusRow
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public decimal PeriodSalary { get; set; }
}

public sealed class EmployeeLoanRow
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string LoanNumber { get; set; } = string.Empty;
    public decimal PrincipalAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public int InstallmentsLeft { get; set; }
    public DateTime LoanDate { get; set; }
}

public sealed class DepartmentCostRow
{
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal TotalSalary { get; set; }
}

public sealed class ConceptTotalRow
{
    public Guid PayrollConceptId { get; set; }
    public string ConceptCode { get; set; } = string.Empty;
    public string ConceptName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int IncidentCount { get; set; }
    public int EmployeeCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalQuantity { get; set; }
}

public sealed class MissingDataRow
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public sealed class PayrollSummaryResult
{
    public bool NotFound { get; set; }
    public PayrollSummaryRow? Period { get; set; }
    public List<DepartmentCostRow> ByDepartment { get; set; } = new();
}

public sealed class EmployeeIncidentsResult
{
    public Guid? PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public int TotalIncidents { get; set; }
    public int EmployeesWithIncidents { get; set; }
    public List<EmployeeIncidentsRow> Rows { get; set; } = new();
}

public sealed class TodayIncidentsResult
{
    public DateTime ReferenceDate { get; set; }
    public int Total { get; set; }
    public List<EmployeeIncidentsRow> Rows { get; set; } = new();
}

public sealed class ConceptTotalsResult
{
    public string Query { get; set; } = string.Empty;
    public Guid? PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public List<ConceptTotalRow> Rows { get; set; } = new();
    public decimal Total { get; set; }
}

public static class PayrollHrTools
{
    private static IQueryable<T> ApplyScope<T>(IQueryable<T> q, AiScope scope, Func<T, Guid> tenantSelector)
        => scope.IsPlatformOwner || !scope.TenantId.HasValue ? q : q.Where(x => tenantSelector(x) == scope.TenantId!.Value);

    private static async Task<Guid?> ResolveOpenPeriodIdAsync(NanchesoftDbContext db, AiScope scope)
    {
        var query = db.PayrollPeriods.AsNoTracking().Where(x => !x.IsClosed);
        if (!scope.IsPlatformOwner)
        {
            if (scope.TenantId.HasValue) query = query.Where(x => x.TenantId == scope.TenantId.Value);
            if (scope.CompanyId.HasValue) query = query.Where(x => x.CompanyId == scope.CompanyId.Value);
        }

        return await query
            .OrderByDescending(x => x.EndDate)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();
    }

    public static async Task<PayrollSummaryResult> GetPayrollSummaryAsync(NanchesoftDbContext db, AiScope scope)
    {
        var query = db.PayrollPeriods.AsNoTracking().Where(x => !x.IsClosed);
        if (!scope.IsPlatformOwner)
        {
            if (scope.TenantId.HasValue) query = query.Where(x => x.TenantId == scope.TenantId.Value);
            if (scope.CompanyId.HasValue) query = query.Where(x => x.CompanyId == scope.CompanyId.Value);
        }

        var period = await query
            .OrderByDescending(x => x.EndDate)
            .Select(x => new { x.Id, x.Code, x.Name, x.StartDate, x.EndDate, x.PaymentDate, x.Status })
            .FirstOrDefaultAsync();

        if (period is null) return new PayrollSummaryResult { NotFound = true };

        var adjustments = await db.PrePayrollAdjustments.AsNoTracking()
            .Where(x => x.PayrollPeriodId == period.Id)
            .GroupBy(x => 1)
            .Select(g => new
            {
                Perceptions = g.Where(x => x.AdjustmentType == "perception").Sum(x => x.Amount),
                Deductions = g.Where(x => x.AdjustmentType == "deduction").Sum(x => x.Amount),
                EmployeeCount = g.Select(x => x.EmployeeId).Distinct().Count()
            })
            .FirstOrDefaultAsync();

        var runs = await db.PayrollRuns.AsNoTracking()
            .Where(x => x.PayrollPeriodId == period.Id)
            .GroupBy(x => 1)
            .Select(g => new
            {
                Runs = g.Count(),
                Gross = g.Sum(x => x.GrossAmount),
                Deductions = g.Sum(x => x.DeductionsAmount),
                Net = g.Sum(x => x.NetAmount),
                Employees = g.Sum(x => x.EmployeeCount)
            })
            .FirstOrDefaultAsync();

        var perceptions = adjustments?.Perceptions ?? 0m;
        var deductions = adjustments?.Deductions ?? 0m;
        var employeeCount = runs is not null && runs.Employees > 0 ? runs.Employees : (adjustments?.EmployeeCount ?? 0);
        var grossAmount = runs?.Gross ?? perceptions;
        var deductionsAmount = runs?.Deductions ?? deductions;
        var netAmount = runs?.Net ?? (perceptions - deductions);

        // Department breakdown from active employees in this company
        var empQuery = db.Employees.AsNoTracking().Where(x => x.Status == "active");
        if (!scope.IsPlatformOwner)
        {
            if (scope.TenantId.HasValue) empQuery = empQuery.Where(x => x.TenantId == scope.TenantId.Value);
            if (scope.CompanyId.HasValue) empQuery = empQuery.Where(x => x.CompanyId == scope.CompanyId.Value);
        }

        var byDept = await empQuery
            .GroupJoin(db.Departments.AsNoTracking(),
                e => e.DepartmentId,
                d => d.Id,
                (e, ds) => new { Employee = e, Department = ds.FirstOrDefault() })
            .GroupBy(x => new
            {
                DepartmentId = x.Department != null ? x.Department.Id : (Guid?)null,
                DepartmentCode = x.Department != null ? x.Department.Code : "(sin dpto)",
                DepartmentName = x.Department != null ? x.Department.Name : "(Sin departamento)"
            })
            .Select(g => new DepartmentCostRow
            {
                DepartmentId = g.Key.DepartmentId ?? Guid.Empty,
                DepartmentCode = g.Key.DepartmentCode,
                DepartmentName = g.Key.DepartmentName,
                EmployeeCount = g.Count(),
                TotalSalary = g.Sum(x => x.Employee.PeriodSalary)
            })
            .OrderByDescending(x => x.TotalSalary)
            .Take(10)
            .ToListAsync();

        return new PayrollSummaryResult
        {
            Period = new PayrollSummaryRow
            {
                PeriodId = period.Id,
                PeriodCode = period.Code,
                PeriodName = period.Name,
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                PaymentDate = period.PaymentDate,
                Status = period.Status,
                EmployeeCount = employeeCount,
                Gross = grossAmount,
                Deductions = deductionsAmount,
                Net = netAmount,
                HasRuns = runs is not null && runs.Runs > 0
            },
            ByDepartment = byDept
        };
    }

    public static async Task<EmployeeIncidentsResult> GetEmployeeIncidentsAsync(NanchesoftDbContext db, AiScope scope, string? departmentHint)
    {
        var periodId = await ResolveOpenPeriodIdAsync(db, scope);
        string periodName = string.Empty;
        if (periodId.HasValue)
        {
            periodName = await db.PayrollPeriods.AsNoTracking()
                .Where(x => x.Id == periodId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync() ?? string.Empty;
        }

        var query = db.EmployeeIncidents.AsNoTracking().Where(x => !x.IsDeleted);
        if (!scope.IsPlatformOwner)
        {
            if (scope.TenantId.HasValue) query = query.Where(x => x.TenantId == scope.TenantId.Value);
            if (scope.CompanyId.HasValue) query = query.Where(x => x.CompanyId == scope.CompanyId.Value);
        }
        if (periodId.HasValue)
        {
            query = query.Where(x => x.PayrollPeriodId == periodId.Value);
        }

        var rows = await query
            .Join(db.Employees.AsNoTracking(),
                i => i.EmployeeId,
                e => e.Id,
                (i, e) => new { Incident = i, Employee = e })
            .GroupJoin(db.Departments.AsNoTracking(),
                x => x.Employee.DepartmentId,
                d => d.Id,
                (x, ds) => new { x.Incident, x.Employee, Department = ds.FirstOrDefault() })
            .Where(x => departmentHint == null
                || (x.Department != null && (EF.Functions.ILike(x.Department.Name, $"%{departmentHint}%")
                                              || EF.Functions.ILike(x.Department.Code, $"%{departmentHint}%"))))
            .GroupBy(x => new
            {
                x.Employee.Id,
                x.Employee.Code,
                FirstName = x.Employee.FirstName,
                LastName = x.Employee.LastName,
                SecondLastName = x.Employee.SecondLastName,
                DepartmentName = x.Department != null ? x.Department.Name : "(Sin departamento)"
            })
            .Select(g => new EmployeeIncidentsRow
            {
                EmployeeId = g.Key.Id,
                EmployeeCode = g.Key.Code,
                EmployeeName = (g.Key.FirstName + " " + g.Key.LastName + " " + (g.Key.SecondLastName ?? "")).Trim(),
                DepartmentName = g.Key.DepartmentName,
                IncidentCount = g.Count(),
                TotalAmount = g.Sum(x => x.Incident.Amount),
                LastIncidentDate = g.Max(x => x.Incident.IncidentDate)
            })
            .OrderByDescending(x => x.IncidentCount)
            .Take(50)
            .ToListAsync();

        return new EmployeeIncidentsResult
        {
            PeriodId = periodId,
            PeriodName = periodName,
            TotalIncidents = rows.Sum(x => x.IncidentCount),
            EmployeesWithIncidents = rows.Count,
            Rows = rows
        };
    }

    public static async Task<TodayIncidentsResult> GetTodayIncidentsAsync(NanchesoftDbContext db, AiScope scope)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var query = db.EmployeeIncidents.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IncidentDate >= today && x.IncidentDate < tomorrow);

        if (!scope.IsPlatformOwner)
        {
            if (scope.TenantId.HasValue) query = query.Where(x => x.TenantId == scope.TenantId.Value);
            if (scope.CompanyId.HasValue) query = query.Where(x => x.CompanyId == scope.CompanyId.Value);
        }

        var rows = await query
            .Join(db.Employees.AsNoTracking(),
                i => i.EmployeeId,
                e => e.Id,
                (i, e) => new { Incident = i, Employee = e })
            .GroupJoin(db.Departments.AsNoTracking(),
                x => x.Employee.DepartmentId,
                d => d.Id,
                (x, ds) => new { x.Incident, x.Employee, Department = ds.FirstOrDefault() })
            .GroupBy(x => new
            {
                x.Employee.Id,
                x.Employee.Code,
                x.Employee.FirstName,
                x.Employee.LastName,
                x.Employee.SecondLastName,
                DepartmentName = x.Department != null ? x.Department.Name : "(Sin departamento)"
            })
            .Select(g => new EmployeeIncidentsRow
            {
                EmployeeId = g.Key.Id,
                EmployeeCode = g.Key.Code,
                EmployeeName = (g.Key.FirstName + " " + g.Key.LastName + " " + (g.Key.SecondLastName ?? "")).Trim(),
                DepartmentName = g.Key.DepartmentName,
                IncidentCount = g.Count(),
                TotalAmount = g.Sum(x => x.Incident.Amount),
                LastIncidentDate = g.Max(x => x.Incident.IncidentDate)
            })
            .OrderByDescending(x => x.IncidentCount)
            .ToListAsync();

        return new TodayIncidentsResult
        {
            ReferenceDate = today,
            Total = rows.Sum(x => x.IncidentCount),
            Rows = rows
        };
    }

    public static async Task<List<EmployeeStatusRow>> GetEmployeesByStatusAsync(NanchesoftDbContext db, AiScope scope, string status)
    {
        var query = db.Employees.AsNoTracking().Where(x => x.Status == status);
        if (!scope.IsPlatformOwner)
        {
            if (scope.TenantId.HasValue) query = query.Where(x => x.TenantId == scope.TenantId.Value);
            if (scope.CompanyId.HasValue) query = query.Where(x => x.CompanyId == scope.CompanyId.Value);
        }

        return await query
            .GroupJoin(db.Departments.AsNoTracking(),
                e => e.DepartmentId,
                d => d.Id,
                (e, ds) => new { Employee = e, Department = ds.FirstOrDefault() })
            .OrderBy(x => x.Employee.LastName)
            .Take(100)
            .Select(x => new EmployeeStatusRow
            {
                EmployeeId = x.Employee.Id,
                EmployeeCode = x.Employee.Code,
                EmployeeName = (x.Employee.FirstName + " " + x.Employee.LastName + " " + (x.Employee.SecondLastName ?? "")).Trim(),
                DepartmentName = x.Department != null ? x.Department.Name : "(Sin departamento)",
                Status = x.Employee.Status,
                HireDate = x.Employee.HireDate,
                PeriodSalary = x.Employee.PeriodSalary
            })
            .ToListAsync();
    }

    public static async Task<List<EmployeeLoanRow>> GetEmployeeLoansAsync(NanchesoftDbContext db, AiScope scope)
    {
        var query = db.EmployeeLoans.AsNoTracking().Where(x => x.Status == "active");
        if (!scope.IsPlatformOwner)
        {
            if (scope.TenantId.HasValue) query = query.Where(x => x.TenantId == scope.TenantId.Value);
            if (scope.CompanyId.HasValue) query = query.Where(x => x.CompanyId == scope.CompanyId.Value);
        }

        return await query
            .Join(db.Employees.AsNoTracking(),
                l => l.EmployeeId,
                e => e.Id,
                (l, e) => new EmployeeLoanRow
                {
                    EmployeeId = e.Id,
                    EmployeeCode = e.Code,
                    EmployeeName = (e.FirstName + " " + e.LastName + " " + (e.SecondLastName ?? "")).Trim(),
                    LoanNumber = l.LoanNumber,
                    PrincipalAmount = l.PrincipalAmount,
                    BalanceAmount = l.BalanceAmount,
                    InstallmentAmount = l.InstallmentAmount,
                    InstallmentsLeft = l.Installments - l.InstallmentsPaid,
                    LoanDate = l.LoanDate
                })
            .OrderByDescending(x => x.BalanceAmount)
            .Take(50)
            .ToListAsync();
    }

    public static async Task<List<DepartmentCostRow>> GetDepartmentsCostAsync(NanchesoftDbContext db, AiScope scope)
    {
        var query = db.Employees.AsNoTracking().Where(x => x.Status == "active");
        if (!scope.IsPlatformOwner)
        {
            if (scope.TenantId.HasValue) query = query.Where(x => x.TenantId == scope.TenantId.Value);
            if (scope.CompanyId.HasValue) query = query.Where(x => x.CompanyId == scope.CompanyId.Value);
        }

        return await query
            .GroupJoin(db.Departments.AsNoTracking(),
                e => e.DepartmentId,
                d => d.Id,
                (e, ds) => new { Employee = e, Department = ds.FirstOrDefault() })
            .GroupBy(x => new
            {
                DepartmentId = x.Department != null ? x.Department.Id : (Guid?)null,
                DepartmentCode = x.Department != null ? x.Department.Code : "(sin)",
                DepartmentName = x.Department != null ? x.Department.Name : "(Sin departamento)"
            })
            .Select(g => new DepartmentCostRow
            {
                DepartmentId = g.Key.DepartmentId ?? Guid.Empty,
                DepartmentCode = g.Key.DepartmentCode,
                DepartmentName = g.Key.DepartmentName,
                EmployeeCount = g.Count(),
                TotalSalary = g.Sum(x => x.Employee.PeriodSalary)
            })
            .OrderByDescending(x => x.TotalSalary)
            .Take(20)
            .ToListAsync();
    }

    public static async Task<EmployeeIncidentsResult> GetOvertimeEmployeesAsync(NanchesoftDbContext db, AiScope scope)
    {
        var periodId = await ResolveOpenPeriodIdAsync(db, scope);
        string periodName = string.Empty;
        if (periodId.HasValue)
        {
            periodName = await db.PayrollPeriods.AsNoTracking()
                .Where(x => x.Id == periodId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync() ?? string.Empty;
        }

        var overtimeCategories = new[] { "overtime", "extra", "horas extra" };
        var query = db.EmployeeIncidents.AsNoTracking().Where(x => !x.IsDeleted);
        if (!scope.IsPlatformOwner)
        {
            if (scope.TenantId.HasValue) query = query.Where(x => x.TenantId == scope.TenantId.Value);
            if (scope.CompanyId.HasValue) query = query.Where(x => x.CompanyId == scope.CompanyId.Value);
        }
        if (periodId.HasValue) query = query.Where(x => x.PayrollPeriodId == periodId.Value);

        var rows = await query
            .Join(db.NomPayrollIncidentTypes.AsNoTracking(),
                i => i.PayrollIncidentTypeId,
                t => t.Id,
                (i, t) => new { Incident = i, Type = t })
            .Where(x => x.Type.Code.StartsWith("HE") || EF.Functions.ILike(x.Type.Name, "%hora%extra%") || EF.Functions.ILike(x.Type.Name, "%tiempo%extra%"))
            .Join(db.Employees.AsNoTracking(),
                x => x.Incident.EmployeeId,
                e => e.Id,
                (x, e) => new { x.Incident, Employee = e })
            .GroupJoin(db.Departments.AsNoTracking(),
                x => x.Employee.DepartmentId,
                d => d.Id,
                (x, ds) => new { x.Incident, x.Employee, Department = ds.FirstOrDefault() })
            .GroupBy(x => new
            {
                x.Employee.Id,
                x.Employee.Code,
                x.Employee.FirstName,
                x.Employee.LastName,
                x.Employee.SecondLastName,
                DepartmentName = x.Department != null ? x.Department.Name : "(Sin departamento)"
            })
            .Select(g => new EmployeeIncidentsRow
            {
                EmployeeId = g.Key.Id,
                EmployeeCode = g.Key.Code,
                EmployeeName = (g.Key.FirstName + " " + g.Key.LastName + " " + (g.Key.SecondLastName ?? "")).Trim(),
                DepartmentName = g.Key.DepartmentName,
                IncidentCount = g.Count(),
                TotalAmount = g.Sum(x => x.Incident.Amount),
                LastIncidentDate = g.Max(x => x.Incident.IncidentDate)
            })
            .OrderByDescending(x => x.IncidentCount)
            .Take(50)
            .ToListAsync();

        return new EmployeeIncidentsResult
        {
            PeriodId = periodId,
            PeriodName = periodName,
            TotalIncidents = rows.Sum(x => x.IncidentCount),
            EmployeesWithIncidents = rows.Count,
            Rows = rows
        };
    }

    public static async Task<EmployeeIncidentsResult> GetFaltasAsync(NanchesoftDbContext db, AiScope scope)
    {
        var periodId = await ResolveOpenPeriodIdAsync(db, scope);
        string periodName = string.Empty;
        if (periodId.HasValue)
        {
            periodName = await db.PayrollPeriods.AsNoTracking()
                .Where(x => x.Id == periodId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync() ?? string.Empty;
        }

        var query = db.EmployeeIncidents.AsNoTracking().Where(x => !x.IsDeleted);
        if (!scope.IsPlatformOwner)
        {
            if (scope.TenantId.HasValue) query = query.Where(x => x.TenantId == scope.TenantId.Value);
            if (scope.CompanyId.HasValue) query = query.Where(x => x.CompanyId == scope.CompanyId.Value);
        }
        if (periodId.HasValue) query = query.Where(x => x.PayrollPeriodId == periodId.Value);

        var rows = await query
            .Join(db.NomPayrollIncidentTypes.AsNoTracking(),
                i => i.PayrollIncidentTypeId,
                t => t.Id,
                (i, t) => new { Incident = i, Type = t })
            .Where(x => x.Type.Code == "FINJ" || x.Type.Code == "FJ" || EF.Functions.ILike(x.Type.Name, "%falta%") || EF.Functions.ILike(x.Type.Name, "%ausen%"))
            .Join(db.Employees.AsNoTracking(),
                x => x.Incident.EmployeeId,
                e => e.Id,
                (x, e) => new { x.Incident, Employee = e })
            .GroupJoin(db.Departments.AsNoTracking(),
                x => x.Employee.DepartmentId,
                d => d.Id,
                (x, ds) => new { x.Incident, x.Employee, Department = ds.FirstOrDefault() })
            .GroupBy(x => new
            {
                x.Employee.Id,
                x.Employee.Code,
                x.Employee.FirstName,
                x.Employee.LastName,
                x.Employee.SecondLastName,
                DepartmentName = x.Department != null ? x.Department.Name : "(Sin departamento)"
            })
            .Select(g => new EmployeeIncidentsRow
            {
                EmployeeId = g.Key.Id,
                EmployeeCode = g.Key.Code,
                EmployeeName = (g.Key.FirstName + " " + g.Key.LastName + " " + (g.Key.SecondLastName ?? "")).Trim(),
                DepartmentName = g.Key.DepartmentName,
                IncidentCount = g.Count(),
                TotalAmount = g.Sum(x => x.Incident.Amount),
                LastIncidentDate = g.Max(x => x.Incident.IncidentDate)
            })
            .OrderByDescending(x => x.IncidentCount)
            .Take(50)
            .ToListAsync();

        return new EmployeeIncidentsResult
        {
            PeriodId = periodId,
            PeriodName = periodName,
            TotalIncidents = rows.Sum(x => x.IncidentCount),
            EmployeesWithIncidents = rows.Count,
            Rows = rows
        };
    }

    public static async Task<ConceptTotalsResult> GetConceptTotalsAsync(NanchesoftDbContext db, AiScope scope, string? hint)
    {
        var periodId = await ResolveOpenPeriodIdAsync(db, scope);
        string periodName = string.Empty;
        if (periodId.HasValue)
        {
            periodName = await db.PayrollPeriods.AsNoTracking()
                .Where(x => x.Id == periodId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync() ?? string.Empty;
        }

        var query = db.EmployeeIncidents.AsNoTracking().Where(x => !x.IsDeleted);
        if (!scope.IsPlatformOwner)
        {
            if (scope.TenantId.HasValue) query = query.Where(x => x.TenantId == scope.TenantId.Value);
            if (scope.CompanyId.HasValue) query = query.Where(x => x.CompanyId == scope.CompanyId.Value);
        }
        if (periodId.HasValue) query = query.Where(x => x.PayrollPeriodId == periodId.Value);

        var joined = query
            .Join(db.NomPayrollIncidentTypes.AsNoTracking(),
                i => i.PayrollIncidentTypeId,
                t => t.Id,
                (i, t) => new { Incident = i, Type = t });

        if (!string.IsNullOrWhiteSpace(hint))
        {
            var pattern = $"%{hint}%";
            joined = joined.Where(x => EF.Functions.ILike(x.Type.Name, pattern)
                                       || EF.Functions.ILike(x.Type.Code, pattern)
                                       || EF.Functions.ILike(x.Type.Description, pattern));
        }

        var rows = await joined
            .GroupBy(x => new { x.Type.Id, x.Type.Code, x.Type.Name, x.Type.IncidentCategory })
            .Select(g => new ConceptTotalRow
            {
                PayrollConceptId = g.Key.Id,
                ConceptCode = g.Key.Code,
                ConceptName = g.Key.Name,
                Category = g.Key.IncidentCategory,
                IncidentCount = g.Count(),
                EmployeeCount = g.Select(x => x.Incident.EmployeeId).Distinct().Count(),
                TotalAmount = g.Sum(x => x.Incident.Amount),
                TotalQuantity = g.Sum(x => x.Incident.Quantity)
            })
            .OrderByDescending(x => x.TotalAmount)
            .Take(20)
            .ToListAsync();

        return new ConceptTotalsResult
        {
            Query = hint ?? string.Empty,
            PeriodId = periodId,
            PeriodName = periodName,
            Rows = rows,
            Total = rows.Sum(x => x.TotalAmount)
        };
    }

    public static async Task<List<MissingDataRow>> GetEmployeesWithoutDepartmentAsync(NanchesoftDbContext db, AiScope scope)
    {
        var query = db.Employees.AsNoTracking()
            .Where(x => x.Status == "active" && x.DepartmentId == null);
        if (!scope.IsPlatformOwner)
        {
            if (scope.TenantId.HasValue) query = query.Where(x => x.TenantId == scope.TenantId.Value);
            if (scope.CompanyId.HasValue) query = query.Where(x => x.CompanyId == scope.CompanyId.Value);
        }

        return await query
            .OrderBy(x => x.LastName)
            .Select(x => new MissingDataRow
            {
                EmployeeId = x.Id,
                EmployeeCode = x.Code,
                EmployeeName = (x.FirstName + " " + x.LastName + " " + (x.SecondLastName ?? "")).Trim(),
                Status = x.Status,
                Detail = "Sin departamento asignado"
            })
            .Take(100)
            .ToListAsync();
    }

    public static async Task<List<MissingDataRow>> GetEmployeesWithoutSalaryAsync(NanchesoftDbContext db, AiScope scope)
    {
        var query = db.Employees.AsNoTracking()
            .Where(x => x.Status == "active" && x.PeriodSalary <= 0m);
        if (!scope.IsPlatformOwner)
        {
            if (scope.TenantId.HasValue) query = query.Where(x => x.TenantId == scope.TenantId.Value);
            if (scope.CompanyId.HasValue) query = query.Where(x => x.CompanyId == scope.CompanyId.Value);
        }

        return await query
            .OrderBy(x => x.LastName)
            .Select(x => new MissingDataRow
            {
                EmployeeId = x.Id,
                EmployeeCode = x.Code,
                EmployeeName = (x.FirstName + " " + x.LastName + " " + (x.SecondLastName ?? "")).Trim(),
                Status = x.Status,
                Detail = "Sin sueldo del periodo capturado"
            })
            .Take(100)
            .ToListAsync();
    }
}

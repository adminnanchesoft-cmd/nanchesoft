using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PortalEndpoints
{
    public static void MapPortalEndpoints(this WebApplication app)
    {
        var portal = app.MapGroup("/api/portal").WithTags("Portal");

        portal.MapGet("/me",                       GetMeAsync);
        portal.MapPut("/link-employee/{empId:guid}", LinkEmployeeAsync);
        portal.MapGet("/payslips",                 GetPayslipsAsync);
        portal.MapGet("/payslips/{runLineId:guid}", GetPayslipDetailAsync);
        portal.MapGet("/incidents",                GetIncidentsAsync);
        portal.MapGet("/vacation-balance",         GetVacationBalanceAsync);
        portal.MapGet("/vacation-requests",        GetVacationRequestsAsync);
        portal.MapPost("/vacation-requests",       CreateVacationRequestAsync);
        portal.MapDelete("/vacation-requests/{id:guid}", CancelVacationRequestAsync);
    }

    // ─── resolve current employee from linked user ────────────────────────────
    private static async Task<(Guid? employeeId, IResult? error)> ResolveEmployeeAsync(
        HttpContext ctx, NanchesoftDbContext db)
    {
        var userId = ApiTenantScope.ResolveUserId(ctx);
        var tenantId = ApiTenantScope.ResolveTenantId(ctx);
        if (!userId.HasValue || !tenantId.HasValue)
            return (null, Results.BadRequest(new { message = "Sin contexto de usuario." }));

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value && u.TenantId == tenantId.Value);
        if (user is null)
            return (null, Results.NotFound(new { message = "Usuario no encontrado." }));

        Guid? empId = user.EmployeeId;

        // fallback: buscar por email si no está vinculado aún
        if (!empId.HasValue && !string.IsNullOrEmpty(user.Email))
        {
            var emp = await db.Employees.AsNoTracking()
                .Where(e => e.TenantId == tenantId.Value && e.Email == user.Email && e.IsActive)
                .Select(e => e.Id)
                .FirstOrDefaultAsync();
            if (emp != Guid.Empty) empId = emp;
        }

        if (!empId.HasValue)
            return (null, Results.NotFound(new { message = "Tu cuenta no está vinculada a un empleado. Contacta a RH." }));

        return (empId.Value, null);
    }

    // ─── GET /api/portal/me ───────────────────────────────────────────────────
    private static async Task<IResult> GetMeAsync(HttpContext ctx, NanchesoftDbContext db)
    {
        var (empId, err) = await ResolveEmployeeAsync(ctx, db);
        if (err is not null) return err;

        var e = await db.Employees.AsNoTracking()
            .Where(x => x.Id == empId!.Value)
            .Select(x => new PortalEmployeeDto
            {
                EmployeeId        = x.Id,
                EmployeeNumber    = x.EmployeeNumber,
                FullName          = x.FirstName + " " + x.LastName,
                Email             = x.Email,
                Phone             = x.Phone ?? string.Empty,
                DepartmentName    = x.Department != null ? x.Department.Name : string.Empty,
                PositionName      = x.Position  != null ? x.Position.Name  : string.Empty,
                HireDate          = x.HireDate,
                DailySalary       = x.DailySalary,
                Status            = x.Status,
            })
            .FirstOrDefaultAsync();

        return e is null ? Results.NotFound() : Results.Ok(e);
    }

    // ─── PUT /api/portal/link-employee/{empId} ────────────────────────────────
    private static async Task<IResult> LinkEmployeeAsync(Guid empId, HttpContext ctx, NanchesoftDbContext db)
    {
        var userId   = ApiTenantScope.ResolveUserId(ctx);
        var tenantId = ApiTenantScope.ResolveTenantId(ctx);
        if (!userId.HasValue || !tenantId.HasValue) return Results.BadRequest();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value && u.TenantId == tenantId.Value);
        if (user is null) return Results.NotFound();

        var empExists = await db.Employees.AnyAsync(e => e.Id == empId && e.TenantId == tenantId.Value && e.IsActive);
        if (!empExists) return Results.NotFound(new { message = "Empleado no encontrado." });

        user.EmployeeId = empId;
        await db.SaveChangesAsync();
        return Results.Ok(new { message = "Vinculado correctamente." });
    }

    // ─── GET /api/portal/payslips ─────────────────────────────────────────────
    private static async Task<IResult> GetPayslipsAsync(HttpContext ctx, NanchesoftDbContext db)
    {
        var (empId, err) = await ResolveEmployeeAsync(ctx, db);
        if (err is not null) return err;

        var rows = await db.PayrollRunLines.AsNoTracking()
            .Where(l => l.EmployeeId == empId!.Value && l.IsActive)
            .OrderByDescending(l => l.PayrollRun!.RunDate)
            .Take(24)
            .Select(l => new PortalPayslipSummaryDto
            {
                PayrollRunLineId  = l.Id,
                PayrollRunFolio   = l.PayrollRun != null ? l.PayrollRun.Folio : string.Empty,
                PeriodName        = l.PayrollRun != null && l.PayrollRun.PayrollPeriod != null
                                        ? l.PayrollRun.PayrollPeriod.Name : string.Empty,
                StartDate         = l.PayrollRun != null && l.PayrollRun.PayrollPeriod != null
                                        ? l.PayrollRun.PayrollPeriod.StartDate : null,
                EndDate           = l.PayrollRun != null && l.PayrollRun.PayrollPeriod != null
                                        ? l.PayrollRun.PayrollPeriod.EndDate : null,
                PaymentDate       = l.PayrollRun != null && l.PayrollRun.PayrollPeriod != null
                                        ? l.PayrollRun.PayrollPeriod.PaymentDate : null,
                RunDate           = l.PayrollRun != null ? l.PayrollRun.RunDate : null,
                DaysPaid          = l.DaysPaid,
                GrossAmount       = l.GrossAmount,
                DeductionsAmount  = l.DeductionsAmount,
                NetAmount         = l.NetAmount,
                Status            = l.PayrollRun != null ? l.PayrollRun.Status : string.Empty,
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    // ─── GET /api/portal/payslips/{id} ────────────────────────────────────────
    private static async Task<IResult> GetPayslipDetailAsync(Guid runLineId, HttpContext ctx, NanchesoftDbContext db)
    {
        var (empId, err) = await ResolveEmployeeAsync(ctx, db);
        if (err is not null) return err;

        var line = await db.PayrollRunLines.AsNoTracking()
            .Where(l => l.Id == runLineId && l.EmployeeId == empId!.Value)
            .Select(l => new PortalPayslipDetailDto
            {
                PayrollRunLineId = l.Id,
                PayrollRunFolio  = l.PayrollRun != null ? l.PayrollRun.Folio : string.Empty,
                PeriodName       = l.PayrollRun != null && l.PayrollRun.PayrollPeriod != null
                                       ? l.PayrollRun.PayrollPeriod.Name : string.Empty,
                StartDate        = l.PayrollRun != null && l.PayrollRun.PayrollPeriod != null
                                       ? l.PayrollRun.PayrollPeriod.StartDate : null,
                EndDate          = l.PayrollRun != null && l.PayrollRun.PayrollPeriod != null
                                       ? l.PayrollRun.PayrollPeriod.EndDate : null,
                PaymentDate      = l.PayrollRun != null && l.PayrollRun.PayrollPeriod != null
                                       ? l.PayrollRun.PayrollPeriod.PaymentDate : null,
                DaysPaid         = l.DaysPaid,
                GrossAmount      = l.GrossAmount,
                DeductionsAmount = l.DeductionsAmount,
                NetAmount        = l.NetAmount,
                EmployeeName     = l.Employee != null
                                       ? l.Employee.FirstName + " " + l.Employee.LastName : string.Empty,
                DepartmentName   = l.Department != null ? l.Department.Name : string.Empty,
                PositionName     = l.Position != null ? l.Position.Name : string.Empty,
            })
            .FirstOrDefaultAsync();

        if (line is null) return Results.NotFound();

        // Detalles de conceptos
        line.Details = await db.PayrollRunLineDetails.AsNoTracking()
            .Where(d => d.PayrollRunLineId == runLineId)
            .OrderBy(d => d.SortOrder)
            .Select(d => new PortalPayslipLineDto
            {
                ConceptCode  = d.PayrollConcept != null ? d.PayrollConcept.Code : string.Empty,
                ConceptName  = d.PayrollConcept != null ? d.PayrollConcept.Name : string.Empty,
                ConceptType  = d.PayrollConcept != null ? d.PayrollConcept.ConceptType : string.Empty,
                Amount       = d.Amount,
                SortOrder    = d.SortOrder,
            })
            .ToListAsync();

        return Results.Ok(line);
    }

    // ─── GET /api/portal/incidents ────────────────────────────────────────────
    private static async Task<IResult> GetIncidentsAsync(
        HttpContext ctx, NanchesoftDbContext db,
        [FromQuery] Guid? periodId, [FromQuery] int limit = 50)
    {
        var (empId, err) = await ResolveEmployeeAsync(ctx, db);
        if (err is not null) return err;

        var query = db.EmployeeIncidents.AsNoTracking()
            .Where(i => i.EmployeeId == empId!.Value && !i.IsDeleted);

        if (periodId.HasValue)
            query = query.Where(i => i.PayrollPeriodId == periodId.Value);
        else
            query = query.OrderByDescending(i => i.IncidentDate).Take(limit);

        var rows = await query
            .OrderByDescending(i => i.IncidentDate)
            .Select(i => new PortalIncidentDto
            {
                IncidentId   = i.Id,
                IncidentDate = i.IncidentDate,
                IncidentType = i.IncidentType,
                Amount       = i.Amount,
                Quantity     = i.Quantity,
                Status       = i.Status,
                Origin       = i.Origin,
                Notes        = i.Notes ?? string.Empty,
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    // ─── GET /api/portal/vacation-balance ────────────────────────────────────
    private static async Task<IResult> GetVacationBalanceAsync(HttpContext ctx, NanchesoftDbContext db)
    {
        var (empId, err) = await ResolveEmployeeAsync(ctx, db);
        if (err is not null) return err;

        var emp = await db.Employees.AsNoTracking()
            .Where(e => e.Id == empId!.Value)
            .Select(e => new { e.HireDate, e.FirstName, e.LastName, e.EmployeeNumber })
            .FirstOrDefaultAsync();
        if (emp is null) return Results.NotFound();

        var today = DateTime.Today;
        var hireDate = emp.HireDate == default ? today : emp.HireDate;
        var totalDays = (today - hireDate).TotalDays;
        var fullYears = (int)(totalDays / 365.25);
        var annualVacDays = GetVacationDays(fullYears);

        // días usados este año (aprobados o pendientes)
        var yearStart = new DateTime(today.Year, 1, 1);
        var usedDays = await db.VacationRequests.AsNoTracking()
            .Where(v => v.EmployeeId == empId!.Value
                     && v.StartDate >= yearStart
                     && (v.Status == "approved" || v.Status == "pending"))
            .SumAsync(v => (decimal?)v.RequestedDays) ?? 0m;

        // saldo proporcional: días transcurridos desde el aniversario más reciente
        var anniversaryThisYear = hireDate.AddYears(today.Year - hireDate.Year);
        if (anniversaryThisYear > today) anniversaryThisYear = anniversaryThisYear.AddYears(-1);
        var daysElapsed = (today - anniversaryThisYear).TotalDays;
        var proportionalEarned = Math.Round(annualVacDays * Math.Min((decimal)daysElapsed, 365m) / 365m, 1);

        return Results.Ok(new PortalVacationBalanceDto
        {
            FullYears         = fullYears,
            AnnualDays        = annualVacDays,
            ProportionalEarned = proportionalEarned,
            DaysUsedThisYear  = usedDays,
            DaysAvailable     = Math.Max(0, proportionalEarned - usedDays),
            HireDate          = hireDate,
        });
    }

    // ─── GET /api/portal/vacation-requests ───────────────────────────────────
    private static async Task<IResult> GetVacationRequestsAsync(HttpContext ctx, NanchesoftDbContext db)
    {
        var (empId, err) = await ResolveEmployeeAsync(ctx, db);
        if (err is not null) return err;

        var rows = await db.VacationRequests.AsNoTracking()
            .Where(v => v.EmployeeId == empId!.Value)
            .OrderByDescending(v => v.StartDate)
            .Take(30)
            .Select(v => new PortalVacationRequestDto
            {
                VacationRequestId = v.Id,
                Folio             = v.Folio,
                StartDate         = v.StartDate,
                EndDate           = v.EndDate,
                RequestedDays     = v.RequestedDays,
                ApprovedDays      = v.ApprovedDays,
                Status            = v.Status,
                Notes             = v.Notes ?? string.Empty,
                CreatedAt         = v.CreatedAt,
                ApprovedBy        = v.ApprovedBy ?? string.Empty,
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    // ─── POST /api/portal/vacation-requests ──────────────────────────────────
    private static async Task<IResult> CreateVacationRequestAsync(
        HttpContext ctx, PortalVacationRequestInput input, NanchesoftDbContext db)
    {
        var (empId, err) = await ResolveEmployeeAsync(ctx, db);
        if (err is not null) return err;

        var tenantId  = ApiTenantScope.ResolveTenantId(ctx)!.Value;
        var companyId = ApiTenantScope.ResolveCompanyId(ctx) ?? Guid.Empty;

        if (input.StartDate >= input.EndDate)
            return Results.BadRequest(new { message = "La fecha de inicio debe ser anterior a la fecha de término." });

        var days = (decimal)(input.EndDate - input.StartDate).TotalDays;

        var entity = new Nanchesoft.Domain.Entities.VacationRequest
        {
            TenantId      = tenantId,
            CompanyId     = companyId,
            EmployeeId    = empId!.Value,
            RequestDate   = DateTime.Today,
            StartDate     = input.StartDate,
            EndDate       = input.EndDate,
            RequestedDays = days,
            ApprovedDays  = 0,
            Folio         = $"VAC-{DateTime.Now:yyyyMMddHHmmss}",
            Status        = "pending",
            Notes         = input.Notes ?? string.Empty,
            CreatedAt     = DateTime.UtcNow,
        };

        db.VacationRequests.Add(entity);
        await db.SaveChangesAsync();

        return Results.Created($"/api/portal/vacation-requests/{entity.Id}", new { entity.Id });
    }

    // ─── DELETE /api/portal/vacation-requests/{id} ───────────────────────────
    private static async Task<IResult> CancelVacationRequestAsync(Guid id, HttpContext ctx, NanchesoftDbContext db)
    {
        var (empId, err) = await ResolveEmployeeAsync(ctx, db);
        if (err is not null) return err;

        var req = await db.VacationRequests
            .FirstOrDefaultAsync(v => v.Id == id && v.EmployeeId == empId!.Value);
        if (req is null) return Results.NotFound();
        if (req.Status != "pending")
            return Results.BadRequest(new { message = "Solo puedes cancelar solicitudes pendientes." });

        req.Status = "cancelled";
        await db.SaveChangesAsync();
        return Results.Ok();
    }

    private static decimal GetVacationDays(int fullYears) => fullYears switch
    {
        <= 1  => 12, 2  => 14, 3  => 16, 4  => 18, 5  => 20,
        <= 10 => 22, <= 15 => 24, <= 20 => 26, <= 25 => 28,
        <= 30 => 30, <= 35 => 32, _  => 34
    };

    // ─── DTOs ──────────────────────────────────────────────────────────────────

    private sealed class PortalEmployeeDto
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public DateTime? HireDate { get; set; }
        public decimal DailySalary { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    private sealed class PortalPayslipSummaryDto
    {
        public Guid PayrollRunLineId { get; set; }
        public string PayrollRunFolio { get; set; } = string.Empty;
        public string PeriodName { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        public DateTime? RunDate { get; set; }
        public decimal DaysPaid { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DeductionsAmount { get; set; }
        public decimal NetAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    private sealed class PortalPayslipDetailDto
    {
        public Guid PayrollRunLineId { get; set; }
        public string PayrollRunFolio { get; set; } = string.Empty;
        public string PeriodName { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal DaysPaid { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DeductionsAmount { get; set; }
        public decimal NetAmount { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public List<PortalPayslipLineDto> Details { get; set; } = [];
    }

    private sealed class PortalPayslipLineDto
    {
        public string ConceptCode { get; set; } = string.Empty;
        public string ConceptName { get; set; } = string.Empty;
        public string ConceptType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int SortOrder { get; set; }
    }

    private sealed class PortalIncidentDto
    {
        public Guid IncidentId { get; set; }
        public DateTime IncidentDate { get; set; }
        public string IncidentType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    private sealed class PortalVacationBalanceDto
    {
        public int FullYears { get; set; }
        public decimal AnnualDays { get; set; }
        public decimal ProportionalEarned { get; set; }
        public decimal DaysUsedThisYear { get; set; }
        public decimal DaysAvailable { get; set; }
        public DateTime HireDate { get; set; }
    }

    private sealed class PortalVacationRequestDto
    {
        public Guid VacationRequestId { get; set; }
        public string Folio { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal RequestedDays { get; set; }
        public decimal ApprovedDays { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
    }

    private sealed record PortalVacationRequestInput(DateTime StartDate, DateTime EndDate, string? Notes);
}

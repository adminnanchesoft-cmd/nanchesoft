using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class HumanResourcesEnterpriseEndpoints
{
    public static IEndpointRouteBuilder MapHumanResourcesEnterpriseEndpoints(this IEndpointRouteBuilder app)
    {
        var shifts = app.MapGroup("/api/hr/shifts").WithTags("HrShifts");
        shifts.MapGet("/", GetWorkShiftsAsync);
        shifts.MapPost("/", CreateWorkShiftAsync);
        shifts.MapPut("/{id:guid}", UpdateWorkShiftAsync);
        shifts.MapDelete("/{id:guid}", DeleteWorkShiftAsync);

        var schedules = app.MapGroup("/api/hr/work-schedules").WithTags("HrWorkSchedules");
        schedules.MapGet("/", GetWorkSchedulesAsync);
        schedules.MapPost("/", CreateWorkScheduleAsync);
        schedules.MapPut("/{id:guid}", UpdateWorkScheduleAsync);
        schedules.MapDelete("/{id:guid}", DeleteWorkScheduleAsync);

        var devices = app.MapGroup("/api/hr/time-clock-devices").WithTags("HrTimeClockDevices");
        devices.MapGet("/", GetTimeClockDevicesAsync);
        devices.MapPost("/", CreateTimeClockDeviceAsync);
        devices.MapPut("/{id:guid}", UpdateTimeClockDeviceAsync);
        devices.MapDelete("/{id:guid}", DeleteTimeClockDeviceAsync);

        var leaveTypes = app.MapGroup("/api/hr/leave-types").WithTags("HrLeaveTypes");
        leaveTypes.MapGet("/", GetLeaveTypesAsync);
        leaveTypes.MapPost("/", CreateLeaveTypeAsync);
        leaveTypes.MapPut("/{id:guid}", UpdateLeaveTypeAsync);
        leaveTypes.MapDelete("/{id:guid}", DeleteLeaveTypeAsync);

        var vacationRequests = app.MapGroup("/api/hr/vacation-requests").WithTags("HrVacationRequests");
        vacationRequests.MapGet("/", GetVacationRequestsAsync);
        vacationRequests.MapPost("/", CreateVacationRequestAsync);
        vacationRequests.MapPut("/{id:guid}", UpdateVacationRequestAsync);
        vacationRequests.MapDelete("/{id:guid}", DeleteVacationRequestAsync);

        return app;
    }

    private static async Task<(Guid? TenantId, Guid? CompanyId, Guid? BranchId)> ResolveDefaultContextAsync(NanchesoftDbContext db)
    {
        var company = await db.Companies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        if (company is null)
            return (null, null, null);

        var branchId = await db.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
        return (company.TenantId, company.Id, branchId);
    }

    private static string NormalizeUpper(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();

    private static string NormalizeText(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string NormalizeLower(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();

    private static DateTime NormalizeUtc(DateTime? value, DateTime fallback)
    {
        var source = value ?? fallback;
        return source.Kind == DateTimeKind.Utc ? source : DateTime.SpecifyKind(source, DateTimeKind.Utc);
    }

    private static decimal GetRequestedDays(DateTime startDate, DateTime endDate, decimal requestedDays)
    {
        if (requestedDays > 0m)
            return requestedDays;

        var days = (endDate.Date - startDate.Date).TotalDays + 1d;
        return days <= 0d ? 1m : Convert.ToDecimal(days);
    }

    private static async Task<IResult> GetWorkShiftsAsync(NanchesoftDbContext db)
    {
        var rows = await db.WorkShifts.AsNoTracking()
            .Include(x => x.Company)
            .OrderBy(x => x.Code)
            .Select(x => new WorkShiftDto
            {
                WorkShiftId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                Code = x.Code,
                Name = x.Name,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                BreakMinutes = x.BreakMinutes,
                ToleranceMinutes = x.ToleranceMinutes,
                IsOvernight = x.IsOvernight,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateWorkShiftAsync(WorkShiftRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto para el turno." });

        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (await db.WorkShifts.AnyAsync(x => x.CompanyId == companyId.Value && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un turno con ese código." });

        var entity = new WorkShift
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            Code = code,
            Name = name,
            StartTime = NormalizeText(request.StartTime, "08:00"),
            EndTime = NormalizeText(request.EndTime, "17:00"),
            BreakMinutes = request.BreakMinutes,
            ToleranceMinutes = request.ToleranceMinutes,
            IsOvernight = request.IsOvernight,
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.WorkShifts.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateWorkShiftAsync(Guid id, WorkShiftRequest request, NanchesoftDbContext db)
    {
        var entity = await db.WorkShifts.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el turno." });

        var code = NormalizeUpper(request.Code, entity.Code);
        if (await db.WorkShifts.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro turno con ese código." });

        entity.Code = code;
        entity.Name = NormalizeText(request.Name, entity.Name);
        entity.StartTime = NormalizeText(request.StartTime, entity.StartTime);
        entity.EndTime = NormalizeText(request.EndTime, entity.EndTime);
        entity.BreakMinutes = request.BreakMinutes;
        entity.ToleranceMinutes = request.ToleranceMinutes;
        entity.IsOvernight = request.IsOvernight;
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteWorkShiftAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.WorkShifts.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el turno." });

        if (await db.WorkSchedules.AnyAsync(x => x.WorkShiftId == id))
            return Results.BadRequest(new { message = "No puedes eliminar un turno asignado a un horario." });

        db.WorkShifts.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetWorkSchedulesAsync(NanchesoftDbContext db)
    {
        var rows = await db.WorkSchedules.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.WorkShift)
            .OrderBy(x => x.Code)
            .Select(x => new WorkScheduleDto
            {
                WorkScheduleId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                WorkShiftId = x.WorkShiftId,
                WorkShiftName = x.WorkShift != null ? x.WorkShift.Name : string.Empty,
                Code = x.Code,
                Name = x.Name,
                Monday = x.Monday,
                Tuesday = x.Tuesday,
                Wednesday = x.Wednesday,
                Thursday = x.Thursday,
                Friday = x.Friday,
                Saturday = x.Saturday,
                Sunday = x.Sunday,
                EntryTime = x.EntryTime,
                ToleranceMinutes = x.ToleranceMinutes,
                LunchStartTime = x.LunchStartTime,
                LunchEndTime = x.LunchEndTime,
                ExitTime = x.ExitTime,
                WeeklyHours = x.WeeklyHours,
                IsFlexible = x.IsFlexible,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateWorkScheduleAsync(WorkScheduleRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto para el horario." });

        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (await db.WorkSchedules.AnyAsync(x => x.CompanyId == companyId.Value && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un horario con ese código." });

        var entity = new WorkSchedule
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            WorkShiftId = request.WorkShiftId,
            Code = code,
            Name = name,
            Monday = request.Monday,
            Tuesday = request.Tuesday,
            Wednesday = request.Wednesday,
            Thursday = request.Thursday,
            Friday = request.Friday,
            Saturday = request.Saturday,
            Sunday = request.Sunday,
            EntryTime = NormalizeText(request.EntryTime),
            ToleranceMinutes = request.ToleranceMinutes,
            LunchStartTime = NormalizeText(request.LunchStartTime),
            LunchEndTime = NormalizeText(request.LunchEndTime),
            ExitTime = NormalizeText(request.ExitTime),
            WeeklyHours = request.WeeklyHours,
            IsFlexible = request.IsFlexible,
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.WorkSchedules.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateWorkScheduleAsync(Guid id, WorkScheduleRequest request, NanchesoftDbContext db)
    {
        var entity = await db.WorkSchedules.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el horario." });

        var code = NormalizeUpper(request.Code, entity.Code);
        if (await db.WorkSchedules.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro horario con ese código." });

        entity.WorkShiftId = request.WorkShiftId;
        entity.Code = code;
        entity.Name = NormalizeText(request.Name, entity.Name);
        entity.Monday = request.Monday;
        entity.Tuesday = request.Tuesday;
        entity.Wednesday = request.Wednesday;
        entity.Thursday = request.Thursday;
        entity.Friday = request.Friday;
        entity.Saturday = request.Saturday;
        entity.Sunday = request.Sunday;
        entity.EntryTime = NormalizeText(request.EntryTime, entity.EntryTime);
        entity.ToleranceMinutes = request.ToleranceMinutes;
        entity.LunchStartTime = NormalizeText(request.LunchStartTime, entity.LunchStartTime);
        entity.LunchEndTime = NormalizeText(request.LunchEndTime, entity.LunchEndTime);
        entity.ExitTime = NormalizeText(request.ExitTime, entity.ExitTime);
        entity.WeeklyHours = request.WeeklyHours;
        entity.IsFlexible = request.IsFlexible;
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteWorkScheduleAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.WorkSchedules.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el horario." });

        db.WorkSchedules.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetTimeClockDevicesAsync(NanchesoftDbContext db)
    {
        var rows = await db.TimeClockDevices.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .OrderBy(x => x.Code)
            .Select(x => new TimeClockDeviceDto
            {
                TimeClockDeviceId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                Code = x.Code,
                Name = x.Name,
                Brand = x.Brand,
                Model = x.Model,
                SerialNumber = x.SerialNumber,
                IpAddress = x.IpAddress,
                ApiUrl = x.ApiUrl,
                Location = x.Location,
                Status = x.Status,
                LastSyncAt = x.LastSyncAt,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateTimeClockDeviceAsync(TimeClockDeviceRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto para el dispositivo." });

        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (await db.TimeClockDevices.AnyAsync(x => x.CompanyId == companyId.Value && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un dispositivo con ese código." });

        var entity = new TimeClockDevice
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = request.BranchId,
            Code = code,
            Name = name,
            Brand = NormalizeText(request.Brand),
            Model = NormalizeText(request.Model),
            SerialNumber = NormalizeUpper(request.SerialNumber),
            IpAddress = NormalizeText(request.IpAddress),
            ApiUrl = NormalizeText(request.ApiUrl),
            Location = NormalizeText(request.Location),
            Status = NormalizeLower(request.Status, "online"),
            LastSyncAt = request.LastSyncAt,
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.TimeClockDevices.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateTimeClockDeviceAsync(Guid id, TimeClockDeviceRequest request, NanchesoftDbContext db)
    {
        var entity = await db.TimeClockDevices.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el dispositivo." });

        var code = NormalizeUpper(request.Code, entity.Code);
        if (await db.TimeClockDevices.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro dispositivo con ese código." });

        entity.BranchId = request.BranchId ?? entity.BranchId;
        entity.Code = code;
        entity.Name = NormalizeText(request.Name, entity.Name);
        entity.Brand = NormalizeText(request.Brand, entity.Brand);
        entity.Model = NormalizeText(request.Model, entity.Model);
        entity.SerialNumber = NormalizeUpper(request.SerialNumber, entity.SerialNumber);
        entity.IpAddress = NormalizeText(request.IpAddress, entity.IpAddress);
        entity.ApiUrl = NormalizeText(request.ApiUrl, entity.ApiUrl);
        entity.Location = NormalizeText(request.Location, entity.Location);
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.LastSyncAt = request.LastSyncAt ?? entity.LastSyncAt;
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteTimeClockDeviceAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.TimeClockDevices.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el dispositivo." });

        db.TimeClockDevices.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetLeaveTypesAsync(NanchesoftDbContext db)
    {
        var rows = await db.LeaveTypes.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.PayrollConcept)
            .OrderBy(x => x.Code)
            .Select(x => new LeaveTypeDto
            {
                LeaveTypeId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                PayrollConceptId = x.PayrollConceptId,
                PayrollConceptName = x.PayrollConcept != null ? x.PayrollConcept.Name : string.Empty,
                Code = x.Code,
                Name = x.Name,
                Category = x.Category,
                WithPay = x.WithPay,
                ImpactsPayroll = x.ImpactsPayroll,
                DefaultDays = x.DefaultDays,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateLeaveTypeAsync(LeaveTypeRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto para el tipo de ausencia." });

        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (await db.LeaveTypes.AnyAsync(x => x.CompanyId == companyId.Value && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un tipo de ausencia con ese código." });

        var entity = new LeaveType
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            PayrollConceptId = request.PayrollConceptId,
            Code = code,
            Name = name,
            Category = NormalizeLower(request.Category, "vacation"),
            WithPay = request.WithPay,
            ImpactsPayroll = request.ImpactsPayroll,
            DefaultDays = request.DefaultDays,
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.LeaveTypes.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateLeaveTypeAsync(Guid id, LeaveTypeRequest request, NanchesoftDbContext db)
    {
        var entity = await db.LeaveTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el tipo de ausencia." });

        var code = NormalizeUpper(request.Code, entity.Code);
        if (await db.LeaveTypes.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro tipo de ausencia con ese código." });

        entity.PayrollConceptId = request.PayrollConceptId;
        entity.Code = code;
        entity.Name = NormalizeText(request.Name, entity.Name);
        entity.Category = NormalizeLower(request.Category, entity.Category);
        entity.WithPay = request.WithPay;
        entity.ImpactsPayroll = request.ImpactsPayroll;
        entity.DefaultDays = request.DefaultDays;
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteLeaveTypeAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.LeaveTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el tipo de ausencia." });

        if (await db.VacationRequests.AnyAsync(x => x.LeaveTypeId == id))
            return Results.BadRequest(new { message = "No puedes eliminar un tipo de ausencia ligado a solicitudes." });

        db.LeaveTypes.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetVacationRequestsAsync(NanchesoftDbContext db)
    {
        var rows = await db.VacationRequests.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.Employee)
            .Include(x => x.LeaveType)
            .OrderByDescending(x => x.RequestDate)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new VacationRequestDto
            {
                VacationRequestId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                EmployeeNumber = x.Employee != null ? x.Employee.EmployeeNumber : string.Empty,
                LeaveTypeId = x.LeaveTypeId,
                LeaveTypeName = x.LeaveType != null ? x.LeaveType.Name : string.Empty,
                Folio = x.Folio,
                RequestDate = x.RequestDate,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                ReturnDate = x.ReturnDate,
                RequestedDays = x.RequestedDays,
                ApprovedDays = x.ApprovedDays,
                Status = x.Status,
                ApprovedBy = x.ApprovedBy,
                ApprovedAt = x.ApprovedAt,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateVacationRequestAsync(VacationRequestRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        var branchId = request.BranchId ?? context.BranchId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.EmployeeId.HasValue)
            return Results.BadRequest(new { message = "Empresa y colaborador son obligatorios." });

        var requestDate = NormalizeUtc(request.RequestDate, DateTime.UtcNow);
        var startDate = NormalizeUtc(request.StartDate, DateTime.UtcNow.Date);
        var endDate = NormalizeUtc(request.EndDate, startDate);
        if (endDate.Date < startDate.Date)
            return Results.BadRequest(new { message = "La fecha final no puede ser menor a la inicial." });

        var requestedDays = GetRequestedDays(startDate, endDate, request.RequestedDays);
        var approvedDays = request.ApprovedDays > 0m ? request.ApprovedDays : requestedDays;
        var folio = NormalizeUpper(request.Folio, $"VAC-{requestDate:yyyyMMddHHmmss}");
        if (await db.VacationRequests.AnyAsync(x => x.CompanyId == companyId.Value && x.Folio == folio))
            folio = $"{folio}-{DateTime.UtcNow:fff}";

        var entity = new VacationRequest
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = branchId,
            EmployeeId = request.EmployeeId.Value,
            LeaveTypeId = request.LeaveTypeId,
            Folio = folio,
            RequestDate = requestDate,
            StartDate = startDate,
            EndDate = endDate,
            ReturnDate = request.ReturnDate,
            RequestedDays = requestedDays,
            ApprovedDays = approvedDays,
            Status = NormalizeLower(request.Status, "draft"),
            ApprovedBy = NormalizeText(request.ApprovedBy),
            ApprovedAt = request.ApprovedAt,
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.VacationRequests.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateVacationRequestAsync(Guid id, VacationRequestRequest request, NanchesoftDbContext db)
    {
        var entity = await db.VacationRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la solicitud." });

        var startDate = request.StartDate.HasValue ? NormalizeUtc(request.StartDate, entity.StartDate) : entity.StartDate;
        var endDate = request.EndDate.HasValue ? NormalizeUtc(request.EndDate, entity.EndDate) : entity.EndDate;
        if (endDate.Date < startDate.Date)
            return Results.BadRequest(new { message = "La fecha final no puede ser menor a la inicial." });

        var folio = NormalizeUpper(request.Folio, entity.Folio);
        if (await db.VacationRequests.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Folio == folio))
            return Results.BadRequest(new { message = "Ya existe otra solicitud con ese folio." });

        entity.BranchId = request.BranchId ?? entity.BranchId;
        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.LeaveTypeId = request.LeaveTypeId ?? entity.LeaveTypeId;
        entity.Folio = folio;
        entity.RequestDate = request.RequestDate.HasValue ? NormalizeUtc(request.RequestDate, entity.RequestDate) : entity.RequestDate;
        entity.StartDate = startDate;
        entity.EndDate = endDate;
        entity.ReturnDate = request.ReturnDate ?? entity.ReturnDate;
        entity.RequestedDays = GetRequestedDays(startDate, endDate, request.RequestedDays > 0m ? request.RequestedDays : entity.RequestedDays);
        entity.ApprovedDays = request.ApprovedDays > 0m ? request.ApprovedDays : entity.ApprovedDays;
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.ApprovedBy = NormalizeText(request.ApprovedBy, entity.ApprovedBy);
        entity.ApprovedAt = request.ApprovedAt ?? entity.ApprovedAt;
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteVacationRequestAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.VacationRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la solicitud." });

        db.VacationRequests.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }
}

public sealed class WorkShiftRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public int BreakMinutes { get; set; }
    public int ToleranceMinutes { get; set; }
    public bool IsOvernight { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class WorkShiftDto
{
    public Guid WorkShiftId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int BreakMinutes { get; set; }
    public int ToleranceMinutes { get; set; }
    public bool IsOvernight { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class WorkScheduleRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? WorkShiftId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public bool Monday { get; set; }
    public bool Tuesday { get; set; }
    public bool Wednesday { get; set; }
    public bool Thursday { get; set; }
    public bool Friday { get; set; }
    public bool Saturday { get; set; }
    public bool Sunday { get; set; }
    public string? EntryTime { get; set; }
    public int ToleranceMinutes { get; set; }
    public string? LunchStartTime { get; set; }
    public string? LunchEndTime { get; set; }
    public string? ExitTime { get; set; }
    public decimal WeeklyHours { get; set; }
    public bool IsFlexible { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class WorkScheduleDto
{
    public Guid WorkScheduleId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? WorkShiftId { get; set; }
    public string WorkShiftName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Monday { get; set; }
    public bool Tuesday { get; set; }
    public bool Wednesday { get; set; }
    public bool Thursday { get; set; }
    public bool Friday { get; set; }
    public bool Saturday { get; set; }
    public bool Sunday { get; set; }
    public string EntryTime { get; set; } = string.Empty;
    public int ToleranceMinutes { get; set; }
    public string LunchStartTime { get; set; } = string.Empty;
    public string LunchEndTime { get; set; } = string.Empty;
    public string ExitTime { get; set; } = string.Empty;
    public decimal WeeklyHours { get; set; }
    public bool IsFlexible { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class TimeClockDeviceRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? IpAddress { get; set; }
    public string? ApiUrl { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class TimeClockDeviceDto
{
    public Guid TimeClockDeviceId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? LastSyncAt { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class LeaveTypeRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollConceptId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public bool WithPay { get; set; }
    public bool ImpactsPayroll { get; set; }
    public decimal DefaultDays { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class LeaveTypeDto
{
    public Guid LeaveTypeId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? PayrollConceptId { get; set; }
    public string PayrollConceptName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool WithPay { get; set; }
    public bool ImpactsPayroll { get; set; }
    public decimal DefaultDays { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class VacationRequestRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? LeaveTypeId { get; set; }
    public string? Folio { get; set; }
    public DateTime? RequestDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public decimal RequestedDays { get; set; }
    public decimal ApprovedDays { get; set; }
    public string? Status { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class VacationRequestDto
{
    public Guid VacationRequestId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public Guid? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public Guid? LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public decimal RequestedDays { get; set; }
    public decimal ApprovedDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

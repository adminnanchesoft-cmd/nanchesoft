using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class HumanResourcesEnterpriseSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        await EnsureSchemaAsync(dbContext);

        const string seedUser = "seed";
        var company = await dbContext.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (company is null)
            return;

        var branch = await dbContext.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var employee = await dbContext.Employees.Where(x => x.CompanyId == company.Id).OrderBy(x => x.EmployeeNumber).FirstOrDefaultAsync();
        var payrollConcept = await dbContext.PayrollConcepts.Where(x => x.CompanyId == company.Id).OrderBy(x => x.Code).FirstOrDefaultAsync();

        var shiftId = Guid.Parse("E1000000-0000-0000-0000-000000000301");
        var scheduleId = Guid.Parse("E1000000-0000-0000-0000-000000000302");
        var deviceId = Guid.Parse("E1000000-0000-0000-0000-000000000303");
        var leaveTypeId = Guid.Parse("E1000000-0000-0000-0000-000000000304");
        var vacationRequestId = Guid.Parse("E1000000-0000-0000-0000-000000000305");

        if (!await dbContext.WorkShifts.AnyAsync(x => x.CompanyId == company.Id && x.Code == "MATUTINO"))
        {
            dbContext.WorkShifts.Add(new WorkShift
            {
                Id = shiftId,
                TenantId = company.TenantId,
                CompanyId = company.Id,
                Code = "MATUTINO",
                Name = "Turno matutino",
                StartTime = "08:00",
                EndTime = "17:30",
                BreakMinutes = 60,
                ToleranceMinutes = 10,
                IsOvernight = false,
                Notes = "Turno estándar administrativo.",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.WorkSchedules.AnyAsync(x => x.CompanyId == company.Id && x.Code == "ADM-LV"))
        {
            dbContext.WorkSchedules.Add(new WorkSchedule
            {
                Id = scheduleId,
                TenantId = company.TenantId,
                CompanyId = company.Id,
                WorkShiftId = shiftId,
                Code = "ADM-LV",
                Name = "Horario administrativo lunes a viernes",
                Monday = true,
                Tuesday = true,
                Wednesday = true,
                Thursday = true,
                Friday = true,
                Saturday = false,
                Sunday = false,
                WeeklyHours = 45m,
                IsFlexible = false,
                Notes = "Horario base para personal administrativo.",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.TimeClockDevices.AnyAsync(x => x.CompanyId == company.Id && x.Code == "CLOCK-01"))
        {
            dbContext.TimeClockDevices.Add(new TimeClockDevice
            {
                Id = deviceId,
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch?.Id,
                Code = "CLOCK-01",
                Name = "Reloj acceso principal",
                Brand = "ZKTeco",
                Model = "SpeedFace V5L",
                SerialNumber = "NS-DEMO-CLOCK-01",
                IpAddress = "192.168.10.21",
                ApiUrl = "http://192.168.10.21/api",
                Location = branch?.Name ?? "Acceso principal",
                Status = "online",
                LastSyncAt = DateTime.UtcNow,
                Notes = "Dispositivo demo para integración de reloj checador.",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.LeaveTypes.AnyAsync(x => x.CompanyId == company.Id && x.Code == "VAC"))
        {
            dbContext.LeaveTypes.Add(new LeaveType
            {
                Id = leaveTypeId,
                TenantId = company.TenantId,
                CompanyId = company.Id,
                PayrollConceptId = payrollConcept?.Id,
                Code = "VAC",
                Name = "Vacaciones",
                Category = "vacation",
                WithPay = true,
                ImpactsPayroll = true,
                DefaultDays = 6m,
                Notes = "Catálogo base de vacaciones con goce de sueldo.",
                CreatedBy = seedUser
            });
        }

        if (employee is not null && !await dbContext.VacationRequests.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "VAC-2026-0001"))
        {
            dbContext.VacationRequests.Add(new VacationRequest
            {
                Id = vacationRequestId,
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch?.Id,
                EmployeeId = employee.Id,
                LeaveTypeId = leaveTypeId,
                Folio = "VAC-2026-0001",
                RequestDate = new DateTime(2026, 4, 9, 0, 0, 0, DateTimeKind.Utc),
                StartDate = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 4, 22, 0, 0, 0, DateTimeKind.Utc),
                ReturnDate = new DateTime(2026, 4, 23, 0, 0, 0, DateTimeKind.Utc),
                RequestedDays = 3m,
                ApprovedDays = 3m,
                Status = "approved",
                ApprovedBy = "Gerencia RH",
                ApprovedAt = DateTime.UtcNow,
                Notes = "Solicitud demo para pruebas del módulo enterprise de RH.",
                CreatedBy = seedUser
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureSchemaAsync(NanchesoftDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS hr.hr_work_shifts (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    code character varying(30) NOT NULL,
    name character varying(160) NOT NULL,
    start_time character varying(10) NOT NULL,
    end_time character varying(10) NOT NULL,
    break_minutes integer NOT NULL,
    tolerance_minutes integer NOT NULL,
    is_overnight boolean NOT NULL,
    notes character varying(600) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_hr_work_shifts_company_code ON hr.hr_work_shifts (company_id, code);
");

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS hr.hr_work_schedules (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    work_shift_id uuid NULL,
    code character varying(30) NOT NULL,
    name character varying(160) NOT NULL,
    monday boolean NOT NULL,
    tuesday boolean NOT NULL,
    wednesday boolean NOT NULL,
    thursday boolean NOT NULL,
    friday boolean NOT NULL,
    saturday boolean NOT NULL,
    sunday boolean NOT NULL,
    weekly_hours numeric(18,2) NOT NULL,
    is_flexible boolean NOT NULL,
    notes character varying(600) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_hr_work_schedules_company_code ON hr.hr_work_schedules (company_id, code);
");

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS hr.hr_time_clock_devices (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    branch_id uuid NULL,
    code character varying(30) NOT NULL,
    name character varying(160) NOT NULL,
    brand character varying(100) NOT NULL,
    model character varying(100) NOT NULL,
    serial_number character varying(80) NOT NULL,
    ip_address character varying(50) NOT NULL,
    api_url character varying(250) NOT NULL,
    location character varying(160) NOT NULL,
    status character varying(30) NOT NULL,
    last_sync_at timestamp with time zone NULL,
    notes character varying(600) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_hr_time_clock_devices_company_code ON hr.hr_time_clock_devices (company_id, code);
");

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS hr.hr_leave_types (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    payroll_concept_id uuid NULL,
    code character varying(30) NOT NULL,
    name character varying(160) NOT NULL,
    category character varying(50) NOT NULL,
    with_pay boolean NOT NULL,
    impacts_payroll boolean NOT NULL,
    default_days numeric(18,2) NOT NULL,
    notes character varying(600) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_hr_leave_types_company_code ON hr.hr_leave_types (company_id, code);
");

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS hr.hr_vacation_requests (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    branch_id uuid NULL,
    employee_id uuid NOT NULL,
    leave_type_id uuid NULL,
    folio character varying(40) NOT NULL,
    request_date timestamp with time zone NOT NULL,
    start_date timestamp with time zone NOT NULL,
    end_date timestamp with time zone NOT NULL,
    return_date timestamp with time zone NULL,
    requested_days numeric(18,2) NOT NULL,
    approved_days numeric(18,2) NOT NULL,
    status character varying(30) NOT NULL,
    approved_by character varying(120) NOT NULL,
    approved_at timestamp with time zone NULL,
    notes character varying(600) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_hr_vacation_requests_company_folio ON hr.hr_vacation_requests (company_id, folio);
");
    }
}

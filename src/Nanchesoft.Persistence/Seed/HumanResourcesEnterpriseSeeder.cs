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
CREATE TABLE IF NOT EXISTS hr_work_shifts (
    ""Id"" uuid PRIMARY KEY,
    ""TenantId"" uuid NOT NULL,
    ""CompanyId"" uuid NOT NULL,
    ""Code"" character varying(30) NOT NULL,
    ""Name"" character varying(160) NOT NULL,
    ""StartTime"" character varying(10) NOT NULL,
    ""EndTime"" character varying(10) NOT NULL,
    ""BreakMinutes"" integer NOT NULL,
    ""ToleranceMinutes"" integer NOT NULL,
    ""IsOvernight"" boolean NOT NULL,
    ""Notes"" character varying(600) NOT NULL,
    ""IsActive"" boolean NOT NULL,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    ""CreatedBy"" text NULL,
    ""UpdatedAt"" timestamp with time zone NULL,
    ""UpdatedBy"" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_hr_work_shifts_company_code ON hr_work_shifts (""CompanyId"", ""Code"");
");

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS hr_work_schedules (
    ""Id"" uuid PRIMARY KEY,
    ""TenantId"" uuid NOT NULL,
    ""CompanyId"" uuid NOT NULL,
    ""WorkShiftId"" uuid NULL,
    ""Code"" character varying(30) NOT NULL,
    ""Name"" character varying(160) NOT NULL,
    ""Monday"" boolean NOT NULL,
    ""Tuesday"" boolean NOT NULL,
    ""Wednesday"" boolean NOT NULL,
    ""Thursday"" boolean NOT NULL,
    ""Friday"" boolean NOT NULL,
    ""Saturday"" boolean NOT NULL,
    ""Sunday"" boolean NOT NULL,
    ""WeeklyHours"" numeric(18,2) NOT NULL,
    ""IsFlexible"" boolean NOT NULL,
    ""Notes"" character varying(600) NOT NULL,
    ""IsActive"" boolean NOT NULL,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    ""CreatedBy"" text NULL,
    ""UpdatedAt"" timestamp with time zone NULL,
    ""UpdatedBy"" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_hr_work_schedules_company_code ON hr_work_schedules (""CompanyId"", ""Code"");
");

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS hr_time_clock_devices (
    ""Id"" uuid PRIMARY KEY,
    ""TenantId"" uuid NOT NULL,
    ""CompanyId"" uuid NOT NULL,
    ""BranchId"" uuid NULL,
    ""Code"" character varying(30) NOT NULL,
    ""Name"" character varying(160) NOT NULL,
    ""Brand"" character varying(100) NOT NULL,
    ""Model"" character varying(100) NOT NULL,
    ""SerialNumber"" character varying(80) NOT NULL,
    ""IpAddress"" character varying(50) NOT NULL,
    ""ApiUrl"" character varying(250) NOT NULL,
    ""Location"" character varying(160) NOT NULL,
    ""Status"" character varying(30) NOT NULL,
    ""LastSyncAt"" timestamp with time zone NULL,
    ""Notes"" character varying(600) NOT NULL,
    ""IsActive"" boolean NOT NULL,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    ""CreatedBy"" text NULL,
    ""UpdatedAt"" timestamp with time zone NULL,
    ""UpdatedBy"" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_hr_time_clock_devices_company_code ON hr_time_clock_devices (""CompanyId"", ""Code"");
");

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS hr_leave_types (
    ""Id"" uuid PRIMARY KEY,
    ""TenantId"" uuid NOT NULL,
    ""CompanyId"" uuid NOT NULL,
    ""PayrollConceptId"" uuid NULL,
    ""Code"" character varying(30) NOT NULL,
    ""Name"" character varying(160) NOT NULL,
    ""Category"" character varying(50) NOT NULL,
    ""WithPay"" boolean NOT NULL,
    ""ImpactsPayroll"" boolean NOT NULL,
    ""DefaultDays"" numeric(18,2) NOT NULL,
    ""Notes"" character varying(600) NOT NULL,
    ""IsActive"" boolean NOT NULL,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    ""CreatedBy"" text NULL,
    ""UpdatedAt"" timestamp with time zone NULL,
    ""UpdatedBy"" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_hr_leave_types_company_code ON hr_leave_types (""CompanyId"", ""Code"");
");

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS hr_vacation_requests (
    ""Id"" uuid PRIMARY KEY,
    ""TenantId"" uuid NOT NULL,
    ""CompanyId"" uuid NOT NULL,
    ""BranchId"" uuid NULL,
    ""EmployeeId"" uuid NOT NULL,
    ""LeaveTypeId"" uuid NULL,
    ""Folio"" character varying(40) NOT NULL,
    ""RequestDate"" timestamp with time zone NOT NULL,
    ""StartDate"" timestamp with time zone NOT NULL,
    ""EndDate"" timestamp with time zone NOT NULL,
    ""ReturnDate"" timestamp with time zone NULL,
    ""RequestedDays"" numeric(18,2) NOT NULL,
    ""ApprovedDays"" numeric(18,2) NOT NULL,
    ""Status"" character varying(30) NOT NULL,
    ""ApprovedBy"" character varying(120) NOT NULL,
    ""ApprovedAt"" timestamp with time zone NULL,
    ""Notes"" character varying(600) NOT NULL,
    ""IsActive"" boolean NOT NULL,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    ""CreatedBy"" text NULL,
    ""UpdatedAt"" timestamp with time zone NULL,
    ""UpdatedBy"" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_hr_vacation_requests_company_folio ON hr_vacation_requests (""CompanyId"", ""Folio"");
");
    }
}

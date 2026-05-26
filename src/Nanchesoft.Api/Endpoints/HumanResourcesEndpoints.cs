using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class HumanResourcesEndpoints
{
    public static IEndpointRouteBuilder MapHumanResourcesEndpoints(this IEndpointRouteBuilder app)
    {
        var departments = app.MapGroup("/api/hr/departments").WithTags("HrDepartments");
        departments.MapGet("/", GetDepartmentsAsync);
        departments.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.Departments.AsNoTracking().Include(x => x.Company).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el departamento." });
            return Results.Ok(new DepartmentDto { DepartmentId = entity.Id, TenantId = entity.TenantId, CompanyId = entity.CompanyId, CompanyName = entity.Company != null ? entity.Company.Name : string.Empty, Code = entity.Code, Name = entity.Name, Description = entity.Description, IsActive = entity.IsActive });
        });
        departments.MapPost("/", CreateDepartmentAsync);
        departments.MapPut("/{id:guid}", UpdateDepartmentAsync);
        departments.MapDelete("/{id:guid}", DeleteDepartmentAsync);

        var positions = app.MapGroup("/api/hr/positions").WithTags("HrPositions");
        positions.MapGet("/", GetPositionsAsync);
        positions.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.Positions.AsNoTracking().Include(x => x.Company).Include(x => x.Department).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el puesto." });
            return Results.Ok(new PositionDto { PositionId = entity.Id, TenantId = entity.TenantId, CompanyId = entity.CompanyId, CompanyName = entity.Company != null ? entity.Company.Name : string.Empty, DepartmentId = entity.DepartmentId, DepartmentName = entity.Department != null ? entity.Department.Name : string.Empty, Code = entity.Code, Name = entity.Name, Description = entity.Description, PayrollGroup = entity.PayrollGroup, BaseSalary = entity.BaseSalary, IsActive = entity.IsActive });
        });
        positions.MapPost("/", CreatePositionAsync);
        positions.MapPut("/{id:guid}", UpdatePositionAsync);
        positions.MapDelete("/{id:guid}", DeletePositionAsync);

        var employees = app.MapGroup("/api/hr/employees").WithTags("HrEmployees");
        employees.MapGet("/", GetEmployeesAsync);
        employees.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.Employees.AsNoTracking()
                .Include(x => x.Company).Include(x => x.Branch).Include(x => x.Department)
                .Include(x => x.Position).Include(x => x.WorkSchedule)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el colaborador." });
            return Results.Ok(new EmployeeDto
            {
                EmployeeId = entity.Id, TenantId = entity.TenantId, CompanyId = entity.CompanyId,
                CompanyName = entity.Company?.Name ?? string.Empty, BranchId = entity.BranchId,
                BranchName = entity.Branch?.Name ?? string.Empty, DepartmentId = entity.DepartmentId,
                DepartmentName = entity.Department?.Name ?? string.Empty, PositionId = entity.PositionId,
                PositionName = entity.Position?.Name ?? string.Empty, WorkScheduleId = entity.WorkScheduleId,
                WorkScheduleName = entity.WorkSchedule?.Name ?? string.Empty,
                Code = entity.Code, EmployeeNumber = entity.EmployeeNumber,
                ClockKey = entity.ClockKey, NoiKey = entity.NoiKey,
                FirstName = entity.FirstName, LastName = entity.LastName,
                SecondLastName = entity.SecondLastName, MiddleName = entity.MiddleName,
                FullName = entity.GetFullName(), Email = entity.Email, Phone = entity.Phone,
                EmergencyPhone = entity.EmergencyPhone, TaxId = entity.TaxId, NationalId = entity.NationalId,
                Curp = entity.Curp, Nss = entity.Nss, ImssRegId = entity.ImssRegId,
                Gender = entity.Gender, BloodType = entity.BloodType, MaritalStatus = entity.MaritalStatus,
                PlaceOfBirth = entity.PlaceOfBirth, Nationality = entity.Nationality,
                FatherName = entity.FatherName, MotherName = entity.MotherName,
                AddressStreet = entity.AddressStreet, AddressColony = entity.AddressColony,
                AddressCity = entity.AddressCity, AddressState = entity.AddressState,
                AddressZipCode = entity.AddressZipCode, ContractType = entity.ContractType,
                CotizationBase = entity.CotizationBase, SbcFija = entity.SbcFija,
                TaxRegime = entity.TaxRegime, EmployeeType = entity.EmployeeType,
                SalaryZone = entity.SalaryZone, PayrollPeriodType = entity.PayrollPeriodType,
                PaymentForm = entity.PaymentForm, BankCode = entity.BankCode,
                BankAccount = entity.BankAccount, Clabe = entity.Clabe, BankBranch = entity.BankBranch,
                HireDate = entity.HireDate, BirthDate = entity.BirthDate,
                TerminationDate = entity.TerminationDate, TerminationReason = entity.TerminationReason,
                ReentryDate = entity.ReentryDate, IsImssRegistered = entity.IsImssRegistered,
                ImssRegistrationDate = entity.ImssRegistrationDate, ImssTerminationDate = entity.ImssTerminationDate,
                Umf = entity.Umf, Afore = entity.Afore, Fonacot = entity.Fonacot, Infonavit = entity.Infonavit,
                ImmediateSupervisor = entity.ImmediateSupervisor, Category = entity.Category,
                Notes = entity.Notes, PrintReceipt = entity.PrintReceipt,
                DailySalary = entity.DailySalary, IntegratedDailySalary = entity.IntegratedDailySalary,
                Status = entity.Status, IsActive = entity.IsActive
            });
        });
        employees.MapGet("/report/excel", ExportEmployeesExcelAsync);
        employees.MapPost("/", CreateEmployeeAsync);
        employees.MapPut("/{id:guid}", UpdateEmployeeAsync);
        employees.MapDelete("/{id:guid}", DeleteEmployeeAsync);

        var incidents = app.MapGroup("/api/hr/incidents").WithTags("HrEmployeeIncidents");
        incidents.MapGet("/", GetEmployeeIncidentsAsync);
        incidents.MapPost("/", CreateEmployeeIncidentAsync);
        incidents.MapPost("/bulk", BulkCreateEmployeeIncidentsAsync);
        incidents.MapPut("/{id:guid}", UpdateEmployeeIncidentAsync);
        incidents.MapDelete("/{id:guid}", DeleteEmployeeIncidentAsync);

        var recurringIncidents = app.MapGroup("/api/hr/recurring-incidents").WithTags("HrRecurringIncidents");
        recurringIncidents.MapGet("/", GetRecurringIncidentRulesAsync);
        recurringIncidents.MapPost("/", CreateRecurringIncidentRuleAsync);
        recurringIncidents.MapPut("/{id:guid}", UpdateRecurringIncidentRuleAsync);
        recurringIncidents.MapDelete("/{id:guid}", DeleteRecurringIncidentRuleAsync);
        recurringIncidents.MapPost("/preview", PreviewRecurringIncidentsAsync);
        recurringIncidents.MapPost("/generate", GenerateRecurringIncidentsAsync);

        var attendancePolicies = app.MapGroup("/api/hr/attendance-policies").WithTags("AttendancePolicies");
        attendancePolicies.MapGet("/", GetAttendancePoliciesAsync);
        attendancePolicies.MapPost("/", CreateAttendancePolicyAsync);
        attendancePolicies.MapPut("/{id:guid}", UpdateAttendancePolicyAsync);
        attendancePolicies.MapDelete("/{id:guid}", DeleteAttendancePolicyAsync);

        var attendancePolicyRules = app.MapGroup("/api/hr/attendance-policy-rules").WithTags("AttendancePolicyRules");
        attendancePolicyRules.MapGet("/", GetAttendancePolicyRulesAsync);
        attendancePolicyRules.MapGet("/by-policy/{policyId:guid}", GetRulesByPolicyAsync);
        attendancePolicyRules.MapPost("/", CreateAttendancePolicyRuleAsync);
        attendancePolicyRules.MapPut("/{id:guid}", UpdateAttendancePolicyRuleAsync);
        attendancePolicyRules.MapDelete("/{id:guid}", DeleteAttendancePolicyRuleAsync);

        var contracts = app.MapGroup("/api/contracts/employee-contracts").WithTags("EmployeeContracts");
        contracts.MapGet("/", GetEmployeeContractsAsync);
        contracts.MapPost("/", CreateEmployeeContractAsync);
        contracts.MapPut("/{id:guid}", UpdateEmployeeContractAsync);
        contracts.MapDelete("/{id:guid}", DeleteEmployeeContractAsync);

        var periodTypes = app.MapGroup("/api/payroll/period-types").WithTags("PayrollPeriodTypes");
        periodTypes.MapGet("/", GetPayrollPeriodTypesAsync);
        periodTypes.MapPost("/", CreatePayrollPeriodTypeAsync);
        periodTypes.MapPut("/{id:guid}", UpdatePayrollPeriodTypeAsync);
        periodTypes.MapDelete("/{id:guid}", DeletePayrollPeriodTypeAsync);
        periodTypes.MapPost("/{id:guid}/generate-periods", GeneratePeriodsAsync);

        var periods = app.MapGroup("/api/payroll/periods").WithTags("PayrollPeriods");
        periods.MapGet("/", GetPayrollPeriodsAsync);
        periods.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.PayrollPeriods.AsNoTracking().Include(x => x.Company).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el periodo." });
            return Results.Ok(new PayrollPeriodDto { PayrollPeriodId = entity.Id, TenantId = entity.TenantId, CompanyId = entity.CompanyId, CompanyName = entity.Company != null ? entity.Company.Name : string.Empty, Code = entity.Code, Name = entity.Name, PeriodType = entity.PeriodType, StartDate = entity.StartDate, EndDate = entity.EndDate, PaymentDate = entity.PaymentDate, Status = entity.Status, IsImssInsured = entity.IsImssInsured, IsClosed = entity.IsClosed, IsActive = entity.IsActive });
        });
        periods.MapPost("/", CreatePayrollPeriodAsync);
        periods.MapPut("/{id:guid}", UpdatePayrollPeriodAsync);
        periods.MapDelete("/{id:guid}", DeletePayrollPeriodAsync);
        periods.MapPost("/{id:guid}/close", ClosePayrollPeriodAsync);
        periods.MapPost("/{id:guid}/reopen", ReopenPayrollPeriodAsync);

        var concepts = app.MapGroup("/api/payroll/concepts").WithTags("PayrollConcepts");
        concepts.MapGet("/", GetPayrollConceptsAsync);
        concepts.MapPost("/", CreatePayrollConceptAsync);
        concepts.MapPut("/{id:guid}", UpdatePayrollConceptAsync);
        concepts.MapDelete("/{id:guid}", DeletePayrollConceptAsync);

        var runs = app.MapGroup("/api/payroll/runs").WithTags("PayrollRuns");
        runs.MapGet("/", GetPayrollRunsAsync);
        runs.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.PayrollRuns.AsNoTracking().Include(x => x.Company).Include(x => x.Branch).Include(x => x.PayrollPeriod).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el proceso de nómina." });
            return Results.Ok(new PayrollRunDto { PayrollRunId = entity.Id, TenantId = entity.TenantId, CompanyId = entity.CompanyId, CompanyName = entity.Company != null ? entity.Company.Name : string.Empty, BranchId = entity.BranchId, BranchName = entity.Branch != null ? entity.Branch.Name : string.Empty, PayrollPeriodId = entity.PayrollPeriodId, PayrollPeriodName = entity.PayrollPeriod != null ? entity.PayrollPeriod.Name : string.Empty, Folio = entity.Folio, RunDate = entity.RunDate, Status = entity.Status, EmployeeCount = entity.EmployeeCount, GrossAmount = entity.GrossAmount, DeductionsAmount = entity.DeductionsAmount, NetAmount = entity.NetAmount, Notes = entity.Notes, IsActive = entity.IsActive });
        });
        runs.MapPost("/", CreatePayrollRunAsync);
        runs.MapPut("/{id:guid}", UpdatePayrollRunAsync);
        runs.MapDelete("/{id:guid}", DeletePayrollRunAsync);

        var runLines = app.MapGroup("/api/payroll/run-lines").WithTags("PayrollRunLines");
        runLines.MapGet("/", GetPayrollRunLinesAsync);
        runLines.MapPost("/", CreatePayrollRunLineAsync);
        runLines.MapPut("/{id:guid}", UpdatePayrollRunLineAsync);
        runLines.MapDelete("/{id:guid}", DeletePayrollRunLineAsync);

        return app;
    }

    private static async Task<(Guid? TenantId, Guid? CompanyId, Guid? BranchId)> ResolveDefaultContextAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        // Headers sent by ApiTenantScopeHandler always take priority
var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
var companyId = ApiTenantScope.ResolveCompanyId(httpContext);
var branchId = ApiTenantScope.ResolveBranchId(httpContext);
        if (companyId.HasValue)
        {
            if (!tenantId.HasValue)
                tenantId = await db.Companies.Where(x => x.Id == companyId.Value).Select(x => (Guid?)x.TenantId).FirstOrDefaultAsync();
            if (!branchId.HasValue)
                branchId = await db.Branches.Where(x => x.CompanyId == companyId.Value).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
            return (tenantId, companyId, branchId);
        }

        if (tenantId.HasValue)
        {
            var comp = await db.Companies.Where(x => x.TenantId == tenantId.Value).OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
            if (comp is not null)
            {
                if (!branchId.HasValue)
                    branchId = await db.Branches.Where(x => x.CompanyId == comp.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
                return (tenantId, comp.Id, branchId);
            }
        }

        // Last resort: first company in DB (legacy behaviour for seedless setups)
        var company = await db.Companies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        if (company is null)
            return (null, null, null);
        var fallbackBranch = await db.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
        return (company.TenantId, company.Id, fallbackBranch);
    }

    private static string NormalizeUpper(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();

    private static string NormalizeText(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string NormalizeStatus(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();

    private static async Task<IResult> GetDepartmentsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var rows = await db.Departments.AsNoTracking()
            .Where(x => tenantId.HasValue && x.TenantId == tenantId.Value
                     && (!companyId.HasValue || x.CompanyId == companyId.Value))
            .Include(x => x.Company)
            .OrderBy(x => x.Code)
            .Select(x => new DepartmentDto
            {
                DepartmentId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateDepartmentAsync(HttpContext httpContext, DepartmentRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para el departamento." });

        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (await db.Departments.AnyAsync(x => x.CompanyId == companyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un departamento con ese código." });

        var entity = new Department
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            Code = code,
            Name = name,
            Description = NormalizeText(request.Description),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Departments.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateDepartmentAsync(Guid id, DepartmentRequest request, NanchesoftDbContext db)
    {
        var entity = await db.Departments.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el departamento." });

        var code = NormalizeUpper(request.Code, entity.Code);
        if (await db.Departments.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro departamento con ese código." });

        entity.Code = code;
        entity.Name = NormalizeText(request.Name, entity.Name);
        entity.Description = NormalizeText(request.Description, entity.Description);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteDepartmentAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.Departments.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el departamento." });

        if (await db.Positions.AnyAsync(x => x.DepartmentId == id) || await db.Employees.AnyAsync(x => x.DepartmentId == id))
            return Results.BadRequest(new { message = "No puedes eliminar un departamento con puestos o empleados relacionados." });

        db.Departments.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetPositionsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var rows = await db.Positions.AsNoTracking()
            .Where(x => tenantId.HasValue && x.TenantId == tenantId.Value
                     && (!companyId.HasValue || x.CompanyId == companyId.Value))
            .Include(x => x.Company)
            .Include(x => x.Department)
            .OrderBy(x => x.Code)
            .Select(x => new PositionDto
            {
                PositionId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                PayrollGroup = x.PayrollGroup,
                BaseSalary = x.BaseSalary,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePositionAsync(HttpContext httpContext, PositionRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para el puesto." });

        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (await db.Positions.AnyAsync(x => x.CompanyId == companyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un puesto con ese código." });

        if (request.DepartmentId.HasValue && !await db.Departments.AnyAsync(x => x.Id == request.DepartmentId.Value))
            return Results.BadRequest(new { message = "No se encontró el departamento enviado." });

        var entity = new Position
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            DepartmentId = request.DepartmentId,
            Code = code,
            Name = name,
            Description = NormalizeText(request.Description),
            PayrollGroup = NormalizeText(request.PayrollGroup),
            BaseSalary = request.BaseSalary,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Positions.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePositionAsync(Guid id, PositionRequest request, NanchesoftDbContext db)
    {
        var entity = await db.Positions.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el puesto." });

        var code = NormalizeUpper(request.Code, entity.Code);
        if (await db.Positions.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro puesto con ese código." });

        if (request.DepartmentId.HasValue && !await db.Departments.AnyAsync(x => x.Id == request.DepartmentId.Value))
            return Results.BadRequest(new { message = "No se encontró el departamento enviado." });

        entity.DepartmentId = request.DepartmentId;
        entity.Code = code;
        entity.Name = NormalizeText(request.Name, entity.Name);
        entity.Description = NormalizeText(request.Description, entity.Description);
        entity.PayrollGroup = NormalizeText(request.PayrollGroup, entity.PayrollGroup);
        entity.BaseSalary = request.BaseSalary;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePositionAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.Positions.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el puesto." });

        if (await db.Employees.AnyAsync(x => x.PositionId == id))
            return Results.BadRequest(new { message = "No puedes eliminar un puesto con empleados relacionados." });

        db.Positions.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetEmployeesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var rows = await db.Employees.AsNoTracking()
            .Where(x => (!tenantId.HasValue || x.TenantId == tenantId.Value)
                     && (!companyId.HasValue || x.CompanyId == companyId.Value))
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.Department)
            .Include(x => x.Position)
            .Include(x => x.WorkSchedule)
            .OrderBy(x => x.EmployeeNumber)
            .Select(x => new EmployeeDto
            {
                EmployeeId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                PositionId = x.PositionId,
                PositionName = x.Position != null ? x.Position.Name : string.Empty,
                WorkScheduleId = x.WorkScheduleId,
                WorkScheduleName = x.WorkSchedule != null ? x.WorkSchedule.Name : string.Empty,
                Code = x.Code,
                EmployeeNumber = x.EmployeeNumber,
                ClockKey = x.ClockKey,
                NoiKey = x.NoiKey,
                FirstName = x.FirstName,
                LastName = x.LastName,
                SecondLastName = x.SecondLastName,
                MiddleName = x.MiddleName,
                FullName = x.GetFullName(),
                Email = x.Email,
                Phone = x.Phone,
                EmergencyPhone = x.EmergencyPhone,
                TaxId = x.TaxId,
                NationalId = x.NationalId,
                Curp = x.Curp,
                Nss = x.Nss,
                ImssRegId = x.ImssRegId,
                Gender = x.Gender,
                BloodType = x.BloodType,
                MaritalStatus = x.MaritalStatus,
                PlaceOfBirth = x.PlaceOfBirth,
                Nationality = x.Nationality,
                FatherName = x.FatherName,
                MotherName = x.MotherName,
                AddressStreet = x.AddressStreet,
                AddressColony = x.AddressColony,
                AddressCity = x.AddressCity,
                AddressState = x.AddressState,
                AddressZipCode = x.AddressZipCode,
                ContractType = x.ContractType,
                CotizationBase = x.CotizationBase,
                SbcFija = x.SbcFija,
                TaxRegime = x.TaxRegime,
                EmployeeType = x.EmployeeType,
                SalaryZone = x.SalaryZone,
                PayrollPeriodType = x.PayrollPeriodType,
                PaymentForm = x.PaymentForm,
                BankCode = x.BankCode,
                BankAccount = x.BankAccount,
                Clabe = x.Clabe,
                BankBranch = x.BankBranch,
                HireDate = x.HireDate,
                BirthDate = x.BirthDate,
                TerminationDate = x.TerminationDate,
                TerminationReason = x.TerminationReason,
                ReentryDate = x.ReentryDate,
                IsImssRegistered = x.IsImssRegistered,
                ImssRegistrationDate = x.ImssRegistrationDate,
                ImssTerminationDate = x.ImssTerminationDate,
                Umf = x.Umf,
                Afore = x.Afore,
                Fonacot = x.Fonacot,
                Infonavit = x.Infonavit,
                ImmediateSupervisor = x.ImmediateSupervisor,
                Category = x.Category,
                Notes = x.Notes,
                PrintReceipt = x.PrintReceipt,
                PeriodSalary = x.PeriodSalary,
                DailySalary = x.DailySalary,
                IntegratedDailySalary = x.IntegratedDailySalary,
                Status = x.Status,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateEmployeeAsync(HttpContext httpContext, EmployeeRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? ApiTenantScope.ResolveTenantId(httpContext) ?? context.TenantId;
        var companyId = request.CompanyId ?? ApiTenantScope.ResolveCompanyId(httpContext) ?? context.CompanyId;
        var branchId = request.BranchId ?? ApiTenantScope.ResolveBranchId(httpContext) ?? context.BranchId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para el colaborador." });

        var code = NormalizeUpper(request.Code);
        var employeeNumber = NormalizeUpper(request.EmployeeNumber);

        if (string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(employeeNumber) ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName))
        {
            return Results.BadRequest(new { message = "Código, número, nombre y apellido son obligatorios." });
        }

        if (await db.Employees.AnyAsync(x => x.CompanyId == companyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un colaborador con ese código." });

        if (await db.Employees.AnyAsync(x => x.CompanyId == companyId && x.EmployeeNumber == employeeNumber))
            return Results.BadRequest(new { message = "Ya existe un colaborador con ese número." });

        if (request.DepartmentId.HasValue && !await db.Departments.AnyAsync(x => x.Id == request.DepartmentId.Value))
            return Results.BadRequest(new { message = "No se encontró el departamento enviado." });

        if (request.PositionId.HasValue && !await db.Positions.AnyAsync(x => x.Id == request.PositionId.Value))
            return Results.BadRequest(new { message = "No se encontró el puesto enviado." });

        if (branchId.HasValue && !await db.Branches.AnyAsync(x => x.Id == branchId.Value))
            return Results.BadRequest(new { message = "No se encontró la sucursal enviada." });

        var entity = new Employee
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = branchId,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            WorkScheduleId = request.WorkScheduleId,
            Code = code,
            EmployeeNumber = employeeNumber,
            ClockKey = request.ClockKey,
            NoiKey = request.NoiKey,
            FirstName = NormalizeText(request.FirstName),
            LastName = NormalizeText(request.LastName),
            SecondLastName = NormalizeText(request.SecondLastName),
            MiddleName = NormalizeText(request.MiddleName),
            Email = NormalizeText(request.Email),
            Phone = NormalizeText(request.Phone),
            EmergencyPhone = request.EmergencyPhone,
            TaxId = NormalizeUpper(request.TaxId),
            NationalId = NormalizeUpper(request.NationalId),
            Curp = NormalizeUpper(request.Curp),
            Nss = NormalizeUpper(request.Nss),
            ImssRegId = NormalizeUpper(request.ImssRegId),
            Gender = request.Gender,
            BloodType = request.BloodType,
            MaritalStatus = request.MaritalStatus,
            PlaceOfBirth = request.PlaceOfBirth,
            Nationality = request.Nationality,
            FatherName = request.FatherName,
            MotherName = request.MotherName,
            AddressStreet = request.AddressStreet,
            AddressColony = request.AddressColony,
            AddressCity = request.AddressCity,
            AddressState = request.AddressState,
            AddressZipCode = request.AddressZipCode,
            ContractType = NormalizeStatus(request.ContractType, "indefinite"),
            CotizationBase = NormalizeStatus(request.CotizationBase, "fixed"),
            SbcFija = request.SbcFija,
            TaxRegime = NormalizeStatus(request.TaxRegime, "sueldos_salarios"),
            EmployeeType = NormalizeStatus(request.EmployeeType, "base"),
            SalaryZone = string.IsNullOrWhiteSpace(request.SalaryZone) ? "A" : request.SalaryZone.ToUpperInvariant(),
            PayrollPeriodType = NormalizeStatus(request.PayrollPeriodType, "semanal"),
            PaymentForm = NormalizeStatus(request.PaymentForm, "tarjeta"),
            BankCode = NormalizeText(request.BankCode),
            BankAccount = NormalizeText(request.BankAccount),
            Clabe = NormalizeText(request.Clabe),
            BankBranch = request.BankBranch,
            HireDate = request.HireDate?.Date ?? DateTime.UtcNow.Date,
            BirthDate = request.BirthDate?.Date,
            TerminationDate = request.TerminationDate?.Date,
            TerminationReason = request.TerminationReason,
            ReentryDate = request.ReentryDate?.Date,
            IsImssRegistered = request.IsImssRegistered,
            ImssRegistrationDate = request.ImssRegistrationDate?.Date,
            ImssTerminationDate = request.ImssTerminationDate?.Date,
            Umf = request.Umf,
            Afore = request.Afore,
            Fonacot = request.Fonacot,
            Infonavit = request.Infonavit,
            ImmediateSupervisor = request.ImmediateSupervisor,
            Category = request.Category,
            Notes = request.Notes,
            PrintReceipt = request.PrintReceipt,
            PeriodSalary = request.PeriodSalary,
            DailySalary = request.DailySalary,
            IntegratedDailySalary = request.IntegratedDailySalary,
            Status = NormalizeStatus(request.Status, "active"),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Employees.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateEmployeeAsync(Guid id, EmployeeRequest request, NanchesoftDbContext db)
    {
        var entity = await db.Employees.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el colaborador." });

        var code = NormalizeUpper(request.Code, entity.Code);
        var employeeNumber = NormalizeUpper(request.EmployeeNumber, entity.EmployeeNumber);

        if (await db.Employees.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro colaborador con ese código." });

        if (await db.Employees.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.EmployeeNumber == employeeNumber))
            return Results.BadRequest(new { message = "Ya existe otro colaborador con ese número." });

        if (request.DepartmentId.HasValue && !await db.Departments.AnyAsync(x => x.Id == request.DepartmentId.Value))
            return Results.BadRequest(new { message = "No se encontró el departamento enviado." });

        if (request.PositionId.HasValue && !await db.Positions.AnyAsync(x => x.Id == request.PositionId.Value))
            return Results.BadRequest(new { message = "No se encontró el puesto enviado." });

        if (request.BranchId.HasValue && !await db.Branches.AnyAsync(x => x.Id == request.BranchId.Value))
            return Results.BadRequest(new { message = "No se encontró la sucursal enviada." });

        entity.BranchId = request.BranchId;
        entity.DepartmentId = request.DepartmentId;
        entity.PositionId = request.PositionId;
        entity.WorkScheduleId = request.WorkScheduleId;
        entity.Code = code;
        entity.EmployeeNumber = employeeNumber;
        entity.ClockKey = request.ClockKey;
        entity.NoiKey = request.NoiKey;
        entity.FirstName = NormalizeText(request.FirstName, entity.FirstName);
        entity.LastName = NormalizeText(request.LastName, entity.LastName);
        entity.SecondLastName = NormalizeText(request.SecondLastName, entity.SecondLastName);
        entity.MiddleName = NormalizeText(request.MiddleName, entity.MiddleName);
        entity.Email = NormalizeText(request.Email, entity.Email);
        entity.Phone = NormalizeText(request.Phone, entity.Phone);
        entity.EmergencyPhone = request.EmergencyPhone;
        entity.TaxId = NormalizeUpper(request.TaxId, entity.TaxId);
        entity.NationalId = NormalizeUpper(request.NationalId, entity.NationalId);
        entity.Curp = NormalizeUpper(request.Curp, entity.Curp);
        entity.Nss = NormalizeUpper(request.Nss, entity.Nss);
        entity.ImssRegId = NormalizeUpper(request.ImssRegId, entity.ImssRegId);
        entity.Gender = request.Gender;
        entity.BloodType = request.BloodType;
        entity.MaritalStatus = request.MaritalStatus;
        entity.PlaceOfBirth = request.PlaceOfBirth;
        entity.Nationality = request.Nationality;
        entity.FatherName = request.FatherName;
        entity.MotherName = request.MotherName;
        entity.AddressStreet = request.AddressStreet;
        entity.AddressColony = request.AddressColony;
        entity.AddressCity = request.AddressCity;
        entity.AddressState = request.AddressState;
        entity.AddressZipCode = request.AddressZipCode;
        entity.ContractType = NormalizeStatus(request.ContractType, entity.ContractType);
        entity.CotizationBase = NormalizeStatus(request.CotizationBase, entity.CotizationBase);
        entity.SbcFija = request.SbcFija > 0 ? request.SbcFija : entity.SbcFija;
        entity.TaxRegime = NormalizeStatus(request.TaxRegime, entity.TaxRegime);
        entity.EmployeeType = NormalizeStatus(request.EmployeeType, entity.EmployeeType);
        entity.SalaryZone = string.IsNullOrWhiteSpace(request.SalaryZone) ? entity.SalaryZone : request.SalaryZone.ToUpperInvariant();
        entity.PayrollPeriodType = NormalizeStatus(request.PayrollPeriodType, entity.PayrollPeriodType);
        entity.PaymentForm = NormalizeStatus(request.PaymentForm, entity.PaymentForm);
        entity.BankCode = NormalizeText(request.BankCode, entity.BankCode);
        entity.BankAccount = NormalizeText(request.BankAccount, entity.BankAccount);
        entity.Clabe = NormalizeText(request.Clabe, entity.Clabe);
        entity.BankBranch = request.BankBranch;
        entity.HireDate = request.HireDate?.Date ?? entity.HireDate;
        entity.BirthDate = request.BirthDate?.Date;
        entity.TerminationDate = request.TerminationDate?.Date;
        entity.TerminationReason = request.TerminationReason;
        entity.ReentryDate = request.ReentryDate?.Date;
        entity.IsImssRegistered = request.IsImssRegistered;
        entity.ImssRegistrationDate = request.ImssRegistrationDate?.Date;
        entity.ImssTerminationDate = request.ImssTerminationDate?.Date;
        entity.Umf = request.Umf;
        entity.Afore = request.Afore;
        entity.Fonacot = request.Fonacot;
        entity.Infonavit = request.Infonavit;
        entity.ImmediateSupervisor = request.ImmediateSupervisor;
        entity.Category = request.Category;
        entity.Notes = request.Notes;
        entity.PrintReceipt = request.PrintReceipt;
        entity.PeriodSalary = request.PeriodSalary;
        entity.DailySalary = request.DailySalary;
        entity.IntegratedDailySalary = request.IntegratedDailySalary;
        entity.Status = NormalizeStatus(request.Status, entity.Status);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteEmployeeAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.Employees.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el colaborador." });

        if (await db.EmployeeContracts.AnyAsync(x => x.EmployeeId == id) ||
            await db.EmployeeIncidents.AnyAsync(x => x.EmployeeId == id) ||
            await db.PayrollRunLines.AnyAsync(x => x.EmployeeId == id))
        {
            return Results.BadRequest(new { message = "No puedes eliminar un colaborador con contratos, incidencias o recibos relacionados." });
        }

        db.Employees.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> ExportEmployeesExcelAsync(NanchesoftDbContext db)
    {
        var rows = await db.Employees.AsNoTracking()
            .Include(x => x.Company).Include(x => x.Branch)
            .Include(x => x.Department).Include(x => x.Position).Include(x => x.WorkSchedule)
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Colaboradores");

        string[] headers =
        [
            "Código", "No. Empleado", "Apellido Pat.", "Apellido Mat.", "Nombre(s)",
            "Nombre completo", "RFC", "CURP", "NSS", "Género", "F. Nacimiento",
            "Estado civil", "Teléfono", "Tel. emergencia", "Email",
            "Empresa", "Sucursal", "Departamento", "Puesto", "Horario",
            "F. Ingreso", "F. Baja", "Tipo contrato", "Período nómina", "Forma pago",
            "Salario diario", "SBC", "Zona", "Banco", "CLABE",
            "IMSS reg.", "Afore", "Fonacot", "Infonavit", "Estatus"
        ];

        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var e in rows)
        {
            ws.Cell(row, 1).Value = e.Code;
            ws.Cell(row, 2).Value = e.EmployeeNumber;
            ws.Cell(row, 3).Value = e.LastName;
            ws.Cell(row, 4).Value = e.SecondLastName ?? "";
            ws.Cell(row, 5).Value = e.FirstName + (string.IsNullOrWhiteSpace(e.MiddleName) ? "" : " " + e.MiddleName);
            ws.Cell(row, 6).Value = e.GetFullName();
            ws.Cell(row, 7).Value = e.TaxId;
            ws.Cell(row, 8).Value = e.Curp;
            ws.Cell(row, 9).Value = e.Nss;
            ws.Cell(row, 10).Value = e.Gender ?? "";
            ws.Cell(row, 11).Value = e.BirthDate.HasValue ? e.BirthDate.Value.ToString("yyyy-MM-dd") : "";
            ws.Cell(row, 12).Value = e.MaritalStatus ?? "";
            ws.Cell(row, 13).Value = e.Phone;
            ws.Cell(row, 14).Value = e.EmergencyPhone ?? "";
            ws.Cell(row, 15).Value = e.Email;
            ws.Cell(row, 16).Value = e.Company?.Name ?? "";
            ws.Cell(row, 17).Value = e.Branch?.Name ?? "";
            ws.Cell(row, 18).Value = e.Department?.Name ?? "";
            ws.Cell(row, 19).Value = e.Position?.Name ?? "";
            ws.Cell(row, 20).Value = e.WorkSchedule?.Name ?? "";
            ws.Cell(row, 21).Value = e.HireDate.ToString("yyyy-MM-dd");
            ws.Cell(row, 22).Value = e.TerminationDate.HasValue ? e.TerminationDate.Value.ToString("yyyy-MM-dd") : "";
            ws.Cell(row, 23).Value = e.ContractType;
            ws.Cell(row, 24).Value = e.PayrollPeriodType;
            ws.Cell(row, 25).Value = e.PaymentForm;
            ws.Cell(row, 26).Value = e.DailySalary;
            ws.Cell(row, 27).Value = e.IntegratedDailySalary;
            ws.Cell(row, 28).Value = e.SalaryZone;
            ws.Cell(row, 29).Value = e.BankCode;
            ws.Cell(row, 30).Value = e.Clabe;
            ws.Cell(row, 31).Value = e.IsImssRegistered ? "Sí" : "No";
            ws.Cell(row, 32).Value = e.Afore ?? "";
            ws.Cell(row, 33).Value = e.Fonacot ?? "";
            ws.Cell(row, 34).Value = e.Infonavit ?? "";
            ws.Cell(row, 35).Value = e.Status;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        var bytes = ms.ToArray();
        var fileName = $"colaboradores_{DateTime.Now:yyyyMMdd}.xlsx";
        return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private static async Task<IResult> GetEmployeeContractsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var rows = await db.EmployeeContracts.AsNoTracking()
            .Where(x => tenantId.HasValue && x.TenantId == tenantId.Value
                     && (!companyId.HasValue || x.CompanyId == companyId.Value))
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.Employee)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new EmployeeContractDto
            {
                EmployeeContractId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? x.Employee.GetFullName() : string.Empty,
                ContractNumber = x.ContractNumber,
                ContractType = x.ContractType,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                PaymentFrequency = x.PaymentFrequency,
                BaseSalary = x.BaseSalary,
                IntegratedSalary = x.IntegratedSalary,
                Status = x.Status,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateEmployeeContractAsync(HttpContext httpContext, EmployeeContractRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? ApiTenantScope.ResolveTenantId(httpContext) ?? context.TenantId;
        var companyId = request.CompanyId ?? ApiTenantScope.ResolveCompanyId(httpContext) ?? context.CompanyId;
        var branchId = request.BranchId ?? ApiTenantScope.ResolveBranchId(httpContext) ?? context.BranchId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para el contrato." });

        if (!request.EmployeeId.HasValue || !await db.Employees.AnyAsync(x => x.Id == request.EmployeeId.Value))
            return Results.BadRequest(new { message = "No se encontró el colaborador enviado." });

        var contractNumber = NormalizeUpper(request.ContractNumber);
        if (string.IsNullOrWhiteSpace(contractNumber))
            return Results.BadRequest(new { message = "El número de contrato es obligatorio." });

        if (await db.EmployeeContracts.AnyAsync(x => x.CompanyId == companyId && x.ContractNumber == contractNumber))
            return Results.BadRequest(new { message = "Ya existe un contrato con ese número." });

        var entity = new EmployeeContract
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = branchId,
            EmployeeId = request.EmployeeId.Value,
            ContractNumber = contractNumber,
            ContractType = NormalizeStatus(request.ContractType, "indefinite"),
            StartDate = request.StartDate?.Date ?? DateTime.UtcNow.Date,
            EndDate = request.EndDate?.Date,
            PaymentFrequency = NormalizeStatus(request.PaymentFrequency, "quincenal"),
            BaseSalary = request.BaseSalary,
            IntegratedSalary = request.IntegratedSalary,
            Status = NormalizeStatus(request.Status, "draft"),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.EmployeeContracts.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateEmployeeContractAsync(Guid id, EmployeeContractRequest request, NanchesoftDbContext db)
    {
        var entity = await db.EmployeeContracts.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el contrato." });

        var contractNumber = NormalizeUpper(request.ContractNumber, entity.ContractNumber);
        if (await db.EmployeeContracts.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.ContractNumber == contractNumber))
            return Results.BadRequest(new { message = "Ya existe otro contrato con ese número." });

        if (!request.EmployeeId.HasValue || !await db.Employees.AnyAsync(x => x.Id == request.EmployeeId.Value))
            return Results.BadRequest(new { message = "No se encontró el colaborador enviado." });

        entity.BranchId = request.BranchId;
        entity.EmployeeId = request.EmployeeId.Value;
        entity.ContractNumber = contractNumber;
        entity.ContractType = NormalizeStatus(request.ContractType, entity.ContractType);
        entity.StartDate = request.StartDate?.Date ?? entity.StartDate;
        entity.EndDate = request.EndDate?.Date;
        entity.PaymentFrequency = NormalizeStatus(request.PaymentFrequency, entity.PaymentFrequency);
        entity.BaseSalary = request.BaseSalary;
        entity.IntegratedSalary = request.IntegratedSalary;
        entity.Status = NormalizeStatus(request.Status, entity.Status);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteEmployeeContractAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.EmployeeContracts.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el contrato." });

        db.EmployeeContracts.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetEmployeeIncidentsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);
        var branchId = ApiTenantScope.ResolveBranchId(httpContext);

        var qs = httpContext.Request.Query;
        Guid? periodId = qs.TryGetValue("periodId", out var pv) && Guid.TryParse(pv, out var pg) ? pg : null;
        Guid? employeeId = qs.TryGetValue("employeeId", out var ev) && Guid.TryParse(ev, out var eg) ? eg : null;

        var rows = await db.EmployeeIncidents.AsNoTracking()
            .Where(x => (!tenantId.HasValue || x.TenantId == tenantId.Value)
                     && (!companyId.HasValue || x.CompanyId == companyId.Value))
            .Where(x => !branchId.HasValue || !x.BranchId.HasValue || x.BranchId == branchId.Value)
            .Where(x => !periodId.HasValue || x.PayrollPeriodId == periodId.Value)
            .Where(x => !employeeId.HasValue || x.EmployeeId == employeeId.Value)
            .Where(x => !x.IsDeleted)
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.Employee)
            .Include(x => x.PayrollPeriod)
            .Include(x => x.PayrollIncidentType)
            .OrderByDescending(x => x.IncidentDate)
            .Select(x => new EmployeeIncidentDto
            {
                EmployeeIncidentId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? x.Employee.GetFullName() : string.Empty,
                PayrollPeriodId = x.PayrollPeriodId,
                PayrollPeriodName = x.PayrollPeriod != null ? x.PayrollPeriod.Name : string.Empty,
                PayrollIncidentTypeId = x.PayrollIncidentTypeId,
                NomPayrollIncidentTypeId = x.PayrollIncidentTypeId,
                NomPayrollIncidentTypeCode = x.PayrollIncidentType != null ? x.PayrollIncidentType.Code : string.Empty,
                NomPayrollIncidentTypeName = x.PayrollIncidentType != null ? x.PayrollIncidentType.Name : string.Empty,
                IncidentCategory = x.PayrollIncidentType != null ? x.PayrollIncidentType.IncidentCategory : string.Empty,
                AffectType = x.PayrollIncidentType != null ? x.PayrollIncidentType.AffectType : string.Empty,
                PayrollConceptType = x.PayrollIncidentType != null ? x.PayrollIncidentType.PayrollConceptType : string.Empty,
                Color = x.PayrollIncidentType != null ? x.PayrollIncidentType.Color : string.Empty,
                Icon = x.PayrollIncidentType != null ? x.PayrollIncidentType.Icon : string.Empty,
                IncidentDate = x.IncidentDate,
                IncidentType = x.IncidentType,
                Quantity = x.Quantity,
                Amount = x.Amount,
                Notes = x.Notes,
                Status = x.Status,
                Origin = x.Origin,
                ManuallyEdited = x.ManuallyEdited,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateEmployeeIncidentAsync(HttpContext httpContext, EmployeeIncidentRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para la incidencia." });

        var branchId = request.BranchId ?? context.BranchId;

        var validation = ValidateIncidentRequiredFields(request);
        if (validation is not null)
            return Results.BadRequest(new { message = validation });

        validation = await ValidateIncidentScopeAsync(db, tenantId.Value, companyId.Value, branchId, request.EmployeeId, request.PayrollPeriodId);
        if (validation is not null)
            return Results.BadRequest(new { message = validation });

        var incidentTypeId = request.PayrollIncidentTypeId ?? request.NomPayrollIncidentTypeId;
        if (!incidentTypeId.HasValue || incidentTypeId.Value == Guid.Empty)
            return Results.BadRequest(new { message = "payroll_incident_type_id es obligatorio." });

        var incidentType = await ResolveIncidentTypeAsync(db, tenantId.Value, companyId.Value, incidentTypeId.Value);
        if (incidentType is null)
            return Results.BadRequest(new { message = "El tipo de incidencia debe existir en el catálogo formal." });
        validation = ValidateIncidentAmounts(incidentType, request.Amount, request.Quantity);
        if (validation is not null)
            return Results.BadRequest(new { message = validation });

        var entity = new EmployeeIncident
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = branchId,
            EmployeeId = request.EmployeeId.Value,
            PayrollPeriodId = request.PayrollPeriodId,
            PayrollIncidentTypeId = incidentType.Id,
            IncidentDate = request.IncidentDate?.Date ?? DateTime.UtcNow.Date,
            IncidentType = incidentType.Code,
            Quantity = request.Quantity,
            Amount = request.Amount,
            Notes = NormalizeText(request.Notes),
            Status = NormalizeStatus(request.Status, "draft"),
            Origin = string.IsNullOrWhiteSpace(request.Origin) ? "manual" : request.Origin,
            ManuallyEdited = request.ManuallyEdited,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.EmployeeIncidents.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> BulkCreateEmployeeIncidentsAsync(HttpContext httpContext, BulkIncidentRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa." });
        if (request.EmployeeIds is null || request.EmployeeIds.Count == 0)
            return Results.BadRequest(new { message = "Debe seleccionar al menos un empleado." });
        if (!request.NomPayrollIncidentTypeId.HasValue)
            return Results.BadRequest(new { message = "payroll_incident_type_id es obligatorio." });

        var incidentType = await ResolveIncidentTypeAsync(db, tenantId.Value, companyId.Value, request.NomPayrollIncidentTypeId.Value);
        if (incidentType is null)
            return Results.BadRequest(new { message = "El tipo de incidencia debe existir en el catálogo formal." });

        if (request.PayrollPeriodId.HasValue)
        {
            var period = await db.PayrollPeriods.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.PayrollPeriodId.Value);
            if (period?.IsClosed == true)
                return Results.BadRequest(new { message = "El periodo está cerrado; no se pueden agregar incidencias." });
        }

        var incidentDate = request.IncidentDate?.Date ?? DateTime.UtcNow.Date;
        var created = 0;
        foreach (var empId in request.EmployeeIds.Distinct())
        {
            db.EmployeeIncidents.Add(new EmployeeIncident
            {
                TenantId = tenantId.Value,
                CompanyId = companyId.Value,
                EmployeeId = empId,
                PayrollPeriodId = request.PayrollPeriodId,
                PayrollIncidentTypeId = incidentType.Id,
                IncidentDate = incidentDate,
                IncidentType = incidentType.Code,
                Quantity = request.Quantity,
                Amount = request.Amount,
                Notes = NormalizeText(request.Notes),
                Status = "draft",
                Origin = "manual",
                ManuallyEdited = true,
                IsActive = true,
                CreatedBy = "web-api"
            });
            created++;
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, created });
    }

    private static async Task<IResult> UpdateEmployeeIncidentAsync(Guid id, EmployeeIncidentRequest request, NanchesoftDbContext db)
    {
        var entity = await db.EmployeeIncidents.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la incidencia." });

        var branchId = request.BranchId ?? entity.BranchId;
        var validation = ValidateIncidentRequiredFields(request);
        if (validation is not null)
            return Results.BadRequest(new { message = validation });

        validation = await ValidateIncidentScopeAsync(db, entity.TenantId, entity.CompanyId, branchId, request.EmployeeId, request.PayrollPeriodId);
        if (validation is not null)
            return Results.BadRequest(new { message = validation });

        var incidentTypeId = request.PayrollIncidentTypeId ?? request.NomPayrollIncidentTypeId;
        if (!incidentTypeId.HasValue || incidentTypeId.Value == Guid.Empty)
            return Results.BadRequest(new { message = "payroll_incident_type_id es obligatorio." });

        var incidentType = await ResolveIncidentTypeAsync(db, entity.TenantId, entity.CompanyId, incidentTypeId.Value);
        if (incidentType is null)
            return Results.BadRequest(new { message = "El tipo de incidencia debe existir en el catálogo formal." });
        validation = ValidateIncidentAmounts(incidentType, request.Amount, request.Quantity);
        if (validation is not null)
            return Results.BadRequest(new { message = validation });

        entity.BranchId = branchId;
        entity.EmployeeId = request.EmployeeId.Value;
        entity.PayrollPeriodId = request.PayrollPeriodId;
        entity.PayrollIncidentTypeId = incidentType.Id;
        entity.IncidentDate = request.IncidentDate?.Date ?? entity.IncidentDate;
        entity.IncidentType = incidentType.Code;
        entity.Quantity = request.Quantity;
        entity.Amount = request.Amount;
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.Status = NormalizeStatus(request.Status, entity.Status);
        entity.ManuallyEdited = true;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteEmployeeIncidentAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.EmployeeIncidents.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la incidencia." });

        entity.IsActive = false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = "web-api";
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<NomPayrollIncidentType?> ResolveIncidentTypeAsync(NanchesoftDbContext db, Guid tenantId, Guid companyId, Guid incidentTypeId)
    {
        return await db.NomPayrollIncidentTypes.FirstOrDefaultAsync(x =>
            x.Id == incidentTypeId
            && x.TenantId == tenantId
            && x.CompanyId == companyId
            && x.IsActive
            && !x.IsDeleted);
    }

    private static async Task<string?> ValidateIncidentScopeAsync(NanchesoftDbContext db, Guid tenantId, Guid companyId, Guid? branchId, Guid? employeeId, Guid? payrollPeriodId)
    {
        if (branchId.HasValue && !await db.Branches.AnyAsync(x =>
                x.Id == branchId.Value
                && x.TenantId == tenantId
                && x.CompanyId == companyId
                && x.IsActive))
            return "La sucursal no pertenece al tenant/empresa actual o está inactiva.";

        if (!employeeId.HasValue)
            return "El colaborador es obligatorio.";

        var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(x =>
            x.Id == employeeId.Value
            && x.TenantId == tenantId
            && x.CompanyId == companyId
            && x.IsActive);
        if (employee is null)
            return "El colaborador no pertenece al tenant/empresa actual o está inactivo.";
        if (branchId.HasValue && employee.BranchId.HasValue && employee.BranchId.Value != branchId.Value)
            return "El colaborador no pertenece a la sucursal seleccionada.";

        if (!payrollPeriodId.HasValue)
            return null;

        var period = await db.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(x =>
            x.Id == payrollPeriodId.Value
            && x.TenantId == tenantId
            && x.CompanyId == companyId
            && x.IsActive);
        if (period is null)
            return "El periodo de nómina no pertenece al tenant/empresa actual.";
        if (!IsOpenPayrollPeriod(period))
            return "No se permiten incidencias en periodos cerrados, timbrados, cancelados o bloqueados.";

        return null;
    }

    private static string? ValidateIncidentAmounts(NomPayrollIncidentType incidentType, decimal amount, decimal quantity)
    {
        if (quantity < 0m)
            return "La cantidad debe ser mayor o igual a 0.";
        if (amount < 0m)
            return "El importe debe ser mayor o igual a 0.";
        if (incidentType.RequiresAmount && amount <= 0m)
            return "El tipo de incidencia requiere importe mayor a cero.";
        if (incidentType.RequiresQuantity && quantity <= 0m)
            return "El tipo de incidencia requiere cantidad mayor a cero.";
        return null;
    }

    private static string? ValidateIncidentRequiredFields(EmployeeIncidentRequest request)
    {
        if (!request.EmployeeId.HasValue || request.EmployeeId.Value == Guid.Empty)
            return "El colaborador es obligatorio.";
        var incidentTypeId = request.PayrollIncidentTypeId ?? request.NomPayrollIncidentTypeId;
        if (!incidentTypeId.HasValue || incidentTypeId.Value == Guid.Empty)
            return "El tipo de incidencia es obligatorio.";
        if (!request.IncidentDate.HasValue)
            return "La fecha es obligatoria.";
        if (request.Quantity < 0m)
            return "La cantidad debe ser mayor o igual a 0.";
        if (request.Amount < 0m)
            return "El importe debe ser mayor o igual a 0.";
        return null;
    }

    private static bool IsOpenPayrollPeriod(PayrollPeriod period)
    {
        if (!period.IsActive || period.IsClosed)
            return false;
        var status = NormalizeStatus(period.Status, "open");
        return status is "open" or "abierto" or "draft" or "captura" or "active" or "activo";
    }

    private static bool IsPayrollRunEditable(string? status)
    {
        var normalized = NormalizeStatus(status, "draft");
        return normalized is "draft" or "borrador" or "open" or "abierto" or "captura" or "pending" or "pendiente";
    }

    private static async Task<IResult> GetRecurringIncidentRulesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var rows = await db.HrRecurringIncidentRules.AsNoTracking()
            .Where(x => tenantId.HasValue && x.TenantId == tenantId.Value
                     && (!companyId.HasValue || x.CompanyId == companyId.Value))
            .Where(x => !x.IsDeleted)
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.Employee)
            .Include(x => x.NomPayrollIncidentType)
            .OrderBy(x => x.Employee != null ? x.Employee.LastName : string.Empty)
            .ThenBy(x => x.StartDate)
            .Select(x => new HrRecurringIncidentRuleDto
            {
                HrRecurringIncidentRuleId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? x.Employee.GetFullName() : string.Empty,
                NomPayrollIncidentTypeId = x.NomPayrollIncidentTypeId,
                NomPayrollIncidentTypeName = x.NomPayrollIncidentType != null ? x.NomPayrollIncidentType.Code + " · " + x.NomPayrollIncidentType.Name : string.Empty,
                IncidentCategory = x.NomPayrollIncidentType != null ? x.NomPayrollIncidentType.IncidentCategory : string.Empty,
                AffectType = x.NomPayrollIncidentType != null ? x.NomPayrollIncidentType.AffectType : string.Empty,
                Amount = x.Amount,
                Quantity = x.Quantity,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Frequency = x.Frequency,
                Notes = x.Notes,
                RequiresAuthorization = x.RequiresAuthorization,
                AuthorizedBy = x.AuthorizedBy,
                AuthorizedAt = x.AuthorizedAt,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateRecurringIncidentRuleAsync(HttpContext httpContext, HrRecurringIncidentRuleRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        var branchId = request.BranchId ?? context.BranchId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para la regla recurrente." });

        var validation = await ValidateIncidentScopeAsync(db, tenantId.Value, companyId.Value, branchId, request.EmployeeId, null);
        if (validation is not null)
            return Results.BadRequest(new { message = validation });

        if (!request.NomPayrollIncidentTypeId.HasValue || request.NomPayrollIncidentTypeId.Value == Guid.Empty)
            return Results.BadRequest(new { message = "payroll_incident_type_id es obligatorio." });
        var incidentType = await ResolveIncidentTypeAsync(db, tenantId.Value, companyId.Value, request.NomPayrollIncidentTypeId.Value);
        if (incidentType is null)
            return Results.BadRequest(new { message = "El tipo de incidencia debe existir y estar activo." });
        validation = ValidateIncidentAmounts(incidentType, request.Amount, request.Quantity);
        if (validation is not null)
            return Results.BadRequest(new { message = validation });

        var frequency = NormalizeFrequency(request.Frequency);
        if (frequency is null)
            return Results.BadRequest(new { message = "Frecuencia inválida." });

        var entity = new HrRecurringIncidentRule
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = branchId,
            EmployeeId = request.EmployeeId!.Value,
            NomPayrollIncidentTypeId = incidentType.Id,
            Amount = request.Amount,
            Quantity = request.Quantity <= 0m ? 1m : request.Quantity,
            StartDate = request.StartDate?.Date ?? DateTime.UtcNow.Date,
            EndDate = request.EndDate?.Date,
            Frequency = frequency,
            Notes = NormalizeText(request.Notes),
            RequiresAuthorization = request.RequiresAuthorization || incidentType.RequiresAuthorization,
            AuthorizedBy = NormalizeText(request.AuthorizedBy),
            AuthorizedAt = request.AuthorizedAt,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.HrRecurringIncidentRules.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateRecurringIncidentRuleAsync(Guid id, HrRecurringIncidentRuleRequest request, NanchesoftDbContext db)
    {
        var entity = await db.HrRecurringIncidentRules.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la regla recurrente." });

        var branchId = request.BranchId ?? entity.BranchId;
        var validation = await ValidateIncidentScopeAsync(db, entity.TenantId, entity.CompanyId, branchId, request.EmployeeId, null);
        if (validation is not null)
            return Results.BadRequest(new { message = validation });

        if (!request.NomPayrollIncidentTypeId.HasValue || request.NomPayrollIncidentTypeId.Value == Guid.Empty)
            return Results.BadRequest(new { message = "payroll_incident_type_id es obligatorio." });
        var incidentType = await ResolveIncidentTypeAsync(db, entity.TenantId, entity.CompanyId, request.NomPayrollIncidentTypeId.Value);
        if (incidentType is null)
            return Results.BadRequest(new { message = "El tipo de incidencia debe existir y estar activo." });
        validation = ValidateIncidentAmounts(incidentType, request.Amount, request.Quantity);
        if (validation is not null)
            return Results.BadRequest(new { message = validation });

        var frequency = NormalizeFrequency(request.Frequency);
        if (frequency is null)
            return Results.BadRequest(new { message = "Frecuencia inválida." });

        entity.BranchId = branchId;
        entity.EmployeeId = request.EmployeeId!.Value;
        entity.NomPayrollIncidentTypeId = incidentType.Id;
        entity.Amount = request.Amount;
        entity.Quantity = request.Quantity <= 0m ? 1m : request.Quantity;
        entity.StartDate = request.StartDate?.Date ?? entity.StartDate;
        entity.EndDate = request.EndDate?.Date;
        entity.Frequency = frequency;
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.RequiresAuthorization = request.RequiresAuthorization || incidentType.RequiresAuthorization;
        entity.AuthorizedBy = NormalizeText(request.AuthorizedBy);
        entity.AuthorizedAt = request.AuthorizedAt;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteRecurringIncidentRuleAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.HrRecurringIncidentRules.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la regla recurrente." });

        entity.IsActive = false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = "web-api";
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> PreviewRecurringIncidentsAsync(HttpContext httpContext, RecurringIncidentGenerationRequest request, NanchesoftDbContext db)
        => Results.Ok(await BuildRecurringIncidentGenerationAsync(httpContext, db, request, persist: false));

    private static async Task<IResult> GenerateRecurringIncidentsAsync(HttpContext httpContext, RecurringIncidentGenerationRequest request, NanchesoftDbContext db)
        => Results.Ok(await BuildRecurringIncidentGenerationAsync(httpContext, db, request, persist: true));

    private static async Task<RecurringIncidentGenerationResult> BuildRecurringIncidentGenerationAsync(HttpContext httpContext, NanchesoftDbContext db, RecurringIncidentGenerationRequest request, bool persist)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue)
            return new() { Success = false, Message = "No existe contexto de tenant/empresa." };

        var period = await db.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(x =>
            x.Id == request.PayrollPeriodId
            && x.TenantId == tenantId.Value
            && x.CompanyId == companyId.Value
            && x.IsActive);
        if (period is null || !IsOpenPayrollPeriod(period))
            return new() { Success = false, Message = "El periodo no existe o no está abierto." };

        var rules = await db.HrRecurringIncidentRules
            .Include(x => x.Employee)
            .Include(x => x.NomPayrollIncidentType)
            .Where(x => x.TenantId == tenantId.Value && x.CompanyId == companyId.Value && x.IsActive && !x.IsDeleted)
            .Where(x => !request.RuleId.HasValue || x.Id == request.RuleId.Value)
            .Where(x => x.StartDate.Date <= period.EndDate.Date && (!x.EndDate.HasValue || x.EndDate.Value.Date >= period.StartDate.Date))
            .ToListAsync();

        var result = new RecurringIncidentGenerationResult { Success = true, PayrollPeriodId = period.Id, PayrollPeriodName = period.Name };
        foreach (var rule in rules)
        {
            var type = rule.NomPayrollIncidentType;
            if (rule.Employee is null || type is null)
            {
                result.Omitted++;
                result.Items.Add(new() { RuleId = rule.Id, EmployeeName = "", IncidentTypeName = "", Action = "omitida", Reason = "Regla incompleta." });
                continue;
            }
            if (rule.RequiresAuthorization && string.IsNullOrWhiteSpace(rule.AuthorizedBy))
            {
                result.Omitted++;
                result.Items.Add(new() { RuleId = rule.Id, EmployeeName = rule.Employee.GetFullName(), IncidentTypeName = type.Name, Action = "omitida", Reason = "Requiere autorización." });
                continue;
            }
            if (await db.EmployeeIncidents.AnyAsync(x => x.RecurrentRuleId == rule.Id && x.PayrollPeriodId == period.Id && !x.IsDeleted))
            {
                result.Omitted++;
                result.Items.Add(new() { RuleId = rule.Id, EmployeeName = rule.Employee.GetFullName(), IncidentTypeName = type.Name, Action = "omitida", Reason = "Ya existe incidencia generada para este periodo." });
                continue;
            }

            result.EmployeesAffected++;
            if (type.IncidentCategory == "DEDUCCION") result.TotalDeductions += rule.Amount;
            if (type.IncidentCategory == "PERCEPCION") result.TotalPerceptions += rule.Amount;
            result.Generated++;
            result.Items.Add(new() { RuleId = rule.Id, EmployeeName = rule.Employee.GetFullName(), IncidentTypeName = type.Name, Action = persist ? "generada" : "vista_previa", Reason = "" });

            if (persist)
            {
                db.EmployeeIncidents.Add(new EmployeeIncident
                {
                    TenantId = rule.TenantId,
                    CompanyId = rule.CompanyId,
                    BranchId = rule.BranchId ?? rule.Employee.BranchId,
                    EmployeeId = rule.EmployeeId,
                    PayrollPeriodId = period.Id,
                    NomPayrollIncidentTypeId = rule.NomPayrollIncidentTypeId,
                    RecurrentRuleId = rule.Id,
                    IncidentDate = period.StartDate.Date,
                    IncidentType = type.Code,
                    Quantity = rule.Quantity,
                    Amount = rule.Amount,
                    Notes = string.IsNullOrWhiteSpace(rule.Notes) ? "Generada desde regla recurrente." : rule.Notes,
                    Status = "generated",
                    IsActive = true,
                    CreatedBy = "recurring-rule"
                });
            }
        }

        if (persist)
            await db.SaveChangesAsync();

        return result;
    }

    private static string? NormalizeFrequency(string? value)
    {
        var frequency = NormalizeStatus(value, "cada_periodo");
        return frequency is "semanal" or "quincenal" or "mensual" or "cada_periodo" ? frequency : null;
    }

    private static async Task<IResult> GetPayrollPeriodsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);
        var onlyOpen = string.Equals(httpContext.Request.Query["status"].ToString(), "open", StringComparison.OrdinalIgnoreCase)
            || string.Equals(httpContext.Request.Query["onlyOpen"].ToString(), "true", StringComparison.OrdinalIgnoreCase);

        var query = db.PayrollPeriods.AsNoTracking()
            .Where(x => (!tenantId.HasValue || x.TenantId == tenantId.Value)
                     && (!companyId.HasValue || x.CompanyId == companyId.Value));

        if (onlyOpen)
        {
            query = query.Where(x =>
                x.IsActive
                && !x.IsClosed
                && (x.Status == "open"
                    || x.Status == "abierto"
                    || x.Status == "draft"
                    || x.Status == "captura"
                    || x.Status == "active"
                    || x.Status == "activo"));
        }

        var rows = await query
            .Include(x => x.Company)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new PayrollPeriodDto
            {
                PayrollPeriodId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                Code = x.Code,
                Name = x.Name,
                PeriodType = x.PeriodType,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                PaymentDate = x.PaymentDate,
                Status = x.Status,
                IsImssInsured = x.IsImssInsured,
                IsClosed = x.IsClosed,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePayrollPeriodAsync(HttpContext httpContext, PayrollPeriodRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para el periodo." });

        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (await db.PayrollPeriods.AnyAsync(x => x.CompanyId == companyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un periodo con ese código." });

        var entity = new PayrollPeriod
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            Code = code,
            Name = name,
            PeriodType = NormalizeStatus(request.PeriodType, "quincenal"),
            StartDate = request.StartDate?.Date ?? DateTime.UtcNow.Date,
            EndDate = request.EndDate?.Date ?? DateTime.UtcNow.Date,
            PaymentDate = request.PaymentDate?.Date ?? DateTime.UtcNow.Date,
            Status = NormalizeStatus(request.Status, "draft"),
            IsImssInsured = request.IsImssInsured,
            IsClosed = request.IsClosed,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.PayrollPeriods.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePayrollPeriodAsync(Guid id, PayrollPeriodRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollPeriods.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el periodo." });

        var code = NormalizeUpper(request.Code, entity.Code);
        if (await db.PayrollPeriods.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro periodo con ese código." });

        entity.Code = code;
        entity.Name = NormalizeText(request.Name, entity.Name);
        entity.PeriodType = NormalizeStatus(request.PeriodType, entity.PeriodType);
        entity.StartDate = request.StartDate?.Date ?? entity.StartDate;
        entity.EndDate = request.EndDate?.Date ?? entity.EndDate;
        entity.PaymentDate = request.PaymentDate?.Date ?? entity.PaymentDate;
        entity.Status = NormalizeStatus(request.Status, entity.Status);
        entity.IsImssInsured = request.IsImssInsured;
        entity.IsClosed = request.IsClosed;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePayrollPeriodAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollPeriods.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el periodo." });

        if (await db.EmployeeIncidents.AnyAsync(x => x.PayrollPeriodId == id) || await db.PayrollRuns.AnyAsync(x => x.PayrollPeriodId == id))
            return Results.BadRequest(new { message = "No puedes eliminar un periodo con incidencias o procesos relacionados." });

        db.PayrollPeriods.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> ClosePayrollPeriodAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollPeriods.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el periodo." });
        if (entity.IsClosed)
            return Results.BadRequest(new { message = "El periodo ya está cerrado." });

        var hasDraft = await db.EmployeeIncidents
            .AnyAsync(x => x.PayrollPeriodId == id && x.Status == "draft" && !x.IsDeleted);
        if (hasDraft)
            return Results.BadRequest(new { message = "Existen incidencias en borrador. Apruébalas o elimínalas antes de cerrar el periodo." });

        entity.IsClosed = true;
        entity.Status = "cerrado";
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, message = "Periodo cerrado correctamente." });
    }

    private static async Task<IResult> ReopenPayrollPeriodAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollPeriods.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el periodo." });
        if (!entity.IsClosed)
            return Results.BadRequest(new { message = "El periodo ya está abierto." });

        entity.IsClosed = false;
        entity.Status = "captura";
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, message = "Periodo reabierto correctamente." });
    }

    private static async Task<IResult> GetPayrollPeriodTypesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var rows = await db.PayrollPeriodTypes.AsNoTracking()
            .Where(x => tenantId.HasValue && x.TenantId == tenantId.Value
                     && (!companyId.HasValue || x.CompanyId == companyId.Value))
            .Include(x => x.Company)
            .OrderBy(x => x.Code)
            .Select(x => new PayrollPeriodTypeDto
            {
                PayrollPeriodTypeId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                Code = x.Code,
                Name = x.Name,
                DaysPerPeriod = x.DaysPerPeriod,
                PeriodsPerYear = x.PeriodsPerYear,
                Notes = x.Notes,
                IsActive = x.IsActive,
                PaymentDays = x.PaymentDays,
                WorkingDays = x.WorkingDays,
                AdjustToCalendarMonth = x.AdjustToCalendarMonth,
                QuinceaAdjustType = x.QuinceaAdjustType,
                SeventhDayPosition = x.SeventhDayPosition,
                PaymentDayPosition = x.PaymentDayPosition
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePayrollPeriodTypeAsync(HttpContext httpContext, PayrollPeriodTypeRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para el tipo de nómina." });

        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (await db.PayrollPeriodTypes.AnyAsync(x => x.CompanyId == companyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un tipo de nómina con ese código." });

        var entity = new PayrollPeriodType
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            Code = code,
            Name = name,
            DaysPerPeriod = request.DaysPerPeriod,
            PeriodsPerYear = request.PeriodsPerYear,
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            PaymentDays = request.PaymentDays,
            WorkingDays = request.WorkingDays,
            AdjustToCalendarMonth = request.AdjustToCalendarMonth,
            QuinceaAdjustType = string.IsNullOrWhiteSpace(request.QuinceaAdjustType) ? "LaborDays" : request.QuinceaAdjustType,
            SeventhDayPosition = request.SeventhDayPosition,
            PaymentDayPosition = request.PaymentDayPosition,
            CreatedBy = "web-api"
        };

        db.PayrollPeriodTypes.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePayrollPeriodTypeAsync(Guid id, PayrollPeriodTypeRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollPeriodTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el tipo de nómina." });

        var code = NormalizeUpper(request.Code, entity.Code);
        if (await db.PayrollPeriodTypes.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro tipo de nómina con ese código." });

        entity.Code = code;
        entity.Name = NormalizeText(request.Name, entity.Name);
        entity.DaysPerPeriod = request.DaysPerPeriod;
        entity.PeriodsPerYear = request.PeriodsPerYear;
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.PaymentDays = request.PaymentDays;
        entity.WorkingDays = request.WorkingDays;
        entity.AdjustToCalendarMonth = request.AdjustToCalendarMonth;
        entity.QuinceaAdjustType = string.IsNullOrWhiteSpace(request.QuinceaAdjustType) ? "LaborDays" : request.QuinceaAdjustType;
        entity.SeventhDayPosition = request.SeventhDayPosition;
        entity.PaymentDayPosition = request.PaymentDayPosition;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePayrollPeriodTypeAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollPeriodTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el tipo de nómina." });

        db.PayrollPeriodTypes.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GeneratePeriodsAsync(Guid id, GeneratePeriodsRequest request, NanchesoftDbContext db)
    {
        var periodType = await db.PayrollPeriodTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (periodType is null)
            return Results.NotFound(new { message = "No se encontró el tipo de período." });

        if (periodType.DaysPerPeriod <= 0)
            return Results.BadRequest(new { message = "El tipo de período debe tener DaysPerPeriod > 0." });

        if (request.FiscalYear < 2000 || request.FiscalYear > 2100)
            return Results.BadRequest(new { message = "Año fiscal inválido." });

        var startDate = request.StartDate.Date;
        var daysPerPeriod = periodType.DaysPerPeriod;
        var periodsPerYear = periodType.PeriodsPerYear > 0 ? periodType.PeriodsPerYear : (365 / daysPerPeriod);

        var existing = await db.PayrollPeriods
            .Where(x => x.TenantId == periodType.TenantId
                     && x.CompanyId == periodType.CompanyId
                     && x.PayrollPeriodTypeId == id
                     && x.FiscalYear == request.FiscalYear)
            .AnyAsync();

        if (existing && !request.Overwrite)
            return Results.BadRequest(new { message = $"Ya existen periodos para el ejercicio {request.FiscalYear} con este tipo. Usa Overwrite=true para reemplazarlos." });

        if (existing && request.Overwrite)
        {
            var toDelete = await db.PayrollPeriods
                .Where(x => x.TenantId == periodType.TenantId
                         && x.CompanyId == periodType.CompanyId
                         && x.PayrollPeriodTypeId == id
                         && x.FiscalYear == request.FiscalYear
                         && x.Status == "draft")
                .ToListAsync();
            db.PayrollPeriods.RemoveRange(toDelete);
        }

        var generated = new List<PayrollPeriod>();
        var current = startDate;

        for (int n = 1; n <= periodsPerYear; n++)
        {
            var end = current.AddDays(daysPerPeriod - 1);
            var payDate = end.AddDays(periodType.PaymentDayPosition > 0 ? periodType.PaymentDayPosition : 3);

            var period = new PayrollPeriod
            {
                TenantId = periodType.TenantId,
                CompanyId = periodType.CompanyId,
                PayrollPeriodTypeId = id,
                FiscalYear = request.FiscalYear,
                PeriodNumber = n,
                Code = $"{periodType.Code}-{request.FiscalYear}-{n:D2}",
                Name = $"{periodType.Name} {n:D2}/{request.FiscalYear}",
                PeriodType = periodType.Code,
                StartDate = current,
                EndDate = end,
                PaymentDate = payDate,
                IsStartOfMonth = current.Day == 1,
                IsEndOfMonth = end.Day == DateTime.DaysInMonth(end.Year, end.Month),
                IsStartOfYear = n == 1,
                IsEndOfYear = n == periodsPerYear,
                IsBimesterStart = n % 2 == 1,
                IsBimesterEnd = n % 2 == 0,
                Status = "draft",
                IsImssInsured = true,
                CreatedBy = "generate-periods"
            };

            generated.Add(period);
            current = end.AddDays(1);
        }

        db.PayrollPeriods.AddRange(generated);
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, generated = generated.Count, fiscalYear = request.FiscalYear });
    }

    private static async Task<IResult> GetPayrollConceptsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var scope = ApiTenantScope.RequireScope(httpContext);
        if (!scope.IsValid) return scope.Error!;

        var rows = await db.PayrollConcepts.AsNoTracking()
            .Where(x => x.TenantId == scope.TenantId
                     && (!scope.CompanyId.HasValue || x.CompanyId == scope.CompanyId.Value))
            .Include(x => x.Company)
            .OrderBy(x => x.Code)
            .Select(x => new PayrollConceptDto
            {
                PayrollConceptId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                Code = x.Code,
                Name = x.Name,
                ConceptType = x.ConceptType,
                CalculationType = x.CalculationType,
                SatCode = x.SatCode,
                SatAgrupador = x.SatAgrupador,
                TaxableType = x.TaxableType,
                TaxablePercent = x.TaxablePercent,
                ExemptPercent = x.ExemptPercent,
                IsRecurring = x.IsRecurring,
                IsAutomatic = x.IsAutomatic,
                PrintOnReceipt = x.PrintOnReceipt,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive,
                Formula = x.Formula,
                TaxableFormula = x.TaxableFormula,
                ExemptFormula = x.ExemptFormula,
                ImssTaxableFormula = x.ImssTaxableFormula,
                SatTipoPercepcionCode = x.SatTipoPercepcionCode,
                SatTipoDeduccionCode = x.SatTipoDeduccionCode,
                SatTipoOtroPagoCode = x.SatTipoOtroPagoCode,
                AutomaticOnGlobalRun = x.AutomaticOnGlobalRun,
                AutomaticOnTermination = x.AutomaticOnTermination,
                IsInKind = x.IsInKind,
                AffectsSeventhDay = x.AffectsSeventhDay,
                AffectsHolidayPay = x.AffectsHolidayPay,
                AffectsImss = x.AffectsImss,
                AffectsIsr = x.AffectsIsr,
                AffectsAccumulators = x.AffectsAccumulators,
                RequiresSatStamping = x.RequiresSatStamping,
                MinAmount = x.MinAmount,
                MaxAmount = x.MaxAmount
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePayrollConceptAsync(HttpContext httpContext, PayrollConceptRequest request, NanchesoftDbContext db)
    {
        var scope = ApiTenantScope.RequireScope(httpContext);
        if (!scope.IsValid) return scope.Error!;

        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = scope.TenantId;
        var companyId = scope.CompanyId ?? request.CompanyId ?? context.CompanyId;

        if (!companyId.HasValue)
            return Results.BadRequest(new { message = "Selecciona empresa activa para crear el concepto." });

        // Forzar que la compañía pertenezca al tenant del usuario.
        var owns = await db.Companies.AnyAsync(x => x.Id == companyId.Value && x.TenantId == tenantId);
        if (!owns)
            return Results.BadRequest(new { message = "La empresa no pertenece a tu tenant." });

        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (await db.PayrollConcepts.AnyAsync(x => x.CompanyId == companyId.Value && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un concepto con ese código." });

        var entity = new PayrollConcept
        {
            TenantId = tenantId,
            CompanyId = companyId.Value,
            Code = code,
            Name = name,
            ConceptType = NormalizeStatus(request.ConceptType, "earning"),
            CalculationType = NormalizeStatus(request.CalculationType, "manual"),
            SatCode = NormalizeText(request.SatCode),
            SatAgrupador = NormalizeText(request.SatAgrupador),
            TaxableType = NormalizeStatus(request.TaxableType, "not_applicable"),
            TaxablePercent = request.TaxablePercent > 0 ? request.TaxablePercent : 100m,
            ExemptPercent = request.ExemptPercent,
            IsRecurring = request.IsRecurring,
            IsAutomatic = request.IsAutomatic,
            PrintOnReceipt = request.PrintOnReceipt,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            Formula = NormalizeText(request.Formula),
            TaxableFormula = NormalizeText(request.TaxableFormula),
            ExemptFormula = NormalizeText(request.ExemptFormula),
            ImssTaxableFormula = NormalizeText(request.ImssTaxableFormula),
            SatTipoPercepcionCode = NormalizeUpper(request.SatTipoPercepcionCode),
            SatTipoDeduccionCode = NormalizeUpper(request.SatTipoDeduccionCode),
            SatTipoOtroPagoCode = NormalizeUpper(request.SatTipoOtroPagoCode),
            AutomaticOnGlobalRun = request.AutomaticOnGlobalRun,
            AutomaticOnTermination = request.AutomaticOnTermination,
            IsInKind = request.IsInKind,
            AffectsSeventhDay = request.AffectsSeventhDay,
            AffectsHolidayPay = request.AffectsHolidayPay,
            AffectsImss = request.AffectsImss,
            AffectsIsr = request.AffectsIsr,
            AffectsAccumulators = request.AffectsAccumulators,
            RequiresSatStamping = request.RequiresSatStamping,
            MinAmount = Math.Max(0m, request.MinAmount),
            MaxAmount = Math.Max(0m, request.MaxAmount),
            CreatedBy = "web-api"
        };

        db.PayrollConcepts.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePayrollConceptAsync(HttpContext httpContext, Guid id, PayrollConceptRequest request, NanchesoftDbContext db)
    {
        var scope = ApiTenantScope.RequireScope(httpContext);
        if (!scope.IsValid) return scope.Error!;

        var entity = await db.PayrollConcepts.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == scope.TenantId);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el concepto en tu tenant." });

        var code = NormalizeUpper(request.Code, entity.Code);
        if (await db.PayrollConcepts.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro concepto con ese código." });

        entity.Code = code;
        entity.Name = NormalizeText(request.Name, entity.Name);
        entity.ConceptType = NormalizeStatus(request.ConceptType, entity.ConceptType);
        entity.CalculationType = NormalizeStatus(request.CalculationType, entity.CalculationType);
        entity.SatCode = NormalizeText(request.SatCode, entity.SatCode);
        entity.SatAgrupador = NormalizeText(request.SatAgrupador, entity.SatAgrupador);
        entity.TaxableType = NormalizeStatus(request.TaxableType, entity.TaxableType);
        entity.TaxablePercent = request.TaxablePercent > 0 ? request.TaxablePercent : entity.TaxablePercent;
        entity.ExemptPercent = request.ExemptPercent;
        entity.IsRecurring = request.IsRecurring;
        entity.IsAutomatic = request.IsAutomatic;
        entity.PrintOnReceipt = request.PrintOnReceipt;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        entity.Formula = NormalizeText(request.Formula, entity.Formula);
        entity.TaxableFormula = NormalizeText(request.TaxableFormula, entity.TaxableFormula);
        entity.ExemptFormula = NormalizeText(request.ExemptFormula, entity.ExemptFormula);
        entity.ImssTaxableFormula = NormalizeText(request.ImssTaxableFormula, entity.ImssTaxableFormula);
        entity.SatTipoPercepcionCode = NormalizeUpper(request.SatTipoPercepcionCode, entity.SatTipoPercepcionCode);
        entity.SatTipoDeduccionCode = NormalizeUpper(request.SatTipoDeduccionCode, entity.SatTipoDeduccionCode);
        entity.SatTipoOtroPagoCode = NormalizeUpper(request.SatTipoOtroPagoCode, entity.SatTipoOtroPagoCode);
        entity.AutomaticOnGlobalRun = request.AutomaticOnGlobalRun;
        entity.AutomaticOnTermination = request.AutomaticOnTermination;
        entity.IsInKind = request.IsInKind;
        entity.AffectsSeventhDay = request.AffectsSeventhDay;
        entity.AffectsHolidayPay = request.AffectsHolidayPay;
        entity.AffectsImss = request.AffectsImss;
        entity.AffectsIsr = request.AffectsIsr;
        entity.AffectsAccumulators = request.AffectsAccumulators;
        entity.RequiresSatStamping = request.RequiresSatStamping;
        entity.MinAmount = Math.Max(0m, request.MinAmount);
        entity.MaxAmount = Math.Max(0m, request.MaxAmount);
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePayrollConceptAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        var scope = ApiTenantScope.RequireScope(httpContext);
        if (!scope.IsValid) return scope.Error!;

        var entity = await db.PayrollConcepts.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == scope.TenantId);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el concepto en tu tenant." });

        db.PayrollConcepts.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetPayrollRunsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var rows = await db.PayrollRuns.AsNoTracking()
            .Where(x => tenantId.HasValue && x.TenantId == tenantId.Value
                     && (!companyId.HasValue || x.CompanyId == companyId.Value))
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.PayrollPeriod)
            .OrderByDescending(x => x.RunDate)
            .Select(x => new PayrollRunDto
            {
                PayrollRunId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                PayrollPeriodId = x.PayrollPeriodId,
                PayrollPeriodName = x.PayrollPeriod != null ? x.PayrollPeriod.Name : string.Empty,
                Folio = x.Folio,
                RunDate = x.RunDate,
                Status = x.Status,
                EmployeeCount = x.EmployeeCount,
                GrossAmount = x.GrossAmount,
                DeductionsAmount = x.DeductionsAmount,
                NetAmount = x.NetAmount,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePayrollRunAsync(HttpContext httpContext, PayrollRunRequest request, NanchesoftDbContext db)
    {
        if (!request.PayrollPeriodId.HasValue)
            return Results.BadRequest(new { message = "No se encontró el periodo de nómina enviado." });

        var period = await db.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.PayrollPeriodId.Value);
        if (period is null)
            return Results.BadRequest(new { message = "No se encontró el periodo de nómina enviado." });

        // Fuente de verdad: el periodo. Así la corrida no cae al tenant/company/branch DEMO.
        var tenantId = period.TenantId;
        var companyId = period.CompanyId;
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var requestedBranchId = request.BranchId ?? context.BranchId;
        Guid? branchId = null;

        if (requestedBranchId.HasValue && await db.Branches.AnyAsync(x => x.Id == requestedBranchId.Value && x.CompanyId == companyId && x.IsActive))
            branchId = requestedBranchId.Value;
        else
            branchId = await db.Branches
                .Where(x => x.CompanyId == companyId && x.IsActive)
                .OrderBy(x => x.CreatedAt)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync();

        var folio = NormalizeUpper(request.Folio);
        if (string.IsNullOrWhiteSpace(folio))
            return Results.BadRequest(new { message = "El folio es obligatorio." });

        if (await db.PayrollRuns.AnyAsync(x => x.CompanyId == companyId && x.Folio == folio))
            return Results.BadRequest(new { message = "Ya existe un proceso de nómina con ese folio." });

        var entity = new PayrollRun
        {
            TenantId = tenantId,
            CompanyId = companyId,
            BranchId = branchId,
            PayrollPeriodId = request.PayrollPeriodId.Value,
            Folio = folio,
            RunDate = DateTime.SpecifyKind(request.RunDate?.Date ?? DateTime.UtcNow.Date, DateTimeKind.Utc),
            Status = NormalizeStatus(request.Status, "draft"),
            EmployeeCount = request.EmployeeCount,
            GrossAmount = request.GrossAmount,
            DeductionsAmount = request.DeductionsAmount,
            NetAmount = request.NetAmount,
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.PayrollRuns.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePayrollRunAsync(Guid id, PayrollRunRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollRuns.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el proceso de nómina." });

        var locked = !IsPayrollRunEditable(entity.Status);
        var folio = NormalizeUpper(request.Folio, entity.Folio);
        if (!locked && await db.PayrollRuns.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Folio == folio))
            return Results.BadRequest(new { message = "Ya existe otro proceso de nómina con ese folio." });

        if (!locked && (!request.PayrollPeriodId.HasValue || !await db.PayrollPeriods.AnyAsync(x => x.Id == request.PayrollPeriodId.Value)))
            return Results.BadRequest(new { message = "No se encontró el periodo de nómina enviado." });

        if (!locked)
        {
            entity.BranchId = request.BranchId;
            entity.PayrollPeriodId = request.PayrollPeriodId!.Value;
            entity.Folio = folio;
            entity.RunDate = request.RunDate?.Date ?? entity.RunDate;
            entity.EmployeeCount = request.EmployeeCount;
            entity.GrossAmount = request.GrossAmount;
            entity.DeductionsAmount = request.DeductionsAmount;
            entity.NetAmount = request.NetAmount;
        }

        entity.Status = NormalizeStatus(request.Status, entity.Status);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePayrollRunAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollRuns.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el proceso de nómina." });

        if (!IsPayrollRunEditable(entity.Status))
            return Results.BadRequest(new { message = "No puedes eliminar una corrida calculada/autorizada/cerrada." });

        if (await db.PayrollRunLines.AnyAsync(x => x.PayrollRunId == id))
            return Results.BadRequest(new { message = "No puedes eliminar un proceso con recibos relacionados." });

        db.PayrollRuns.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetPayrollRunLinesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var rows = await db.PayrollRunLines.AsNoTracking()
            .Where(x => tenantId.HasValue && x.TenantId == tenantId.Value
                     && (!companyId.HasValue || x.CompanyId == companyId.Value))
            .Include(x => x.Company)
            .Include(x => x.PayrollRun)
            .Include(x => x.Employee)
            .Include(x => x.Department)
            .Include(x => x.Position)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PayrollRunLineDto
            {
                PayrollRunLineId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                PayrollRunId = x.PayrollRunId,
                PayrollRunFolio = x.PayrollRun != null ? x.PayrollRun.Folio : string.Empty,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? x.Employee.GetFullName() : string.Empty,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                PositionId = x.PositionId,
                PositionName = x.Position != null ? x.Position.Name : string.Empty,
                DaysPaid = x.DaysPaid,
                GrossAmount = x.GrossAmount,
                DeductionsAmount = x.DeductionsAmount,
                NetAmount = x.NetAmount,
                IncidentsAmount = x.IncidentsAmount,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePayrollRunLineAsync(HttpContext httpContext, PayrollRunLineRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para el recibo." });

        if (!request.PayrollRunId.HasValue)
            return Results.BadRequest(new { message = "No se encontró el proceso de nómina enviado." });

        var run = await db.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.PayrollRunId.Value);
        if (run is null)
            return Results.BadRequest(new { message = "No se encontró el proceso de nómina enviado." });
        if (!IsPayrollRunEditable(run.Status))
            return Results.BadRequest(new { message = "La corrida ya fue calculada/autorizada/cerrada. No se pueden agregar recibos." });

        if (!request.EmployeeId.HasValue || !await db.Employees.AnyAsync(x => x.Id == request.EmployeeId.Value))
            return Results.BadRequest(new { message = "No se encontró el colaborador enviado." });

        if (await db.PayrollRunLines.AnyAsync(x => x.PayrollRunId == request.PayrollRunId.Value && x.EmployeeId == request.EmployeeId.Value))
            return Results.BadRequest(new { message = "Ya existe un recibo para ese colaborador dentro del mismo proceso." });

        var entity = new PayrollRunLine
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            PayrollRunId = request.PayrollRunId.Value,
            EmployeeId = request.EmployeeId.Value,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            DaysPaid = request.DaysPaid,
            GrossAmount = request.GrossAmount,
            DeductionsAmount = request.DeductionsAmount,
            NetAmount = request.NetAmount,
            IncidentsAmount = request.IncidentsAmount,
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.PayrollRunLines.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePayrollRunLineAsync(Guid id, PayrollRunLineRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollRunLines.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el recibo." });

        var currentRun = await db.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.PayrollRunId);
        if (currentRun is not null && !IsPayrollRunEditable(currentRun.Status))
            return Results.BadRequest(new { message = "La corrida ya fue calculada/autorizada/cerrada. No se puede modificar el recibo." });

        if (!request.PayrollRunId.HasValue)
            return Results.BadRequest(new { message = "No se encontró el proceso de nómina enviado." });

        var targetRun = await db.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.PayrollRunId.Value);
        if (targetRun is null)
            return Results.BadRequest(new { message = "No se encontró el proceso de nómina enviado." });
        if (!IsPayrollRunEditable(targetRun.Status))
            return Results.BadRequest(new { message = "La corrida destino ya fue calculada/autorizada/cerrada. No se puede modificar el recibo." });

        if (!request.EmployeeId.HasValue || !await db.Employees.AnyAsync(x => x.Id == request.EmployeeId.Value))
            return Results.BadRequest(new { message = "No se encontró el colaborador enviado." });

        if (await db.PayrollRunLines.AnyAsync(x =>
            x.Id != id &&
            x.PayrollRunId == request.PayrollRunId.Value &&
            x.EmployeeId == request.EmployeeId.Value))
        {
            return Results.BadRequest(new { message = "Ya existe otro recibo para ese colaborador dentro del mismo proceso." });
        }

        entity.PayrollRunId = request.PayrollRunId.Value;
        entity.EmployeeId = request.EmployeeId.Value;
        entity.DepartmentId = request.DepartmentId;
        entity.PositionId = request.PositionId;
        entity.DaysPaid = request.DaysPaid;
        entity.GrossAmount = request.GrossAmount;
        entity.DeductionsAmount = request.DeductionsAmount;
        entity.NetAmount = request.NetAmount;
        entity.IncidentsAmount = request.IncidentsAmount;
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePayrollRunLineAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollRunLines.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el recibo." });

        var run = await db.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.PayrollRunId);
        if (run is not null && !IsPayrollRunEditable(run.Status))
            return Results.BadRequest(new { message = "La corrida ya fue calculada/autorizada/cerrada. No se puede eliminar el recibo." });

        db.PayrollRunLines.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    // ── Attendance Policies ─────────────────────────────────────────────────────

    private static async Task<IResult> GetAttendancePoliciesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var rows = await db.AttendancePolicies.AsNoTracking()
            .Where(x => tenantId.HasValue && x.TenantId == tenantId.Value
                     && (!companyId.HasValue || x.CompanyId == companyId.Value))
            .Where(x => !x.IsDeleted)
            .Include(x => x.Company)
            .Include(x => x.WorkShift)
            .OrderBy(x => x.Code)
            .Select(x => new
            {
                AttendancePolicyId = x.Id,
                x.TenantId, x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                x.WorkShiftId,
                WorkShiftName = x.WorkShift != null ? x.WorkShift.Name : string.Empty,
                x.Code, x.Name, x.Description,
                x.Scope, x.DepartmentId, x.Priority,
                x.ToleranceMinutes, x.MinOvertimeMinutes,
                x.RequiresPunchIn, x.RequiresPunchOut,
                x.IsDefault, x.Notes, x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateAttendancePolicyAsync(HttpContext httpContext, AttendancePolicyRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa." });
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        var entity = new AttendancePolicy
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            WorkShiftId = request.WorkShiftId,
            DepartmentId = request.DepartmentId,
            Scope = string.IsNullOrWhiteSpace(request.Scope) ? "company" : request.Scope,
            Priority = request.Priority <= 0 ? 100 : request.Priority,
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            ToleranceMinutes = request.ToleranceMinutes,
            MinOvertimeMinutes = request.MinOvertimeMinutes <= 0 ? 15 : request.MinOvertimeMinutes,
            RequiresPunchIn = request.RequiresPunchIn,
            RequiresPunchOut = request.RequiresPunchOut,
            IsDefault = request.IsDefault,
            Notes = request.Notes?.Trim() ?? string.Empty,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.AttendancePolicies.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateAttendancePolicyAsync(Guid id, AttendancePolicyRequest request, NanchesoftDbContext db)
    {
        var entity = await db.AttendancePolicies.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la política de asistencia." });
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        entity.WorkShiftId = request.WorkShiftId;
        entity.DepartmentId = request.DepartmentId;
        entity.Scope = string.IsNullOrWhiteSpace(request.Scope) ? "company" : request.Scope;
        entity.Priority = request.Priority <= 0 ? 100 : request.Priority;
        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim() ?? string.Empty;
        entity.ToleranceMinutes = request.ToleranceMinutes;
        entity.MinOvertimeMinutes = request.MinOvertimeMinutes <= 0 ? 15 : request.MinOvertimeMinutes;
        entity.RequiresPunchIn = request.RequiresPunchIn;
        entity.RequiresPunchOut = request.RequiresPunchOut;
        entity.IsDefault = request.IsDefault;
        entity.Notes = request.Notes?.Trim() ?? string.Empty;
        entity.IsActive = request.IsActive;
        entity.UpdatedBy = "web-api";
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteAttendancePolicyAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.AttendancePolicies.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la política de asistencia." });

        entity.IsDeleted = true;
        entity.UpdatedBy = "web-api";
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    // ── Attendance Policy Rules ─────────────────────────────────────────────────

    private static async Task<IResult> GetAttendancePolicyRulesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var rows = await db.AttendancePolicyRules.AsNoTracking()
            .Where(x => tenantId.HasValue && x.TenantId == tenantId.Value
                     && (!companyId.HasValue || x.CompanyId == companyId.Value))
            .Where(x => !x.IsDeleted)
            .Include(x => x.Policy)
            .OrderBy(x => x.Policy != null ? x.Policy.Code : string.Empty)
            .ThenBy(x => x.SortOrder).ThenBy(x => x.Code)
            .Select(x => new
            {
                AttendancePolicyRuleId = x.Id,
                x.TenantId, x.CompanyId,
                x.AttendancePolicyId,
                PolicyName = x.Policy != null ? x.Policy.Name : string.Empty,
                x.Code, x.Name, x.RuleType, x.ConditionType,
                x.ThresholdMinutes, x.ThresholdDays,
                x.ActionType, x.ActionValue, x.IncidentTypeCode,
                x.SortOrder, x.Notes, x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> GetRulesByPolicyAsync(Guid policyId, NanchesoftDbContext db)
    {
        var rows = await db.AttendancePolicyRules.AsNoTracking()
            .Where(x => x.AttendancePolicyId == policyId && !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
            .Select(x => new
            {
                AttendancePolicyRuleId = x.Id,
                x.AttendancePolicyId,
                x.Code, x.Name, x.RuleType, x.ConditionType,
                x.ThresholdMinutes, x.ThresholdDays,
                x.ActionType, x.ActionValue, x.IncidentTypeCode,
                x.SortOrder, x.Notes, x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateAttendancePolicyRuleAsync(HttpContext httpContext, AttendancePolicyRuleRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa." });
        if (request.AttendancePolicyId == Guid.Empty)
            return Results.BadRequest(new { message = "attendance_policy_id es obligatorio." });
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        var policyExists = await db.AttendancePolicies.AnyAsync(x => x.Id == request.AttendancePolicyId && !x.IsDeleted);
        if (!policyExists)
            return Results.BadRequest(new { message = "La política de asistencia no existe." });

        var entity = new AttendancePolicyRule
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            AttendancePolicyId = request.AttendancePolicyId,
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            RuleType = request.RuleType?.Trim() ?? string.Empty,
            ConditionType = request.ConditionType?.Trim() ?? "GreaterThan",
            ThresholdMinutes = request.ThresholdMinutes,
            ThresholdDays = request.ThresholdDays,
            ActionType = request.ActionType?.Trim() ?? "CreateIncident",
            ActionValue = request.ActionValue,
            IncidentTypeCode = request.IncidentTypeCode?.Trim().ToUpperInvariant(),
            SortOrder = request.SortOrder,
            Notes = request.Notes?.Trim() ?? string.Empty,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.AttendancePolicyRules.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateAttendancePolicyRuleAsync(Guid id, AttendancePolicyRuleRequest request, NanchesoftDbContext db)
    {
        var entity = await db.AttendancePolicyRules.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la regla." });
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        entity.AttendancePolicyId = request.AttendancePolicyId != Guid.Empty ? request.AttendancePolicyId : entity.AttendancePolicyId;
        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();
        entity.RuleType = request.RuleType?.Trim() ?? string.Empty;
        entity.ConditionType = request.ConditionType?.Trim() ?? "GreaterThan";
        entity.ThresholdMinutes = request.ThresholdMinutes;
        entity.ThresholdDays = request.ThresholdDays;
        entity.ActionType = request.ActionType?.Trim() ?? "CreateIncident";
        entity.ActionValue = request.ActionValue;
        entity.IncidentTypeCode = request.IncidentTypeCode?.Trim().ToUpperInvariant();
        entity.SortOrder = request.SortOrder;
        entity.Notes = request.Notes?.Trim() ?? string.Empty;
        entity.IsActive = request.IsActive;
        entity.UpdatedBy = "web-api";
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteAttendancePolicyRuleAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.AttendancePolicyRules.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la regla." });

        entity.IsDeleted = true;
        entity.UpdatedBy = "web-api";
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }
}

public class DepartmentRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class DepartmentDto : DepartmentRequest
{
    public Guid DepartmentId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}

public class PositionRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? PayrollGroup { get; set; }
    public decimal BaseSalary { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PositionDto : PositionRequest
{
    public Guid PositionId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
}

public class EmployeeRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? WorkScheduleId { get; set; }
    public string? Code { get; set; }
    public string? EmployeeNumber { get; set; }
    public string? ClockKey { get; set; }
    public string? NoiKey { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? SecondLastName { get; set; }
    public string? MiddleName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? TaxId { get; set; }
    public string? NationalId { get; set; }
    public string? Curp { get; set; }
    public string? Nss { get; set; }
    public string? ImssRegId { get; set; }
    public string? Gender { get; set; }
    public string? BloodType { get; set; }
    public string? MaritalStatus { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? FatherName { get; set; }
    public string? MotherName { get; set; }
    public string? AddressStreet { get; set; }
    public string? AddressColony { get; set; }
    public string? AddressCity { get; set; }
    public string? AddressState { get; set; }
    public string? AddressZipCode { get; set; }
    public string? ContractType { get; set; }
    public string? CotizationBase { get; set; }
    public decimal SbcFija { get; set; }
    public string? TaxRegime { get; set; }
    public string? EmployeeType { get; set; }
    public string? SalaryZone { get; set; }
    public string? PayrollPeriodType { get; set; }
    public string? PaymentForm { get; set; }
    public string? BankCode { get; set; }
    public string? BankAccount { get; set; }
    public string? Clabe { get; set; }
    public string? BankBranch { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? TerminationReason { get; set; }
    public DateTime? ReentryDate { get; set; }
    public bool IsImssRegistered { get; set; }
    public DateTime? ImssRegistrationDate { get; set; }
    public DateTime? ImssTerminationDate { get; set; }
    public string? Umf { get; set; }
    public string? Afore { get; set; }
    public string? Fonacot { get; set; }
    public string? Infonavit { get; set; }
    public string? ImmediateSupervisor { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
    public bool PrintReceipt { get; set; } = true;
    public decimal PeriodSalary { get; set; }
    public decimal DailySalary { get; set; }
    public decimal IntegratedDailySalary { get; set; }
    public string? Status { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeDto : EmployeeRequest
{
    public Guid EmployeeId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string WorkScheduleName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class EmployeeContractRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? ContractNumber { get; set; }
    public string? ContractType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? PaymentFrequency { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal IntegratedSalary { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeContractDto : EmployeeContractRequest
{
    public Guid EmployeeContractId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
}

public class EmployeeIncidentRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public Guid? PayrollIncidentTypeId { get; set; }
    public Guid? NomPayrollIncidentTypeId { get; set; }
    public DateTime? IncidentDate { get; set; }
    public string? IncidentType { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public string? Status { get; set; }
    public string? Origin { get; set; }
    public bool ManuallyEdited { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class BulkIncidentRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public Guid? NomPayrollIncidentTypeId { get; set; }
    public DateTime? IncidentDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public List<Guid> EmployeeIds { get; set; } = [];
}

public sealed class EmployeeIncidentDto : EmployeeIncidentRequest
{
    public Guid EmployeeIncidentId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string PayrollPeriodName { get; set; } = string.Empty;
    public string NomPayrollIncidentTypeCode { get; set; } = string.Empty;
    public string NomPayrollIncidentTypeName { get; set; } = string.Empty;
    public string IncidentCategory { get; set; } = string.Empty;
    public string AffectType { get; set; } = string.Empty;
    public string PayrollConceptType { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public class HrRecurringIncidentRuleRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? NomPayrollIncidentTypeId { get; set; }
    public decimal Amount { get; set; }
    public decimal Quantity { get; set; } = 1m;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Frequency { get; set; }
    public string? Notes { get; set; }
    public bool RequiresAuthorization { get; set; }
    public string? AuthorizedBy { get; set; }
    public DateTime? AuthorizedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class HrRecurringIncidentRuleDto : HrRecurringIncidentRuleRequest
{
    public Guid HrRecurringIncidentRuleId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string NomPayrollIncidentTypeName { get; set; } = string.Empty;
    public string IncidentCategory { get; set; } = string.Empty;
    public string AffectType { get; set; } = string.Empty;
}

public sealed class RecurringIncidentGenerationRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid PayrollPeriodId { get; set; }
    public Guid? RuleId { get; set; }
}

public sealed class RecurringIncidentGenerationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid PayrollPeriodId { get; set; }
    public string PayrollPeriodName { get; set; } = string.Empty;
    public int EmployeesAffected { get; set; }
    public decimal TotalPerceptions { get; set; }
    public decimal TotalDeductions { get; set; }
    public int Generated { get; set; }
    public int Omitted { get; set; }
    public List<RecurringIncidentGenerationItem> Items { get; set; } = [];
}

public sealed class RecurringIncidentGenerationItem
{
    public Guid RuleId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string IncidentTypeName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class PayrollPeriodRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? PeriodType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Status { get; set; }
    public bool IsImssInsured { get; set; } = true;
    public bool IsClosed { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollPeriodDto : PayrollPeriodRequest
{
    public Guid PayrollPeriodId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}

public class PayrollConceptRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? ConceptType { get; set; }
    public string? CalculationType { get; set; }
    public string? SatCode { get; set; }
    public string? SatAgrupador { get; set; }
    public string? TaxableType { get; set; }
    public decimal TaxablePercent { get; set; } = 100m;
    public decimal ExemptPercent { get; set; } = 0m;
    public bool IsRecurring { get; set; }
    public bool IsAutomatic { get; set; } = true;
    public bool PrintOnReceipt { get; set; } = true;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Formula { get; set; }
    public string? TaxableFormula { get; set; }
    public string? ExemptFormula { get; set; }
    public string? ImssTaxableFormula { get; set; }
    public string? SatTipoPercepcionCode { get; set; }
    public string? SatTipoDeduccionCode { get; set; }
    public string? SatTipoOtroPagoCode { get; set; }
    public bool AutomaticOnGlobalRun { get; set; }
    public bool AutomaticOnTermination { get; set; }
    public bool IsInKind { get; set; }
    public bool AffectsSeventhDay { get; set; }
    public bool AffectsHolidayPay { get; set; }
    public bool AffectsImss { get; set; } = true;
    public bool AffectsIsr { get; set; } = true;
    public bool AffectsAccumulators { get; set; } = true;
    public bool RequiresSatStamping { get; set; } = true;
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
}

public sealed class PayrollConceptDto : PayrollConceptRequest
{
    public Guid PayrollConceptId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}

public class PayrollRunRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public string? Folio { get; set; }
    public DateTime? RunDate { get; set; }
    public string? Status { get; set; }
    public int EmployeeCount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollRunDto : PayrollRunRequest
{
    public Guid PayrollRunId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string PayrollPeriodName { get; set; } = string.Empty;
}

public class PayrollRunLineRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public decimal DaysPaid { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal IncidentsAmount { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollRunLineDto : PayrollRunLineRequest
{
    public Guid PayrollRunLineId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
}

public class PayrollPeriodTypeRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public int DaysPerPeriod { get; set; }
    public int PeriodsPerYear { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public int PaymentDays { get; set; }
    public int WorkingDays { get; set; }
    public bool AdjustToCalendarMonth { get; set; }
    public string QuinceaAdjustType { get; set; } = "LaborDays";
    public int? SeventhDayPosition { get; set; }
    public int PaymentDayPosition { get; set; }
}

public sealed class PayrollPeriodTypeDto : PayrollPeriodTypeRequest
{
    public Guid PayrollPeriodTypeId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}

public sealed class GeneratePeriodsRequest
{
    public int FiscalYear { get; set; }
    public DateTime StartDate { get; set; }
    public bool Overwrite { get; set; }
}

public sealed class AttendancePolicyRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? WorkShiftId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Scope { get; set; } = "company";
    public int Priority { get; set; } = 100;
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int ToleranceMinutes { get; set; }
    public int MinOvertimeMinutes { get; set; } = 15;
    public bool RequiresPunchIn { get; set; } = true;
    public bool RequiresPunchOut { get; set; } = true;
    public bool IsDefault { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AttendancePolicyRuleRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid AttendancePolicyId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? RuleType { get; set; }
    public string? ConditionType { get; set; }
    public int? ThresholdMinutes { get; set; }
    public decimal? ThresholdDays { get; set; }
    public string? ActionType { get; set; }
    public decimal ActionValue { get; set; }
    public string? IncidentTypeCode { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

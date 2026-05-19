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
        incidents.MapPut("/{id:guid}", UpdateEmployeeIncidentAsync);
        incidents.MapDelete("/{id:guid}", DeleteEmployeeIncidentAsync);

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

        var periods = app.MapGroup("/api/payroll/periods").WithTags("PayrollPeriods");
        periods.MapGet("/", GetPayrollPeriodsAsync);
        periods.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.PayrollPeriods.AsNoTracking().Include(x => x.Company).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró el periodo." });
            return Results.Ok(new PayrollPeriodDto { PayrollPeriodId = entity.Id, TenantId = entity.TenantId, CompanyId = entity.CompanyId, CompanyName = entity.Company != null ? entity.Company.Name : string.Empty, Code = entity.Code, Name = entity.Name, PeriodType = entity.PeriodType, StartDate = entity.StartDate, EndDate = entity.EndDate, PaymentDate = entity.PaymentDate, Status = entity.Status, IsClosed = entity.IsClosed, IsActive = entity.IsActive });
        });
        periods.MapPost("/", CreatePayrollPeriodAsync);
        periods.MapPut("/{id:guid}", UpdatePayrollPeriodAsync);
        periods.MapDelete("/{id:guid}", DeletePayrollPeriodAsync);

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
            .Where(x => (!tenantId.HasValue && !companyId.HasValue)
                     || (x.TenantId == tenantId)
                     || (!tenantId.HasValue && x.CompanyId == companyId))
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
            .Where(x => (!tenantId.HasValue && !companyId.HasValue)
                     || (x.TenantId == tenantId)
                     || (!tenantId.HasValue && x.CompanyId == companyId))
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
            .Where(x => (!tenantId.HasValue && !companyId.HasValue)
                     || (x.TenantId == tenantId)
                     || (!tenantId.HasValue && x.CompanyId == companyId))
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
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        var branchId = request.BranchId ?? context.BranchId;

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
            .Where(x => (!tenantId.HasValue && !companyId.HasValue)
                     || (x.TenantId == tenantId)
                     || (!tenantId.HasValue && x.CompanyId == companyId))
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
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        var branchId = request.BranchId ?? context.BranchId;

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

        var rows = await db.EmployeeIncidents.AsNoTracking()
            .Where(x => (!tenantId.HasValue && !companyId.HasValue)
                     || (x.TenantId == tenantId)
                     || (!tenantId.HasValue && x.CompanyId == companyId))
            .Include(x => x.Company)
            .Include(x => x.Employee)
            .Include(x => x.PayrollPeriod)
            .OrderByDescending(x => x.IncidentDate)
            .Select(x => new EmployeeIncidentDto
            {
                EmployeeIncidentId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? x.Employee.GetFullName() : string.Empty,
                PayrollPeriodId = x.PayrollPeriodId,
                PayrollPeriodName = x.PayrollPeriod != null ? x.PayrollPeriod.Name : string.Empty,
                IncidentDate = x.IncidentDate,
                IncidentType = x.IncidentType,
                Quantity = x.Quantity,
                Amount = x.Amount,
                Notes = x.Notes,
                Status = x.Status,
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

        if (!request.EmployeeId.HasValue || !await db.Employees.AnyAsync(x => x.Id == request.EmployeeId.Value))
            return Results.BadRequest(new { message = "No se encontró el colaborador enviado." });

        if (request.PayrollPeriodId.HasValue && !await db.PayrollPeriods.AnyAsync(x => x.Id == request.PayrollPeriodId.Value))
            return Results.BadRequest(new { message = "No se encontró el periodo de nómina enviado." });

        if (string.IsNullOrWhiteSpace(request.IncidentType))
            return Results.BadRequest(new { message = "El tipo de incidencia es obligatorio." });

        var entity = new EmployeeIncident
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            EmployeeId = request.EmployeeId.Value,
            PayrollPeriodId = request.PayrollPeriodId,
            IncidentDate = request.IncidentDate?.Date ?? DateTime.UtcNow.Date,
            IncidentType = NormalizeStatus(request.IncidentType, "other"),
            Quantity = request.Quantity,
            Amount = request.Amount,
            Notes = NormalizeText(request.Notes),
            Status = NormalizeStatus(request.Status, "draft"),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.EmployeeIncidents.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateEmployeeIncidentAsync(Guid id, EmployeeIncidentRequest request, NanchesoftDbContext db)
    {
        var entity = await db.EmployeeIncidents.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la incidencia." });

        if (!request.EmployeeId.HasValue || !await db.Employees.AnyAsync(x => x.Id == request.EmployeeId.Value))
            return Results.BadRequest(new { message = "No se encontró el colaborador enviado." });

        if (request.PayrollPeriodId.HasValue && !await db.PayrollPeriods.AnyAsync(x => x.Id == request.PayrollPeriodId.Value))
            return Results.BadRequest(new { message = "No se encontró el periodo de nómina enviado." });

        entity.EmployeeId = request.EmployeeId.Value;
        entity.PayrollPeriodId = request.PayrollPeriodId;
        entity.IncidentDate = request.IncidentDate?.Date ?? entity.IncidentDate;
        entity.IncidentType = NormalizeStatus(request.IncidentType, entity.IncidentType);
        entity.Quantity = request.Quantity;
        entity.Amount = request.Amount;
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.Status = NormalizeStatus(request.Status, entity.Status);
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

        db.EmployeeIncidents.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetPayrollPeriodsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var rows = await db.PayrollPeriods.AsNoTracking()
            .Where(x => (!tenantId.HasValue && !companyId.HasValue)
                     || (x.TenantId == tenantId)
                     || (!tenantId.HasValue && x.CompanyId == companyId))
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

    private static async Task<IResult> GetPayrollPeriodTypesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var rows = await db.PayrollPeriodTypes.AsNoTracking()
            .Where(x => (!tenantId.HasValue && !companyId.HasValue)
                     || (x.TenantId == tenantId)
                     || (!tenantId.HasValue && x.CompanyId == companyId))
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
                IsActive = x.IsActive
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

    private static async Task<IResult> GetPayrollConceptsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var rows = await db.PayrollConcepts.AsNoTracking()
            .Where(x => (!tenantId.HasValue && !companyId.HasValue)
                     || (x.TenantId == tenantId)
                     || (!tenantId.HasValue && x.CompanyId == companyId))
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
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePayrollConceptAsync(HttpContext httpContext, PayrollConceptRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para el concepto." });

        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (await db.PayrollConcepts.AnyAsync(x => x.CompanyId == companyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un concepto con ese código." });

        var entity = new PayrollConcept
        {
            TenantId = tenantId.Value,
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
            CreatedBy = "web-api"
        };

        db.PayrollConcepts.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePayrollConceptAsync(Guid id, PayrollConceptRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollConcepts.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el concepto." });

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
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePayrollConceptAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollConcepts.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el concepto." });

        db.PayrollConcepts.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetPayrollRunsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        var rows = await db.PayrollRuns.AsNoTracking()
            .Where(x => (!tenantId.HasValue && !companyId.HasValue)
                     || (x.TenantId == tenantId)
                     || (!tenantId.HasValue && x.CompanyId == companyId))
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
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        var branchId = request.BranchId ?? context.BranchId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para la nómina." });

        if (!request.PayrollPeriodId.HasValue || !await db.PayrollPeriods.AnyAsync(x => x.Id == request.PayrollPeriodId.Value))
            return Results.BadRequest(new { message = "No se encontró el periodo de nómina enviado." });

        var folio = NormalizeUpper(request.Folio);
        if (string.IsNullOrWhiteSpace(folio))
            return Results.BadRequest(new { message = "El folio es obligatorio." });

        if (await db.PayrollRuns.AnyAsync(x => x.CompanyId == companyId && x.Folio == folio))
            return Results.BadRequest(new { message = "Ya existe un proceso de nómina con ese folio." });

        var entity = new PayrollRun
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
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

        var folio = NormalizeUpper(request.Folio, entity.Folio);
        if (await db.PayrollRuns.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Folio == folio))
            return Results.BadRequest(new { message = "Ya existe otro proceso de nómina con ese folio." });

        if (!request.PayrollPeriodId.HasValue || !await db.PayrollPeriods.AnyAsync(x => x.Id == request.PayrollPeriodId.Value))
            return Results.BadRequest(new { message = "No se encontró el periodo de nómina enviado." });

        entity.BranchId = request.BranchId;
        entity.PayrollPeriodId = request.PayrollPeriodId.Value;
        entity.Folio = folio;
        entity.RunDate = request.RunDate?.Date ?? entity.RunDate;
        entity.Status = NormalizeStatus(request.Status, entity.Status);
        entity.EmployeeCount = request.EmployeeCount;
        entity.GrossAmount = request.GrossAmount;
        entity.DeductionsAmount = request.DeductionsAmount;
        entity.NetAmount = request.NetAmount;
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
            .Where(x => (!tenantId.HasValue && !companyId.HasValue)
                     || (x.TenantId == tenantId)
                     || (!tenantId.HasValue && x.CompanyId == companyId))
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

        if (!request.PayrollRunId.HasValue || !await db.PayrollRuns.AnyAsync(x => x.Id == request.PayrollRunId.Value))
            return Results.BadRequest(new { message = "No se encontró el proceso de nómina enviado." });

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

        if (!request.PayrollRunId.HasValue || !await db.PayrollRuns.AnyAsync(x => x.Id == request.PayrollRunId.Value))
            return Results.BadRequest(new { message = "No se encontró el proceso de nómina enviado." });

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

        db.PayrollRunLines.Remove(entity);
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
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public DateTime? IncidentDate { get; set; }
    public string? IncidentType { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public string? Status { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeIncidentDto : EmployeeIncidentRequest
{
    public Guid EmployeeIncidentId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string PayrollPeriodName { get; set; } = string.Empty;
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
}

public sealed class PayrollPeriodTypeDto : PayrollPeriodTypeRequest
{
    public Guid PayrollPeriodTypeId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}

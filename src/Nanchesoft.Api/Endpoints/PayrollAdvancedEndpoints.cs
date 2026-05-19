using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PayrollAdvancedEndpoints
{
    public static IEndpointRouteBuilder MapPayrollAdvancedEndpoints(this IEndpointRouteBuilder app)
    {
        var clock = app.MapGroup("/api/hr/time-clock").WithTags("PayrollTimeClock");
        clock.MapGet("/", GetAttendancePunchesAsync);
        clock.MapPost("/", CreateAttendancePunchAsync);
        clock.MapPut("/{id:guid}", UpdateAttendancePunchAsync);
        clock.MapDelete("/{id:guid}", DeleteAttendancePunchAsync);

        var recurring = app.MapGroup("/api/payroll/recurring-movements").WithTags("PayrollRecurringMovements");
        recurring.MapGet("/", GetRecurringMovementsAsync);
        recurring.MapPost("/", CreateRecurringMovementAsync);
        recurring.MapPut("/{id:guid}", UpdateRecurringMovementAsync);
        recurring.MapDelete("/{id:guid}", DeleteRecurringMovementAsync);

        var loans = app.MapGroup("/api/payroll/loans").WithTags("PayrollLoans");
        loans.MapGet("/", GetLoansAsync);
        loans.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var row = await db.EmployeeLoans.AsNoTracking()
                .Include(x => x.Company).Include(x => x.Employee).Include(x => x.PayrollConcept)
                .Where(x => x.Id == id)
                .Select(x => new EmployeeLoanDto { EmployeeLoanId = x.Id, TenantId = x.TenantId, CompanyId = x.CompanyId, CompanyName = x.Company != null ? x.Company.Name : string.Empty, EmployeeId = x.EmployeeId, EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty, PayrollConceptId = x.PayrollConceptId, PayrollConceptName = x.PayrollConcept != null ? x.PayrollConcept.Name : string.Empty, LoanNumber = x.LoanNumber, LoanDate = x.LoanDate, StartDate = x.StartDate, EndDate = x.EndDate, PrincipalAmount = x.PrincipalAmount, BalanceAmount = x.BalanceAmount, InstallmentAmount = x.InstallmentAmount, Installments = x.Installments, InstallmentsPaid = x.InstallmentsPaid, Status = x.Status, Notes = x.Notes, IsActive = x.IsActive })
                .FirstOrDefaultAsync();
            if (row is null) return Results.NotFound(new { message = "No se encontró el préstamo." });
            return Results.Ok(row);
        });
        loans.MapPost("/", CreateLoanAsync);
        loans.MapPut("/{id:guid}", UpdateLoanAsync);
        loans.MapDelete("/{id:guid}", DeleteLoanAsync);

        var loanDeductions = app.MapGroup("/api/payroll/loan-deductions").WithTags("PayrollLoanDeductions");
        loanDeductions.MapGet("/", GetLoanDeductionsAsync);
        loanDeductions.MapPost("/", CreateLoanDeductionAsync);
        loanDeductions.MapPut("/{id:guid}", UpdateLoanDeductionAsync);
        loanDeductions.MapDelete("/{id:guid}", DeleteLoanDeductionAsync);

        var operations = app.MapGroup("/api/payroll").WithTags("PayrollOperations");
        operations.MapPost("/runs/{runId:guid}/apply-advanced-sources", ApplyAdvancedSourcesAsync);
        operations.MapGet("/runs/{runId:guid}/receipt-lines", GetReceiptLinesAsync);
        operations.MapGet("/runs/{runId:guid}/print-html", GetPayrollRunPrintHtmlAsync);
        operations.MapGet("/run-lines/{lineId:guid}/receipt-html", GetPayrollReceiptHtmlAsync);

        return app;
    }

    private static async Task<(Guid? TenantId, Guid? CompanyId)> ResolveDefaultContextAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        if (companyId.HasValue)
        {
            if (!tenantId.HasValue)
                tenantId = await db.Companies.Where(x => x.Id == companyId.Value).Select(x => (Guid?)x.TenantId).FirstOrDefaultAsync();
            return (tenantId, companyId);
        }

        if (tenantId.HasValue)
        {
            var comp = await db.Companies.Where(x => x.TenantId == tenantId.Value).OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
            if (comp is not null)
                return (tenantId, comp.Id);
        }

        var company = await db.Companies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        if (company is null) return (null, null);
        return (company.TenantId, company.Id);
    }

    private static DateTime NormalizeUtc(DateTime? value, DateTime fallback)
    {
        var source = value ?? fallback;
        return source.Kind == DateTimeKind.Utc ? source : DateTime.SpecifyKind(source, DateTimeKind.Utc);
    }

    private static string NormalizeText(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string NormalizeUpper(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();

    private static string NormalizeLower(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();

    private static (decimal Taxable, decimal Exempt) SplitTaxableAmount(string taxableType, decimal amount)
    {
        var normalized = NormalizeLower(taxableType, "taxable");
        if (amount <= 0m)
            return (0m, 0m);

        return normalized switch
        {
            "exempt" => (0m, amount),
            "mixed" => (Math.Round(amount * 0.70m, 2), Math.Round(amount * 0.30m, 2)),
            _ => (amount, 0m)
        };
    }

    private static async Task<IResult> GetAttendancePunchesAsync(NanchesoftDbContext db)
    {
        var rows = await db.AttendancePunches.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.Employee)
            .OrderByDescending(x => x.PunchDateTime)
            .Select(x => new AttendancePunchDto
            {
                AttendancePunchId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                EmployeeNumber = x.Employee != null ? x.Employee.EmployeeNumber : string.Empty,
                WorkDate = x.WorkDate,
                PunchDateTime = x.PunchDateTime,
                PunchType = x.PunchType,
                Source = x.Source,
                DeviceName = x.DeviceName,
                DeviceSerial = x.DeviceSerial,
                ExternalReference = x.ExternalReference,
                Status = x.Status,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateAttendancePunchAsync(HttpContext httpContext, AttendancePunchRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.EmployeeId.HasValue)
            return Results.BadRequest(new { message = "Empresa y colaborador son obligatorios." });

        var entity = new AttendancePunch
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = request.BranchId,
            EmployeeId = request.EmployeeId.Value,
            WorkDate = NormalizeUtc(request.WorkDate, DateTime.UtcNow.Date),
            PunchDateTime = NormalizeUtc(request.PunchDateTime, DateTime.UtcNow),
            PunchType = NormalizeLower(request.PunchType, "entry"),
            Source = NormalizeLower(request.Source, "manual"),
            DeviceName = NormalizeText(request.DeviceName),
            DeviceSerial = NormalizeText(request.DeviceSerial),
            ExternalReference = NormalizeText(request.ExternalReference),
            Status = NormalizeLower(request.Status, "captured"),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };
        db.AttendancePunches.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateAttendancePunchAsync(Guid id, AttendancePunchRequest request, NanchesoftDbContext db)
    {
        var entity = await db.AttendancePunches.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la marcación." });

        entity.BranchId = request.BranchId ?? entity.BranchId;
        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.WorkDate = request.WorkDate.HasValue ? NormalizeUtc(request.WorkDate, entity.WorkDate) : entity.WorkDate;
        entity.PunchDateTime = request.PunchDateTime.HasValue ? NormalizeUtc(request.PunchDateTime, entity.PunchDateTime) : entity.PunchDateTime;
        entity.PunchType = NormalizeLower(request.PunchType, entity.PunchType);
        entity.Source = NormalizeLower(request.Source, entity.Source);
        entity.DeviceName = NormalizeText(request.DeviceName, entity.DeviceName);
        entity.DeviceSerial = NormalizeText(request.DeviceSerial, entity.DeviceSerial);
        entity.ExternalReference = NormalizeText(request.ExternalReference, entity.ExternalReference);
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteAttendancePunchAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.AttendancePunches.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la marcación." });
        db.AttendancePunches.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetRecurringMovementsAsync(NanchesoftDbContext db)
    {
        var rows = await db.PayrollRecurringMovements.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Employee)
            .Include(x => x.PayrollConcept)
            .OrderBy(x => x.Employee!.LastName)
            .ThenBy(x => x.MovementCode)
            .Select(x => new PayrollRecurringMovementDto
            {
                PayrollRecurringMovementId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                PayrollConceptId = x.PayrollConceptId,
                PayrollConceptName = x.PayrollConcept != null ? x.PayrollConcept.Name : string.Empty,
                MovementCode = x.MovementCode,
                MovementName = x.MovementName,
                MovementType = x.MovementType,
                CalculationMode = x.CalculationMode,
                Quantity = x.Quantity,
                Amount = x.Amount,
                Percentage = x.Percentage,
                EffectiveStartDate = x.EffectiveStartDate,
                EffectiveEndDate = x.EffectiveEndDate,
                ApplyEveryRun = x.ApplyEveryRun,
                DayOfPeriod = x.DayOfPeriod,
                IsProrated = x.IsProrated,
                Status = x.Status,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();
        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateRecurringMovementAsync(HttpContext httpContext, PayrollRecurringMovementRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.EmployeeId.HasValue || !request.PayrollConceptId.HasValue)
            return Results.BadRequest(new { message = "Empresa, colaborador y concepto son obligatorios." });

        var entity = new PayrollRecurringMovement
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            EmployeeId = request.EmployeeId.Value,
            PayrollConceptId = request.PayrollConceptId.Value,
            MovementCode = NormalizeUpper(request.MovementCode),
            MovementName = NormalizeText(request.MovementName),
            MovementType = NormalizeLower(request.MovementType, "perception"),
            CalculationMode = NormalizeLower(request.CalculationMode, "fixed"),
            Quantity = request.Quantity <= 0m ? 1m : request.Quantity,
            Amount = request.Amount,
            Percentage = request.Percentage,
            EffectiveStartDate = NormalizeUtc(request.EffectiveStartDate, DateTime.UtcNow.Date),
            EffectiveEndDate = request.EffectiveEndDate.HasValue ? NormalizeUtc(request.EffectiveEndDate, DateTime.UtcNow.Date) : null,
            ApplyEveryRun = request.ApplyEveryRun,
            DayOfPeriod = request.DayOfPeriod,
            IsProrated = request.IsProrated,
            Status = NormalizeLower(request.Status, "active"),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };
        db.PayrollRecurringMovements.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateRecurringMovementAsync(Guid id, PayrollRecurringMovementRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollRecurringMovements.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el movimiento periódico." });

        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.PayrollConceptId = request.PayrollConceptId ?? entity.PayrollConceptId;
        entity.MovementCode = NormalizeUpper(request.MovementCode, entity.MovementCode);
        entity.MovementName = NormalizeText(request.MovementName, entity.MovementName);
        entity.MovementType = NormalizeLower(request.MovementType, entity.MovementType);
        entity.CalculationMode = NormalizeLower(request.CalculationMode, entity.CalculationMode);
        entity.Quantity = request.Quantity <= 0m ? entity.Quantity : request.Quantity;
        entity.Amount = request.Amount;
        entity.Percentage = request.Percentage;
        entity.EffectiveStartDate = request.EffectiveStartDate.HasValue ? NormalizeUtc(request.EffectiveStartDate, entity.EffectiveStartDate) : entity.EffectiveStartDate;
        entity.EffectiveEndDate = request.EffectiveEndDate.HasValue ? NormalizeUtc(request.EffectiveEndDate, entity.EffectiveEndDate ?? entity.EffectiveStartDate) : entity.EffectiveEndDate;
        entity.ApplyEveryRun = request.ApplyEveryRun;
        entity.DayOfPeriod = request.DayOfPeriod;
        entity.IsProrated = request.IsProrated;
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteRecurringMovementAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollRecurringMovements.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el movimiento periódico." });
        db.PayrollRecurringMovements.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetLoansAsync(NanchesoftDbContext db)
    {
        var rows = await db.EmployeeLoans.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Employee)
            .Include(x => x.PayrollConcept)
            .OrderByDescending(x => x.LoanDate)
            .Select(x => new EmployeeLoanDto
            {
                EmployeeLoanId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                PayrollConceptId = x.PayrollConceptId,
                PayrollConceptName = x.PayrollConcept != null ? x.PayrollConcept.Name : string.Empty,
                LoanNumber = x.LoanNumber,
                LoanDate = x.LoanDate,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                PrincipalAmount = x.PrincipalAmount,
                BalanceAmount = x.BalanceAmount,
                InstallmentAmount = x.InstallmentAmount,
                Installments = x.Installments,
                InstallmentsPaid = x.InstallmentsPaid,
                Status = x.Status,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();
        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateLoanAsync(HttpContext httpContext, EmployeeLoanRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.EmployeeId.HasValue || !request.PayrollConceptId.HasValue)
            return Results.BadRequest(new { message = "Empresa, colaborador y concepto del descuento son obligatorios." });

        var amount = request.PrincipalAmount <= 0m ? request.BalanceAmount : request.PrincipalAmount;
        var entity = new EmployeeLoan
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            EmployeeId = request.EmployeeId.Value,
            PayrollConceptId = request.PayrollConceptId.Value,
            LoanNumber = NormalizeUpper(request.LoanNumber),
            LoanDate = NormalizeUtc(request.LoanDate, DateTime.UtcNow.Date),
            StartDate = NormalizeUtc(request.StartDate, DateTime.UtcNow.Date),
            EndDate = request.EndDate.HasValue ? NormalizeUtc(request.EndDate, DateTime.UtcNow.Date) : null,
            PrincipalAmount = amount,
            BalanceAmount = request.BalanceAmount <= 0m ? amount : request.BalanceAmount,
            InstallmentAmount = request.InstallmentAmount,
            Installments = request.Installments,
            InstallmentsPaid = request.InstallmentsPaid,
            Status = NormalizeLower(request.Status, "active"),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };
        db.EmployeeLoans.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateLoanAsync(Guid id, EmployeeLoanRequest request, NanchesoftDbContext db)
    {
        var entity = await db.EmployeeLoans.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el préstamo." });

        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.PayrollConceptId = request.PayrollConceptId ?? entity.PayrollConceptId;
        entity.LoanNumber = NormalizeUpper(request.LoanNumber, entity.LoanNumber);
        entity.LoanDate = request.LoanDate.HasValue ? NormalizeUtc(request.LoanDate, entity.LoanDate) : entity.LoanDate;
        entity.StartDate = request.StartDate.HasValue ? NormalizeUtc(request.StartDate, entity.StartDate) : entity.StartDate;
        entity.EndDate = request.EndDate.HasValue ? NormalizeUtc(request.EndDate, entity.EndDate ?? entity.StartDate) : entity.EndDate;
        entity.PrincipalAmount = request.PrincipalAmount <= 0m ? entity.PrincipalAmount : request.PrincipalAmount;
        entity.BalanceAmount = request.BalanceAmount <= 0m ? entity.BalanceAmount : request.BalanceAmount;
        entity.InstallmentAmount = request.InstallmentAmount <= 0m ? entity.InstallmentAmount : request.InstallmentAmount;
        entity.Installments = request.Installments <= 0 ? entity.Installments : request.Installments;
        entity.InstallmentsPaid = request.InstallmentsPaid;
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteLoanAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.EmployeeLoans.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el préstamo." });
        db.EmployeeLoans.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetLoanDeductionsAsync(NanchesoftDbContext db)
    {
        var rows = await db.EmployeeLoanDeductions.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Employee)
            .Include(x => x.EmployeeLoan)
            .Include(x => x.PayrollRun)
            .OrderByDescending(x => x.DeductionDate)
            .Select(x => new EmployeeLoanDeductionDto
            {
                EmployeeLoanDeductionId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                EmployeeLoanId = x.EmployeeLoanId,
                LoanNumber = x.EmployeeLoan != null ? x.EmployeeLoan.LoanNumber : string.Empty,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                PayrollPeriodId = x.PayrollPeriodId,
                PayrollRunId = x.PayrollRunId,
                PayrollRunFolio = x.PayrollRun != null ? x.PayrollRun.Folio : string.Empty,
                PayrollRunLineId = x.PayrollRunLineId,
                DeductionDate = x.DeductionDate,
                InstallmentNumber = x.InstallmentNumber,
                Amount = x.Amount,
                PrincipalApplied = x.PrincipalApplied,
                InterestApplied = x.InterestApplied,
                RemainingBalance = x.RemainingBalance,
                Status = x.Status,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();
        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateLoanDeductionAsync(HttpContext httpContext, EmployeeLoanDeductionRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.EmployeeLoanId.HasValue || !request.EmployeeId.HasValue)
            return Results.BadRequest(new { message = "Empresa, préstamo y colaborador son obligatorios." });

        var entity = new EmployeeLoanDeduction
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            EmployeeLoanId = request.EmployeeLoanId.Value,
            EmployeeId = request.EmployeeId.Value,
            PayrollPeriodId = request.PayrollPeriodId,
            PayrollRunId = request.PayrollRunId,
            PayrollRunLineId = request.PayrollRunLineId,
            DeductionDate = NormalizeUtc(request.DeductionDate, DateTime.UtcNow.Date),
            InstallmentNumber = request.InstallmentNumber,
            Amount = request.Amount,
            PrincipalApplied = request.PrincipalApplied,
            InterestApplied = request.InterestApplied,
            RemainingBalance = request.RemainingBalance,
            Status = NormalizeLower(request.Status, "applied"),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };
        db.EmployeeLoanDeductions.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateLoanDeductionAsync(Guid id, EmployeeLoanDeductionRequest request, NanchesoftDbContext db)
    {
        var entity = await db.EmployeeLoanDeductions.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el descuento del préstamo." });

        entity.EmployeeLoanId = request.EmployeeLoanId ?? entity.EmployeeLoanId;
        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.PayrollPeriodId = request.PayrollPeriodId ?? entity.PayrollPeriodId;
        entity.PayrollRunId = request.PayrollRunId ?? entity.PayrollRunId;
        entity.PayrollRunLineId = request.PayrollRunLineId ?? entity.PayrollRunLineId;
        entity.DeductionDate = request.DeductionDate.HasValue ? NormalizeUtc(request.DeductionDate, entity.DeductionDate) : entity.DeductionDate;
        entity.InstallmentNumber = request.InstallmentNumber <= 0 ? entity.InstallmentNumber : request.InstallmentNumber;
        entity.Amount = request.Amount <= 0m ? entity.Amount : request.Amount;
        entity.PrincipalApplied = request.PrincipalApplied;
        entity.InterestApplied = request.InterestApplied;
        entity.RemainingBalance = request.RemainingBalance;
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteLoanDeductionAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.EmployeeLoanDeductions.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el descuento del préstamo." });
        db.EmployeeLoanDeductions.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> ApplyAdvancedSourcesAsync(Guid runId, NanchesoftDbContext db)
    {
        var run = await db.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null)
            return Results.NotFound(new { message = "No se encontró la corrida de nómina." });

        var lines = await db.PayrollRunLines.Where(x => x.PayrollRunId == runId).ToListAsync();
        if (lines.Count == 0)
            return Results.BadRequest(new { message = "La corrida no tiene recibos por colaborador." });

        var employeeIds = lines.Select(x => x.EmployeeId).Distinct().ToList();
        var employees = await db.Employees.Where(x => employeeIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        var concepts = await db.PayrollConcepts.Where(x => x.CompanyId == run.CompanyId && x.IsActive).ToDictionaryAsync(x => x.Id);

        var existingAutoAdvanced = await db.PayrollRunLineDetails
            .Where(x => x.PayrollRunId == runId && x.IsGenerated && x.Notes.StartsWith("[AUTO-ADV]"))
            .ToListAsync();
        if (existingAutoAdvanced.Count > 0)
            db.PayrollRunLineDetails.RemoveRange(existingAutoAdvanced);

        var recurring = await db.PayrollRecurringMovements
            .Where(x => x.CompanyId == run.CompanyId && x.IsActive && employeeIds.Contains(x.EmployeeId))
            .ToListAsync();

        var loans = await db.EmployeeLoans
            .Where(x => x.CompanyId == run.CompanyId && x.IsActive && employeeIds.Contains(x.EmployeeId) && x.Status == "active")
            .ToListAsync();

        var period = await db.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == run.PayrollPeriodId);
        var createdDetails = 0;
        var createdLoanDeductions = 0;

        foreach (var line in lines)
        {
            var employee = employees.GetValueOrDefault(line.EmployeeId);
            var employeeDailySalary = employee?.IntegratedDailySalary > 0m ? employee!.IntegratedDailySalary : employee?.DailySalary ?? 0m;
            var baseGross = line.GrossAmount > 0m ? line.GrossAmount : Math.Round(employeeDailySalary * Math.Max(line.DaysPaid, 1m), 2);

            foreach (var movement in recurring.Where(x => x.EmployeeId == line.EmployeeId))
            {
                if (movement.EffectiveStartDate.Date > run.RunDate.Date)
                    continue;
                if (movement.EffectiveEndDate.HasValue && movement.EffectiveEndDate.Value.Date < run.RunDate.Date)
                    continue;
                if (movement.DayOfPeriod.HasValue && period is not null && period.PaymentDate.Day != movement.DayOfPeriod.Value && !movement.ApplyEveryRun)
                    continue;

                if (!concepts.TryGetValue(movement.PayrollConceptId, out var concept))
                    continue;

                var amount = movement.CalculationMode == "percent_of_salary"
                    ? Math.Round(baseGross * (movement.Percentage / 100m), 2)
                    : Math.Round(movement.Amount * Math.Max(movement.Quantity, 1m), 2);

                if (amount <= 0m)
                    continue;

                var split = movement.MovementType == "deduction" ? (0m, 0m) : SplitTaxableAmount(concept.TaxableType, amount);

                db.PayrollRunLineDetails.Add(new PayrollRunLineDetail
                {
                    TenantId = line.TenantId,
                    CompanyId = line.CompanyId,
                    PayrollRunId = line.PayrollRunId,
                    PayrollRunLineId = line.Id,
                    EmployeeId = line.EmployeeId,
                    PayrollConceptId = concept.Id,
                    ConceptCode = concept.Code,
                    ConceptName = string.IsNullOrWhiteSpace(movement.MovementName) ? concept.Name : movement.MovementName,
                    ConceptType = NormalizeLower(movement.MovementType, concept.ConceptType),
                    SatCode = concept.SatCode,
                    TaxableType = concept.TaxableType,
                    Quantity = movement.Quantity <= 0m ? 1m : movement.Quantity,
                    Amount = amount,
                    TaxableAmount = split.Item1,
                    ExemptAmount = split.Item2,
                    SortOrder = movement.MovementType == "deduction" ? 80 : 40,
                    IsGenerated = true,
                    Status = "applied",
                    Notes = $"[AUTO-ADV] Movimiento periódico {movement.MovementCode} aplicado automáticamente.",
                    CreatedBy = "web-api"
                });
                createdDetails++;
            }

            foreach (var loan in loans.Where(x => x.EmployeeId == line.EmployeeId && x.BalanceAmount > 0m && x.StartDate.Date <= run.RunDate.Date && (!x.EndDate.HasValue || x.EndDate.Value.Date >= run.RunDate.Date)))
            {
                if (!concepts.TryGetValue(loan.PayrollConceptId, out var concept))
                    continue;

                var deductionAmount = Math.Min(loan.BalanceAmount, loan.InstallmentAmount <= 0m ? loan.BalanceAmount : loan.InstallmentAmount);
                if (deductionAmount <= 0m)
                    continue;

                var installmentNumber = loan.InstallmentsPaid + 1;
                var remainingBalance = Math.Max(0m, loan.BalanceAmount - deductionAmount);

                db.PayrollRunLineDetails.Add(new PayrollRunLineDetail
                {
                    TenantId = line.TenantId,
                    CompanyId = line.CompanyId,
                    PayrollRunId = line.PayrollRunId,
                    PayrollRunLineId = line.Id,
                    EmployeeId = line.EmployeeId,
                    PayrollConceptId = concept.Id,
                    ConceptCode = concept.Code,
                    ConceptName = $"Préstamo {loan.LoanNumber}",
                    ConceptType = "deduction",
                    SatCode = concept.SatCode,
                    TaxableType = concept.TaxableType,
                    Quantity = 1m,
                    Amount = deductionAmount,
                    TaxableAmount = 0m,
                    ExemptAmount = 0m,
                    SortOrder = 95,
                    IsGenerated = true,
                    Status = "applied",
                    Notes = $"[AUTO-ADV] Descuento automático del préstamo {loan.LoanNumber}.",
                    CreatedBy = "web-api"
                });
                createdDetails++;

                if (!await db.EmployeeLoanDeductions.AnyAsync(x => x.EmployeeLoanId == loan.Id && x.InstallmentNumber == installmentNumber))
                {
                    db.EmployeeLoanDeductions.Add(new EmployeeLoanDeduction
                    {
                        TenantId = loan.TenantId,
                        CompanyId = loan.CompanyId,
                        EmployeeLoanId = loan.Id,
                        EmployeeId = loan.EmployeeId,
                        PayrollPeriodId = run.PayrollPeriodId,
                        PayrollRunId = run.Id,
                        PayrollRunLineId = line.Id,
                        DeductionDate = run.RunDate,
                        InstallmentNumber = installmentNumber,
                        Amount = deductionAmount,
                        PrincipalApplied = deductionAmount,
                        InterestApplied = 0m,
                        RemainingBalance = remainingBalance,
                        Status = "applied",
                        Notes = $"Descuento generado desde la corrida {run.Folio}.",
                        CreatedBy = "web-api"
                    });
                    createdLoanDeductions++;
                }

                loan.BalanceAmount = remainingBalance;
                loan.InstallmentsPaid = installmentNumber;
                loan.Status = remainingBalance <= 0m ? "closed" : loan.Status;
                loan.UpdatedAt = DateTime.UtcNow;
                loan.UpdatedBy = "web-api";
            }
        }

        await db.SaveChangesAsync();
        await RecalculatePayrollRunAsync(runId, db);
        return Results.Ok(new { success = true, createdDetails, createdLoanDeductions });
    }

    private static async Task<IResult> GetReceiptLinesAsync(Guid runId, NanchesoftDbContext db)
    {
        if (!await db.PayrollRuns.AnyAsync(x => x.Id == runId))
            return Results.NotFound(new { message = "No se encontró la corrida de nómina." });

        var rows = await db.PayrollRunLines.AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.PayrollRun)
            .Where(x => x.PayrollRunId == runId)
            .OrderBy(x => x.Employee!.LastName)
            .Select(x => new PayrollReceiptLineDto
            {
                PayrollRunLineId = x.Id,
                PayrollRunId = x.PayrollRunId,
                PayrollRunFolio = x.PayrollRun != null ? x.PayrollRun.Folio : string.Empty,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                EmployeeNumber = x.Employee != null ? x.Employee.EmployeeNumber : string.Empty,
                DaysPaid = x.DaysPaid,
                GrossAmount = x.GrossAmount,
                DeductionsAmount = x.DeductionsAmount,
                NetAmount = x.NetAmount,
                IncidentsAmount = x.IncidentsAmount
            })
            .ToListAsync();
        return Results.Ok(rows);
    }

    private static async Task<IResult> GetPayrollRunPrintHtmlAsync(Guid runId, NanchesoftDbContext db)
    {
        var run = await db.PayrollRuns.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.PayrollPeriod)
            .FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null)
            return Results.NotFound();

        var rows = await db.PayrollRunLines.AsNoTracking()
            .Include(x => x.Employee)
            .Where(x => x.PayrollRunId == runId)
            .OrderBy(x => x.Employee!.LastName)
            .Select(x => new
            {
                EmployeeNumber = x.Employee != null ? x.Employee.EmployeeNumber : string.Empty,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                x.DaysPaid,
                x.GrossAmount,
                x.DeductionsAmount,
                x.NetAmount,
                x.IncidentsAmount
            })
            .ToListAsync();

        var html = BuildPayrollRunHtml(run, rows.Select(x => new PayrollSummaryRow(
            x.EmployeeNumber,
            x.EmployeeName,
            x.DaysPaid,
            x.GrossAmount,
            x.DeductionsAmount,
            x.NetAmount,
            x.IncidentsAmount)).ToList());

        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task<IResult> GetPayrollReceiptHtmlAsync(Guid lineId, NanchesoftDbContext db)
    {
        var line = await db.PayrollRunLines.AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.PayrollRun)
                .ThenInclude(x => x!.PayrollPeriod)
            .Include(x => x.Department)
            .Include(x => x.Position)
            .FirstOrDefaultAsync(x => x.Id == lineId);
        if (line is null)
            return Results.NotFound();

        var details = await db.PayrollRunLineDetails.AsNoTracking()
            .Where(x => x.PayrollRunLineId == lineId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => new PayrollReceiptDetailRow(
                x.ConceptCode,
                x.ConceptName,
                x.ConceptType,
                x.Quantity,
                x.Amount,
                x.TaxableAmount,
                x.ExemptAmount))
            .ToListAsync();

        var html = BuildPayrollReceiptHtml(line, details);
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task RecalculatePayrollRunAsync(Guid runId, NanchesoftDbContext db)
    {
        var run = await db.PayrollRuns.FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null)
            return;

        var lines = await db.PayrollRunLines.Where(x => x.PayrollRunId == runId).ToListAsync();
        var detailGroups = await db.PayrollRunLineDetails.AsNoTracking()
            .Where(x => x.PayrollRunId == runId && x.IsActive)
            .GroupBy(x => x.PayrollRunLineId)
            .Select(g => new
            {
                PayrollRunLineId = g.Key,
                GrossAmount = g.Where(x => x.ConceptType != "deduction").Sum(x => x.Amount),
                DeductionsAmount = g.Where(x => x.ConceptType == "deduction").Sum(x => x.Amount),
                IncidentsAmount = g.Where(x => x.ConceptCode == "BON" || x.ConceptCode == "BONO" || x.ConceptCode == "BONO-MENSUAL").Sum(x => x.Amount)
            })
            .ToListAsync();

        foreach (var line in lines)
        {
            var detail = detailGroups.FirstOrDefault(x => x.PayrollRunLineId == line.Id);
            if (detail is null)
                continue;

            line.GrossAmount = detail.GrossAmount;
            line.DeductionsAmount = detail.DeductionsAmount;
            line.NetAmount = detail.GrossAmount - detail.DeductionsAmount;
            line.IncidentsAmount = detail.IncidentsAmount;
            line.UpdatedAt = DateTime.UtcNow;
            line.UpdatedBy = "web-api";
        }

        run.EmployeeCount = lines.Count;
        run.GrossAmount = lines.Sum(x => x.GrossAmount);
        run.DeductionsAmount = lines.Sum(x => x.DeductionsAmount);
        run.NetAmount = lines.Sum(x => x.NetAmount);
        run.UpdatedAt = DateTime.UtcNow;
        run.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
    }

    private static string BuildPayrollRunHtml(PayrollRun run, List<PayrollSummaryRow> rows)
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'/><title>Nómina ")
            .Append(WebUtility.HtmlEncode(run.Folio))
            .Append("</title><style>")
            .Append("body{font-family:Arial,sans-serif;color:#0f172a;margin:24px;}h1{margin:0 0 8px 0;}table{width:100%;border-collapse:collapse;margin-top:16px;}th,td{border:1px solid #cbd5e1;padding:8px;font-size:12px;}th{background:#f8fafc;} .meta{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:8px 16px;margin-top:16px;} .totals{display:flex;justify-content:flex-end;gap:24px;margin-top:16px;font-weight:bold;} @media print{body{margin:0;padding:12px;}th{background:#f1f5f9 !important;-webkit-print-color-adjust:exact;print-color-adjust:exact;}}")
            .Append("</style></head><body>");
        sb.Append("<div><h1>Nómina del periodo</h1><div>")
            .Append(WebUtility.HtmlEncode(run.PayrollPeriod?.Name ?? string.Empty))
            .Append("</div></div>");
        sb.Append("<div class='meta'>")
            .Append("<div><strong>Folio:</strong> ").Append(WebUtility.HtmlEncode(run.Folio)).Append("</div>")
            .Append("<div><strong>Empresa:</strong> ").Append(WebUtility.HtmlEncode(run.Company?.Name ?? string.Empty)).Append("</div>")
            .Append("<div><strong>Sucursal:</strong> ").Append(WebUtility.HtmlEncode(run.Branch?.Name ?? string.Empty)).Append("</div>")
            .Append("<div><strong>Fecha corrida:</strong> ").Append(run.RunDate.ToString("dd/MM/yyyy")).Append("</div>")
            .Append("<div><strong>Estatus:</strong> ").Append(WebUtility.HtmlEncode(run.Status)).Append("</div>")
            .Append("<div><strong>Empleados:</strong> ").Append(run.EmployeeCount).Append("</div>")
            .Append("</div>");
        sb.Append("<table><thead><tr><th>No.</th><th>Colaborador</th><th>Días</th><th>Percepciones</th><th>Deducciones</th><th>Neto</th><th>Incidencias</th></tr></thead><tbody>");
        foreach (var row in rows)
        {
            sb.Append("<tr>")
                .Append("<td>").Append(WebUtility.HtmlEncode(row.EmployeeNumber)).Append("</td>")
                .Append("<td>").Append(WebUtility.HtmlEncode(row.EmployeeName)).Append("</td>")
                .Append("<td>").Append(row.DaysPaid.ToString("N2")).Append("</td>")
                .Append("<td>").Append(row.GrossAmount.ToString("C2")).Append("</td>")
                .Append("<td>").Append(row.DeductionsAmount.ToString("C2")).Append("</td>")
                .Append("<td>").Append(row.NetAmount.ToString("C2")).Append("</td>")
                .Append("<td>").Append(row.IncidentsAmount.ToString("C2")).Append("</td>")
                .Append("</tr>");
        }
        sb.Append("</tbody></table>");
        sb.Append("<div class='totals'>")
            .Append("<div>Percepciones: ").Append(run.GrossAmount.ToString("C2")).Append("</div>")
            .Append("<div>Deducciones: ").Append(run.DeductionsAmount.ToString("C2")).Append("</div>")
            .Append("<div>Neto: ").Append(run.NetAmount.ToString("C2")).Append("</div>")
            .Append("</div>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static string BuildPayrollReceiptHtml(PayrollRunLine line, List<PayrollReceiptDetailRow> details)
    {
        var employeeName = line.Employee is null ? string.Empty : (line.Employee.FirstName + " " + line.Employee.LastName).Trim();
        var perceptions = details.Where(x => x.ConceptType != "deduction").ToList();
        var deductions = details.Where(x => x.ConceptType == "deduction").ToList();

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'/><title>Recibo ")
            .Append(WebUtility.HtmlEncode(line.PayrollRun?.Folio ?? string.Empty))
            .Append("</title><style>")
            .Append("body{font-family:Arial,sans-serif;color:#0f172a;margin:24px;}h1{margin:0 0 8px 0;}h2{font-size:16px;margin:18px 0 8px 0;}table{width:100%;border-collapse:collapse;margin-top:8px;}th,td{border:1px solid #cbd5e1;padding:8px;font-size:12px;}th{background:#f8fafc;} .meta{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px 16px;margin-top:16px;} .totals{display:flex;justify-content:flex-end;gap:24px;margin-top:16px;font-weight:bold;} @media print{body{margin:0;padding:12px;}th{background:#f1f5f9 !important;-webkit-print-color-adjust:exact;print-color-adjust:exact;}}")
            .Append("</style></head><body>");
        sb.Append("<div><h1>Recibo de nómina</h1><div>")
            .Append(WebUtility.HtmlEncode(line.PayrollRun?.PayrollPeriod?.Name ?? string.Empty))
            .Append("</div></div>");
        sb.Append("<div class='meta'>")
            .Append("<div><strong>Folio nómina:</strong> ").Append(WebUtility.HtmlEncode(line.PayrollRun?.Folio ?? string.Empty)).Append("</div>")
            .Append("<div><strong>Colaborador:</strong> ").Append(WebUtility.HtmlEncode(employeeName)).Append("</div>")
            .Append("<div><strong>No. empleado:</strong> ").Append(WebUtility.HtmlEncode(line.Employee?.EmployeeNumber ?? string.Empty)).Append("</div>")
            .Append("<div><strong>Departamento:</strong> ").Append(WebUtility.HtmlEncode(line.Department?.Name ?? string.Empty)).Append("</div>")
            .Append("<div><strong>Puesto:</strong> ").Append(WebUtility.HtmlEncode(line.Position?.Name ?? string.Empty)).Append("</div>")
            .Append("<div><strong>Días pagados:</strong> ").Append(line.DaysPaid.ToString("N2")).Append("</div>")
            .Append("</div>");

        sb.Append("<h2>Percepciones</h2>")
            .Append(BuildReceiptSection(perceptions));
        sb.Append("<h2>Deducciones</h2>")
            .Append(BuildReceiptSection(deductions));
        sb.Append("<div class='totals'>")
            .Append("<div>Bruto: ").Append(line.GrossAmount.ToString("C2")).Append("</div>")
            .Append("<div>Deducciones: ").Append(line.DeductionsAmount.ToString("C2")).Append("</div>")
            .Append("<div>Neto: ").Append(line.NetAmount.ToString("C2")).Append("</div>")
            .Append("</div>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static string BuildReceiptSection(List<PayrollReceiptDetailRow> rows)
    {
        var sb = new StringBuilder();
        sb.Append("<table><thead><tr><th>Código</th><th>Concepto</th><th>Cantidad</th><th>Importe</th><th>Gravado</th><th>Exento</th></tr></thead><tbody>");
        if (rows.Count == 0)
        {
            sb.Append("<tr><td colspan='6'>Sin movimientos.</td></tr>");
        }
        else
        {
            foreach (var row in rows)
            {
                sb.Append("<tr>")
                    .Append("<td>").Append(WebUtility.HtmlEncode(row.ConceptCode)).Append("</td>")
                    .Append("<td>").Append(WebUtility.HtmlEncode(row.ConceptName)).Append("</td>")
                    .Append("<td>").Append(row.Quantity.ToString("N2")).Append("</td>")
                    .Append("<td>").Append(row.Amount.ToString("C2")).Append("</td>")
                    .Append("<td>").Append(row.TaxableAmount.ToString("C2")).Append("</td>")
                    .Append("<td>").Append(row.ExemptAmount.ToString("C2")).Append("</td>")
                    .Append("</tr>");
            }
        }
        sb.Append("</tbody></table>");
        return sb.ToString();
    }
}

public class AttendancePunchRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public DateTime? WorkDate { get; set; }
    public DateTime? PunchDateTime { get; set; }
    public string? PunchType { get; set; }
    public string? Source { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceSerial { get; set; }
    public string? ExternalReference { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AttendancePunchDto
{
    public Guid AttendancePunchId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public Guid? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public DateTime PunchDateTime { get; set; }
    public string PunchType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceSerial { get; set; } = string.Empty;
    public string ExternalReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class PayrollRecurringMovementRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollConceptId { get; set; }
    public string? MovementCode { get; set; }
    public string? MovementName { get; set; }
    public string? MovementType { get; set; }
    public string? CalculationMode { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public DateTime? EffectiveStartDate { get; set; }
    public DateTime? EffectiveEndDate { get; set; }
    public bool ApplyEveryRun { get; set; } = true;
    public int? DayOfPeriod { get; set; }
    public bool IsProrated { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollRecurringMovementDto
{
    public Guid PayrollRecurringMovementId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid? PayrollConceptId { get; set; }
    public string PayrollConceptName { get; set; } = string.Empty;
    public string MovementCode { get; set; } = string.Empty;
    public string MovementName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public string CalculationMode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public DateTime EffectiveStartDate { get; set; }
    public DateTime? EffectiveEndDate { get; set; }
    public bool ApplyEveryRun { get; set; }
    public int? DayOfPeriod { get; set; }
    public bool IsProrated { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class EmployeeLoanRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollConceptId { get; set; }
    public string? LoanNumber { get; set; }
    public DateTime? LoanDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public int Installments { get; set; }
    public int InstallmentsPaid { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeLoanDto
{
    public Guid EmployeeLoanId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid? PayrollConceptId { get; set; }
    public string PayrollConceptName { get; set; } = string.Empty;
    public string LoanNumber { get; set; } = string.Empty;
    public DateTime LoanDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public int Installments { get; set; }
    public int InstallmentsPaid { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class EmployeeLoanDeductionRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? EmployeeLoanId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? PayrollRunLineId { get; set; }
    public DateTime? DeductionDate { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalApplied { get; set; }
    public decimal InterestApplied { get; set; }
    public decimal RemainingBalance { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeLoanDeductionDto
{
    public Guid EmployeeLoanDeductionId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? EmployeeLoanId { get; set; }
    public string LoanNumber { get; set; } = string.Empty;
    public Guid? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid? PayrollPeriodId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public string PayrollRunFolio { get; set; } = string.Empty;
    public Guid? PayrollRunLineId { get; set; }
    public DateTime DeductionDate { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalApplied { get; set; }
    public decimal InterestApplied { get; set; }
    public decimal RemainingBalance { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class PayrollReceiptLineDto
{
    public Guid PayrollRunLineId { get; set; }
    public Guid PayrollRunId { get; set; }
    public string PayrollRunFolio { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public decimal DaysPaid { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal IncidentsAmount { get; set; }
}

public sealed record PayrollSummaryRow(string EmployeeNumber, string EmployeeName, decimal DaysPaid, decimal GrossAmount, decimal DeductionsAmount, decimal NetAmount, decimal IncidentsAmount);
public sealed record PayrollReceiptDetailRow(string ConceptCode, string ConceptName, string ConceptType, decimal Quantity, decimal Amount, decimal TaxableAmount, decimal ExemptAmount);

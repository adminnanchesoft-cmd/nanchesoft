using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PayrollFiscalEndpoints
{
    public static IEndpointRouteBuilder MapPayrollFiscalEndpoints(this IEndpointRouteBuilder app)
    {
        var accumulators = app.MapGroup("/api/payroll/tax-accumulators").WithTags("PayrollTaxAccumulators");
        accumulators.MapGet("/", GetPayrollTaxAccumulatorsAsync);
        accumulators.MapPost("/", CreatePayrollTaxAccumulatorAsync);
        accumulators.MapPut("/{id:guid}", UpdatePayrollTaxAccumulatorAsync);
        accumulators.MapDelete("/{id:guid}", DeletePayrollTaxAccumulatorAsync);
        accumulators.MapPost("/runs/{runId:guid}/generate", GeneratePayrollTaxAccumulatorsAsync);

        var obligations = app.MapGroup("/api/payroll/employer-obligations").WithTags("PayrollEmployerObligations");
        obligations.MapGet("/", GetPayrollEmployerObligationsAsync);
        obligations.MapPost("/", CreatePayrollEmployerObligationAsync);
        obligations.MapPut("/{id:guid}", UpdatePayrollEmployerObligationAsync);
        obligations.MapDelete("/{id:guid}", DeletePayrollEmployerObligationAsync);
        obligations.MapPost("/runs/{runId:guid}/generate", GeneratePayrollEmployerObligationsAsync);

        var reconciliations = app.MapGroup("/api/payroll/fiscal-reconciliations").WithTags("PayrollFiscalReconciliations");
        reconciliations.MapGet("/", GetPayrollFiscalReconciliationsAsync);
        reconciliations.MapPost("/", CreatePayrollFiscalReconciliationAsync);
        reconciliations.MapPut("/{id:guid}", UpdatePayrollFiscalReconciliationAsync);
        reconciliations.MapDelete("/{id:guid}", DeletePayrollFiscalReconciliationAsync);
        reconciliations.MapPost("/runs/{runId:guid}/reconcile", GeneratePayrollFiscalReconciliationAsync);

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

    private static string NormalizeText(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string NormalizeLower(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();

    private static async Task<IResult> GetPayrollTaxAccumulatorsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var scope = ApiTenantScope.RequireScope(httpContext);
        if (!scope.IsValid) return scope.Error!;

        var rows = await db.PayrollTaxAccumulators.AsNoTracking()
            .Where(x => x.TenantId == scope.TenantId
                     && (!scope.CompanyId.HasValue || x.CompanyId == scope.CompanyId.Value))
            .Include(x => x.Company)
            .Include(x => x.PayrollRun)
            .Include(x => x.PayrollPeriod)
            .Include(x => x.Employee)
            .OrderByDescending(x => x.LastCalculatedAt)
            .Select(x => new PayrollTaxAccumulatorDto
            {
                PayrollTaxAccumulatorId = x.Id,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                PayrollRunId = x.PayrollRunId,
                PayrollRunFolio = x.PayrollRun != null ? x.PayrollRun.Folio : string.Empty,
                PayrollRunLineId = x.PayrollRunLineId,
                PayrollPeriodId = x.PayrollPeriodId,
                PayrollPeriodName = x.PayrollPeriod != null ? x.PayrollPeriod.Name : string.Empty,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                AccumulatorCode = x.AccumulatorCode,
                AccumulatorName = x.AccumulatorName,
                FiscalYear = x.FiscalYear,
                FiscalMonth = x.FiscalMonth,
                TaxableAmount = x.TaxableAmount,
                ExemptAmount = x.ExemptAmount,
                WithheldIsr = x.WithheldIsr,
                SubsidyApplied = x.SubsidyApplied,
                SocialSecurityBase = x.SocialSecurityBase,
                NetAmount = x.NetAmount,
                LastCalculatedAt = x.LastCalculatedAt,
                Status = x.Status,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePayrollTaxAccumulatorAsync(HttpContext httpContext, PayrollTaxAccumulatorRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue || !request.PayrollRunId.HasValue || !request.PayrollRunLineId.HasValue || !request.PayrollPeriodId.HasValue || !request.EmployeeId.HasValue)
            return Results.BadRequest(new { message = "Empresa, corrida, línea, periodo y colaborador son obligatorios." });

        var entity = new PayrollTaxAccumulator
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            PayrollRunId = request.PayrollRunId.Value,
            PayrollRunLineId = request.PayrollRunLineId.Value,
            PayrollPeriodId = request.PayrollPeriodId.Value,
            EmployeeId = request.EmployeeId.Value,
            AccumulatorCode = NormalizeText(request.AccumulatorCode),
            AccumulatorName = NormalizeText(request.AccumulatorName),
            FiscalYear = request.FiscalYear,
            FiscalMonth = request.FiscalMonth,
            TaxableAmount = request.TaxableAmount,
            ExemptAmount = request.ExemptAmount,
            WithheldIsr = request.WithheldIsr,
            SubsidyApplied = request.SubsidyApplied,
            SocialSecurityBase = request.SocialSecurityBase,
            NetAmount = request.NetAmount,
            LastCalculatedAt = request.LastCalculatedAt ?? DateTime.UtcNow,
            Status = NormalizeLower(request.Status, "draft"),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.PayrollTaxAccumulators.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePayrollTaxAccumulatorAsync(Guid id, PayrollTaxAccumulatorRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollTaxAccumulators.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el acumulado fiscal." });

        entity.PayrollRunId = request.PayrollRunId ?? entity.PayrollRunId;
        entity.PayrollRunLineId = request.PayrollRunLineId ?? entity.PayrollRunLineId;
        entity.PayrollPeriodId = request.PayrollPeriodId ?? entity.PayrollPeriodId;
        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.AccumulatorCode = NormalizeText(request.AccumulatorCode, entity.AccumulatorCode);
        entity.AccumulatorName = NormalizeText(request.AccumulatorName, entity.AccumulatorName);
        entity.FiscalYear = request.FiscalYear == 0 ? entity.FiscalYear : request.FiscalYear;
        entity.FiscalMonth = request.FiscalMonth == 0 ? entity.FiscalMonth : request.FiscalMonth;
        entity.TaxableAmount = request.TaxableAmount;
        entity.ExemptAmount = request.ExemptAmount;
        entity.WithheldIsr = request.WithheldIsr;
        entity.SubsidyApplied = request.SubsidyApplied;
        entity.SocialSecurityBase = request.SocialSecurityBase;
        entity.NetAmount = request.NetAmount;
        entity.LastCalculatedAt = request.LastCalculatedAt ?? entity.LastCalculatedAt;
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePayrollTaxAccumulatorAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollTaxAccumulators.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el acumulado fiscal." });

        db.PayrollTaxAccumulators.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GeneratePayrollTaxAccumulatorsAsync(Guid runId, NanchesoftDbContext db)
    {
        var run = await db.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null)
            return Results.NotFound(new { message = "No se encontró el proceso de nómina." });

        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == run.CompanyId);
        var period = await db.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == run.PayrollPeriodId);
        if (company is null || period is null)
            return Results.BadRequest(new { message = "No se encontró el contexto fiscal de la corrida." });

        var lines = await db.PayrollRunLines.AsNoTracking()
            .Where(x => x.PayrollRunId == runId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        var inserted = 0;
        foreach (var line in lines)
        {
            var code = $"ACU-{line.EmployeeId.ToString("N")[..8]}";
            var exists = await db.PayrollTaxAccumulators.AnyAsync(x =>
                x.PayrollRunId == runId &&
                x.EmployeeId == line.EmployeeId &&
                x.AccumulatorCode == code);

            if (exists)
                continue;

            db.PayrollTaxAccumulators.Add(new PayrollTaxAccumulator
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                PayrollRunId = run.Id,
                PayrollRunLineId = line.Id,
                PayrollPeriodId = period.Id,
                EmployeeId = line.EmployeeId,
                AccumulatorCode = code,
                AccumulatorName = "ACUMULADO ISR / CFDI",
                FiscalYear = period.StartDate.Year,
                FiscalMonth = period.StartDate.Month,
                TaxableAmount = Math.Round(line.GrossAmount * 0.82m, 2),
                ExemptAmount = Math.Round(line.GrossAmount * 0.18m, 2),
                WithheldIsr = Math.Round(line.GrossAmount * 0.07m, 2),
                SubsidyApplied = 0m,
                SocialSecurityBase = Math.Round(line.GrossAmount * 0.65m, 2),
                NetAmount = line.NetAmount,
                LastCalculatedAt = DateTime.UtcNow,
                Status = "calculated",
                Notes = "Generado automáticamente desde la corrida de nómina.",
                CreatedBy = "web-api"
            });
            inserted++;
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, inserted });
    }

    private static async Task<IResult> GetPayrollEmployerObligationsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var scope = ApiTenantScope.RequireScope(httpContext);
        if (!scope.IsValid) return scope.Error!;

        var rows = await db.PayrollEmployerObligations.AsNoTracking()
            .Where(x => x.TenantId == scope.TenantId
                     && (!scope.CompanyId.HasValue || x.CompanyId == scope.CompanyId.Value))
            .Include(x => x.Company)
            .Include(x => x.PayrollRun)
            .Include(x => x.PayrollPeriod)
            .OrderByDescending(x => x.DueDate)
            .Select(x => new PayrollEmployerObligationDto
            {
                PayrollEmployerObligationId = x.Id,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                PayrollRunId = x.PayrollRunId,
                PayrollRunFolio = x.PayrollRun != null ? x.PayrollRun.Folio : string.Empty,
                PayrollPeriodId = x.PayrollPeriodId,
                PayrollPeriodName = x.PayrollPeriod != null ? x.PayrollPeriod.Name : string.Empty,
                ObligationCode = x.ObligationCode,
                ObligationName = x.ObligationName,
                ObligationType = x.ObligationType,
                FiscalYear = x.FiscalYear,
                FiscalMonth = x.FiscalMonth,
                BaseAmount = x.BaseAmount,
                Amount = x.Amount,
                EmployeesCount = x.EmployeesCount,
                DueDate = x.DueDate,
                Status = x.Status,
                PaidAt = x.PaidAt,
                ReferenceNumber = x.ReferenceNumber,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePayrollEmployerObligationAsync(HttpContext httpContext, PayrollEmployerObligationRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue || !request.PayrollRunId.HasValue || !request.PayrollPeriodId.HasValue)
            return Results.BadRequest(new { message = "Empresa, corrida y periodo son obligatorios." });

        var entity = new PayrollEmployerObligation
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            PayrollRunId = request.PayrollRunId.Value,
            PayrollPeriodId = request.PayrollPeriodId.Value,
            ObligationCode = NormalizeText(request.ObligationCode),
            ObligationName = NormalizeText(request.ObligationName),
            ObligationType = NormalizeLower(request.ObligationType, "tax"),
            FiscalYear = request.FiscalYear,
            FiscalMonth = request.FiscalMonth,
            BaseAmount = request.BaseAmount,
            Amount = request.Amount,
            EmployeesCount = request.EmployeesCount,
            DueDate = request.DueDate ?? DateTime.UtcNow.Date,
            Status = NormalizeLower(request.Status, "draft"),
            PaidAt = request.PaidAt,
            ReferenceNumber = NormalizeText(request.ReferenceNumber),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.PayrollEmployerObligations.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePayrollEmployerObligationAsync(Guid id, PayrollEmployerObligationRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollEmployerObligations.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la obligación patronal." });

        entity.PayrollRunId = request.PayrollRunId ?? entity.PayrollRunId;
        entity.PayrollPeriodId = request.PayrollPeriodId ?? entity.PayrollPeriodId;
        entity.ObligationCode = NormalizeText(request.ObligationCode, entity.ObligationCode);
        entity.ObligationName = NormalizeText(request.ObligationName, entity.ObligationName);
        entity.ObligationType = NormalizeLower(request.ObligationType, entity.ObligationType);
        entity.FiscalYear = request.FiscalYear == 0 ? entity.FiscalYear : request.FiscalYear;
        entity.FiscalMonth = request.FiscalMonth == 0 ? entity.FiscalMonth : request.FiscalMonth;
        entity.BaseAmount = request.BaseAmount;
        entity.Amount = request.Amount;
        entity.EmployeesCount = request.EmployeesCount <= 0 ? entity.EmployeesCount : request.EmployeesCount;
        entity.DueDate = request.DueDate ?? entity.DueDate;
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.PaidAt = request.PaidAt ?? entity.PaidAt;
        entity.ReferenceNumber = NormalizeText(request.ReferenceNumber, entity.ReferenceNumber);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePayrollEmployerObligationAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollEmployerObligations.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la obligación patronal." });

        db.PayrollEmployerObligations.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GeneratePayrollEmployerObligationsAsync(Guid runId, NanchesoftDbContext db)
    {
        var run = await db.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null)
            return Results.NotFound(new { message = "No se encontró el proceso de nómina." });

        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == run.CompanyId);
        var period = await db.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == run.PayrollPeriodId);
        if (company is null || period is null)
            return Results.BadRequest(new { message = "No se encontró el contexto patronal de la corrida." });

        var templates = new[]
        {
            new { Code = "IMSS", Name = "Cuotas IMSS", Type = "social-security", Rate = 0.185m, DueOffset = 17 },
            new { Code = "INFONAVIT", Name = "Aportación INFONAVIT", Type = "housing", Rate = 0.05m, DueOffset = 17 },
            new { Code = "ISN", Name = "Impuesto sobre nómina", Type = "state-tax", Rate = 0.03m, DueOffset = 20 },
        };

        var inserted = 0;
        foreach (var template in templates)
        {
            var exists = await db.PayrollEmployerObligations.AnyAsync(x => x.PayrollRunId == runId && x.ObligationCode == template.Code);
            if (exists)
                continue;

            db.PayrollEmployerObligations.Add(new PayrollEmployerObligation
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                PayrollRunId = run.Id,
                PayrollPeriodId = period.Id,
                ObligationCode = template.Code,
                ObligationName = template.Name,
                ObligationType = template.Type,
                FiscalYear = period.StartDate.Year,
                FiscalMonth = period.StartDate.Month,
                BaseAmount = run.GrossAmount,
                Amount = Math.Round(run.GrossAmount * template.Rate, 2),
                EmployeesCount = run.EmployeeCount,
                DueDate = new DateTime(period.EndDate.Year, period.EndDate.Month, 1).AddMonths(1).AddDays(template.DueOffset - 1),
                Status = "pending",
                ReferenceNumber = string.Empty,
                Notes = "Generado automáticamente a partir de la corrida de nómina.",
                CreatedBy = "web-api"
            });
            inserted++;
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, inserted });
    }

    private static async Task<IResult> GetPayrollFiscalReconciliationsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var scope = ApiTenantScope.RequireScope(httpContext);
        if (!scope.IsValid) return scope.Error!;

        var rows = await db.PayrollFiscalReconciliations.AsNoTracking()
            .Where(x => x.TenantId == scope.TenantId
                     && (!scope.CompanyId.HasValue || x.CompanyId == scope.CompanyId.Value))
            .Include(x => x.Company)
            .Include(x => x.PayrollRun)
            .Include(x => x.PayrollPeriod)
            .OrderByDescending(x => x.ReconciledAt)
            .Select(x => new PayrollFiscalReconciliationDto
            {
                PayrollFiscalReconciliationId = x.Id,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                PayrollRunId = x.PayrollRunId,
                PayrollRunFolio = x.PayrollRun != null ? x.PayrollRun.Folio : string.Empty,
                PayrollPeriodId = x.PayrollPeriodId,
                PayrollPeriodName = x.PayrollPeriod != null ? x.PayrollPeriod.Name : string.Empty,
                PayrollDispersionBatchId = x.PayrollDispersionBatchId,
                PayrollAccountingPostingId = x.PayrollAccountingPostingId,
                ReconciliationCode = x.ReconciliationCode,
                FiscalYear = x.FiscalYear,
                FiscalMonth = x.FiscalMonth,
                ReceiptsStampedCount = x.ReceiptsStampedCount,
                DispersionValidatedCount = x.DispersionValidatedCount,
                AccountingPostedCount = x.AccountingPostedCount,
                TaxAccumulatorsCount = x.TaxAccumulatorsCount,
                GrossAmount = x.GrossAmount,
                WithheldIsrAmount = x.WithheldIsrAmount,
                EmployerTaxesAmount = x.EmployerTaxesAmount,
                NetAmount = x.NetAmount,
                DifferenceAmount = x.DifferenceAmount,
                Status = x.Status,
                ReconciledAt = x.ReconciledAt,
                ClosedBy = x.ClosedBy,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePayrollFiscalReconciliationAsync(HttpContext httpContext, PayrollFiscalReconciliationRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue || !request.PayrollRunId.HasValue || !request.PayrollPeriodId.HasValue)
            return Results.BadRequest(new { message = "Empresa, corrida y periodo son obligatorios." });

        var entity = new PayrollFiscalReconciliation
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            PayrollRunId = request.PayrollRunId.Value,
            PayrollPeriodId = request.PayrollPeriodId.Value,
            PayrollDispersionBatchId = request.PayrollDispersionBatchId,
            PayrollAccountingPostingId = request.PayrollAccountingPostingId,
            ReconciliationCode = NormalizeText(request.ReconciliationCode),
            FiscalYear = request.FiscalYear,
            FiscalMonth = request.FiscalMonth,
            ReceiptsStampedCount = request.ReceiptsStampedCount,
            DispersionValidatedCount = request.DispersionValidatedCount,
            AccountingPostedCount = request.AccountingPostedCount,
            TaxAccumulatorsCount = request.TaxAccumulatorsCount,
            GrossAmount = request.GrossAmount,
            WithheldIsrAmount = request.WithheldIsrAmount,
            EmployerTaxesAmount = request.EmployerTaxesAmount,
            NetAmount = request.NetAmount,
            DifferenceAmount = request.DifferenceAmount,
            Status = NormalizeLower(request.Status, "draft"),
            ReconciledAt = request.ReconciledAt,
            ClosedBy = NormalizeText(request.ClosedBy),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.PayrollFiscalReconciliations.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePayrollFiscalReconciliationAsync(Guid id, PayrollFiscalReconciliationRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollFiscalReconciliations.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la conciliación fiscal." });

        entity.PayrollRunId = request.PayrollRunId ?? entity.PayrollRunId;
        entity.PayrollPeriodId = request.PayrollPeriodId ?? entity.PayrollPeriodId;
        entity.PayrollDispersionBatchId = request.PayrollDispersionBatchId ?? entity.PayrollDispersionBatchId;
        entity.PayrollAccountingPostingId = request.PayrollAccountingPostingId ?? entity.PayrollAccountingPostingId;
        entity.ReconciliationCode = NormalizeText(request.ReconciliationCode, entity.ReconciliationCode);
        entity.FiscalYear = request.FiscalYear == 0 ? entity.FiscalYear : request.FiscalYear;
        entity.FiscalMonth = request.FiscalMonth == 0 ? entity.FiscalMonth : request.FiscalMonth;
        entity.ReceiptsStampedCount = request.ReceiptsStampedCount;
        entity.DispersionValidatedCount = request.DispersionValidatedCount;
        entity.AccountingPostedCount = request.AccountingPostedCount;
        entity.TaxAccumulatorsCount = request.TaxAccumulatorsCount;
        entity.GrossAmount = request.GrossAmount;
        entity.WithheldIsrAmount = request.WithheldIsrAmount;
        entity.EmployerTaxesAmount = request.EmployerTaxesAmount;
        entity.NetAmount = request.NetAmount;
        entity.DifferenceAmount = request.DifferenceAmount;
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.ReconciledAt = request.ReconciledAt ?? entity.ReconciledAt;
        entity.ClosedBy = NormalizeText(request.ClosedBy, entity.ClosedBy);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePayrollFiscalReconciliationAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollFiscalReconciliations.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la conciliación fiscal." });

        db.PayrollFiscalReconciliations.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GeneratePayrollFiscalReconciliationAsync(Guid runId, NanchesoftDbContext db)
    {
        var run = await db.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null)
            return Results.NotFound(new { message = "No se encontró el proceso de nómina." });

        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == run.CompanyId);
        var period = await db.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == run.PayrollPeriodId);
        if (company is null || period is null)
            return Results.BadRequest(new { message = "No se encontró el contexto fiscal de la corrida." });

        var code = $"FISC-{run.Folio}";
        var existing = await db.PayrollFiscalReconciliations.FirstOrDefaultAsync(x => x.PayrollRunId == runId && x.ReconciliationCode == code);
        if (existing is not null)
            return Results.Ok(new { success = true, id = existing.Id, message = "La conciliación ya existía." });

        var dispersionBatchId = await db.PayrollDispersionBatches
            .Where(x => x.PayrollRunId == runId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();

        var accountingPostingId = await db.PayrollAccountingPostings
            .Where(x => x.PayrollRunId == runId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();

        var receiptsCount = await db.PayrollReceiptControls.CountAsync(x => x.PayrollRunId == runId);
        var taxCount = await db.PayrollTaxAccumulators.CountAsync(x => x.PayrollRunId == runId);
        var employerTaxesAmount = await db.PayrollEmployerObligations.Where(x => x.PayrollRunId == runId).SumAsync(x => (decimal?)x.Amount) ?? 0m;

        var entity = new PayrollFiscalReconciliation
        {
            TenantId = company.TenantId,
            CompanyId = company.Id,
            PayrollRunId = run.Id,
            PayrollPeriodId = period.Id,
            PayrollDispersionBatchId = dispersionBatchId,
            PayrollAccountingPostingId = accountingPostingId,
            ReconciliationCode = code,
            FiscalYear = period.StartDate.Year,
            FiscalMonth = period.StartDate.Month,
            ReceiptsStampedCount = receiptsCount,
            DispersionValidatedCount = dispersionBatchId.HasValue ? Math.Max(run.EmployeeCount, 0) : 0,
            AccountingPostedCount = accountingPostingId.HasValue ? 1 : 0,
            TaxAccumulatorsCount = taxCount,
            GrossAmount = run.GrossAmount,
            WithheldIsrAmount = Math.Round(run.GrossAmount * 0.07m, 2),
            EmployerTaxesAmount = employerTaxesAmount,
            NetAmount = run.NetAmount,
            DifferenceAmount = 0m,
            Status = "ready",
            ReconciledAt = DateTime.UtcNow,
            ClosedBy = "web-api",
            Notes = "Conciliación fiscal generada entre recibos, acumulados, dispersión y contabilidad.",
            CreatedBy = "web-api"
        };

        db.PayrollFiscalReconciliations.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }
}

public class PayrollTaxAccumulatorRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? PayrollRunLineId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string AccumulatorCode { get; set; } = string.Empty;
    public string AccumulatorName { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    public int FiscalMonth { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal ExemptAmount { get; set; }
    public decimal WithheldIsr { get; set; }
    public decimal SubsidyApplied { get; set; }
    public decimal SocialSecurityBase { get; set; }
    public decimal NetAmount { get; set; }
    public DateTime? LastCalculatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollTaxAccumulatorDto : PayrollTaxAccumulatorRequest
{
    public Guid PayrollTaxAccumulatorId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
    public string PayrollPeriodName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
}

public class PayrollEmployerObligationRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public string ObligationCode { get; set; } = string.Empty;
    public string ObligationName { get; set; } = string.Empty;
    public string ObligationType { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    public int FiscalMonth { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal Amount { get; set; }
    public int EmployeesCount { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollEmployerObligationDto : PayrollEmployerObligationRequest
{
    public Guid PayrollEmployerObligationId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
    public string PayrollPeriodName { get; set; } = string.Empty;
}

public class PayrollFiscalReconciliationRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public Guid? PayrollDispersionBatchId { get; set; }
    public Guid? PayrollAccountingPostingId { get; set; }
    public string ReconciliationCode { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    public int FiscalMonth { get; set; }
    public int ReceiptsStampedCount { get; set; }
    public int DispersionValidatedCount { get; set; }
    public int AccountingPostedCount { get; set; }
    public int TaxAccumulatorsCount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal WithheldIsrAmount { get; set; }
    public decimal EmployerTaxesAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal DifferenceAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ReconciledAt { get; set; }
    public string ClosedBy { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollFiscalReconciliationDto : PayrollFiscalReconciliationRequest
{
    public Guid PayrollFiscalReconciliationId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
    public string PayrollPeriodName { get; set; } = string.Empty;
}

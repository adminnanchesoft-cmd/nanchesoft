using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PayrollGlobalMovementsEndpoints
{
    public static IEndpointRouteBuilder MapPayrollGlobalMovementsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payroll/global-movements").WithTags("PayrollGlobalMovements");
        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
        group.MapGet("/{id:guid}/preview", PreviewAsync);
        group.MapPost("/{id:guid}/apply", ApplyAsync);
        group.MapPost("/{id:guid}/close", CloseAsync);
        group.MapGet("/{id:guid}/lines", GetLinesAsync);

        return app;
    }

    private static string Trim(string? value, int max = 0, string fallback = "")
    {
        var text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        if (max > 0 && text.Length > max) text = text.Substring(0, max);
        return text;
    }

    private static string Lower(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();

    private static string Upper(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();

    private static DateTime Utc(DateTime? value, DateTime fallback)
    {
        var source = value ?? fallback;
        return source.Kind == DateTimeKind.Utc ? source : DateTime.SpecifyKind(source, DateTimeKind.Utc);
    }

    private static List<Guid> ParseGuidList(string raw)
        => (raw ?? string.Empty)
            .Split(new[] { ',', ';', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Guid.TryParse(x, out var id) ? id : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

    private static string SerializeGuidList(IEnumerable<Guid>? ids)
        => ids is null ? string.Empty : string.Join(",", ids.Where(x => x != Guid.Empty).Distinct().Select(x => x.ToString("D")));

    private static async Task<(Guid? TenantId, Guid? CompanyId)> ResolveContextAsync(HttpContext httpContext, NanchesoftDbContext db)
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
                return (comp.TenantId, comp.Id);
        }

        var fallback = await db.Companies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        return fallback is null ? (null, null) : (fallback.TenantId, fallback.Id);
    }

    private static async Task<IResult> ListAsync(NanchesoftDbContext db)
    {
        var rows = await db.PayrollGlobalMovements.AsNoTracking()
            .Include(x => x.PayrollConcept)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PayrollGlobalMovementDto
            {
                PayrollGlobalMovementId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                PayrollConceptId = x.PayrollConceptId,
                PayrollConceptCode = x.PayrollConcept != null ? x.PayrollConcept.Code : string.Empty,
                PayrollConceptName = x.PayrollConcept != null ? x.PayrollConcept.Name : string.Empty,
                BatchCode = x.BatchCode,
                BatchName = x.BatchName,
                MovementType = x.MovementType,
                CalculationMode = x.CalculationMode,
                Quantity = x.Quantity,
                Amount = x.Amount,
                Percentage = x.Percentage,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                TimesToApply = x.TimesToApply,
                TimesApplied = x.TimesApplied,
                MaxAmount = x.MaxAmount,
                AccumulatedAmount = x.AccumulatedAmount,
                ControlNumber = x.ControlNumber,
                FilterDepartmentIds = x.FilterDepartmentIds,
                FilterPositionIds = x.FilterPositionIds,
                FilterBranchIds = x.FilterBranchIds,
                FilterEmployerRegistrationIds = x.FilterEmployerRegistrationIds,
                FilterWorkShiftIds = x.FilterWorkShiftIds,
                FilterEmployeeIds = x.FilterEmployeeIds,
                ExcludeEmployeeIds = x.ExcludeEmployeeIds,
                MinSalary = x.MinSalary,
                MaxSalary = x.MaxSalary,
                MakeRecurring = x.MakeRecurring,
                Status = x.Status,
                AppliedAt = x.AppliedAt,
                AppliedBy = x.AppliedBy,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> GetAsync(Guid id, NanchesoftDbContext db)
    {
        var x = await db.PayrollGlobalMovements.AsNoTracking().Include(p => p.PayrollConcept).FirstOrDefaultAsync(p => p.Id == id);
        if (x is null) return Results.NotFound(new { message = "No se encontró el movimiento global." });

        return Results.Ok(new PayrollGlobalMovementDto
        {
            PayrollGlobalMovementId = x.Id,
            TenantId = x.TenantId,
            CompanyId = x.CompanyId,
            PayrollConceptId = x.PayrollConceptId,
            PayrollConceptCode = x.PayrollConcept != null ? x.PayrollConcept.Code : string.Empty,
            PayrollConceptName = x.PayrollConcept != null ? x.PayrollConcept.Name : string.Empty,
            BatchCode = x.BatchCode,
            BatchName = x.BatchName,
            MovementType = x.MovementType,
            CalculationMode = x.CalculationMode,
            Quantity = x.Quantity,
            Amount = x.Amount,
            Percentage = x.Percentage,
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            TimesToApply = x.TimesToApply,
            TimesApplied = x.TimesApplied,
            MaxAmount = x.MaxAmount,
            AccumulatedAmount = x.AccumulatedAmount,
            ControlNumber = x.ControlNumber,
            FilterDepartmentIds = x.FilterDepartmentIds,
            FilterPositionIds = x.FilterPositionIds,
            FilterBranchIds = x.FilterBranchIds,
            FilterEmployerRegistrationIds = x.FilterEmployerRegistrationIds,
            FilterWorkShiftIds = x.FilterWorkShiftIds,
            FilterEmployeeIds = x.FilterEmployeeIds,
            ExcludeEmployeeIds = x.ExcludeEmployeeIds,
            MinSalary = x.MinSalary,
            MaxSalary = x.MaxSalary,
            MakeRecurring = x.MakeRecurring,
            Status = x.Status,
            AppliedAt = x.AppliedAt,
            AppliedBy = x.AppliedBy,
            Notes = x.Notes,
            IsActive = x.IsActive
        });
    }

    private static async Task<IResult> CreateAsync(HttpContext httpContext, PayrollGlobalMovementRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.PayrollConceptId.HasValue)
            return Results.BadRequest(new { message = "Empresa y concepto son obligatorios." });

        var concept = await db.PayrollConcepts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.PayrollConceptId.Value && x.CompanyId == companyId.Value);
        if (concept is null)
            return Results.BadRequest(new { message = "El concepto no existe para la empresa." });

        var code = Upper(request.BatchCode, $"MG-{DateTime.UtcNow:yyMMddHHmmss}");
        if (await db.PayrollGlobalMovements.AnyAsync(x => x.CompanyId == companyId.Value && x.BatchCode == code))
            return Results.BadRequest(new { message = "Ya existe un movimiento global con ese código." });

        var entity = new PayrollGlobalMovement
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            PayrollConceptId = concept.Id,
            BatchCode = code,
            BatchName = Trim(request.BatchName, 160, concept.Name),
            MovementType = Lower(request.MovementType, concept.ConceptType),
            CalculationMode = Lower(request.CalculationMode, "fixed"),
            Quantity = request.Quantity <= 0m ? 1m : Math.Round(request.Quantity, 4),
            Amount = Math.Max(0m, Math.Round(request.Amount, 2)),
            Percentage = Math.Max(0m, request.Percentage),
            StartDate = Utc(request.StartDate, DateTime.UtcNow.Date),
            EndDate = request.EndDate.HasValue ? Utc(request.EndDate, request.EndDate.Value) : null,
            TimesToApply = Math.Max(0, request.TimesToApply),
            TimesApplied = 0,
            MaxAmount = Math.Max(0m, Math.Round(request.MaxAmount, 2)),
            AccumulatedAmount = 0m,
            ControlNumber = Trim(request.ControlNumber, 60),
            FilterDepartmentIds = SerializeGuidList(request.FilterDepartmentIds),
            FilterPositionIds = SerializeGuidList(request.FilterPositionIds),
            FilterBranchIds = SerializeGuidList(request.FilterBranchIds),
            FilterEmployerRegistrationIds = SerializeGuidList(request.FilterEmployerRegistrationIds),
            FilterWorkShiftIds = SerializeGuidList(request.FilterWorkShiftIds),
            FilterEmployeeIds = SerializeGuidList(request.FilterEmployeeIds),
            ExcludeEmployeeIds = SerializeGuidList(request.ExcludeEmployeeIds),
            MinSalary = Math.Max(0m, request.MinSalary),
            MaxSalary = Math.Max(0m, request.MaxSalary),
            MakeRecurring = request.MakeRecurring,
            Status = Lower(request.Status, "draft"),
            AppliedAt = null,
            AppliedBy = string.Empty,
            Notes = Trim(request.Notes, 600),
            IsActive = request.IsActive,
            CreatedBy = "global-movements"
        };

        db.PayrollGlobalMovements.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id, batchCode = entity.BatchCode });
    }

    private static async Task<IResult> UpdateAsync(Guid id, PayrollGlobalMovementRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollGlobalMovements.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el movimiento global." });

        if (entity.Status == "closed" || entity.Status == "cancelled")
            return Results.BadRequest(new { message = "No se puede editar un lote cerrado o cancelado." });

        if (request.PayrollConceptId.HasValue && request.PayrollConceptId.Value != entity.PayrollConceptId)
        {
            var concept = await db.PayrollConcepts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.PayrollConceptId.Value && x.CompanyId == entity.CompanyId);
            if (concept is null) return Results.BadRequest(new { message = "El concepto no existe." });
            entity.PayrollConceptId = concept.Id;
            entity.MovementType = Lower(request.MovementType, concept.ConceptType);
        }

        if (!string.IsNullOrWhiteSpace(request.BatchCode))
        {
            var code = Upper(request.BatchCode, entity.BatchCode);
            if (code != entity.BatchCode && await db.PayrollGlobalMovements.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.BatchCode == code))
                return Results.BadRequest(new { message = "Ya existe otro movimiento global con ese código." });
            entity.BatchCode = code;
        }

        entity.BatchName = Trim(request.BatchName, 160, entity.BatchName);
        entity.MovementType = Lower(request.MovementType, entity.MovementType);
        entity.CalculationMode = Lower(request.CalculationMode, entity.CalculationMode);
        entity.Quantity = request.Quantity <= 0m ? entity.Quantity : Math.Round(request.Quantity, 4);
        entity.Amount = Math.Max(0m, Math.Round(request.Amount, 2));
        entity.Percentage = Math.Max(0m, request.Percentage);
        entity.StartDate = request.StartDate.HasValue ? Utc(request.StartDate, entity.StartDate) : entity.StartDate;
        entity.EndDate = request.EndDate.HasValue ? Utc(request.EndDate, request.EndDate.Value) : entity.EndDate;
        entity.TimesToApply = Math.Max(0, request.TimesToApply);
        entity.MaxAmount = Math.Max(0m, Math.Round(request.MaxAmount, 2));
        entity.ControlNumber = Trim(request.ControlNumber, 60, entity.ControlNumber);
        entity.FilterDepartmentIds = SerializeGuidList(request.FilterDepartmentIds);
        entity.FilterPositionIds = SerializeGuidList(request.FilterPositionIds);
        entity.FilterBranchIds = SerializeGuidList(request.FilterBranchIds);
        entity.FilterEmployerRegistrationIds = SerializeGuidList(request.FilterEmployerRegistrationIds);
        entity.FilterWorkShiftIds = SerializeGuidList(request.FilterWorkShiftIds);
        entity.FilterEmployeeIds = SerializeGuidList(request.FilterEmployeeIds);
        entity.ExcludeEmployeeIds = SerializeGuidList(request.ExcludeEmployeeIds);
        entity.MinSalary = Math.Max(0m, request.MinSalary);
        entity.MaxSalary = Math.Max(0m, request.MaxSalary);
        entity.MakeRecurring = request.MakeRecurring;
        entity.Status = Lower(request.Status, entity.Status);
        entity.Notes = Trim(request.Notes, 600, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "global-movements";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollGlobalMovements.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el movimiento global." });

        var hasLines = await db.PayrollGlobalMovementLines.AnyAsync(x => x.PayrollGlobalMovementId == id);
        if (hasLines)
            return Results.BadRequest(new { message = "No se puede eliminar un lote que ya tiene aplicaciones. Cancélelo en su lugar." });

        db.PayrollGlobalMovements.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static IQueryable<Employee> ApplyFilters(IQueryable<Employee> query, PayrollGlobalMovement movement)
    {
        var departments = ParseGuidList(movement.FilterDepartmentIds);
        var positions = ParseGuidList(movement.FilterPositionIds);
        var branches = ParseGuidList(movement.FilterBranchIds);
        var employeesInclude = ParseGuidList(movement.FilterEmployeeIds);
        var employeesExclude = ParseGuidList(movement.ExcludeEmployeeIds);

        if (departments.Count > 0)
            query = query.Where(x => x.DepartmentId.HasValue && departments.Contains(x.DepartmentId.Value));
        if (positions.Count > 0)
            query = query.Where(x => x.PositionId.HasValue && positions.Contains(x.PositionId.Value));
        if (branches.Count > 0)
            query = query.Where(x => x.BranchId.HasValue && branches.Contains(x.BranchId.Value));
        if (employeesInclude.Count > 0)
            query = query.Where(x => employeesInclude.Contains(x.Id));
        if (employeesExclude.Count > 0)
            query = query.Where(x => !employeesExclude.Contains(x.Id));
        if (movement.MinSalary > 0m)
            query = query.Where(x => x.PeriodSalary >= movement.MinSalary || x.DailySalary >= movement.MinSalary);
        if (movement.MaxSalary > 0m)
            query = query.Where(x => x.PeriodSalary <= movement.MaxSalary || x.DailySalary <= movement.MaxSalary);

        return query;
    }

    private static async Task<IResult> PreviewAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollGlobalMovements.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el movimiento global." });

        var query = db.Employees.AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Position)
            .Where(x => x.CompanyId == entity.CompanyId && x.IsActive && x.Status == "active");

        query = ApplyFilters(query, entity);

        var employees = await query
            .OrderBy(x => x.EmployeeNumber)
            .Select(x => new PayrollGlobalMovementPreviewEmployee
            {
                EmployeeId = x.Id,
                EmployeeNumber = x.EmployeeNumber,
                EmployeeName = (x.FirstName + " " + x.LastName).Trim(),
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                PositionName = x.Position != null ? x.Position.Name : string.Empty,
                Salary = x.PeriodSalary > 0 ? x.PeriodSalary : x.DailySalary,
                Amount = entity.Amount,
                Quantity = entity.Quantity
            })
            .ToListAsync();

        return Results.Ok(new PayrollGlobalMovementPreviewDto
        {
            PayrollGlobalMovementId = entity.Id,
            BatchCode = entity.BatchCode,
            BatchName = entity.BatchName,
            Employees = employees,
            TotalEmployees = employees.Count,
            TotalAmount = employees.Sum(x => x.Amount)
        });
    }

    private static (decimal Taxable, decimal Exempt) SplitConceptAmount(PayrollConcept concept, decimal amount)
    {
        if (amount <= 0m) return (0m, 0m);
        var taxableType = (concept.TaxableType ?? "taxable").Trim().ToLowerInvariant();
        return taxableType switch
        {
            "exempt" => (0m, amount),
            "mixed" => (Math.Round(amount * (concept.TaxablePercent / 100m), 2), Math.Round(amount * (concept.ExemptPercent / 100m), 2)),
            _ => (amount, 0m)
        };
    }

    private static async Task<IResult> ApplyAsync(Guid id, PayrollGlobalMovementApplyRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollGlobalMovements.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el movimiento global." });
        if (entity.Status == "closed" || entity.Status == "cancelled")
            return Results.BadRequest(new { message = "El lote está cerrado o cancelado." });

        var period = await db.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.PayrollPeriodId && x.CompanyId == entity.CompanyId);
        if (period is null)
            return Results.BadRequest(new { message = "El periodo no existe para la empresa." });

        var concept = await db.PayrollConcepts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.PayrollConceptId);
        if (concept is null)
            return Results.BadRequest(new { message = "El concepto del lote no existe." });

        if (entity.TimesToApply > 0 && entity.TimesApplied >= entity.TimesToApply)
            return Results.BadRequest(new { message = "El lote ya alcanzó el número máximo de aplicaciones." });

        if (entity.MaxAmount > 0m && entity.AccumulatedAmount >= entity.MaxAmount)
            return Results.BadRequest(new { message = "El lote ya alcanzó su monto límite." });

        var query = db.Employees
            .Where(x => x.CompanyId == entity.CompanyId && x.IsActive && x.Status == "active");
        query = ApplyFilters(query, entity);
        var employees = await query.ToListAsync();
        if (employees.Count == 0)
            return Results.BadRequest(new { message = "No hay colaboradores que coincidan con los filtros." });

        var alreadyApplied = await db.PayrollGlobalMovementLines
            .Where(x => x.PayrollGlobalMovementId == id && x.PayrollPeriodId == request.PayrollPeriodId)
            .Select(x => x.EmployeeId)
            .ToListAsync();
        var alreadySet = alreadyApplied.ToHashSet();

        var existingAdjustments = await db.PrePayrollAdjustments
            .Where(x => x.CompanyId == entity.CompanyId
                && x.PayrollPeriodId == request.PayrollPeriodId
                && x.PayrollConceptId == entity.PayrollConceptId)
            .ToListAsync();
        var existingAdjustmentMap = existingAdjustments.ToDictionary(x => x.EmployeeId);

        var existingRecurring = await db.PayrollRecurringMovements
            .Where(x => x.CompanyId == entity.CompanyId && x.PayrollConceptId == entity.PayrollConceptId)
            .ToListAsync();
        var recurringMap = existingRecurring.GroupBy(x => x.EmployeeId).ToDictionary(g => g.Key, g => g.First());

        var newLines = new List<PayrollGlobalMovementLine>();
        var appliedAmountTotal = 0m;
        var skipped = 0;
        var applied = 0;

        var unitAmount = Math.Round(entity.Amount, 2);
        var unitQty = entity.Quantity <= 0m ? 1m : entity.Quantity;

        foreach (var emp in employees)
        {
            if (alreadySet.Contains(emp.Id))
            {
                skipped++;
                continue;
            }

            if (entity.MaxAmount > 0m && entity.AccumulatedAmount + appliedAmountTotal + unitAmount > entity.MaxAmount)
                break;

            var split = SplitConceptAmount(concept, unitAmount);

            Guid? adjustmentId = null;
            if (existingAdjustmentMap.TryGetValue(emp.Id, out var existingAdj))
            {
                existingAdj.AdjustmentCode = (concept.Code ?? string.Empty).ToUpperInvariant();
                existingAdj.AdjustmentName = concept.Name ?? string.Empty;
                existingAdj.AdjustmentType = (concept.ConceptType ?? "perception").ToLowerInvariant();
                existingAdj.CaptureSource = "global-movement";
                existingAdj.ReferenceDate = period.EndDate;
                existingAdj.Quantity = unitQty;
                existingAdj.Amount = unitAmount;
                existingAdj.TaxableAmount = split.Taxable;
                existingAdj.ExemptAmount = split.Exempt;
                existingAdj.Status = "captured";
                existingAdj.Notes = string.IsNullOrWhiteSpace(entity.Notes) ? existingAdj.Notes : entity.Notes;
                existingAdj.IsActive = true;
                existingAdj.UpdatedAt = DateTime.UtcNow;
                existingAdj.UpdatedBy = "global-movement";
                adjustmentId = existingAdj.Id;
            }
            else
            {
                var newAdj = new PrePayrollAdjustment
                {
                    TenantId = entity.TenantId,
                    CompanyId = entity.CompanyId,
                    EmployeeId = emp.Id,
                    PayrollPeriodId = period.Id,
                    PayrollConceptId = concept.Id,
                    AdjustmentCode = (concept.Code ?? string.Empty).ToUpperInvariant(),
                    AdjustmentName = concept.Name ?? string.Empty,
                    AdjustmentType = (concept.ConceptType ?? "perception").ToLowerInvariant(),
                    CaptureSource = "global-movement",
                    ReferenceDate = period.EndDate,
                    Quantity = unitQty,
                    Amount = unitAmount,
                    TaxableAmount = split.Taxable,
                    ExemptAmount = split.Exempt,
                    Status = "captured",
                    Notes = entity.Notes ?? string.Empty,
                    IsActive = true,
                    CreatedBy = "global-movement"
                };
                db.PrePayrollAdjustments.Add(newAdj);
                adjustmentId = newAdj.Id;
            }

            Guid? recurringId = null;
            if (entity.MakeRecurring)
            {
                if (recurringMap.TryGetValue(emp.Id, out var rec))
                {
                    rec.Amount = unitAmount;
                    rec.Quantity = unitQty;
                    rec.MovementCode = entity.BatchCode;
                    rec.MovementName = entity.BatchName;
                    rec.MovementType = (concept.ConceptType ?? "perception").ToLowerInvariant();
                    rec.CalculationMode = entity.CalculationMode;
                    rec.EffectiveStartDate = entity.StartDate;
                    rec.EffectiveEndDate = entity.EndDate;
                    rec.ApplyEveryRun = true;
                    rec.Status = "active";
                    rec.IsActive = true;
                    rec.Notes = entity.Notes ?? string.Empty;
                    rec.UpdatedAt = DateTime.UtcNow;
                    rec.UpdatedBy = "global-movement";
                    recurringId = rec.Id;
                }
                else
                {
                    var newRec = new PayrollRecurringMovement
                    {
                        TenantId = entity.TenantId,
                        CompanyId = entity.CompanyId,
                        EmployeeId = emp.Id,
                        PayrollConceptId = concept.Id,
                        MovementCode = entity.BatchCode,
                        MovementName = entity.BatchName,
                        MovementType = (concept.ConceptType ?? "perception").ToLowerInvariant(),
                        CalculationMode = entity.CalculationMode,
                        Quantity = unitQty,
                        Amount = unitAmount,
                        Percentage = entity.Percentage,
                        EffectiveStartDate = entity.StartDate,
                        EffectiveEndDate = entity.EndDate,
                        ApplyEveryRun = true,
                        IsProrated = false,
                        Status = "active",
                        Notes = entity.Notes ?? string.Empty,
                        IsActive = true,
                        CreatedBy = "global-movement"
                    };
                    db.PayrollRecurringMovements.Add(newRec);
                    recurringId = newRec.Id;
                }
            }

            newLines.Add(new PayrollGlobalMovementLine
            {
                TenantId = entity.TenantId,
                CompanyId = entity.CompanyId,
                PayrollGlobalMovementId = entity.Id,
                EmployeeId = emp.Id,
                PayrollPeriodId = period.Id,
                Quantity = unitQty,
                Amount = unitAmount,
                AppliedAt = DateTime.UtcNow,
                AppliedBy = "global-movement",
                ResultingAdjustmentId = adjustmentId,
                ResultingRecurringMovementId = recurringId,
                Status = "applied",
                Notes = string.Empty,
                IsActive = true,
                CreatedBy = "global-movement"
            });

            appliedAmountTotal += unitAmount;
            applied++;
        }

        if (newLines.Count > 0)
            db.PayrollGlobalMovementLines.AddRange(newLines);

        entity.TimesApplied += applied > 0 ? 1 : 0;
        entity.AccumulatedAmount += appliedAmountTotal;
        entity.AppliedAt = DateTime.UtcNow;
        entity.AppliedBy = "global-movement";
        if (entity.Status == "draft" && applied > 0) entity.Status = "active";
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "global-movement";

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            success = true,
            applied,
            skipped,
            totalAmount = appliedAmountTotal,
            timesApplied = entity.TimesApplied,
            accumulatedAmount = entity.AccumulatedAmount
        });
    }

    private static async Task<IResult> CloseAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollGlobalMovements.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el movimiento global." });

        entity.Status = "closed";
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "global-movement";
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetLinesAsync(Guid id, NanchesoftDbContext db)
    {
        var rows = await db.PayrollGlobalMovementLines.AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.PayrollPeriod)
            .Where(x => x.PayrollGlobalMovementId == id)
            .OrderByDescending(x => x.AppliedAt)
            .Select(x => new PayrollGlobalMovementLineDto
            {
                PayrollGlobalMovementLineId = x.Id,
                PayrollGlobalMovementId = x.PayrollGlobalMovementId,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                EmployeeNumber = x.Employee != null ? x.Employee.EmployeeNumber : string.Empty,
                PayrollPeriodId = x.PayrollPeriodId,
                PayrollPeriodName = x.PayrollPeriod != null ? x.PayrollPeriod.Name : string.Empty,
                Quantity = x.Quantity,
                Amount = x.Amount,
                AppliedAt = x.AppliedAt,
                AppliedBy = x.AppliedBy,
                ResultingAdjustmentId = x.ResultingAdjustmentId,
                ResultingRecurringMovementId = x.ResultingRecurringMovementId,
                Status = x.Status,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }
}

public sealed class PayrollGlobalMovementRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollConceptId { get; set; }
    public string? BatchCode { get; set; }
    public string? BatchName { get; set; }
    public string? MovementType { get; set; }
    public string? CalculationMode { get; set; }
    public decimal Quantity { get; set; } = 1m;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int TimesToApply { get; set; }
    public decimal MaxAmount { get; set; }
    public string? ControlNumber { get; set; }
    public List<Guid> FilterDepartmentIds { get; set; } = [];
    public List<Guid> FilterPositionIds { get; set; } = [];
    public List<Guid> FilterBranchIds { get; set; } = [];
    public List<Guid> FilterEmployerRegistrationIds { get; set; } = [];
    public List<Guid> FilterWorkShiftIds { get; set; } = [];
    public List<Guid> FilterEmployeeIds { get; set; } = [];
    public List<Guid> ExcludeEmployeeIds { get; set; } = [];
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public bool MakeRecurring { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollGlobalMovementDto
{
    public Guid PayrollGlobalMovementId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PayrollConceptId { get; set; }
    public string PayrollConceptCode { get; set; } = string.Empty;
    public string PayrollConceptName { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public string CalculationMode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int TimesToApply { get; set; }
    public int TimesApplied { get; set; }
    public decimal MaxAmount { get; set; }
    public decimal AccumulatedAmount { get; set; }
    public string ControlNumber { get; set; } = string.Empty;
    public string FilterDepartmentIds { get; set; } = string.Empty;
    public string FilterPositionIds { get; set; } = string.Empty;
    public string FilterBranchIds { get; set; } = string.Empty;
    public string FilterEmployerRegistrationIds { get; set; } = string.Empty;
    public string FilterWorkShiftIds { get; set; } = string.Empty;
    public string FilterEmployeeIds { get; set; } = string.Empty;
    public string ExcludeEmployeeIds { get; set; } = string.Empty;
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public bool MakeRecurring { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? AppliedAt { get; set; }
    public string AppliedBy { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class PayrollGlobalMovementPreviewDto
{
    public Guid PayrollGlobalMovementId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public decimal TotalAmount { get; set; }
    public List<PayrollGlobalMovementPreviewEmployee> Employees { get; set; } = [];
}

public sealed class PayrollGlobalMovementPreviewEmployee
{
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public decimal Amount { get; set; }
    public decimal Quantity { get; set; }
}

public sealed class PayrollGlobalMovementApplyRequest
{
    public Guid PayrollPeriodId { get; set; }
}

public sealed class PayrollGlobalMovementLineDto
{
    public Guid PayrollGlobalMovementLineId { get; set; }
    public Guid PayrollGlobalMovementId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public Guid? PayrollPeriodId { get; set; }
    public string PayrollPeriodName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public DateTime AppliedAt { get; set; }
    public string AppliedBy { get; set; } = string.Empty;
    public Guid? ResultingAdjustmentId { get; set; }
    public Guid? ResultingRecurringMovementId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

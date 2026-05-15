using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class HumanResourcesPerformanceEndpoints
{
    public static IEndpointRouteBuilder MapHumanResourcesPerformanceEndpoints(this IEndpointRouteBuilder app)
    {
        var reviews = app.MapGroup("/api/hr/performance-reviews").WithTags("HrPerformanceReviews");
        reviews.MapGet("/", GetPerformanceReviewsAsync);
        reviews.MapPost("/", CreatePerformanceReviewAsync);
        reviews.MapPut("/{id:guid}", UpdatePerformanceReviewAsync);
        reviews.MapDelete("/{id:guid}", DeletePerformanceReviewAsync);

        var competencies = app.MapGroup("/api/hr/competency-assessments").WithTags("HrCompetencyAssessments");
        competencies.MapGet("/", GetCompetencyAssessmentsAsync);
        competencies.MapPost("/", CreateCompetencyAssessmentAsync);
        competencies.MapPut("/{id:guid}", UpdateCompetencyAssessmentAsync);
        competencies.MapDelete("/{id:guid}", DeleteCompetencyAssessmentAsync);

        var succession = app.MapGroup("/api/hr/succession-plans").WithTags("HrSuccessionPlans");
        succession.MapGet("/", GetSuccessionPlansAsync);
        succession.MapPost("/", CreateSuccessionPlanAsync);
        succession.MapPut("/{id:guid}", UpdateSuccessionPlanAsync);
        succession.MapDelete("/{id:guid}", DeleteSuccessionPlanAsync);

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

    private static async Task<IResult> GetPerformanceReviewsAsync(NanchesoftDbContext db)
    {
        var rows = await db.EmployeePerformanceReviews.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.Employee)
            .Include(x => x.Department)
            .Include(x => x.Position)
            .OrderByDescending(x => x.ReviewDate)
            .Select(x => new EmployeePerformanceReviewDto
            {
                EmployeePerformanceReviewId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                EmployeeId = x.EmployeeId,
                DepartmentId = x.DepartmentId,
                PositionId = x.PositionId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                PositionName = x.Position != null ? x.Position.Name : string.Empty,
                ReviewCode = x.ReviewCode,
                ReviewCycle = x.ReviewCycle,
                PeriodStart = x.PeriodStart,
                PeriodEnd = x.PeriodEnd,
                ReviewDate = x.ReviewDate,
                ReviewerName = x.ReviewerName,
                Score = x.Score,
                CalibrationScore = x.CalibrationScore,
                GoalCompletionPercent = x.GoalCompletionPercent,
                PotentialLevel = x.PotentialLevel,
                Status = x.Status,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePerformanceReviewAsync(EmployeePerformanceReviewRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        var branchId = request.BranchId ?? context.BranchId;

        if (!tenantId.HasValue || !companyId.HasValue || !request.EmployeeId.HasValue)
            return Results.BadRequest(new { message = "Empresa y colaborador son obligatorios para la evaluación." });

        var reviewCode = NormalizeUpper(request.ReviewCode);
        if (string.IsNullOrWhiteSpace(reviewCode))
            return Results.BadRequest(new { message = "El código de evaluación es obligatorio." });

        if (await db.EmployeePerformanceReviews.AnyAsync(x => x.CompanyId == companyId.Value && x.EmployeeId == request.EmployeeId.Value && x.ReviewCode == reviewCode))
            return Results.BadRequest(new { message = "Ya existe una evaluación con ese código para el colaborador." });

        var entity = new EmployeePerformanceReview
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = branchId,
            EmployeeId = request.EmployeeId.Value,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            ReviewCode = reviewCode,
            ReviewCycle = NormalizeText(request.ReviewCycle, "annual"),
            PeriodStart = NormalizeUtc(request.PeriodStart, DateTime.UtcNow.AddMonths(-1)),
            PeriodEnd = NormalizeUtc(request.PeriodEnd, DateTime.UtcNow),
            ReviewDate = NormalizeUtc(request.ReviewDate, DateTime.UtcNow),
            ReviewerName = NormalizeText(request.ReviewerName, "capital_humano"),
            Score = request.Score,
            CalibrationScore = request.CalibrationScore,
            GoalCompletionPercent = request.GoalCompletionPercent,
            PotentialLevel = NormalizeLower(request.PotentialLevel, "medium"),
            Status = NormalizeLower(request.Status, "draft"),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.EmployeePerformanceReviews.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePerformanceReviewAsync(Guid id, EmployeePerformanceReviewRequest request, NanchesoftDbContext db)
    {
        var entity = await db.EmployeePerformanceReviews.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la evaluación de desempeño." });

        var reviewCode = NormalizeUpper(request.ReviewCode, entity.ReviewCode);
        if (await db.EmployeePerformanceReviews.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.EmployeeId == (request.EmployeeId ?? entity.EmployeeId) && x.ReviewCode == reviewCode))
            return Results.BadRequest(new { message = "Ya existe otra evaluación con ese código para el colaborador." });

        entity.BranchId = request.BranchId ?? entity.BranchId;
        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.DepartmentId = request.DepartmentId ?? entity.DepartmentId;
        entity.PositionId = request.PositionId ?? entity.PositionId;
        entity.ReviewCode = reviewCode;
        entity.ReviewCycle = NormalizeText(request.ReviewCycle, entity.ReviewCycle);
        entity.PeriodStart = request.PeriodStart is null ? entity.PeriodStart : NormalizeUtc(request.PeriodStart, entity.PeriodStart);
        entity.PeriodEnd = request.PeriodEnd is null ? entity.PeriodEnd : NormalizeUtc(request.PeriodEnd, entity.PeriodEnd);
        entity.ReviewDate = request.ReviewDate is null ? entity.ReviewDate : NormalizeUtc(request.ReviewDate, entity.ReviewDate);
        entity.ReviewerName = NormalizeText(request.ReviewerName, entity.ReviewerName);
        entity.Score = request.Score;
        entity.CalibrationScore = request.CalibrationScore;
        entity.GoalCompletionPercent = request.GoalCompletionPercent;
        entity.PotentialLevel = NormalizeLower(request.PotentialLevel, entity.PotentialLevel);
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePerformanceReviewAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.EmployeePerformanceReviews.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la evaluación de desempeño." });

        db.EmployeePerformanceReviews.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetCompetencyAssessmentsAsync(NanchesoftDbContext db)
    {
        var rows = await db.EmployeeCompetencyAssessments.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.Employee)
            .Include(x => x.Department)
            .Include(x => x.Position)
            .OrderByDescending(x => x.AssessedAt)
            .Select(x => new EmployeeCompetencyAssessmentDto
            {
                EmployeeCompetencyAssessmentId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                EmployeeId = x.EmployeeId,
                DepartmentId = x.DepartmentId,
                PositionId = x.PositionId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                PositionName = x.Position != null ? x.Position.Name : string.Empty,
                AssessmentCode = x.AssessmentCode,
                CompetencyCode = x.CompetencyCode,
                CompetencyName = x.CompetencyName,
                ExpectedLevel = x.ExpectedLevel,
                AchievedLevel = x.AchievedLevel,
                GapLevel = x.GapLevel,
                AssessedAt = x.AssessedAt,
                AssessorName = x.AssessorName,
                DevelopmentAction = x.DevelopmentAction,
                Status = x.Status,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateCompetencyAssessmentAsync(EmployeeCompetencyAssessmentRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        var branchId = request.BranchId ?? context.BranchId;

        if (!tenantId.HasValue || !companyId.HasValue || !request.EmployeeId.HasValue)
            return Results.BadRequest(new { message = "Empresa y colaborador son obligatorios para la evaluación de competencia." });

        var assessmentCode = NormalizeUpper(request.AssessmentCode);
        var competencyCode = NormalizeUpper(request.CompetencyCode);
        if (string.IsNullOrWhiteSpace(assessmentCode) || string.IsNullOrWhiteSpace(competencyCode))
            return Results.BadRequest(new { message = "Código de evaluación y código de competencia son obligatorios." });

        if (await db.EmployeeCompetencyAssessments.AnyAsync(x => x.CompanyId == companyId.Value && x.EmployeeId == request.EmployeeId.Value && x.AssessmentCode == assessmentCode && x.CompetencyCode == competencyCode))
            return Results.BadRequest(new { message = "Ya existe esa evaluación de competencia para el colaborador." });

        var entity = new EmployeeCompetencyAssessment
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = branchId,
            EmployeeId = request.EmployeeId.Value,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            AssessmentCode = assessmentCode,
            CompetencyCode = competencyCode,
            CompetencyName = NormalizeText(request.CompetencyName),
            ExpectedLevel = request.ExpectedLevel,
            AchievedLevel = request.AchievedLevel,
            GapLevel = request.GapLevel,
            AssessedAt = NormalizeUtc(request.AssessedAt, DateTime.UtcNow),
            AssessorName = NormalizeText(request.AssessorName, "capital_humano"),
            DevelopmentAction = NormalizeText(request.DevelopmentAction),
            Status = NormalizeLower(request.Status, "captured"),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.EmployeeCompetencyAssessments.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateCompetencyAssessmentAsync(Guid id, EmployeeCompetencyAssessmentRequest request, NanchesoftDbContext db)
    {
        var entity = await db.EmployeeCompetencyAssessments.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la evaluación de competencia." });

        var assessmentCode = NormalizeUpper(request.AssessmentCode, entity.AssessmentCode);
        var competencyCode = NormalizeUpper(request.CompetencyCode, entity.CompetencyCode);
        if (await db.EmployeeCompetencyAssessments.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.EmployeeId == (request.EmployeeId ?? entity.EmployeeId) && x.AssessmentCode == assessmentCode && x.CompetencyCode == competencyCode))
            return Results.BadRequest(new { message = "Ya existe otra evaluación de competencia con esos códigos." });

        entity.BranchId = request.BranchId ?? entity.BranchId;
        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.DepartmentId = request.DepartmentId ?? entity.DepartmentId;
        entity.PositionId = request.PositionId ?? entity.PositionId;
        entity.AssessmentCode = assessmentCode;
        entity.CompetencyCode = competencyCode;
        entity.CompetencyName = NormalizeText(request.CompetencyName, entity.CompetencyName);
        entity.ExpectedLevel = request.ExpectedLevel;
        entity.AchievedLevel = request.AchievedLevel;
        entity.GapLevel = request.GapLevel;
        entity.AssessedAt = request.AssessedAt is null ? entity.AssessedAt : NormalizeUtc(request.AssessedAt, entity.AssessedAt);
        entity.AssessorName = NormalizeText(request.AssessorName, entity.AssessorName);
        entity.DevelopmentAction = NormalizeText(request.DevelopmentAction, entity.DevelopmentAction);
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteCompetencyAssessmentAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.EmployeeCompetencyAssessments.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la evaluación de competencia." });

        db.EmployeeCompetencyAssessments.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetSuccessionPlansAsync(NanchesoftDbContext db)
    {
        var rows = await db.SuccessionPlanRecords.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.Position)
            .Include(x => x.IncumbentEmployee)
            .Include(x => x.SuccessorEmployee)
            .OrderByDescending(x => x.ReviewDate)
            .Select(x => new SuccessionPlanRecordDto
            {
                SuccessionPlanRecordId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                PositionId = x.PositionId,
                IncumbentEmployeeId = x.IncumbentEmployeeId,
                SuccessorEmployeeId = x.SuccessorEmployeeId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                PositionName = x.Position != null ? x.Position.Name : string.Empty,
                IncumbentEmployeeName = x.IncumbentEmployee != null ? (x.IncumbentEmployee.FirstName + " " + x.IncumbentEmployee.LastName).Trim() : string.Empty,
                SuccessorEmployeeName = x.SuccessorEmployee != null ? (x.SuccessorEmployee.FirstName + " " + x.SuccessorEmployee.LastName).Trim() : string.Empty,
                PlanCode = x.PlanCode,
                Criticality = x.Criticality,
                ReadinessLevel = x.ReadinessLevel,
                RiskOfLoss = x.RiskOfLoss,
                ReviewDate = x.ReviewDate,
                TargetReadyDate = x.TargetReadyDate,
                IsNominationApproved = x.IsNominationApproved,
                DevelopmentPlan = x.DevelopmentPlan,
                Status = x.Status,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateSuccessionPlanAsync(SuccessionPlanRecordRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        var branchId = request.BranchId ?? context.BranchId;

        if (!tenantId.HasValue || !companyId.HasValue || !request.PositionId.HasValue || !request.SuccessorEmployeeId.HasValue)
            return Results.BadRequest(new { message = "Empresa, puesto objetivo y sucesor son obligatorios." });

        var planCode = NormalizeUpper(request.PlanCode);
        if (string.IsNullOrWhiteSpace(planCode))
            return Results.BadRequest(new { message = "El código del plan es obligatorio." });

        if (await db.SuccessionPlanRecords.AnyAsync(x => x.CompanyId == companyId.Value && x.PlanCode == planCode))
            return Results.BadRequest(new { message = "Ya existe un plan con ese código." });

        var entity = new SuccessionPlanRecord
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = branchId,
            PositionId = request.PositionId.Value,
            IncumbentEmployeeId = request.IncumbentEmployeeId,
            SuccessorEmployeeId = request.SuccessorEmployeeId.Value,
            PlanCode = planCode,
            Criticality = NormalizeLower(request.Criticality, "high"),
            ReadinessLevel = NormalizeLower(request.ReadinessLevel, "ready_1_2_years"),
            RiskOfLoss = NormalizeLower(request.RiskOfLoss, "medium"),
            ReviewDate = NormalizeUtc(request.ReviewDate, DateTime.UtcNow),
            TargetReadyDate = request.TargetReadyDate is null ? null : NormalizeUtc(request.TargetReadyDate, DateTime.UtcNow),
            IsNominationApproved = request.IsNominationApproved,
            DevelopmentPlan = NormalizeText(request.DevelopmentPlan),
            Status = NormalizeLower(request.Status, "candidate_pool"),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.SuccessionPlanRecords.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateSuccessionPlanAsync(Guid id, SuccessionPlanRecordRequest request, NanchesoftDbContext db)
    {
        var entity = await db.SuccessionPlanRecords.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el plan de sucesión." });

        var planCode = NormalizeUpper(request.PlanCode, entity.PlanCode);
        if (await db.SuccessionPlanRecords.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.PlanCode == planCode))
            return Results.BadRequest(new { message = "Ya existe otro plan con ese código." });

        entity.BranchId = request.BranchId ?? entity.BranchId;
        entity.PositionId = request.PositionId ?? entity.PositionId;
        entity.IncumbentEmployeeId = request.IncumbentEmployeeId ?? entity.IncumbentEmployeeId;
        entity.SuccessorEmployeeId = request.SuccessorEmployeeId ?? entity.SuccessorEmployeeId;
        entity.PlanCode = planCode;
        entity.Criticality = NormalizeLower(request.Criticality, entity.Criticality);
        entity.ReadinessLevel = NormalizeLower(request.ReadinessLevel, entity.ReadinessLevel);
        entity.RiskOfLoss = NormalizeLower(request.RiskOfLoss, entity.RiskOfLoss);
        entity.ReviewDate = request.ReviewDate is null ? entity.ReviewDate : NormalizeUtc(request.ReviewDate, entity.ReviewDate);
        entity.TargetReadyDate = request.TargetReadyDate is null ? entity.TargetReadyDate : NormalizeUtc(request.TargetReadyDate, entity.TargetReadyDate ?? DateTime.UtcNow);
        entity.IsNominationApproved = request.IsNominationApproved;
        entity.DevelopmentPlan = NormalizeText(request.DevelopmentPlan, entity.DevelopmentPlan);
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteSuccessionPlanAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.SuccessionPlanRecords.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el plan de sucesión." });

        db.SuccessionPlanRecords.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }
}

public sealed class EmployeePerformanceReviewDto
{
    public Guid EmployeePerformanceReviewId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string ReviewCode { get; set; } = string.Empty;
    public string ReviewCycle { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime ReviewDate { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal CalibrationScore { get; set; }
    public decimal GoalCompletionPercent { get; set; }
    public string PotentialLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class EmployeePerformanceReviewRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string? ReviewCode { get; set; }
    public string? ReviewCycle { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public DateTime? ReviewDate { get; set; }
    public string? ReviewerName { get; set; }
    public decimal Score { get; set; }
    public decimal CalibrationScore { get; set; }
    public decimal GoalCompletionPercent { get; set; }
    public string? PotentialLevel { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeCompetencyAssessmentDto
{
    public Guid EmployeeCompetencyAssessmentId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string AssessmentCode { get; set; } = string.Empty;
    public string CompetencyCode { get; set; } = string.Empty;
    public string CompetencyName { get; set; } = string.Empty;
    public int ExpectedLevel { get; set; }
    public int AchievedLevel { get; set; }
    public int GapLevel { get; set; }
    public DateTime AssessedAt { get; set; }
    public string AssessorName { get; set; } = string.Empty;
    public string DevelopmentAction { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class EmployeeCompetencyAssessmentRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string? AssessmentCode { get; set; }
    public string? CompetencyCode { get; set; }
    public string? CompetencyName { get; set; }
    public int ExpectedLevel { get; set; }
    public int AchievedLevel { get; set; }
    public int GapLevel { get; set; }
    public DateTime? AssessedAt { get; set; }
    public string? AssessorName { get; set; }
    public string? DevelopmentAction { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SuccessionPlanRecordDto
{
    public Guid SuccessionPlanRecordId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? IncumbentEmployeeId { get; set; }
    public Guid? SuccessorEmployeeId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string IncumbentEmployeeName { get; set; } = string.Empty;
    public string SuccessorEmployeeName { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public string Criticality { get; set; } = string.Empty;
    public string ReadinessLevel { get; set; } = string.Empty;
    public string RiskOfLoss { get; set; } = string.Empty;
    public DateTime ReviewDate { get; set; }
    public DateTime? TargetReadyDate { get; set; }
    public bool IsNominationApproved { get; set; }
    public string DevelopmentPlan { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class SuccessionPlanRecordRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? IncumbentEmployeeId { get; set; }
    public Guid? SuccessorEmployeeId { get; set; }
    public string? PlanCode { get; set; }
    public string? Criticality { get; set; }
    public string? ReadinessLevel { get; set; }
    public string? RiskOfLoss { get; set; }
    public DateTime? ReviewDate { get; set; }
    public DateTime? TargetReadyDate { get; set; }
    public bool IsNominationApproved { get; set; }
    public string? DevelopmentPlan { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

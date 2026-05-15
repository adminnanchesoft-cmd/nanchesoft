using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class HumanResourcesTalentEndpoints
{
    public static IEndpointRouteBuilder MapHumanResourcesTalentEndpoints(this IEndpointRouteBuilder app)
    {
        var vacancies = app.MapGroup("/api/hr/recruitment-vacancies").WithTags("HrRecruitmentVacancies");
        vacancies.MapGet("/", GetRecruitmentVacanciesAsync);
        vacancies.MapPost("/", CreateRecruitmentVacancyAsync);
        vacancies.MapPut("/{id:guid}", UpdateRecruitmentVacancyAsync);
        vacancies.MapDelete("/{id:guid}", DeleteRecruitmentVacancyAsync);

        var candidates = app.MapGroup("/api/hr/candidate-applications").WithTags("HrCandidateApplications");
        candidates.MapGet("/", GetCandidateApplicationsAsync);
        candidates.MapPost("/", CreateCandidateApplicationAsync);
        candidates.MapPut("/{id:guid}", UpdateCandidateApplicationAsync);
        candidates.MapDelete("/{id:guid}", DeleteCandidateApplicationAsync);

        var onboarding = app.MapGroup("/api/hr/onboarding-checklists").WithTags("HrOnboardingChecklists");
        onboarding.MapGet("/", GetOnboardingChecklistAsync);
        onboarding.MapPost("/", CreateOnboardingChecklistAsync);
        onboarding.MapPut("/{id:guid}", UpdateOnboardingChecklistAsync);
        onboarding.MapDelete("/{id:guid}", DeleteOnboardingChecklistAsync);

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

    private static string NormalizeText(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string NormalizeUpper(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();

    private static string NormalizeLower(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();

    private static DateTime NormalizeUtc(DateTime? value, DateTime fallback)
    {
        var source = value ?? fallback;
        return source.Kind == DateTimeKind.Utc ? source : DateTime.SpecifyKind(source, DateTimeKind.Utc);
    }

    private static async Task<IResult> GetRecruitmentVacanciesAsync(NanchesoftDbContext db)
    {
        var rows = await db.RecruitmentVacancies.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.Department)
            .Include(x => x.Position)
            .OrderBy(x => x.VacancyCode)
            .Select(x => new RecruitmentVacancyDto
            {
                RecruitmentVacancyId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                DepartmentId = x.DepartmentId,
                PositionId = x.PositionId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                PositionName = x.Position != null ? x.Position.Name : string.Empty,
                VacancyCode = x.VacancyCode,
                Title = x.Title,
                EmploymentType = x.EmploymentType,
                OpenDate = x.OpenDate,
                CloseDate = x.CloseDate,
                Headcount = x.Headcount,
                SalaryMin = x.SalaryMin,
                SalaryMax = x.SalaryMax,
                HiringManager = x.HiringManager,
                Priority = x.Priority,
                Status = x.Status,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateRecruitmentVacancyAsync(RecruitmentVacancyRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        var branchId = request.BranchId ?? context.BranchId;
        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "Empresa obligatoria para la vacante." });

        var code = NormalizeUpper(request.VacancyCode);
        var title = NormalizeText(request.Title);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(title))
            return Results.BadRequest(new { message = "Código y título son obligatorios." });

        if (await db.RecruitmentVacancies.AnyAsync(x => x.CompanyId == companyId.Value && x.VacancyCode == code))
            return Results.BadRequest(new { message = "Ya existe una vacante con ese código." });

        var entity = new RecruitmentVacancy
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = branchId,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            VacancyCode = code,
            Title = title,
            EmploymentType = NormalizeLower(request.EmploymentType, "full_time"),
            OpenDate = NormalizeUtc(request.OpenDate, DateTime.UtcNow),
            CloseDate = request.CloseDate is null ? null : NormalizeUtc(request.CloseDate, DateTime.UtcNow),
            Headcount = request.Headcount <= 0 ? 1 : request.Headcount,
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax,
            HiringManager = NormalizeText(request.HiringManager),
            Priority = NormalizeLower(request.Priority, "medium"),
            Status = NormalizeLower(request.Status, "open"),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.RecruitmentVacancies.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateRecruitmentVacancyAsync(Guid id, RecruitmentVacancyRequest request, NanchesoftDbContext db)
    {
        var entity = await db.RecruitmentVacancies.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la vacante." });

        var code = NormalizeUpper(request.VacancyCode, entity.VacancyCode);
        if (await db.RecruitmentVacancies.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.VacancyCode == code))
            return Results.BadRequest(new { message = "Ya existe otra vacante con ese código." });

        entity.BranchId = request.BranchId ?? entity.BranchId;
        entity.DepartmentId = request.DepartmentId ?? entity.DepartmentId;
        entity.PositionId = request.PositionId ?? entity.PositionId;
        entity.VacancyCode = code;
        entity.Title = NormalizeText(request.Title, entity.Title);
        entity.EmploymentType = NormalizeLower(request.EmploymentType, entity.EmploymentType);
        entity.OpenDate = request.OpenDate is null ? entity.OpenDate : NormalizeUtc(request.OpenDate, entity.OpenDate);
        entity.CloseDate = request.CloseDate is null ? entity.CloseDate : NormalizeUtc(request.CloseDate, DateTime.UtcNow);
        entity.Headcount = request.Headcount <= 0 ? entity.Headcount : request.Headcount;
        entity.SalaryMin = request.SalaryMin == default ? entity.SalaryMin : request.SalaryMin;
        entity.SalaryMax = request.SalaryMax == default ? entity.SalaryMax : request.SalaryMax;
        entity.HiringManager = NormalizeText(request.HiringManager, entity.HiringManager);
        entity.Priority = NormalizeLower(request.Priority, entity.Priority);
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteRecruitmentVacancyAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.RecruitmentVacancies.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound();

        db.RecruitmentVacancies.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetCandidateApplicationsAsync(NanchesoftDbContext db)
    {
        var rows = await db.CandidateApplications.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.RecruitmentVacancy)
            .Include(x => x.HiredEmployee)
            .OrderBy(x => x.CandidateCode)
            .Select(x => new CandidateApplicationDto
            {
                CandidateApplicationId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                RecruitmentVacancyId = x.RecruitmentVacancyId,
                HiredEmployeeId = x.HiredEmployeeId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                VacancyName = x.RecruitmentVacancy != null ? x.RecruitmentVacancy.Title : string.Empty,
                EmployeeName = x.HiredEmployee != null ? (x.HiredEmployee.FirstName + " " + x.HiredEmployee.LastName).Trim() : string.Empty,
                CandidateCode = x.CandidateCode,
                FullName = x.FullName,
                Email = x.Email,
                Phone = x.Phone,
                Source = x.Source,
                AppliedAt = x.AppliedAt,
                Stage = x.Stage,
                Score = x.Score,
                OfferAmount = x.OfferAmount,
                CvFileName = x.CvFileName,
                CvFilePath = x.CvFilePath,
                Status = x.Status,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateCandidateApplicationAsync(CandidateApplicationRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        var branchId = request.BranchId ?? context.BranchId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.RecruitmentVacancyId.HasValue)
            return Results.BadRequest(new { message = "Empresa y vacante son obligatorias para el candidato." });

        var code = NormalizeUpper(request.CandidateCode);
        var fullName = NormalizeText(request.FullName);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(fullName))
            return Results.BadRequest(new { message = "Código y nombre del candidato son obligatorios." });

        if (await db.CandidateApplications.AnyAsync(x => x.CompanyId == companyId.Value && x.CandidateCode == code))
            return Results.BadRequest(new { message = "Ya existe un candidato con ese código." });

        var entity = new CandidateApplication
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = branchId,
            RecruitmentVacancyId = request.RecruitmentVacancyId.Value,
            HiredEmployeeId = request.HiredEmployeeId,
            CandidateCode = code,
            FullName = fullName,
            Email = NormalizeText(request.Email),
            Phone = NormalizeText(request.Phone),
            Source = NormalizeLower(request.Source, "direct"),
            AppliedAt = NormalizeUtc(request.AppliedAt, DateTime.UtcNow),
            Stage = NormalizeLower(request.Stage, "screening"),
            Score = request.Score,
            OfferAmount = request.OfferAmount,
            CvFileName = NormalizeText(request.CvFileName),
            CvFilePath = NormalizeText(request.CvFilePath),
            Status = NormalizeLower(request.Status, "applied"),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.CandidateApplications.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateCandidateApplicationAsync(Guid id, CandidateApplicationRequest request, NanchesoftDbContext db)
    {
        var entity = await db.CandidateApplications.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el candidato." });

        var code = NormalizeUpper(request.CandidateCode, entity.CandidateCode);
        if (await db.CandidateApplications.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.CandidateCode == code))
            return Results.BadRequest(new { message = "Ya existe otro candidato con ese código." });

        entity.BranchId = request.BranchId ?? entity.BranchId;
        entity.RecruitmentVacancyId = request.RecruitmentVacancyId ?? entity.RecruitmentVacancyId;
        entity.HiredEmployeeId = request.HiredEmployeeId ?? entity.HiredEmployeeId;
        entity.CandidateCode = code;
        entity.FullName = NormalizeText(request.FullName, entity.FullName);
        entity.Email = NormalizeText(request.Email, entity.Email);
        entity.Phone = NormalizeText(request.Phone, entity.Phone);
        entity.Source = NormalizeLower(request.Source, entity.Source);
        entity.AppliedAt = request.AppliedAt is null ? entity.AppliedAt : NormalizeUtc(request.AppliedAt, entity.AppliedAt);
        entity.Stage = NormalizeLower(request.Stage, entity.Stage);
        entity.Score = request.Score == default ? entity.Score : request.Score;
        entity.OfferAmount = request.OfferAmount == default ? entity.OfferAmount : request.OfferAmount;
        entity.CvFileName = NormalizeText(request.CvFileName, entity.CvFileName);
        entity.CvFilePath = NormalizeText(request.CvFilePath, entity.CvFilePath);
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteCandidateApplicationAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.CandidateApplications.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound();

        db.CandidateApplications.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetOnboardingChecklistAsync(NanchesoftDbContext db)
    {
        var rows = await db.OnboardingChecklistRecords.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.Employee)
            .Include(x => x.CandidateApplication)
            .OrderBy(x => x.ChecklistCode)
            .Select(x => new OnboardingChecklistRecordDto
            {
                OnboardingChecklistRecordId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                EmployeeId = x.EmployeeId,
                CandidateApplicationId = x.CandidateApplicationId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                CandidateName = x.CandidateApplication != null ? x.CandidateApplication.FullName : string.Empty,
                ChecklistCode = x.ChecklistCode,
                ChecklistName = x.ChecklistName,
                PlannedDate = x.PlannedDate,
                CompletedAt = x.CompletedAt,
                ResponsibleArea = x.ResponsibleArea,
                Status = x.Status,
                CompletionPercent = x.CompletionPercent,
                AssetsAssigned = x.AssetsAssigned,
                CredentialsIssued = x.CredentialsIssued,
                InductionCompleted = x.InductionCompleted,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateOnboardingChecklistAsync(OnboardingChecklistRecordRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        var branchId = request.BranchId ?? context.BranchId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.EmployeeId.HasValue)
            return Results.BadRequest(new { message = "Empresa y colaborador son obligatorios para onboarding." });

        var code = NormalizeUpper(request.ChecklistCode);
        var name = NormalizeText(request.ChecklistName);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre del checklist son obligatorios." });

        if (await db.OnboardingChecklistRecords.AnyAsync(x => x.CompanyId == companyId.Value && x.EmployeeId == request.EmployeeId.Value && x.ChecklistCode == code))
            return Results.BadRequest(new { message = "Ya existe ese checklist para el colaborador." });

        var entity = new OnboardingChecklistRecord
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = branchId,
            EmployeeId = request.EmployeeId.Value,
            CandidateApplicationId = request.CandidateApplicationId,
            ChecklistCode = code,
            ChecklistName = name,
            PlannedDate = NormalizeUtc(request.PlannedDate, DateTime.UtcNow),
            CompletedAt = request.CompletedAt is null ? null : NormalizeUtc(request.CompletedAt, DateTime.UtcNow),
            ResponsibleArea = NormalizeLower(request.ResponsibleArea, "human_resources"),
            Status = NormalizeLower(request.Status, "pending"),
            CompletionPercent = request.CompletionPercent,
            AssetsAssigned = request.AssetsAssigned,
            CredentialsIssued = request.CredentialsIssued,
            InductionCompleted = request.InductionCompleted,
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.OnboardingChecklistRecords.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateOnboardingChecklistAsync(Guid id, OnboardingChecklistRecordRequest request, NanchesoftDbContext db)
    {
        var entity = await db.OnboardingChecklistRecords.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el checklist de onboarding." });

        var code = NormalizeUpper(request.ChecklistCode, entity.ChecklistCode);
        if (await db.OnboardingChecklistRecords.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.EmployeeId == entity.EmployeeId && x.ChecklistCode == code))
            return Results.BadRequest(new { message = "Ya existe otro checklist con ese código." });

        entity.BranchId = request.BranchId ?? entity.BranchId;
        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.CandidateApplicationId = request.CandidateApplicationId ?? entity.CandidateApplicationId;
        entity.ChecklistCode = code;
        entity.ChecklistName = NormalizeText(request.ChecklistName, entity.ChecklistName);
        entity.PlannedDate = request.PlannedDate is null ? entity.PlannedDate : NormalizeUtc(request.PlannedDate, entity.PlannedDate);
        entity.CompletedAt = request.CompletedAt is null ? entity.CompletedAt : NormalizeUtc(request.CompletedAt, DateTime.UtcNow);
        entity.ResponsibleArea = NormalizeLower(request.ResponsibleArea, entity.ResponsibleArea);
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.CompletionPercent = request.CompletionPercent == default ? entity.CompletionPercent : request.CompletionPercent;
        entity.AssetsAssigned = request.AssetsAssigned;
        entity.CredentialsIssued = request.CredentialsIssued;
        entity.InductionCompleted = request.InductionCompleted;
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteOnboardingChecklistAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.OnboardingChecklistRecords.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound();

        db.OnboardingChecklistRecords.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }
}

public sealed class RecruitmentVacancyDto
{
    public Guid RecruitmentVacancyId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string VacancyCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public DateTime OpenDate { get; set; }
    public DateTime? CloseDate { get; set; }
    public int Headcount { get; set; }
    public decimal SalaryMin { get; set; }
    public decimal SalaryMax { get; set; }
    public string HiringManager { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class RecruitmentVacancyRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string? VacancyCode { get; set; }
    public string? Title { get; set; }
    public string? EmploymentType { get; set; }
    public DateTime? OpenDate { get; set; }
    public DateTime? CloseDate { get; set; }
    public int Headcount { get; set; }
    public decimal SalaryMin { get; set; }
    public decimal SalaryMax { get; set; }
    public string? HiringManager { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CandidateApplicationDto
{
    public Guid CandidateApplicationId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? RecruitmentVacancyId { get; set; }
    public Guid? HiredEmployeeId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string VacancyName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string CandidateCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
    public string Stage { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal OfferAmount { get; set; }
    public string CvFileName { get; set; } = string.Empty;
    public string CvFilePath { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CandidateApplicationRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? RecruitmentVacancyId { get; set; }
    public Guid? HiredEmployeeId { get; set; }
    public string? CandidateCode { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Source { get; set; }
    public DateTime? AppliedAt { get; set; }
    public string? Stage { get; set; }
    public decimal Score { get; set; }
    public decimal OfferAmount { get; set; }
    public string? CvFileName { get; set; }
    public string? CvFilePath { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class OnboardingChecklistRecordDto
{
    public Guid OnboardingChecklistRecordId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? CandidateApplicationId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public string ChecklistCode { get; set; } = string.Empty;
    public string ChecklistName { get; set; } = string.Empty;
    public DateTime PlannedDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string ResponsibleArea { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal CompletionPercent { get; set; }
    public bool AssetsAssigned { get; set; }
    public bool CredentialsIssued { get; set; }
    public bool InductionCompleted { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class OnboardingChecklistRecordRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? CandidateApplicationId { get; set; }
    public string? ChecklistCode { get; set; }
    public string? ChecklistName { get; set; }
    public DateTime? PlannedDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ResponsibleArea { get; set; }
    public string? Status { get; set; }
    public decimal CompletionPercent { get; set; }
    public bool AssetsAssigned { get; set; }
    public bool CredentialsIssued { get; set; }
    public bool InductionCompleted { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

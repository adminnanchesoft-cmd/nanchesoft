using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class HumanResourcesLifecycleEndpoints
{
    public static IEndpointRouteBuilder MapHumanResourcesLifecycleEndpoints(this IEndpointRouteBuilder app)
    {
        var docs = app.MapGroup("/api/hr/employee-documents").WithTags("HrEmployeeDocuments");
        docs.MapGet("/", GetEmployeeDocumentsAsync);
        docs.MapPost("/", CreateEmployeeDocumentAsync);
        docs.MapPut("/{id:guid}", UpdateEmployeeDocumentAsync);
        docs.MapDelete("/{id:guid}", DeleteEmployeeDocumentAsync);

        var moves = app.MapGroup("/api/hr/employee-movements").WithTags("HrEmployeeMovements");
        moves.MapGet("/", GetEmployeeMovementsAsync);
        moves.MapPost("/", CreateEmployeeMovementAsync);
        moves.MapPut("/{id:guid}", UpdateEmployeeMovementAsync);
        moves.MapDelete("/{id:guid}", DeleteEmployeeMovementAsync);

        var certs = app.MapGroup("/api/hr/employee-certifications").WithTags("HrEmployeeCertifications");
        certs.MapGet("/", GetEmployeeCertificationsAsync);
        certs.MapPost("/", CreateEmployeeCertificationAsync);
        certs.MapPut("/{id:guid}", UpdateEmployeeCertificationAsync);
        certs.MapDelete("/{id:guid}", DeleteEmployeeCertificationAsync);

        return app;
    }

    private static async Task<(Guid? TenantId, Guid? CompanyId, Guid? BranchId)> ResolveDefaultContextAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
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

        var company = await db.Companies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        if (company is null) return (null, null, null);
        var fallbackBranch = await db.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
        return (company.TenantId, company.Id, fallbackBranch);
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

    private static async Task<IResult> GetEmployeeDocumentsAsync(NanchesoftDbContext db)
    {
        var rows = await db.EmployeeDocumentRecords.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.Employee)
            .OrderBy(x => x.DocumentCode)
            .Select(x => new EmployeeDocumentRecordDto
            {
                EmployeeDocumentRecordId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                EmployeeId = x.EmployeeId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                DocumentCode = x.DocumentCode,
                DocumentName = x.DocumentName,
                DocumentType = x.DocumentType,
                DocumentNumber = x.DocumentNumber,
                IssueDate = x.IssueDate,
                ExpirationDate = x.ExpirationDate,
                UploadedAt = x.UploadedAt,
                VerifiedAt = x.VerifiedAt,
                FileName = x.FileName,
                FilePath = x.FilePath,
                Status = x.Status,
                IsRequired = x.IsRequired,
                IsVerified = x.IsVerified,
                VerifiedBy = x.VerifiedBy,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateEmployeeDocumentAsync(HttpContext httpContext, EmployeeDocumentRecordRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        var branchId = request.BranchId ?? context.BranchId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.EmployeeId.HasValue)
            return Results.BadRequest(new { message = "Empresa y colaborador son obligatorios para el documento." });

        var code = NormalizeUpper(request.DocumentCode);
        var name = NormalizeText(request.DocumentName);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre del documento son obligatorios." });

        if (await db.EmployeeDocumentRecords.AnyAsync(x => x.CompanyId == companyId.Value && x.EmployeeId == request.EmployeeId.Value && x.DocumentCode == code))
            return Results.BadRequest(new { message = "Ya existe ese documento para el colaborador." });

        var entity = new EmployeeDocumentRecord
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = branchId,
            EmployeeId = request.EmployeeId.Value,
            DocumentCode = code,
            DocumentName = name,
            DocumentType = NormalizeLower(request.DocumentType, "general"),
            DocumentNumber = NormalizeText(request.DocumentNumber),
            IssueDate = request.IssueDate is null ? null : NormalizeUtc(request.IssueDate, DateTime.UtcNow),
            ExpirationDate = request.ExpirationDate is null ? null : NormalizeUtc(request.ExpirationDate, DateTime.UtcNow),
            UploadedAt = request.UploadedAt is null ? DateTime.UtcNow : NormalizeUtc(request.UploadedAt, DateTime.UtcNow),
            VerifiedAt = request.VerifiedAt is null ? null : NormalizeUtc(request.VerifiedAt, DateTime.UtcNow),
            FileName = NormalizeText(request.FileName),
            FilePath = NormalizeText(request.FilePath),
            Status = NormalizeLower(request.Status, "captured"),
            IsRequired = request.IsRequired,
            IsVerified = request.IsVerified,
            VerifiedBy = NormalizeText(request.VerifiedBy),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.EmployeeDocumentRecords.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateEmployeeDocumentAsync(Guid id, EmployeeDocumentRecordRequest request, NanchesoftDbContext db)
    {
        var entity = await db.EmployeeDocumentRecords.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el documento del colaborador." });

        var code = NormalizeUpper(request.DocumentCode, entity.DocumentCode);
        if (await db.EmployeeDocumentRecords.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.EmployeeId == entity.EmployeeId && x.DocumentCode == code))
            return Results.BadRequest(new { message = "Ya existe otro documento con ese código para el colaborador." });

        entity.BranchId = request.BranchId ?? entity.BranchId;
        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.DocumentCode = code;
        entity.DocumentName = NormalizeText(request.DocumentName, entity.DocumentName);
        entity.DocumentType = NormalizeLower(request.DocumentType, entity.DocumentType);
        entity.DocumentNumber = NormalizeText(request.DocumentNumber, entity.DocumentNumber);
        entity.IssueDate = request.IssueDate is null ? entity.IssueDate : NormalizeUtc(request.IssueDate, DateTime.UtcNow);
        entity.ExpirationDate = request.ExpirationDate is null ? entity.ExpirationDate : NormalizeUtc(request.ExpirationDate, DateTime.UtcNow);
        entity.UploadedAt = request.UploadedAt is null ? entity.UploadedAt : NormalizeUtc(request.UploadedAt, DateTime.UtcNow);
        entity.VerifiedAt = request.VerifiedAt is null ? entity.VerifiedAt : NormalizeUtc(request.VerifiedAt, DateTime.UtcNow);
        entity.FileName = NormalizeText(request.FileName, entity.FileName);
        entity.FilePath = NormalizeText(request.FilePath, entity.FilePath);
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.IsRequired = request.IsRequired;
        entity.IsVerified = request.IsVerified;
        entity.VerifiedBy = NormalizeText(request.VerifiedBy, entity.VerifiedBy);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteEmployeeDocumentAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.EmployeeDocumentRecords.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el documento del colaborador." });

        db.EmployeeDocumentRecords.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetEmployeeMovementsAsync(NanchesoftDbContext db)
    {
        var rows = await db.EmployeeLaborMovements.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.Employee)
            .Include(x => x.Department)
            .Include(x => x.Position)
            .OrderByDescending(x => x.EffectiveDate)
            .Select(x => new EmployeeLaborMovementDto
            {
                EmployeeLaborMovementId = x.Id,
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
                MovementCode = x.MovementCode,
                MovementType = x.MovementType,
                EffectiveDate = x.EffectiveDate,
                AppliedAt = x.AppliedAt,
                PreviousValue = x.PreviousValue,
                NewValue = x.NewValue,
                SalaryBefore = x.SalaryBefore,
                SalaryAfter = x.SalaryAfter,
                AuthorizedBy = x.AuthorizedBy,
                Status = x.Status,
                ImpactsPayroll = x.ImpactsPayroll,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateEmployeeMovementAsync(HttpContext httpContext, EmployeeLaborMovementRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        var branchId = request.BranchId ?? context.BranchId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.EmployeeId.HasValue)
            return Results.BadRequest(new { message = "Empresa y colaborador son obligatorios para el movimiento laboral." });

        var code = NormalizeUpper(request.MovementCode);
        var type = NormalizeLower(request.MovementType);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(type))
            return Results.BadRequest(new { message = "Código y tipo del movimiento son obligatorios." });

        if (await db.EmployeeLaborMovements.AnyAsync(x => x.CompanyId == companyId.Value && x.MovementCode == code))
            return Results.BadRequest(new { message = "Ya existe un movimiento laboral con ese código." });

        var entity = new EmployeeLaborMovement
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = branchId,
            EmployeeId = request.EmployeeId.Value,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            MovementCode = code,
            MovementType = type,
            EffectiveDate = NormalizeUtc(request.EffectiveDate, DateTime.UtcNow),
            AppliedAt = request.AppliedAt is null ? null : NormalizeUtc(request.AppliedAt, DateTime.UtcNow),
            PreviousValue = NormalizeText(request.PreviousValue),
            NewValue = NormalizeText(request.NewValue),
            SalaryBefore = request.SalaryBefore,
            SalaryAfter = request.SalaryAfter,
            AuthorizedBy = NormalizeText(request.AuthorizedBy),
            Status = NormalizeLower(request.Status, "approved"),
            ImpactsPayroll = request.ImpactsPayroll,
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.EmployeeLaborMovements.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateEmployeeMovementAsync(Guid id, EmployeeLaborMovementRequest request, NanchesoftDbContext db)
    {
        var entity = await db.EmployeeLaborMovements.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el movimiento laboral." });

        var code = NormalizeUpper(request.MovementCode, entity.MovementCode);
        if (await db.EmployeeLaborMovements.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.MovementCode == code))
            return Results.BadRequest(new { message = "Ya existe otro movimiento laboral con ese código." });

        entity.BranchId = request.BranchId ?? entity.BranchId;
        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.DepartmentId = request.DepartmentId ?? entity.DepartmentId;
        entity.PositionId = request.PositionId ?? entity.PositionId;
        entity.MovementCode = code;
        entity.MovementType = NormalizeLower(request.MovementType, entity.MovementType);
        entity.EffectiveDate = request.EffectiveDate is null ? entity.EffectiveDate : NormalizeUtc(request.EffectiveDate, DateTime.UtcNow);
        entity.AppliedAt = request.AppliedAt is null ? entity.AppliedAt : NormalizeUtc(request.AppliedAt, DateTime.UtcNow);
        entity.PreviousValue = NormalizeText(request.PreviousValue, entity.PreviousValue);
        entity.NewValue = NormalizeText(request.NewValue, entity.NewValue);
        entity.SalaryBefore = request.SalaryBefore == 0m ? entity.SalaryBefore : request.SalaryBefore;
        entity.SalaryAfter = request.SalaryAfter == 0m ? entity.SalaryAfter : request.SalaryAfter;
        entity.AuthorizedBy = NormalizeText(request.AuthorizedBy, entity.AuthorizedBy);
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.ImpactsPayroll = request.ImpactsPayroll;
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteEmployeeMovementAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.EmployeeLaborMovements.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el movimiento laboral." });

        db.EmployeeLaborMovements.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetEmployeeCertificationsAsync(NanchesoftDbContext db)
    {
        var rows = await db.EmployeeCertificationRecords.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.Employee)
            .OrderBy(x => x.CertificationCode)
            .Select(x => new EmployeeCertificationRecordDto
            {
                EmployeeCertificationRecordId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                EmployeeId = x.EmployeeId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                CertificationCode = x.CertificationCode,
                CertificationName = x.CertificationName,
                Category = x.Category,
                IssuedBy = x.IssuedBy,
                IssueDate = x.IssueDate,
                ExpirationDate = x.ExpirationDate,
                Score = x.Score,
                Status = x.Status,
                IsMandatory = x.IsMandatory,
                RenewalRequired = x.RenewalRequired,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateEmployeeCertificationAsync(HttpContext httpContext, EmployeeCertificationRecordRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        var branchId = request.BranchId ?? context.BranchId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.EmployeeId.HasValue)
            return Results.BadRequest(new { message = "Empresa y colaborador son obligatorios para la certificación." });

        var code = NormalizeUpper(request.CertificationCode);
        var name = NormalizeText(request.CertificationName);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre de la certificación son obligatorios." });

        if (await db.EmployeeCertificationRecords.AnyAsync(x => x.CompanyId == companyId.Value && x.EmployeeId == request.EmployeeId.Value && x.CertificationCode == code))
            return Results.BadRequest(new { message = "Ya existe esa certificación para el colaborador." });

        var entity = new EmployeeCertificationRecord
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = branchId,
            EmployeeId = request.EmployeeId.Value,
            CertificationCode = code,
            CertificationName = name,
            Category = NormalizeLower(request.Category, "training"),
            IssuedBy = NormalizeText(request.IssuedBy),
            IssueDate = NormalizeUtc(request.IssueDate, DateTime.UtcNow),
            ExpirationDate = request.ExpirationDate is null ? null : NormalizeUtc(request.ExpirationDate, DateTime.UtcNow),
            Score = request.Score,
            Status = NormalizeLower(request.Status, "active"),
            IsMandatory = request.IsMandatory,
            RenewalRequired = request.RenewalRequired,
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.EmployeeCertificationRecords.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateEmployeeCertificationAsync(Guid id, EmployeeCertificationRecordRequest request, NanchesoftDbContext db)
    {
        var entity = await db.EmployeeCertificationRecords.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la certificación." });

        var code = NormalizeUpper(request.CertificationCode, entity.CertificationCode);
        if (await db.EmployeeCertificationRecords.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.EmployeeId == entity.EmployeeId && x.CertificationCode == code))
            return Results.BadRequest(new { message = "Ya existe otra certificación con ese código para el colaborador." });

        entity.BranchId = request.BranchId ?? entity.BranchId;
        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.CertificationCode = code;
        entity.CertificationName = NormalizeText(request.CertificationName, entity.CertificationName);
        entity.Category = NormalizeLower(request.Category, entity.Category);
        entity.IssuedBy = NormalizeText(request.IssuedBy, entity.IssuedBy);
        entity.IssueDate = request.IssueDate is null ? entity.IssueDate : NormalizeUtc(request.IssueDate, DateTime.UtcNow);
        entity.ExpirationDate = request.ExpirationDate is null ? entity.ExpirationDate : NormalizeUtc(request.ExpirationDate, DateTime.UtcNow);
        entity.Score = request.Score == 0m ? entity.Score : request.Score;
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.IsMandatory = request.IsMandatory;
        entity.RenewalRequired = request.RenewalRequired;
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteEmployeeCertificationAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.EmployeeCertificationRecords.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la certificación." });

        db.EmployeeCertificationRecords.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }
}

public sealed class EmployeeDocumentRecordDto
{
    public Guid EmployeeDocumentRecordId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DocumentCode { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? UploadedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsVerified { get; set; }
    public string VerifiedBy { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class EmployeeDocumentRecordRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? DocumentCode { get; set; }
    public string? DocumentName { get; set; }
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? UploadedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public string? Status { get; set; }
    public bool IsRequired { get; set; }
    public bool IsVerified { get; set; }
    public string? VerifiedBy { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeLaborMovementDto
{
    public Guid EmployeeLaborMovementId { get; set; }
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
    public string MovementCode { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public DateTime? EffectiveDate { get; set; }
    public DateTime? AppliedAt { get; set; }
    public string PreviousValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public decimal SalaryBefore { get; set; }
    public decimal SalaryAfter { get; set; }
    public string AuthorizedBy { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool ImpactsPayroll { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class EmployeeLaborMovementRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string? MovementCode { get; set; }
    public string? MovementType { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? AppliedAt { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public decimal SalaryBefore { get; set; }
    public decimal SalaryAfter { get; set; }
    public string? AuthorizedBy { get; set; }
    public string? Status { get; set; }
    public bool ImpactsPayroll { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeCertificationRecordDto
{
    public Guid EmployeeCertificationRecordId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string CertificationCode { get; set; } = string.Empty;
    public string CertificationName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string IssuedBy { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public decimal Score { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public bool RenewalRequired { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class EmployeeCertificationRecordRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? CertificationCode { get; set; }
    public string? CertificationName { get; set; }
    public string? Category { get; set; }
    public string? IssuedBy { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public decimal Score { get; set; }
    public string? Status { get; set; }
    public bool IsMandatory { get; set; }
    public bool RenewalRequired { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class HumanResourcesCatalogsEndpoints
{
    public static IEndpointRouteBuilder MapHumanResourcesCatalogsEndpoints(this IEndpointRouteBuilder app)
    {
        var banks = app.MapGroup("/api/hr/banks").WithTags("HrBanks");
        banks.MapGet("/", GetBanksAsync);
        banks.MapPost("/", CreateBankAsync);
        banks.MapPut("/{id:guid}", UpdateBankAsync);
        banks.MapDelete("/{id:guid}", DeleteBankAsync);

        var reasons = app.MapGroup("/api/hr/termination-reasons").WithTags("HrTerminationReasons");
        reasons.MapGet("/", GetTerminationReasonsAsync);
        reasons.MapPost("/", CreateTerminationReasonAsync);
        reasons.MapPut("/{id:guid}", UpdateTerminationReasonAsync);
        reasons.MapDelete("/{id:guid}", DeleteTerminationReasonAsync);

        var registrations = app.MapGroup("/api/hr/employer-registrations").WithTags("HrEmployerRegistrations");
        registrations.MapGet("/", GetEmployerRegistrationsAsync);
        registrations.MapPost("/", CreateEmployerRegistrationAsync);
        registrations.MapPut("/{id:guid}", UpdateEmployerRegistrationAsync);
        registrations.MapDelete("/{id:guid}", DeleteEmployerRegistrationAsync);

        return app;
    }

    // ──────── BANCOS ────────

    private static async Task<IResult> GetBanksAsync(NanchesoftDbContext db)
    {
        var rows = await db.HrBanks.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new HrBankDto
            {
                BankId = x.Id, TenantId = x.TenantId, Code = x.Code,
                ShortName = x.ShortName, Name = x.Name, SatCode = x.SatCode,
                IsActive = x.IsActive
            }).ToListAsync();
        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateBankAsync(HrBankRequest req, NanchesoftDbContext db)
    {
        if (await ResolveContextAsync(db) is not { } ctx)
            return Results.BadRequest(new { message = "No existe contexto de tenant." });

        var code = req.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(req.Name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (await db.HrBanks.AnyAsync(x => x.TenantId == ctx.TenantId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un banco con ese código." });

        var entity = new HrBank
        {
            TenantId = ctx.TenantId,
            Code = code,
            ShortName = req.ShortName?.Trim() ?? code,
            Name = req.Name.Trim(),
            SatCode = req.SatCode?.Trim(),
            IsActive = req.IsActive,
            CreatedBy = "web-api"
        };
        db.HrBanks.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateBankAsync(Guid id, HrBankRequest req, NanchesoftDbContext db)
    {
        var entity = await db.HrBanks.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return Results.NotFound(new { message = "Banco no encontrado." });

        var code = req.Code?.Trim().ToUpperInvariant() ?? entity.Code;
        if (await db.HrBanks.AnyAsync(x => x.Id != id && x.TenantId == entity.TenantId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro banco con ese código." });

        entity.Code = code;
        entity.ShortName = req.ShortName?.Trim() ?? entity.ShortName;
        entity.Name = req.Name?.Trim() ?? entity.Name;
        entity.SatCode = req.SatCode?.Trim();
        entity.IsActive = req.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteBankAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.HrBanks.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return Results.NotFound();
        db.HrBanks.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    // ──────── MOTIVOS DE BAJA ────────

    private static async Task<IResult> GetTerminationReasonsAsync(NanchesoftDbContext db)
    {
        var rows = await db.HrTerminationReasons.AsNoTracking()
            .Include(x => x.Company)
            .OrderBy(x => x.Code)
            .Select(x => new HrTerminationReasonDto
            {
                TerminationReasonId = x.Id, TenantId = x.TenantId,
                CompanyId = x.CompanyId, CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                Code = x.Code, Name = x.Name, Description = x.Description, IsActive = x.IsActive
            }).ToListAsync();
        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateTerminationReasonAsync(HrTerminationReasonRequest req, NanchesoftDbContext db)
    {
        if (await ResolveContextAsync(db) is not { } ctx || !ctx.CompanyId.HasValue)
            return Results.BadRequest(new { message = "Sin contexto de empresa." });

        var code = req.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(req.Name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        var companyId = req.CompanyId ?? ctx.CompanyId.Value;
        if (await db.HrTerminationReasons.AnyAsync(x => x.CompanyId == companyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un motivo con ese código." });

        var entity = new HrTerminationReason
        {
            TenantId = ctx.TenantId,
            CompanyId = companyId,
            Code = code,
            Name = req.Name.Trim(),
            Description = req.Description?.Trim(),
            IsActive = req.IsActive,
            CreatedBy = "web-api"
        };
        db.HrTerminationReasons.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateTerminationReasonAsync(Guid id, HrTerminationReasonRequest req, NanchesoftDbContext db)
    {
        var entity = await db.HrTerminationReasons.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return Results.NotFound(new { message = "Motivo no encontrado." });

        var code = req.Code?.Trim().ToUpperInvariant() ?? entity.Code;
        if (await db.HrTerminationReasons.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro motivo con ese código." });

        entity.Code = code;
        entity.Name = req.Name?.Trim() ?? entity.Name;
        entity.Description = req.Description?.Trim();
        entity.IsActive = req.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteTerminationReasonAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.HrTerminationReasons.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return Results.NotFound();
        db.HrTerminationReasons.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    // ──────── REGISTROS PATRONALES ────────

    private static async Task<IResult> GetEmployerRegistrationsAsync(NanchesoftDbContext db)
    {
        var rows = await db.HrEmployerRegistrations.AsNoTracking()
            .Include(x => x.Company)
            .OrderBy(x => x.Code)
            .Select(x => new HrEmployerRegistrationDto
            {
                EmployerRegistrationId = x.Id, TenantId = x.TenantId,
                CompanyId = x.CompanyId, CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                Code = x.Code, Name = x.Name, RegistrationNumber = x.RegistrationNumber,
                RiskClass = x.RiskClass, State = x.State, Notes = x.Notes, IsActive = x.IsActive
            }).ToListAsync();
        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateEmployerRegistrationAsync(HrEmployerRegistrationRequest req, NanchesoftDbContext db)
    {
        if (await ResolveContextAsync(db) is not { } ctx || !ctx.CompanyId.HasValue)
            return Results.BadRequest(new { message = "Sin contexto de empresa." });

        var code = req.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(req.Name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        var companyId = req.CompanyId ?? ctx.CompanyId.Value;
        if (await db.HrEmployerRegistrations.AnyAsync(x => x.CompanyId == companyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un registro con ese código." });

        var entity = new HrEmployerRegistration
        {
            TenantId = ctx.TenantId,
            CompanyId = companyId,
            Code = code,
            Name = req.Name.Trim(),
            RegistrationNumber = req.RegistrationNumber?.Trim() ?? string.Empty,
            RiskClass = req.RiskClass?.Trim(),
            State = req.State?.Trim(),
            Notes = req.Notes?.Trim(),
            IsActive = req.IsActive,
            CreatedBy = "web-api"
        };
        db.HrEmployerRegistrations.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateEmployerRegistrationAsync(Guid id, HrEmployerRegistrationRequest req, NanchesoftDbContext db)
    {
        var entity = await db.HrEmployerRegistrations.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return Results.NotFound(new { message = "Registro patronal no encontrado." });

        var code = req.Code?.Trim().ToUpperInvariant() ?? entity.Code;
        if (await db.HrEmployerRegistrations.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro registro con ese código." });

        entity.Code = code;
        entity.Name = req.Name?.Trim() ?? entity.Name;
        entity.RegistrationNumber = req.RegistrationNumber?.Trim() ?? entity.RegistrationNumber;
        entity.RiskClass = req.RiskClass?.Trim();
        entity.State = req.State?.Trim();
        entity.Notes = req.Notes?.Trim();
        entity.IsActive = req.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteEmployerRegistrationAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.HrEmployerRegistrations.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return Results.NotFound();
        db.HrEmployerRegistrations.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    // ──────── helpers ────────

    private static async Task<(Guid TenantId, Guid? CompanyId)?> ResolveContextAsync(NanchesoftDbContext db)
    {
        var tenant = await db.Tenants.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (tenant is null) return null;
        var company = await db.Companies.Where(x => x.TenantId == tenant.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        return (tenant.Id, company?.Id);
    }
}

// ──────── DTOs y Requests ────────

public class HrBankRequest
{
    public string? Code { get; set; }
    public string? ShortName { get; set; }
    public string? Name { get; set; }
    public string? SatCode { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class HrBankDto : HrBankRequest
{
    public Guid BankId { get; set; }
    public Guid? TenantId { get; set; }
}

public class HrTerminationReasonRequest
{
    public Guid? CompanyId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class HrTerminationReasonDto : HrTerminationReasonRequest
{
    public Guid TerminationReasonId { get; set; }
    public Guid? TenantId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}

public class HrEmployerRegistrationRequest
{
    public Guid? CompanyId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? RiskClass { get; set; }
    public string? State { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class HrEmployerRegistrationDto : HrEmployerRegistrationRequest
{
    public Guid EmployerRegistrationId { get; set; }
    public Guid? TenantId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}

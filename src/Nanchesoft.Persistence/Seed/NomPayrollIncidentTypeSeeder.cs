using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class NomPayrollIncidentTypeSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        await EnsurePermissionsAsync(dbContext);

        var companies = await dbContext.Companies.AsNoTracking().Where(x => x.IsActive).ToListAsync();
        foreach (var company in companies)
        {
            await EnsureAsync(dbContext, company.TenantId, company.Id, "DESCUENTO_DANOS", "Descuento por danos", "DEDUCCION", "RESTA", "DESCUENTO_DANOS", "#DC2626", "triangle-alert", 10, true, false, false, true, false, true, true);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "REPOSICION_TIJERAS", "Reposicion de tijeras", "DEDUCCION", "RESTA", "DESCUENTO_DANOS", "#DC2626", "scissors", 20, true, false, false, true, false, true, true);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "HORAS_EXTRA", "Horas extra", "PERCEPCION", "SUMA", "HORAS_EXTRA", "#16A34A", "clock-plus", 30, false, true, false, true, true, true, true);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "BONO_PRODUCTIVIDAD", "Bono de productividad", "PERCEPCION", "SUMA", "BONO", "#16A34A", "badge-dollar-sign", 40, false, true, false, true, false, true, true);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "FALTA_INJUSTIFICADA", "Falta injustificada", "DEDUCCION", "RESTA", "FALTA", "#DC2626", "calendar-x", 50, true, false, false, false, true, false, true);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "RETARDO", "Retardo", "DEDUCCION", "RESTA", "RETARDO", "#DC2626", "clock-alert", 60, true, false, false, false, true, false, true);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "INFORMATIVA_ASISTENCIA", "Informativa asistencia", "INFORMATIVA", "NO_AFECTA", "OTRO", "#2563EB", "info", 70, false, false, true, false, false, false, false);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "OTRO", "Otro", "INFORMATIVA", "NO_AFECTA", "OTRO", "#2563EB", "circle-ellipsis", 999, false, false, true, false, false, false, false);

            // Claves estándar CONTPAQi / operativas
            await EnsureAsync(dbContext, company.TenantId, company.Id, "TRAB",  "Días trabajados",                  "INFORMATIVA", "NO_AFECTA", "OTRO",      "#16A34A", "check-circle",   100, false, false, true,  true,  false, false, true);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "FINJ",  "Falta injustificada",              "DEDUCCION",   "RESTA",     "FALTA",      "#DC2626", "calendar-x",     110, true,  false, false, false, true,  false, true);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "CAST",  "Días de castigo",                  "DEDUCCION",   "RESTA",     "FALTA",      "#B91C1C", "ban",            120, true,  false, false, false, true,  false, true);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "ENFG",  "Enfermedad general",               "INFORMATIVA", "NO_AFECTA", "OTRO",       "#7C3AED", "heart-pulse",    130, false, false, true,  true,  false, false, false);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "ATRB",  "Accidente de trabajo",             "INFORMATIVA", "NO_AFECTA", "OTRO",       "#7C3AED", "shield-alert",   140, false, false, true,  true,  false, false, false);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "ATRY",  "Accidente de trayecto",            "INFORMATIVA", "NO_AFECTA", "OTRO",       "#7C3AED", "car-crash",      150, false, false, true,  true,  false, false, false);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "INC",   "Incapacidad (pagada empresa)",     "DEDUCCION",   "RESTA",     "FALTA",      "#D97706", "file-heart",     160, true,  false, false, false, true,  false, false);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "MAT",   "Incapacidad maternidad",           "INFORMATIVA", "NO_AFECTA", "OTRO",       "#EC4899", "baby",           170, false, false, true,  true,  false, false, false);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "PCS",   "Permiso con goce de sueldo",       "INFORMATIVA", "NO_AFECTA", "OTRO",       "#0EA5E9", "calendar-check", 180, false, false, true,  true,  false, false, true);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "PSS",   "Permiso sin goce de sueldo",       "DEDUCCION",   "RESTA",     "FALTA",      "#64748B", "calendar-minus", 190, true,  false, false, false, true,  false, false);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "HE1",   "Hora extra sencilla",              "PERCEPCION",  "SUMA",      "HORAS_EXTRA", "#16A34A", "clock-plus",    200, false, true,  false, false, true,  true,  true);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "HE2",   "Hora extra doble",                 "PERCEPCION",  "SUMA",      "HORAS_EXTRA", "#15803D", "clock-plus",    210, false, true,  false, false, true,  true,  true);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "HE3",   "Hora extra triple",                "PERCEPCION",  "SUMA",      "HORAS_EXTRA", "#14532D", "clock-plus",    220, false, true,  false, false, true,  true,  true);
            await EnsureAsync(dbContext, company.TenantId, company.Id, "RET",   "Retardo",                          "DEDUCCION",   "RESTA",     "RETARDO",    "#F59E0B", "clock-alert",   230, true,  false, false, false, true,  false, true);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsurePermissionsAsync(NanchesoftDbContext dbContext)
    {
        var permissions = new[]
        {
            new Permission { Id = Guid.Parse("72000000-0000-0000-0000-000000000101"), Module = "payroll", Resource = "incidenttype", Action = "view", Code = "payroll.incidenttype.view", Name = "Ver tipos de incidencia", CreatedBy = "seed" },
            new Permission { Id = Guid.Parse("72000000-0000-0000-0000-000000000102"), Module = "payroll", Resource = "incidenttype", Action = "create", Code = "payroll.incidenttype.create", Name = "Crear tipos de incidencia", CreatedBy = "seed" },
            new Permission { Id = Guid.Parse("72000000-0000-0000-0000-000000000103"), Module = "payroll", Resource = "incidenttype", Action = "edit", Code = "payroll.incidenttype.edit", Name = "Editar tipos de incidencia", CreatedBy = "seed" },
            new Permission { Id = Guid.Parse("72000000-0000-0000-0000-000000000104"), Module = "payroll", Resource = "incidenttype", Action = "delete", Code = "payroll.incidenttype.delete", Name = "Eliminar tipos de incidencia", CreatedBy = "seed" }
        };

        foreach (var permission in permissions)
        {
            if (!await dbContext.Permissions.AnyAsync(x => x.Code == permission.Code))
                dbContext.Permissions.Add(permission);
        }

        await dbContext.SaveChangesAsync();

        var roles = await dbContext.Roles
            .Where(x => x.Code == "PLATFORM_OWNER" || x.Code == "TENANT_ADMIN")
            .ToListAsync();
        var savedPermissions = await dbContext.Permissions
            .Where(x => x.Module == "payroll" && x.Resource == "incidenttype")
            .ToListAsync();

        foreach (var role in roles)
        {
            foreach (var permission in savedPermissions)
            {
                if (await dbContext.RolePermissions.AnyAsync(x => x.RoleId == role.Id && x.PermissionId == permission.Id))
                    continue;

                dbContext.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                    AssignedBy = "seed",
                    CreatedBy = "seed"
                });
            }
        }
    }

    private static async Task EnsureAsync(
        NanchesoftDbContext dbContext,
        Guid tenantId,
        Guid companyId,
        string code,
        string name,
        string category,
        string affectType,
        string payrollConceptType,
        string color,
        string icon,
        int sortOrder,
        bool isDiscount,
        bool isPerception,
        bool isInformative,
        bool requiresAmount,
        bool requiresQuantity,
        bool requiresAuthorization,
        bool appliesToPayroll)
    {
        if (await dbContext.NomPayrollIncidentTypes.AnyAsync(x => x.TenantId == tenantId && x.CompanyId == companyId && x.Code == code))
            return;

        dbContext.NomPayrollIncidentTypes.Add(new NomPayrollIncidentType
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = code,
            Name = name,
            Description = string.Empty,
            IncidentCategory = category,
            AffectType = affectType,
            PayrollConceptType = payrollConceptType,
            Color = color,
            Icon = icon,
            SortOrder = sortOrder,
            IsDiscount = isDiscount,
            IsPerception = isPerception,
            IsInformative = isInformative,
            RequiresAmount = requiresAmount,
            RequiresQuantity = requiresQuantity,
            RequiresAuthorization = requiresAuthorization,
            AppliesToPayroll = appliesToPayroll,
            IsSystem = true,
            IsActive = true,
            CreatedBy = "seed"
        });
    }
}

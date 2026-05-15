using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Domain.Enums;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class CommercialTenantsSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        const string seedUser = "seed-commercial";
        var plan = await dbContext.Plans.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (plan is null)
        {
            return;
        }

        var demoTenant = await dbContext.Tenants.FirstOrDefaultAsync(x => x.Code == "NANCHESOFT_DEMO");
        if (demoTenant is not null && demoTenant.Name != "DEMO")
        {
            demoTenant.Name = "DEMO";
            demoTenant.LegalName = string.IsNullOrWhiteSpace(demoTenant.LegalName) ? "Tenant Demo" : demoTenant.LegalName;
            demoTenant.Status = TenantStatus.Active;
            demoTenant.IsActive = true;
            demoTenant.UpdatedAt = DateTime.UtcNow;
            demoTenant.UpdatedBy = seedUser;
            await dbContext.SaveChangesAsync();
        }

        await EnsureTenantAsync(
            dbContext,
            plan.Id,
            code: "SILVASOFT",
            name: "SILVASOFT",
            legalName: "Silvasoft Servicios de Desarrollo de Software",
            companyCode: "SIL001",
            companyName: "Silvasoft",
            companyLegalName: "Silvasoft Servicios de Desarrollo de Software",
            adminUsername: "silvasoft.admin",
            adminEmail: "silvasoft.admin@nanchesoft.local",
            sampleCustomerCode: "RETRYVER",
            sampleCustomerName: "Retryver",
            servicePrefix: "R",
            currentFolioNumber: 11,
            seedSampleNote: false,
            seedUser: seedUser);

        await EnsureTenantAsync(
            dbContext,
            plan.Id,
            code: "WORKERTERRA",
            name: "WORKERTERRA",
            legalName: "Workerterra Soluciones Operativas",
            companyCode: "WRK001",
            companyName: "Workerterra",
            companyLegalName: "Workerterra Soluciones Operativas",
            adminUsername: "workerterra.admin",
            adminEmail: "workerterra.admin@nanchesoft.local",
            sampleCustomerCode: null,
            sampleCustomerName: null,
            servicePrefix: "W",
            currentFolioNumber: 0,
            seedSampleNote: false,
            seedUser: seedUser);
    }

    private static async Task EnsureTenantAsync(
        NanchesoftDbContext dbContext,
        Guid planId,
        string code,
        string name,
        string legalName,
        string companyCode,
        string companyName,
        string companyLegalName,
        string adminUsername,
        string adminEmail,
        string? sampleCustomerCode,
        string? sampleCustomerName,
        string servicePrefix,
        int currentFolioNumber,
        bool seedSampleNote,
        string seedUser)
    {
        var now = DateTime.UtcNow;
        adminUsername = adminUsername.Trim();
        adminEmail = adminEmail.Trim().ToLowerInvariant();

        var tenant = await dbContext.Tenants.FirstOrDefaultAsync(x => x.Code == code);
        if (tenant is null)
        {
            tenant = new Tenant
            {
                Code = code,
                Name = name,
                LegalName = legalName,
                PlanId = planId,
                Status = TenantStatus.Active,
                CreatedBy = seedUser
            };
            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync();
        }
        else
        {
            tenant.Name = name;
            tenant.LegalName = legalName;
            tenant.Status = TenantStatus.Active;
            tenant.IsActive = true;
            tenant.PlanId = planId;
            tenant.UpdatedAt = now;
            tenant.UpdatedBy = seedUser;
            await dbContext.SaveChangesAsync();
        }

        var company = await dbContext.Companies.FirstOrDefaultAsync(x => x.TenantId == tenant.Id && x.Code == companyCode);
        if (company is null)
        {
            company = new Company
            {
                TenantId = tenant.Id,
                Code = companyCode,
                Name = companyName,
                LegalName = companyLegalName,
                TaxId = "XAXX010101000",
                Timezone = "America/Mexico_City",
                CreatedBy = seedUser
            };
            dbContext.Companies.Add(company);
            await dbContext.SaveChangesAsync();
        }

        var branch = await dbContext.Branches.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "MAT");
        if (branch is null)
        {
            branch = new Branch
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                Code = "MAT",
                Name = "Matriz",
                Address = "León, Guanajuato",
                Phone = "4770000000",
                Email = $"matriz.{code.ToLowerInvariant()}@nanchesoft.local",
                CreatedBy = seedUser
            };
            dbContext.Branches.Add(branch);
            await dbContext.SaveChangesAsync();
        }

        await EnsureRoleAsync(dbContext, tenant.Id, "TENANT_ADMIN", "Tenant Admin", "Administrador principal del tenant.", true, seedUser);
        await EnsureRoleAsync(dbContext, tenant.Id, "CAPTURISTA", "Capturista", "Captura y edición operativa.", false, seedUser);
        await EnsureRoleAsync(dbContext, tenant.Id, "TESORERIA", "Tesorería", "Cobranza, depósitos y control de ingresos.", false, seedUser);
        await EnsureRoleAsync(dbContext, tenant.Id, "CONSULTA", "Consulta", "Acceso solo lectura.", false, seedUser);

        var adminUser = await dbContext.Users.FirstOrDefaultAsync(x =>
            x.TenantId == tenant.Id &&
            (x.Username == adminUsername || (x.Email != null && x.Email.ToLower() == adminEmail)));

        if (adminUser is null)
        {
            adminUser = new User
            {
                TenantId = tenant.Id,
                Username = adminUsername,
                Email = adminEmail,
                FirstName = companyName,
                LastName = "Admin",
                PasswordHash = "TEMP_HASH",
                Status = UserStatus.Active,
                MustChangePassword = true,
                CreatedBy = seedUser
            };
            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangesAsync();
        }
        else
        {
            var changed = false;

            if (!string.Equals(adminUser.Username, adminUsername, StringComparison.Ordinal))
            {
                adminUser.Username = adminUsername;
                changed = true;
            }

            if (!string.Equals(adminUser.Email, adminEmail, StringComparison.OrdinalIgnoreCase))
            {
                adminUser.Email = adminEmail;
                changed = true;
            }

            if (adminUser.Status != UserStatus.Active)
            {
                adminUser.Status = UserStatus.Active;
                changed = true;
            }

            if (!adminUser.MustChangePassword)
            {
                adminUser.MustChangePassword = true;
                changed = true;
            }

            if (changed)
            {
                adminUser.UpdatedAt = now;
                adminUser.UpdatedBy = seedUser;
                await dbContext.SaveChangesAsync();
            }
        }

        var adminRole = await dbContext.Roles.FirstAsync(x => x.TenantId == tenant.Id && x.Code == "TENANT_ADMIN");
        if (!await dbContext.UserRoles.AnyAsync(x => x.UserId == adminUser.Id && x.RoleId == adminRole.Id))
        {
            dbContext.UserRoles.Add(new UserRole
            {
                TenantId = tenant.Id,
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                AssignedAt = now,
                AssignedBy = seedUser,
                CreatedBy = seedUser
            });
            await dbContext.SaveChangesAsync();
        }

        Customer? customer = null;
        if (!string.IsNullOrWhiteSpace(sampleCustomerCode) && !string.IsNullOrWhiteSpace(sampleCustomerName))
        {
            customer = await dbContext.Customers.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == sampleCustomerCode);
            if (customer is null)
            {
                customer = new Customer
                {
                    TenantId = tenant.Id,
                    CompanyId = company.Id,
                    Code = sampleCustomerCode,
                    Name = sampleCustomerName,
                    LegalName = sampleCustomerName,
                    TaxId = "XAXX010101000",
                    Email = "",
                    Phone = "",
                    PaymentTermDays = 1,
                    CreditLimit = 0,
                    CreatedBy = seedUser
                };
                dbContext.Customers.Add(customer);
                await dbContext.SaveChangesAsync();
            }
        }

        var series = await dbContext.DocumentSeries.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.DocumentType == "SERVICE_NOTE");
        if (series is null)
        {
            series = new DocumentSeries
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                DocumentType = "SERVICE_NOTE",
                Code = "SERVICE_NOTE",
                Name = "Nota de servicio",
                Prefix = servicePrefix,
                CurrentNumber = currentFolioNumber,
                NumberLength = 3,
                IsDefault = true,
                CreatedBy = seedUser
            };
            dbContext.DocumentSeries.Add(series);
            await dbContext.SaveChangesAsync();
        }
        else if (series.CurrentNumber < currentFolioNumber)
        {
            series.CurrentNumber = currentFolioNumber;
            series.UpdatedAt = now;
            series.UpdatedBy = seedUser;
            await dbContext.SaveChangesAsync();
        }

        var folio = await dbContext.DocumentFolios.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.DocumentType == "SERVICE_NOTE" && x.SeriesId == series.Id);
        if (folio is null)
        {
            folio = new DocumentFolio
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                DocumentType = "SERVICE_NOTE",
                SeriesId = series.Id,
                CurrentNumber = currentFolioNumber,
                CreatedBy = seedUser
            };
            dbContext.DocumentFolios.Add(folio);
            await dbContext.SaveChangesAsync();
        }
        else if (folio.CurrentNumber < currentFolioNumber)
        {
            folio.CurrentNumber = currentFolioNumber;
            folio.UpdatedAt = now;
            folio.UpdatedBy = seedUser;
            await dbContext.SaveChangesAsync();
        }

        if (seedSampleNote && customer is not null)
        {
            const string sampleFolio = "R011";
            if (!await dbContext.ServiceNotes.AnyAsync(x => x.CompanyId == company.Id && x.Folio == sampleFolio))
            {
                dbContext.ServiceNotes.Add(new ServiceNote
                {
                    TenantId = tenant.Id,
                    CompanyId = company.Id,
                    CustomerId = customer.Id,
                    CustomerNameSnapshot = customer.Name,
                    Folio = sampleFolio,
                    NoteDate = new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc),
                    Description = "DESARROLLO DE SISTEMAS DE 8:00 AM A 5:30",
                    HoursWorked = 9m,
                    HourlyRate = 350m,
                    Subtotal = 3150m,
                    Total = 3150m,
                    PaymentStatus = "PENDIENTE",
                    PaymentMethod = "POR_DEFINIR",
                    Notes = "Nota base migrada desde formato PDF del cliente.",
                    CreatedBy = seedUser
                });
                await dbContext.SaveChangesAsync();
            }
        }
    }

    private static async Task EnsureRoleAsync(NanchesoftDbContext dbContext, Guid tenantId, string code, string name, string description, bool isSystemRole, string seedUser)
    {
        if (await dbContext.Roles.AnyAsync(x => x.TenantId == tenantId && x.Code == code))
        {
            return;
        }

        dbContext.Roles.Add(new Role
        {
            TenantId = tenantId,
            Code = code,
            Name = name,
            Description = description,
            IsSystemRole = isSystemRole,
            CreatedBy = seedUser
        });

        await dbContext.SaveChangesAsync();
    }
}
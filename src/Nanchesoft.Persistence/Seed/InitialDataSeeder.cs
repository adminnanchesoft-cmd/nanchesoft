using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Domain.Enums;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class InitialDataSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        const string seedUser = "seed";
        var now = DateTime.UtcNow;

        var ids = new
        {
            Plan = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Company = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            Branch = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            Warehouse = Guid.Parse("40000000-0000-0000-0000-000000000001"),
            PlatformOwnerRole = Guid.Parse("50000000-0000-0000-0000-000000000001"),
            TenantAdminRole = Guid.Parse("50000000-0000-0000-0000-000000000002"),
            CompanyAdminRole = Guid.Parse("50000000-0000-0000-0000-000000000003"),
            AdminUser = Guid.Parse("60000000-0000-0000-0000-000000000001"),
            OpsUser = Guid.Parse("60000000-0000-0000-0000-000000000002"),
            AdminSession = Guid.Parse("A0000000-0000-0000-0000-000000000001"),
            AccessLog1 = Guid.Parse("B0000000-0000-0000-0000-000000000001"),
            AccessLog2 = Guid.Parse("B0000000-0000-0000-0000-000000000002"),
            AccessLog3 = Guid.Parse("B0000000-0000-0000-0000-000000000003"),
            Mxn = Guid.Parse("C0000000-0000-0000-0000-000000000001"),
            Usd = Guid.Parse("C0000000-0000-0000-0000-000000000002"),
            Iva16 = Guid.Parse("C0000000-0000-0000-0000-000000000003"),
            Piece = Guid.Parse("C0000000-0000-0000-0000-000000000004"),
            Service = Guid.Parse("C0000000-0000-0000-0000-000000000005"),
            Bbva = Guid.Parse("C0000000-0000-0000-0000-000000000006"),
            Banamex = Guid.Parse("C0000000-0000-0000-0000-000000000007"),
            Mexico = Guid.Parse("C0000000-0000-0000-0000-000000000008"),
            Usa = Guid.Parse("C0000000-0000-0000-0000-000000000009"),
            Guanajuato = Guid.Parse("C0000000-0000-0000-0000-000000000010"),
            Jalisco = Guid.Parse("C0000000-0000-0000-0000-000000000011"),
            Leon = Guid.Parse("C0000000-0000-0000-0000-000000000012"),
            Guadalajara = Guid.Parse("C0000000-0000-0000-0000-000000000013"),
            PurchaseOrderSeries = Guid.Parse("D0000000-0000-0000-0000-000000000001"),
            SalesInvoiceSeries = Guid.Parse("D0000000-0000-0000-0000-000000000002"),
            PurchaseOrderFolio = Guid.Parse("D0000000-0000-0000-0000-000000000003"),
            SalesInvoiceFolio = Guid.Parse("D0000000-0000-0000-0000-000000000004"),
            CompanySetting = Guid.Parse("D0000000-0000-0000-0000-000000000005")
        };

        var permissionSeeds = new[]
        {
            new PermissionSeed("dashboard.view", "dashboard", "dashboard", "view", "Ver dashboard", Guid.Parse("70000000-0000-0000-0000-000000000001")),
            new PermissionSeed("organization.company.view", "organization", "company", "view", "Ver empresas", Guid.Parse("70000000-0000-0000-0000-000000000002")),
            new PermissionSeed("organization.company.create", "organization", "company", "create", "Crear empresas", Guid.Parse("70000000-0000-0000-0000-000000000003")),
            new PermissionSeed("organization.company.edit", "organization", "company", "edit", "Editar empresas", Guid.Parse("70000000-0000-0000-0000-000000000004")),
            new PermissionSeed("organization.branch.view", "organization", "branch", "view", "Ver sucursales", Guid.Parse("70000000-0000-0000-0000-000000000005")),
            new PermissionSeed("organization.branch.create", "organization", "branch", "create", "Crear sucursales", Guid.Parse("70000000-0000-0000-0000-000000000006")),
            new PermissionSeed("organization.branch.edit", "organization", "branch", "edit", "Editar sucursales", Guid.Parse("70000000-0000-0000-0000-000000000007")),
            new PermissionSeed("organization.warehouse.view", "organization", "warehouse", "view", "Ver almacenes", Guid.Parse("70000000-0000-0000-0000-000000000008")),
            new PermissionSeed("organization.warehouse.create", "organization", "warehouse", "create", "Crear almacenes", Guid.Parse("70000000-0000-0000-0000-000000000009")),
            new PermissionSeed("organization.warehouse.edit", "organization", "warehouse", "edit", "Editar almacenes", Guid.Parse("70000000-0000-0000-0000-000000000010")),
            new PermissionSeed("security.user.view", "security", "user", "view", "Ver usuarios", Guid.Parse("70000000-0000-0000-0000-000000000011")),
            new PermissionSeed("security.user.create", "security", "user", "create", "Crear usuarios", Guid.Parse("70000000-0000-0000-0000-000000000012")),
            new PermissionSeed("security.user.edit", "security", "user", "edit", "Editar usuarios", Guid.Parse("70000000-0000-0000-0000-000000000013")),
            new PermissionSeed("security.role.view", "security", "role", "view", "Ver roles", Guid.Parse("70000000-0000-0000-0000-000000000014")),
            new PermissionSeed("security.role.create", "security", "role", "create", "Crear roles", Guid.Parse("70000000-0000-0000-0000-000000000015")),
            new PermissionSeed("security.role.edit", "security", "role", "edit", "Editar roles", Guid.Parse("70000000-0000-0000-0000-000000000016")),
            new PermissionSeed("security.permission.view", "security", "permission", "view", "Ver permisos", Guid.Parse("70000000-0000-0000-0000-000000000017")),
            new PermissionSeed("administration.session.view", "administration", "session", "view", "Ver sesiones", Guid.Parse("70000000-0000-0000-0000-000000000018")),
            new PermissionSeed("administration.accesslog.view", "administration", "accesslog", "view", "Ver bitácora de acceso", Guid.Parse("70000000-0000-0000-0000-000000000019")),
            new PermissionSeed("catalog.currency.view", "catalog", "currency", "view", "Ver monedas", Guid.Parse("70000000-0000-0000-0000-000000000020")),
            new PermissionSeed("catalog.currency.create", "catalog", "currency", "create", "Crear monedas", Guid.Parse("70000000-0000-0000-0000-000000000021")),
            new PermissionSeed("catalog.currency.edit", "catalog", "currency", "edit", "Editar monedas", Guid.Parse("70000000-0000-0000-0000-000000000022")),
            new PermissionSeed("catalog.exchangerate.view", "catalog", "exchangerate", "view", "Ver tipos de cambio", Guid.Parse("70000000-0000-0000-0000-000000000023")),
            new PermissionSeed("catalog.exchangerate.create", "catalog", "exchangerate", "create", "Crear tipos de cambio", Guid.Parse("70000000-0000-0000-0000-000000000024")),
            new PermissionSeed("catalog.exchangerate.edit", "catalog", "exchangerate", "edit", "Editar tipos de cambio", Guid.Parse("70000000-0000-0000-0000-000000000025")),
            new PermissionSeed("catalog.tax.view", "catalog", "tax", "view", "Ver impuestos", Guid.Parse("70000000-0000-0000-0000-000000000026")),
            new PermissionSeed("catalog.tax.create", "catalog", "tax", "create", "Crear impuestos", Guid.Parse("70000000-0000-0000-0000-000000000027")),
            new PermissionSeed("catalog.tax.edit", "catalog", "tax", "edit", "Editar impuestos", Guid.Parse("70000000-0000-0000-0000-000000000028")),
            new PermissionSeed("catalog.unit.view", "catalog", "unit", "view", "Ver unidades", Guid.Parse("70000000-0000-0000-0000-000000000029")),
            new PermissionSeed("catalog.unit.create", "catalog", "unit", "create", "Crear unidades", Guid.Parse("70000000-0000-0000-0000-000000000030")),
            new PermissionSeed("catalog.unit.edit", "catalog", "unit", "edit", "Editar unidades", Guid.Parse("70000000-0000-0000-0000-000000000031")),
            new PermissionSeed("catalog.bank.view", "catalog", "bank", "view", "Ver bancos", Guid.Parse("70000000-0000-0000-0000-000000000032")),
            new PermissionSeed("catalog.bank.create", "catalog", "bank", "create", "Crear bancos", Guid.Parse("70000000-0000-0000-0000-000000000033")),
            new PermissionSeed("catalog.bank.edit", "catalog", "bank", "edit", "Editar bancos", Guid.Parse("70000000-0000-0000-0000-000000000034")),
            new PermissionSeed("catalog.country.view", "catalog", "country", "view", "Ver países", Guid.Parse("70000000-0000-0000-0000-000000000035")),
            new PermissionSeed("catalog.country.create", "catalog", "country", "create", "Crear países", Guid.Parse("70000000-0000-0000-0000-000000000036")),
            new PermissionSeed("catalog.country.edit", "catalog", "country", "edit", "Editar países", Guid.Parse("70000000-0000-0000-0000-000000000037")),
            new PermissionSeed("catalog.state.view", "catalog", "state", "view", "Ver estados", Guid.Parse("70000000-0000-0000-0000-000000000038")),
            new PermissionSeed("catalog.state.create", "catalog", "state", "create", "Crear estados", Guid.Parse("70000000-0000-0000-0000-000000000039")),
            new PermissionSeed("catalog.state.edit", "catalog", "state", "edit", "Editar estados", Guid.Parse("70000000-0000-0000-0000-000000000040")),
            new PermissionSeed("catalog.city.view", "catalog", "city", "view", "Ver ciudades", Guid.Parse("70000000-0000-0000-0000-000000000041")),
            new PermissionSeed("catalog.city.create", "catalog", "city", "create", "Crear ciudades", Guid.Parse("70000000-0000-0000-0000-000000000042")),
            new PermissionSeed("catalog.city.edit", "catalog", "city", "edit", "Editar ciudades", Guid.Parse("70000000-0000-0000-0000-000000000043")),
            new PermissionSeed("administration.documentseries.view", "administration", "documentseries", "view", "Ver series documentales", Guid.Parse("70000000-0000-0000-0000-000000000044")),
            new PermissionSeed("administration.documentseries.create", "administration", "documentseries", "create", "Crear series documentales", Guid.Parse("70000000-0000-0000-0000-000000000045")),
            new PermissionSeed("administration.documentseries.edit", "administration", "documentseries", "edit", "Editar series documentales", Guid.Parse("70000000-0000-0000-0000-000000000046")),
            new PermissionSeed("administration.documentfolio.view", "administration", "documentfolio", "view", "Ver folios documentales", Guid.Parse("70000000-0000-0000-0000-000000000047")),
            new PermissionSeed("administration.documentfolio.edit", "administration", "documentfolio", "edit", "Editar folios documentales", Guid.Parse("70000000-0000-0000-0000-000000000048")),
            new PermissionSeed("administration.companysettings.view", "administration", "companysettings", "view", "Ver configuración de empresa", Guid.Parse("70000000-0000-0000-0000-000000000049")),
            new PermissionSeed("administration.companysettings.edit", "administration", "companysettings", "edit", "Editar configuración de empresa", Guid.Parse("70000000-0000-0000-0000-000000000050"))
        };

        var navigationSeeds = new[]
        {
            new NavigationSeed("home", "dashboard", "Inicio", "/dashboard", 1, null, true, Guid.Parse("90000000-0000-0000-0000-000000000001")),
            new NavigationSeed("organization", "organization", "Organización", "/organization/companies", 2, "organization.company.view", true, Guid.Parse("90000000-0000-0000-0000-000000000002")),
            new NavigationSeed("security", "security", "Seguridad", "/security/users", 3, "security.user.view", true, Guid.Parse("90000000-0000-0000-0000-000000000003")),
            new NavigationSeed("catalogs", "catalog", "Catálogos", "/catalogs/currencies", 4, "catalog.currency.view", true, Guid.Parse("90000000-0000-0000-0000-000000000004")),
            new NavigationSeed("administration", "administration", "Administración", "/administration/sessions", 5, "administration.session.view", true, Guid.Parse("90000000-0000-0000-0000-000000000005"))
        };

        var plan = await dbContext.Plans.FirstOrDefaultAsync(x => x.Code == "ENTERPRISE");
        if (plan is null)
        {
            plan = new Plan
            {
                Id = ids.Plan,
                Code = "ENTERPRISE",
                Name = "Enterprise",
                MaxUsers = 999,
                MaxCompanies = 999,
                MaxBranches = 999,
                PriceMonthly = 9999m,
                CreatedBy = seedUser
            };
            dbContext.Plans.Add(plan);
            await dbContext.SaveChangesAsync();
        }

        var tenant = await dbContext.Tenants.FirstOrDefaultAsync(x => x.Code == "NANCHESOFT_DEMO");
        if (tenant is null)
        {
            tenant = new Tenant
            {
                Id = ids.Tenant,
                Code = "NANCHESOFT_DEMO",
                Name = "NANCHESOFT_DEMO",
                LegalName = "Nanchesoft Demo Tenant",
                Status = TenantStatus.Active,
                PlanId = plan.Id,
                CreatedBy = seedUser
            };
            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync();
        }
        else if (tenant.PlanId != plan.Id)
        {
            tenant.PlanId = plan.Id;
            tenant.UpdatedAt = now;
            tenant.UpdatedBy = seedUser;
            await dbContext.SaveChangesAsync();
        }

        var company = await dbContext.Companies.FirstOrDefaultAsync(x => x.TenantId == tenant.Id && x.Code == "NAN001");
        if (company is null)
        {
            company = new Company
            {
                Id = ids.Company,
                TenantId = tenant.Id,
                Code = "NAN001",
                Name = "Nanchesoft Demo Company",
                LegalName = "Nanchesoft Demo Company SA de CV",
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
                Id = ids.Branch,
                TenantId = tenant.Id,
                CompanyId = company.Id,
                Code = "MAT",
                Name = "Matriz",
                Address = "León, Guanajuato",
                Phone = "4770000000",
                Email = "matriz@nanchesoft.com",
                CreatedBy = seedUser
            };
            dbContext.Branches.Add(branch);
            await dbContext.SaveChangesAsync();
        }

        var warehouse = await dbContext.Warehouses.FirstOrDefaultAsync(x => x.BranchId == branch.Id && x.Code == "GENERAL");
        if (warehouse is null)
        {
            warehouse = new Warehouse
            {
                Id = ids.Warehouse,
                TenantId = tenant.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                Code = "GENERAL",
                Name = "General",
                CreatedBy = seedUser
            };
            dbContext.Warehouses.Add(warehouse);
            await dbContext.SaveChangesAsync();
        }

        var rolesByCode = await dbContext.Roles.Where(x => x.TenantId == tenant.Id).ToDictionaryAsync(x => x.Code, x => x);

        var roleSeeds = new[]
        {
            new Role
            {
                Id = ids.PlatformOwnerRole,
                TenantId = tenant.Id,
                Code = "PLATFORM_OWNER",
                Name = "Platform Owner",
                Description = "Control total del tenant demo.",
                IsSystemRole = true,
                CreatedBy = seedUser
            },
            new Role
            {
                Id = ids.TenantAdminRole,
                TenantId = tenant.Id,
                Code = "TENANT_ADMIN",
                Name = "Tenant Admin",
                Description = "Administrador operativo del tenant demo.",
                IsSystemRole = true,
                CreatedBy = seedUser
            },
            new Role
            {
                Id = ids.CompanyAdminRole,
                TenantId = tenant.Id,
                Code = "COMPANY_ADMIN",
                Name = "Company Admin",
                Description = "Administrador base de empresa.",
                IsSystemRole = false,
                CreatedBy = seedUser
            }
        };

        foreach (var roleSeed in roleSeeds)
        {
            if (rolesByCode.ContainsKey(roleSeed.Code))
                continue;

            dbContext.Roles.Add(roleSeed);
            rolesByCode[roleSeed.Code] = roleSeed;
        }

        var permissionsByCode = await dbContext.Permissions.ToDictionaryAsync(x => x.Code, x => x);
        foreach (var permissionSeed in permissionSeeds)
        {
            if (permissionsByCode.ContainsKey(permissionSeed.Code))
                continue;

            var permission = new Permission
            {
                Id = permissionSeed.Id,
                Code = permissionSeed.Code,
                Module = permissionSeed.Module,
                Resource = permissionSeed.Resource,
                Action = permissionSeed.Action,
                Name = permissionSeed.Name,
                CreatedBy = seedUser
            };

            dbContext.Permissions.Add(permission);
            permissionsByCode[permission.Code] = permission;
        }

        await dbContext.SaveChangesAsync();

        rolesByCode = await dbContext.Roles.Where(x => x.TenantId == tenant.Id).ToDictionaryAsync(x => x.Code, x => x);
        permissionsByCode = await dbContext.Permissions.ToDictionaryAsync(x => x.Code, x => x);

        var usersByUsername = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.Id)
            .ToDictionaryAsync(x => x.Username, x => x);

        var userSeeds = new[]
        {
            new User
            {
                Id = ids.AdminUser,
                TenantId = tenant.Id,
                Username = "admin",
                Email = "admin@nanchesoft.com",
                FirstName = "Administrador",
                LastName = "General",
                PasswordHash = "TEMP_HASH",
                Status = UserStatus.Active,
                MustChangePassword = true,
                CreatedBy = seedUser
            },
            new User
            {
                Id = ids.OpsUser,
                TenantId = tenant.Id,
                Username = "operaciones",
                Email = "operaciones@nanchesoft.com",
                FirstName = "María",
                LastName = "Operaciones",
                PasswordHash = "TEMP_HASH",
                Status = UserStatus.Active,
                MustChangePassword = false,
                LastLoginAt = now.AddHours(-4),
                CreatedBy = seedUser
            }
        };

        foreach (var userSeed in userSeeds)
        {
            if (usersByUsername.ContainsKey(userSeed.Username))
                continue;

            var trackedUser = dbContext.Users.Local.FirstOrDefault(x => x.Id == userSeed.Id || x.Username == userSeed.Username);
            if (trackedUser is not null)
            {
                usersByUsername[trackedUser.Username] = trackedUser;
                continue;
            }

            var existingUser = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userSeed.Id || (x.TenantId == tenant.Id && x.Username == userSeed.Username));

            if (existingUser is not null)
            {
                usersByUsername[existingUser.Username] = existingUser;
                continue;
            }

            dbContext.Users.Add(userSeed);
            usersByUsername[userSeed.Username] = userSeed;
        }

        await dbContext.SaveChangesAsync();

        usersByUsername = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.Id)
            .ToDictionaryAsync(x => x.Username, x => x);

        if (!usersByUsername.TryGetValue("admin", out var adminUser))
        {
            adminUser = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == ids.AdminUser || (x.TenantId == tenant.Id && x.Username == "admin"));

            if (adminUser is null)
            {
                adminUser = new User
                {
                    Id = ids.AdminUser,
                    TenantId = tenant.Id,
                    Username = "admin",
                    Email = "admin@nanchesoft.com",
                    FirstName = "Administrador",
                    LastName = "General",
                    PasswordHash = "TEMP_HASH",
                    Status = UserStatus.Active,
                    MustChangePassword = true,
                    CreatedBy = seedUser
                };
                dbContext.Users.Add(adminUser);
            }

            usersByUsername[adminUser.Username] = adminUser;
        }

        if (!usersByUsername.TryGetValue("operaciones", out var opsUser))
        {
            opsUser = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == ids.OpsUser || (x.TenantId == tenant.Id && x.Username == "operaciones"));

            if (opsUser is null)
            {
                opsUser = new User
                {
                    Id = ids.OpsUser,
                    TenantId = tenant.Id,
                    Username = "operaciones",
                    Email = "operaciones@nanchesoft.com",
                    FirstName = "María",
                    LastName = "Operaciones",
                    PasswordHash = "TEMP_HASH",
                    Status = UserStatus.Active,
                    MustChangePassword = false,
                    LastLoginAt = now.AddHours(-4),
                    CreatedBy = seedUser
                };
                dbContext.Users.Add(opsUser);
            }

            usersByUsername[opsUser.Username] = opsUser;
        }

        await dbContext.SaveChangesAsync();
        var platformOwnerRole = rolesByCode["PLATFORM_OWNER"];
        var tenantAdminRole = rolesByCode["TENANT_ADMIN"];

        if (!await dbContext.UserRoles.AnyAsync(x => x.UserId == adminUser.Id && x.RoleId == platformOwnerRole.Id))
        {
            dbContext.UserRoles.Add(new UserRole
            {
                Id = Guid.Parse("80000000-0000-0000-0000-000000000001"),
                TenantId = tenant.Id,
                UserId = adminUser.Id,
                RoleId = platformOwnerRole.Id,
                AssignedBy = seedUser,
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.UserRoles.AnyAsync(x => x.UserId == opsUser.Id && x.RoleId == tenantAdminRole.Id))
        {
            dbContext.UserRoles.Add(new UserRole
            {
                Id = Guid.Parse("80000000-0000-0000-0000-000000000002"),
                TenantId = tenant.Id,
                UserId = opsUser.Id,
                RoleId = tenantAdminRole.Id,
                AssignedBy = seedUser,
                CreatedBy = seedUser
            });
        }

        foreach (var permission in permissionsByCode.Values)
        {
            if (await dbContext.RolePermissions.AnyAsync(x => x.RoleId == platformOwnerRole.Id && x.PermissionId == permission.Id))
                continue;

            dbContext.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = platformOwnerRole.Id,
                PermissionId = permission.Id,
                AssignedBy = seedUser,
                CreatedBy = seedUser
            });
        }

        foreach (var permission in permissionsByCode.Values.Where(x => x.Code != "security.permission.view"))
        {
            if (await dbContext.RolePermissions.AnyAsync(x => x.RoleId == tenantAdminRole.Id && x.PermissionId == permission.Id))
                continue;

            dbContext.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = tenantAdminRole.Id,
                PermissionId = permission.Id,
                AssignedBy = seedUser,
                CreatedBy = seedUser
            });
        }

        var navigationByCode = await dbContext.NavigationItems.ToDictionaryAsync(x => x.Code, x => x);
        foreach (var navigationSeed in navigationSeeds)
        {
            if (navigationByCode.ContainsKey(navigationSeed.Code))
                continue;

            dbContext.NavigationItems.Add(new NavigationItem
            {
                Id = navigationSeed.Id,
                Code = navigationSeed.Code,
                Module = navigationSeed.Module,
                Title = navigationSeed.Title,
                Route = navigationSeed.Route,
                SortOrder = navigationSeed.SortOrder,
                RequiredPermission = navigationSeed.RequiredPermission,
                IsVisible = navigationSeed.IsVisible,
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.Currencies.AnyAsync(x => x.TenantId == tenant.Id && x.Code == "MXN"))
        {
            dbContext.Currencies.Add(new Currency
            {
                Id = ids.Mxn,
                TenantId = tenant.Id,
                Code = "MXN",
                Name = "Peso mexicano",
                Symbol = "$",
                IsDefault = true,
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.Currencies.AnyAsync(x => x.TenantId == tenant.Id && x.Code == "USD"))
        {
            dbContext.Currencies.Add(new Currency
            {
                Id = ids.Usd,
                TenantId = tenant.Id,
                Code = "USD",
                Name = "Dólar americano",
                Symbol = "US$",
                IsDefault = false,
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.Taxes.AnyAsync(x => x.TenantId == tenant.Id && x.Code == "IVA16"))
        {
            dbContext.Taxes.Add(new Tax
            {
                Id = ids.Iva16,
                TenantId = tenant.Id,
                Code = "IVA16",
                Name = "IVA 16%",
                Rate = 16m,
                TaxType = "Traslado",
                IsDefault = true,
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.Units.AnyAsync(x => x.TenantId == tenant.Id && x.Code == "PZA"))
        {
            dbContext.Units.Add(new Unit
            {
                Id = ids.Piece,
                TenantId = tenant.Id,
                Code = "PZA",
                Name = "Pieza",
                Abbreviation = "PZA",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.Units.AnyAsync(x => x.TenantId == tenant.Id && x.Code == "SERV"))
        {
            dbContext.Units.Add(new Unit
            {
                Id = ids.Service,
                TenantId = tenant.Id,
                Code = "SERV",
                Name = "Servicio",
                Abbreviation = "SERV",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.Banks.AnyAsync(x => x.TenantId == tenant.Id && x.Code == "BBVA"))
        {
            dbContext.Banks.Add(new Bank
            {
                Id = ids.Bbva,
                TenantId = tenant.Id,
                Code = "BBVA",
                Name = "BBVA México",
                ShortName = "BBVA",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.Banks.AnyAsync(x => x.TenantId == tenant.Id && x.Code == "CITI"))
        {
            dbContext.Banks.Add(new Bank
            {
                Id = ids.Banamex,
                TenantId = tenant.Id,
                Code = "CITI",
                Name = "Citibanamex",
                ShortName = "Banamex",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.Countries.AnyAsync(x => x.Code == "MEX"))
        {
            dbContext.Countries.Add(new Country
            {
                Id = ids.Mexico,
                Code = "MEX",
                Name = "México",
                Iso2 = "MX",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.Countries.AnyAsync(x => x.Code == "USA"))
        {
            dbContext.Countries.Add(new Country
            {
                Id = ids.Usa,
                Code = "USA",
                Name = "Estados Unidos",
                Iso2 = "US",
                CreatedBy = seedUser
            });
        }

        await dbContext.SaveChangesAsync();

        var mexico = await dbContext.Countries.FirstAsync(x => x.Code == "MEX");

        if (!await dbContext.States.AnyAsync(x => x.CountryId == mexico.Id && x.Code == "GTO"))
        {
            dbContext.States.Add(new State
            {
                Id = ids.Guanajuato,
                CountryId = mexico.Id,
                Code = "GTO",
                Name = "Guanajuato",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.States.AnyAsync(x => x.CountryId == mexico.Id && x.Code == "JAL"))
        {
            dbContext.States.Add(new State
            {
                Id = ids.Jalisco,
                CountryId = mexico.Id,
                Code = "JAL",
                Name = "Jalisco",
                CreatedBy = seedUser
            });
        }

        await dbContext.SaveChangesAsync();

        var guanajuato = await dbContext.States.FirstAsync(x => x.Code == "GTO");
        var jalisco = await dbContext.States.FirstAsync(x => x.Code == "JAL");

        if (!await dbContext.Cities.AnyAsync(x => x.StateId == guanajuato.Id && x.Code == "LEON"))
        {
            dbContext.Cities.Add(new City
            {
                Id = ids.Leon,
                StateId = guanajuato.Id,
                Code = "LEON",
                Name = "León",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.Cities.AnyAsync(x => x.StateId == jalisco.Id && x.Code == "GDL"))
        {
            dbContext.Cities.Add(new City
            {
                Id = ids.Guadalajara,
                StateId = jalisco.Id,
                Code = "GDL",
                Name = "Guadalajara",
                CreatedBy = seedUser
            });
        }

        await dbContext.SaveChangesAsync();

        var mxn = await dbContext.Currencies.FirstAsync(x => x.TenantId == tenant.Id && x.Code == "MXN");
        await UpsertExchangeRateAsync(dbContext, tenant.Id, mxn.Id, now.Date, 1m, 1m, 1m, seedUser);

        var usd = await dbContext.Currencies.FirstAsync(x => x.TenantId == tenant.Id && x.Code == "USD");
        await UpsertExchangeRateAsync(dbContext, tenant.Id, usd.Id, now.Date, 16.95m, 17.15m, 17.05m, seedUser);

        if (!await dbContext.DocumentSeries.AnyAsync(x => x.CompanyId == company.Id && x.DocumentType == "PURCHASE_ORDER" && x.Code == "OC"))
        {
            dbContext.DocumentSeries.Add(new DocumentSeries
            {
                Id = ids.PurchaseOrderSeries,
                TenantId = tenant.Id,
                CompanyId = company.Id,
                DocumentType = "PURCHASE_ORDER",
                Code = "OC",
                Name = "Orden de compra",
                Prefix = "OC",
                CurrentNumber = 120,
                NumberLength = 8,
                IsDefault = true,
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.DocumentSeries.AnyAsync(x => x.CompanyId == company.Id && x.DocumentType == "SALES_INVOICE" && x.Code == "FAC"))
        {
            dbContext.DocumentSeries.Add(new DocumentSeries
            {
                Id = ids.SalesInvoiceSeries,
                TenantId = tenant.Id,
                CompanyId = company.Id,
                DocumentType = "SALES_INVOICE",
                Code = "FAC",
                Name = "Factura de venta",
                Prefix = "FAC",
                CurrentNumber = 845,
                NumberLength = 8,
                IsDefault = true,
                CreatedBy = seedUser
            });
        }

        await dbContext.SaveChangesAsync();

        var purchaseOrderSeries = await dbContext.DocumentSeries.FirstAsync(x => x.CompanyId == company.Id && x.DocumentType == "PURCHASE_ORDER" && x.Code == "OC");
        var salesInvoiceSeries = await dbContext.DocumentSeries.FirstAsync(x => x.CompanyId == company.Id && x.DocumentType == "SALES_INVOICE" && x.Code == "FAC");

        if (!await dbContext.DocumentFolios.AnyAsync(x => x.CompanyId == company.Id && x.DocumentType == "PURCHASE_ORDER" && x.SeriesId == purchaseOrderSeries.Id))
        {
            dbContext.DocumentFolios.Add(new DocumentFolio
            {
                Id = ids.PurchaseOrderFolio,
                TenantId = tenant.Id,
                CompanyId = company.Id,
                DocumentType = "PURCHASE_ORDER",
                SeriesId = purchaseOrderSeries.Id,
                CurrentNumber = 121,
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.DocumentFolios.AnyAsync(x => x.CompanyId == company.Id && x.DocumentType == "SALES_INVOICE" && x.SeriesId == salesInvoiceSeries.Id))
        {
            dbContext.DocumentFolios.Add(new DocumentFolio
            {
                Id = ids.SalesInvoiceFolio,
                TenantId = tenant.Id,
                CompanyId = company.Id,
                DocumentType = "SALES_INVOICE",
                SeriesId = salesInvoiceSeries.Id,
                CurrentNumber = 846,
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.CompanySettings.AnyAsync(x => x.CompanyId == company.Id))
        {
            dbContext.CompanySettings.Add(new CompanySetting
            {
                Id = ids.CompanySetting,
                TenantId = tenant.Id,
                CompanyId = company.Id,
                CurrencyId = mxn.Id,
                Timezone = "America/Mexico_City",
                MonetaryDecimals = 2,
                QuantityDecimals = 2,
                DefaultPurchaseSeriesId = purchaseOrderSeries.Id,
                DefaultSalesSeriesId = salesInvoiceSeries.Id,
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.UserSessions.AnyAsync(x => x.Id == ids.AdminSession))
        {
            dbContext.UserSessions.Add(new UserSession
            {
                Id = ids.AdminSession,
                TenantId = tenant.Id,
                UserId = adminUser.Id,
                RefreshToken = "seed-refresh-token-admin-demo",
                ExpiresAt = now.AddDays(7),
                IpAddress = "127.0.0.1",
                UserAgent = "Seed Session",
                CreatedBy = seedUser
            });
        }

        var accessLogs = new[]
        {
            new AccessLog
            {
                Id = ids.AccessLog1,
                TenantId = tenant.Id,
                UserId = adminUser.Id,
                EventType = AccessEventType.LoginSuccess,
                EventResult = "Success",
                IpAddress = "127.0.0.1",
                UserAgent = "Seed Browser",
                Details = "Acceso inicial del administrador demo.",
                CreatedAt = now.AddMinutes(-50),
                CreatedBy = seedUser
            },
            new AccessLog
            {
                Id = ids.AccessLog2,
                TenantId = tenant.Id,
                UserId = opsUser.Id,
                EventType = AccessEventType.LoginFailed,
                EventResult = "Failed",
                IpAddress = "127.0.0.1",
                UserAgent = "Seed Browser",
                Details = "Intento fallido de acceso del usuario de operaciones.",
                CreatedAt = now.AddMinutes(-30),
                CreatedBy = seedUser
            },
            new AccessLog
            {
                Id = ids.AccessLog3,
                TenantId = tenant.Id,
                UserId = adminUser.Id,
                EventType = AccessEventType.RefreshToken,
                EventResult = "Success",
                IpAddress = "127.0.0.1",
                UserAgent = "Seed Browser",
                Details = "Renovación de token demo.",
                CreatedAt = now.AddMinutes(-10),
                CreatedBy = seedUser
            }
        };

        foreach (var accessLog in accessLogs)
        {
            if (await dbContext.AccessLogs.AnyAsync(x => x.Id == accessLog.Id))
                continue;

            dbContext.AccessLogs.Add(accessLog);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task UpsertExchangeRateAsync(
        NanchesoftDbContext dbContext,
        Guid tenantId,
        Guid currencyId,
        DateTime rateDate,
        decimal buyRate,
        decimal sellRate,
        decimal referenceRate,
        string seedUser)
    {
        var existing = await dbContext.ExchangeRates
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.CurrencyId == currencyId && x.RateDate == rateDate);

        if (existing is null)
        {
            dbContext.ExchangeRates.Add(new ExchangeRate
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CurrencyId = currencyId,
                RateDate = rateDate,
                BuyRate = buyRate,
                SellRate = sellRate,
                ReferenceRate = referenceRate,
                CreatedBy = seedUser
            });

            return;
        }

        existing.BuyRate = buyRate;
        existing.SellRate = sellRate;
        existing.ReferenceRate = referenceRate;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = seedUser;
    }

    private sealed record PermissionSeed(
        string Code,
        string Module,
        string Resource,
        string Action,
        string Name,
        Guid Id);

    private sealed record NavigationSeed(
        string Code,
        string Module,
        string Title,
        string Route,
        int SortOrder,
        string? RequiredPermission,
        bool IsVisible,
        Guid Id);
}

using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class AccountsReceivableSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        const string seedUser = "seed";

        await EnsurePermissionsAsync(dbContext, seedUser);
        await EnsureNavigationAsync(dbContext, seedUser);
        await EnsureRolePermissionsAsync(dbContext, seedUser);
        await EnsureProjectedAccountsAsync(dbContext, seedUser);
    }

    private static async Task EnsurePermissionsAsync(NanchesoftDbContext dbContext, string seedUser)
    {
        var permissions = new[]
        {
            CreatePermission("accountsreceivable.balance.view", "accountsreceivable", "balance", "view", "Ver saldos de clientes", seedUser),
            CreatePermission("accountsreceivable.statement.view", "accountsreceivable", "statement", "view", "Ver estados de cuenta", seedUser),
            CreatePermission("accountsreceivable.aging.view", "accountsreceivable", "aging", "view", "Ver antigüedad de saldos", seedUser),
            CreatePermission("accountsreceivable.application.view", "accountsreceivable", "application", "view", "Ver aplicaciones de recibos", seedUser),
            CreatePermission("accountsreceivable.application.create", "accountsreceivable", "application", "create", "Aplicar recibos a facturas", seedUser),
            CreatePermission("accountsreceivable.dashboard.view", "accountsreceivable", "dashboard", "view", "Ver dashboard de cobranza", seedUser)
        };

        foreach (var permission in permissions)
        {
            if (await dbContext.Permissions.AnyAsync(x => x.Code == permission.Code))
                continue;

            dbContext.Permissions.Add(permission);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureNavigationAsync(NanchesoftDbContext dbContext, string seedUser)
    {
        if (!await dbContext.NavigationItems.AnyAsync(x => x.Code == "accounts-receivable"))
        {
            dbContext.NavigationItems.Add(new NavigationItem
            {
                Id = Guid.Parse("E2000000-0000-0000-0000-000000000001"),
                Module = "accountsreceivable",
                Code = "accounts-receivable",
                Title = "CxC",
                Icon = "money",
                Route = "/accounts-receivable/dashboard",
                SortOrder = 150,
                RequiredPermission = "accountsreceivable.dashboard.view",
                IsVisible = true,
                IsActive = true,
                CreatedBy = seedUser
            });
            await dbContext.SaveChangesAsync();
        }
    }

    private static async Task EnsureRolePermissionsAsync(NanchesoftDbContext dbContext, string seedUser)
    {
        var ownerRole = await dbContext.Roles.FirstOrDefaultAsync(x => x.Code == "PLATFORM_OWNER");
        var tenantAdminRole = await dbContext.Roles.FirstOrDefaultAsync(x => x.Code == "TENANT_ADMIN");
        if (ownerRole is null || tenantAdminRole is null)
            return;

        var arPermissions = await dbContext.Permissions.Where(x => x.Module == "accountsreceivable").ToListAsync();
        foreach (var permission in arPermissions)
        {
            if (!await dbContext.RolePermissions.AnyAsync(x => x.RoleId == ownerRole.Id && x.PermissionId == permission.Id))
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = ownerRole.Id,
                    PermissionId = permission.Id,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = seedUser
                });
            }

            if (!await dbContext.RolePermissions.AnyAsync(x => x.RoleId == tenantAdminRole.Id && x.PermissionId == permission.Id))
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = tenantAdminRole.Id,
                    PermissionId = permission.Id,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = seedUser
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureProjectedAccountsAsync(NanchesoftDbContext dbContext, string seedUser)
    {
        var customers = await dbContext.Customers.AsNoTracking().Where(x => x.IsActive).ToListAsync();
        foreach (var customer in customers)
        {
            var account = await dbContext.AccountsReceivableAccounts.FirstOrDefaultAsync(x => x.CompanyId == customer.CompanyId && x.CustomerId == customer.Id);
            if (account is null)
            {
                account = new AccountsReceivableAccount
                {
                    TenantId = customer.TenantId,
                    CompanyId = customer.CompanyId,
                    CustomerId = customer.Id,
                    CurrencyId = customer.CurrencyId
                        ?? await dbContext.CompanySettings
                            .Where(x => x.CompanyId == customer.CompanyId)
                            .Select(x => (Guid?)x.CurrencyId)
                            .FirstOrDefaultAsync()
                        ?? await dbContext.SalesInvoices
                            .Where(x => x.CompanyId == customer.CompanyId && x.CurrencyId != null)
                            .Select(x => x.CurrencyId)
                            .FirstOrDefaultAsync(),
                    Code = $"CXC-{(customer.Code ?? customer.Name).Trim().Replace(" ", string.Empty).ToUpperInvariant()}",
                    Status = "active",
                    CreatedBy = seedUser
                };
                dbContext.AccountsReceivableAccounts.Add(account);
            }

            var charges = await dbContext.SalesInvoices
                .Where(x => x.CustomerId == customer.Id && x.IsActive && x.Status != "cancelled")
                .SumAsync(x => (decimal?)x.Total) ?? 0m;

            var creditNotes = await dbContext.CreditNotes
                .Where(x => x.CustomerId == customer.Id && x.IsActive && x.Status != "cancelled")
                .SumAsync(x => (decimal?)x.Total) ?? 0m;

            var applications = await dbContext.ReceiptApplications
                .Where(x => x.CustomerId == customer.Id && x.IsActive && x.Status != "cancelled")
                .SumAsync(x => (decimal?)x.AppliedAmount) ?? 0m;

            account.TotalCharges = charges;
            account.TotalCredits = creditNotes + applications;
            account.CurrentBalance = charges - creditNotes - applications;
            account.LastMovementAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedBy = seedUser;
        }

        await dbContext.SaveChangesAsync();
    }

    private static Permission CreatePermission(string code, string module, string resource, string action, string name, string seedUser)
        => new()
        {
            Code = code,
            Module = module,
            Resource = resource,
            Action = action,
            Name = name,
            IsActive = true,
            CreatedBy = seedUser
        };
}

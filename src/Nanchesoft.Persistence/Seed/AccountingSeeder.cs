using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class AccountingSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        var company = await dbContext.Companies
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (company is null)
        {
            return;
        }

        var tenantId = company.TenantId;
        var companyId = company.Id;
        var currentYear = DateTime.UtcNow.Year;

        await EnsureAccountAsync(dbContext, tenantId, companyId, "1000", "Caja", "Asset", "Debit");
        await EnsureAccountAsync(dbContext, tenantId, companyId, "1010", "Bancos", "Asset", "Debit");
        await EnsureAccountAsync(dbContext, tenantId, companyId, "1050", "Clientes", "Asset", "Debit");
        await EnsureAccountAsync(dbContext, tenantId, companyId, "1100", "Inventarios", "Asset", "Debit");
        await EnsureAccountAsync(dbContext, tenantId, companyId, "2000", "Proveedores", "Liability", "Credit");
        await EnsureAccountAsync(dbContext, tenantId, companyId, "2100", "Impuestos por pagar", "Liability", "Credit");
        await EnsureAccountAsync(dbContext, tenantId, companyId, "3000", "Capital", "Equity", "Credit");
        await EnsureAccountAsync(dbContext, tenantId, companyId, "4000", "Ventas", "Income", "Credit");
        await EnsureAccountAsync(dbContext, tenantId, companyId, "4010", "Otros ingresos", "Income", "Credit");
        await EnsureAccountAsync(dbContext, tenantId, companyId, "5000", "Costo de ventas", "Expense", "Debit");
        await EnsureAccountAsync(dbContext, tenantId, companyId, "6000", "Gastos generales", "Expense", "Debit");
        await EnsureAccountAsync(dbContext, tenantId, companyId, "6100", "Gastos de administración", "Expense", "Debit");

        for (var month = 1; month <= 12; month++)
        {
            var start = UtcDate(currentYear, month, 1, 0, 0, 0);
            var end = UtcDate(currentYear, month, DateTime.DaysInMonth(currentYear, month), 23, 59, 59);

            var exists = await dbContext.Set<AccountingFiscalPeriod>()
                .AnyAsync(x => x.CompanyId == companyId && x.Year == currentYear && x.Month == month);

            if (exists)
            {
                continue;
            }

            dbContext.Set<AccountingFiscalPeriod>().Add(new AccountingFiscalPeriod
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CompanyId = companyId,
                Year = currentYear,
                Month = month,
                StartDate = start,
                EndDate = end,
                Status = "open",
                IsActive = true
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureAccountAsync(
        NanchesoftDbContext dbContext,
        Guid tenantId,
        Guid companyId,
        string code,
        string name,
        string type,
        string nature)
    {
        var exists = await dbContext.Set<AccountingAccount>()
            .AnyAsync(x => x.CompanyId == companyId && x.Code == code);

        if (exists)
        {
            return;
        }

        dbContext.Set<AccountingAccount>().Add(new AccountingAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CompanyId = companyId,
            Code = code,
            Name = name,
            AccountType = type,
            Nature = nature,
            AllowsPosting = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });
    }

    private static DateTime UtcDate(int year, int month, int day, int hour, int minute, int second)
        => new(year, month, day, hour, minute, second, DateTimeKind.Utc);
}

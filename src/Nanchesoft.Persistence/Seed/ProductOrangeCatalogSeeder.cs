using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Common;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class ProductOrangeCatalogSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        const string seedUser = "seed-orange-catalogs-20260513";
        var tenant = await dbContext.Tenants.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var company = await dbContext.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (tenant is null || company is null) return;

        await SeedCatalogAsync<ProductColor>(dbContext, tenant.Id, company.Id, seedUser, [
            ("NEGRO", "Negro", "Color base principal", 10),
            ("BLANCO", "Blanco", "Color base principal", 20),
            ("CAFE", "Café", "Color base principal", 30),
            ("MIEL", "Miel", "Color base principal", 40),
            ("MARINO", "Marino", "Color base principal", 50),
            ("ROJO", "Rojo", "Color base principal", 60),
            ("COMBINADO", "Combinado", "Color combinado; registrar descripción exacta", 900)
        ]);

        await SeedCatalogAsync<ProductLeatherType>(dbContext, tenant.Id, company.Id, seedUser, [
            ("PIEL", "Piel", "Material principal natural", 10),
            ("SINT", "Sintético", "Material principal sintético", 20),
            ("NOBUCK", "Nobuck", "Piel tipo nobuck", 30),
            ("CHAROL", "Charol", "Acabado charol", 40),
            ("TEXTIL", "Textil", "Material principal textil", 50),
            ("NO_APLICA", "No aplica", "Para productos sin material principal específico", 999)
        ]);

        await SeedCatalogAsync<ProductToeCap>(dbContext, tenant.Id, company.Id, seedUser, [
            ("NUM1", "Número 1", "Casco estándar número 1", 10),
            ("NUM1_BOTA", "Número 1 Bota", "Casco número 1 para bota", 20),
            ("NUM2", "Número 2", "Casco estándar número 2", 30),
            ("NUM2_BOTA", "Número 2 Bota", "Casco número 2 para bota", 40),
            ("NUM3", "Número 3", "Casco estándar número 3", 50)
        ]);

        await SeedCatalogAsync<ProductManufacturingType>(dbContext, tenant.Id, company.Id, seedUser, [
            ("A_MANO", "A mano", "Manufactura manual", 10),
            ("MAQUINA", "Máquina", "Manufactura con máquina", 20),
            ("MIXTA", "Mixta", "Manufactura combinada", 30)
        ]);

        await SeedCatalogAsync<ProductSoleColor>(dbContext, tenant.Id, company.Id, seedUser, [
            ("NEGRO", "Negro", "Color de suela", 10),
            ("BLANCO", "Blanco", "Color de suela", 20),
            ("CAFE", "Café", "Color de suela", 30),
            ("MIEL", "Miel", "Color de suela", 40),
            ("TRANSP", "Transparente", "Color de suela", 50)
        ]);

        await SeedCatalogAsync<ProductDie>(dbContext, tenant.Id, company.Id, seedUser, [
            ("TROQUEL_CLIENTE", "Troquel cliente", "Troquel proporcionado por cliente", 10),
            ("SIN_TROQUEL", "Sin troquel", "No aplica troquel", 999)
        ]);

        await SeedCatalogAsync<QualityControlDie>(dbContext, tenant.Id, company.Id, seedUser, [
            ("M1", "M1 - Vacuno", "Control de calidad material vacuno", 10),
            ("M2", "M2 - Sintético", "Control de calidad material sintético", 20),
            ("M3", "M3 - Cabra", "Control de calidad material cabra", 30),
            ("M4", "M4 - Textil", "Control de calidad material textil", 40)
        ]);

        await SeedCatalogAsync<ProductFolioPattern>(dbContext, tenant.Id, company.Id, seedUser, [
            ("STD", "Estándar", "Foliado estándar", 10),
            ("SIN", "Sin foliado", "Producto sin foliado", 20),
            ("CLIENTE", "Cliente", "Foliado definido por cliente", 30)
        ]);

        await SeedCatalogAsync<ProcessVoucher>(dbContext, tenant.Id, company.Id, seedUser, [
            ("CORTE", "Corte", "Vale/tarjeta para proceso de corte", 10),
            ("FORRO", "Forro", "Vale/tarjeta para proceso de forro", 20),
            ("PESPUNTE", "Pespunte", "Vale/tarjeta para proceso de pespunte", 30),
            ("MONTADO", "Montado", "Vale/tarjeta para proceso de montado", 40),
            ("ADORNO", "Adorno", "Vale/tarjeta para proceso de adorno", 50)
        ]);

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedCatalogAsync<TEntity>(NanchesoftDbContext dbContext, Guid tenantId, Guid companyId, string seedUser, IEnumerable<(string Code, string Name, string Description, int Sequence)> rows)
        where TEntity : BaseEntity, new()
    {
        foreach (var row in rows)
        {
            if (await dbContext.Set<TEntity>().AnyAsync(x => EF.Property<Guid>(x, "CompanyId") == companyId && EF.Property<string>(x, "Code") == row.Code))
                continue;

            var entity = new TEntity
            {
                CreatedBy = seedUser,
                IsActive = true
            };
            dbContext.Entry(entity).Property("TenantId").CurrentValue = tenantId;
            dbContext.Entry(entity).Property("CompanyId").CurrentValue = companyId;
            dbContext.Entry(entity).Property("Code").CurrentValue = row.Code;
            dbContext.Entry(entity).Property("Name").CurrentValue = row.Name;
            dbContext.Entry(entity).Property("Description").CurrentValue = row.Description;
            dbContext.Entry(entity).Property("Sequence").CurrentValue = row.Sequence;
            dbContext.Set<TEntity>().Add(entity);
        }
    }
}

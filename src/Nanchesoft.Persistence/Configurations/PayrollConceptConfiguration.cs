using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PayrollConceptConfiguration : IEntityTypeConfiguration<PayrollConcept>
{
    public void Configure(EntityTypeBuilder<PayrollConcept> builder)
    {
        builder.ToTable("payroll_concepts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.ConceptType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CalculationType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.SatCode).HasMaxLength(20);
        builder.Property(x => x.SatAgrupador).HasMaxLength(40);
        builder.Property(x => x.TaxableType).HasMaxLength(30);
        builder.Property(x => x.Formula).HasMaxLength(2000);
        builder.Property(x => x.TaxableFormula).HasMaxLength(2000);
        builder.Property(x => x.ExemptFormula).HasMaxLength(2000);
        builder.Property(x => x.ImssTaxableFormula).HasMaxLength(2000);
        builder.Property(x => x.SatTipoPercepcionCode).HasMaxLength(20);
        builder.Property(x => x.SatTipoDeduccionCode).HasMaxLength(20);
        builder.Property(x => x.SatTipoOtroPagoCode).HasMaxLength(20);
        builder.Property(x => x.TaxablePercent).HasPrecision(9, 4);
        builder.Property(x => x.ExemptPercent).HasPrecision(9, 4);
        builder.Property(x => x.MinAmount).HasPrecision(18, 2);
        builder.Property(x => x.MaxAmount).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

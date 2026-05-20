using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PayrollDayMnemonicConfiguration : IEntityTypeConfiguration<PayrollDayMnemonic>
{
    public void Configure(EntityTypeBuilder<PayrollDayMnemonic> builder)
    {
        builder.ToTable("payroll_day_mnemonics");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(20);
        builder.Property(x => x.Name).HasMaxLength(120);
        builder.Property(x => x.Kind).HasMaxLength(30);
        builder.Property(x => x.UnitType).HasMaxLength(20);
        builder.Property(x => x.ColorCode).HasMaxLength(16);
        builder.Property(x => x.ShortLabel).HasMaxLength(12);
        builder.Property(x => x.Notes).HasMaxLength(400);

        builder.Property(x => x.DefaultUnits).HasPrecision(18, 4);
        builder.Property(x => x.Multiplier).HasPrecision(18, 4);

        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();

        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollConcept).WithMany().HasForeignKey(x => x.PayrollConceptId).OnDelete(DeleteBehavior.Restrict);
    }
}

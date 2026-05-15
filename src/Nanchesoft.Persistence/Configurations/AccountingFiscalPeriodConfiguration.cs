using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class AccountingFiscalPeriodConfiguration : IEntityTypeConfiguration<AccountingFiscalPeriod>
{
    public void Configure(EntityTypeBuilder<AccountingFiscalPeriod> builder)
    {
        builder.ToTable("accounting_fiscal_periods");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.CompanyId, x.Year, x.Month }).IsUnique();
    }
}

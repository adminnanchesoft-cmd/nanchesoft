using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class AccountingAccountCompanyConfiguration : IEntityTypeConfiguration<AccountingAccountCompany>
{
    public void Configure(EntityTypeBuilder<AccountingAccountCompany> builder)
    {
        builder.ToTable("accounting_account_companies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompanyName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ImportSource).HasMaxLength(32);
        builder.HasIndex(x => new { x.AccountId, x.CompanyName }).IsUnique();
    }
}

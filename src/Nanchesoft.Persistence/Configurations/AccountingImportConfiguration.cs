using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class AccountingImportConfiguration : IEntityTypeConfiguration<AccountingImport>
{
    public void Configure(EntityTypeBuilder<AccountingImport> builder)
    {
        builder.ToTable("accounting_imports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(128);
        builder.HasIndex(x => x.TenantId);
    }
}

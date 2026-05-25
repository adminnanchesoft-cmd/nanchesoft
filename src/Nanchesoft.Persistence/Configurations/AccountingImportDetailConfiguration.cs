using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class AccountingImportDetailConfiguration : IEntityTypeConfiguration<AccountingImportDetail>
{
    public void Configure(EntityTypeBuilder<AccountingImportDetail> builder)
    {
        builder.ToTable("accounting_import_details");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountCode).HasMaxLength(64);
        builder.Property(x => x.AccountName).HasMaxLength(256);
        builder.Property(x => x.Company).HasMaxLength(128);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.ImportId);
    }
}

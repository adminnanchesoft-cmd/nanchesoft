using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class AccountingGroupCompanyConfiguration : IEntityTypeConfiguration<AccountingGroupCompany>
{
    public void Configure(EntityTypeBuilder<AccountingGroupCompany> builder)
    {
        builder.ToTable("accounting_group_companies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(128);
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}

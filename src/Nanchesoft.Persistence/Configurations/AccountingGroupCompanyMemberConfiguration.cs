using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class AccountingGroupCompanyMemberConfiguration : IEntityTypeConfiguration<AccountingGroupCompanyMember>
{
    public void Configure(EntityTypeBuilder<AccountingGroupCompanyMember> builder)
    {
        builder.ToTable("accounting_group_company_members");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompanyName).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.GroupCompanyId, x.CompanyName }).IsUnique();
    }
}

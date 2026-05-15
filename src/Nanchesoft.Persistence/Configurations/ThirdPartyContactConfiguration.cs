using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ThirdPartyContactConfiguration : IEntityTypeConfiguration<ThirdPartyContact>
{
    public void Configure(EntityTypeBuilder<ThirdPartyContact> builder)
    {
        builder.ToTable("third_party_contacts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ThirdPartyType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Position).HasMaxLength(120);
        builder.Property(x => x.Email).HasMaxLength(160);
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.Mobile).HasMaxLength(30);

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

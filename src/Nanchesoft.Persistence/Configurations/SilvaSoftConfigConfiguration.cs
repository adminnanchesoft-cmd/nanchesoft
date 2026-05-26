using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class SilvaSoftConfigConfiguration : IEntityTypeConfiguration<SilvaSoftConfig>
{
    public void Configure(EntityTypeBuilder<SilvaSoftConfig> builder)
    {
        builder.ToTable("silvasoft_configs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServerHost).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DatabaseName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DbUser).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DbPassword).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => x.CompanyId).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

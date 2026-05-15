using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductSizeRunConfiguration : IEntityTypeConfiguration<ProductSizeRun>
{
    public void Configure(EntityTypeBuilder<ProductSizeRun> builder)
    {
        builder.ToTable("product_size_runs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.LegacyKey).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SecondaryKey).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ConsumptionMode).HasMaxLength(1).IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

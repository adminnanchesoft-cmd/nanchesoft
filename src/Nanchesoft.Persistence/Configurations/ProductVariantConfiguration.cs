using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SizeCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DisplayLabel).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Sku).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Barcode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500).IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.Sku }).IsUnique();
        builder.HasIndex(x => new { x.FinishedProductId, x.ProductSizeRunSizeId }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FinishedProduct).WithMany().HasForeignKey(x => x.FinishedProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ProductSizeRun).WithMany(x => x.Variants).HasForeignKey(x => x.ProductSizeRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductSizeRunSize).WithMany().HasForeignKey(x => x.ProductSizeRunSizeId).OnDelete(DeleteBehavior.Restrict);
    }
}

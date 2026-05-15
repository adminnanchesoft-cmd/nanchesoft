using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductSizeConsumptionVariationConfiguration : IEntityTypeConfiguration<ProductSizeConsumptionVariation>
{
    public void Configure(EntityTypeBuilder<ProductSizeConsumptionVariation> builder)
    {
        builder.ToTable("product_size_consumption_variations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BaseSizeCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.TargetSizeCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.VariationPercent).HasPrecision(18, 4);
        builder.Property(x => x.QuantityDelta).HasPrecision(18, 6);
        builder.Property(x => x.Notes).HasMaxLength(1200);
        builder.HasIndex(x => new { x.FinishedProductId, x.ProductComponentId, x.BaseSizeCode, x.TargetSizeCode }).IsUnique();
    }
}

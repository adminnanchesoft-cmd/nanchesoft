using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductSizeRunSizeConfiguration : IEntityTypeConfiguration<ProductSizeRunSize>
{
    public void Configure(EntityTypeBuilder<ProductSizeRunSize> builder)
    {
        builder.ToTable("product_size_run_sizes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SizeCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DisplayLabel).HasMaxLength(30).IsRequired();
        builder.Property(x => x.BarcodeLabel).HasMaxLength(30).IsRequired();
        builder.Property(x => x.FactorLabel).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Proportion).HasPrecision(18, 6);

        builder.HasIndex(x => new { x.ProductSizeRunId, x.Sequence }).IsUnique();
        builder.HasIndex(x => new { x.ProductSizeRunId, x.SizeCode });

        builder.HasOne(x => x.ProductSizeRun)
            .WithMany(x => x.Sizes)
            .HasForeignKey(x => x.ProductSizeRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

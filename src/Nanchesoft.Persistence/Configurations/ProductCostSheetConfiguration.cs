using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductCostSheetConfiguration : IEntityTypeConfiguration<ProductCostSheet>
{
    public void Configure(EntityTypeBuilder<ProductCostSheet> builder)
    {
        builder.ToTable("product_cost_sheets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CostSheetCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.ApprovedBy).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.DirectMaterialCost).HasPrecision(18, 4);
        builder.Property(x => x.DirectLaborCost).HasPrecision(18, 4);
        builder.Property(x => x.IndirectManufacturingCost).HasPrecision(18, 4);
        builder.Property(x => x.PackagingCost).HasPrecision(18, 4);
        builder.Property(x => x.ServiceCost).HasPrecision(18, 4);
        builder.Property(x => x.TotalCost).HasPrecision(18, 4);
        builder.Property(x => x.TargetMarginPercent).HasPrecision(18, 4);
        builder.Property(x => x.SuggestedSalePrice).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.CompanyId, x.CostSheetCode }).IsUnique();
    }
}

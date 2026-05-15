using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductTechnicalSheetMaterialConfiguration : IEntityTypeConfiguration<ProductTechnicalSheetMaterial>
{
    public void Configure(EntityTypeBuilder<ProductTechnicalSheetMaterial> builder)
    {
        builder.ToTable("product_technical_sheet_materials");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ComponentCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ComponentName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.MaterialCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.MaterialName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UnitCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 6);
        builder.Property(x => x.WastePercent).HasPrecision(18, 4);
        builder.Property(x => x.Notes).HasMaxLength(1200);
        builder.HasIndex(x => new { x.ProductTechnicalSheetId, x.SortOrder });
    }
}

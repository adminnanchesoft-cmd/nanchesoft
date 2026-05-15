using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductTechnicalSheetConfiguration : IEntityTypeConfiguration<ProductTechnicalSheet>
{
    public void Configure(EntityTypeBuilder<ProductTechnicalSheet> builder)
    {
        builder.ToTable("product_technical_sheets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SheetCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.SheetName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ProductDisplayName).HasMaxLength(240).IsRequired();
        builder.Property(x => x.PhotoUrl).HasMaxLength(500);
        builder.Property(x => x.MainMaterialName).HasMaxLength(120);
        builder.Property(x => x.MainColorName).HasMaxLength(120);
        builder.Property(x => x.SizeRunCode).HasMaxLength(30);
        builder.Property(x => x.ApprovedBy).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasIndex(x => new { x.CompanyId, x.SheetCode }).IsUnique();
        builder.HasMany(x => x.Materials).WithOne().HasForeignKey(x => x.ProductTechnicalSheetId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Processes).WithOne().HasForeignKey(x => x.ProductTechnicalSheetId).OnDelete(DeleteBehavior.Cascade);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductStyleConfiguration : IEntityTypeConfiguration<ProductStyle>
{
    public void Configure(EntityTypeBuilder<ProductStyle> builder)
    {
        builder.ToTable("product_styles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.CustomerLabel1).HasMaxLength(80);
        builder.Property(x => x.CustomerLabel2).HasMaxLength(80);
        builder.Property(x => x.ColorLabel).HasMaxLength(80);
        builder.Property(x => x.DieCutReference).HasMaxLength(60);
        builder.Property(x => x.MaxLotSize).HasPrecision(18, 2);
        builder.Property(x => x.TechnicalNotes).HasMaxLength(2000);
        builder.Property(x => x.ProductionCardNotes).HasMaxLength(2000);
        builder.Property(x => x.OutsourcedProcessName).HasMaxLength(120);
        builder.Property(x => x.PhotoUrl).HasMaxLength(400);

        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductLine).WithMany().HasForeignKey(x => x.ProductLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductLast).WithMany().HasForeignKey(x => x.ProductLastId).OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ItemEngineeringProfileConfiguration : IEntityTypeConfiguration<ItemEngineeringProfile>
{
    public void Configure(EntityTypeBuilder<ItemEngineeringProfile> builder)
    {
        builder.ToTable("item_engineering_profiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FolioPattern).HasMaxLength(80);
        builder.Property(x => x.TechnicalSheetMode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ProcessVoucherProfile).HasMaxLength(120);
        builder.Property(x => x.TechnicalSheetNotes).HasMaxLength(2000);
        builder.Property(x => x.ProductionCardNotes).HasMaxLength(2000);

        builder.HasIndex(x => x.ItemId).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ProductStyle).WithMany().HasForeignKey(x => x.ProductStyleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductSizeRun).WithMany().HasForeignKey(x => x.ProductSizeRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.EmbroideryPattern).WithMany().HasForeignKey(x => x.EmbroideryPatternId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PrimaryMaterialItem).WithMany().HasForeignKey(x => x.PrimaryMaterialItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

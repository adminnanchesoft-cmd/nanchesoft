using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class MaterialFamilyConfiguration : IEntityTypeConfiguration<MaterialFamily>
{
    public void Configure(EntityTypeBuilder<MaterialFamily> builder)
    {
        builder.ToTable("material_families");
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(140).IsRequired();
        builder.Property(x => x.InventoryGroup).HasMaxLength(80);
        builder.Property(x => x.Notes).HasMaxLength(1200);
    }
}

public sealed class MaterialSubfamilyConfiguration : IEntityTypeConfiguration<MaterialSubfamily>
{
    public void Configure(EntityTypeBuilder<MaterialSubfamily> builder)
    {
        builder.ToTable("material_subfamilies");
        builder.HasIndex(x => new { x.CompanyId, x.MaterialFamilyId, x.Code }).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(140).IsRequired();
        builder.Property(x => x.MaterialType).HasMaxLength(40).HasDefaultValue(MaterialSubfamily.DirectMaterialType);
        builder.Property(x => x.Notes).HasMaxLength(1200);
        builder.HasOne(x => x.MaterialFamily)
            .WithMany(x => x.Subfamilies)
            .HasForeignKey(x => x.MaterialFamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MaterialItemConfiguration : IEntityTypeConfiguration<MaterialItem>
{
    public void Configure(EntityTypeBuilder<MaterialItem> builder)
    {
        builder.ToTable("material_items");
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(220).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(600);
        builder.Property(x => x.LegacyMaterialName).HasMaxLength(80);
        builder.Property(x => x.CostStatus).HasMaxLength(40).HasDefaultValue(MaterialItem.DraftCostStatus);
        builder.Property(x => x.AuthorizedCost).HasPrecision(18, 2);
        builder.Property(x => x.LastPurchaseCost).HasPrecision(18, 2);
        builder.Property(x => x.StandardCost).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(1200);
        builder.HasOne(x => x.MaterialSubfamily)
            .WithMany(x => x.MaterialItems)
            .HasForeignKey(x => x.MaterialSubfamilyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PurchaseUnit).WithMany().HasForeignKey(x => x.PurchaseUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.IssueUnit).WithMany().HasForeignKey(x => x.IssueUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FinishedProductConfiguration : IEntityTypeConfiguration<FinishedProduct>
{
    public void Configure(EntityTypeBuilder<FinishedProduct> builder)
    {
        builder.ToTable("finished_products");
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        builder.HasOne(x => x.ProductStyle).WithMany().HasForeignKey(x => x.ProductStyleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ItemModel).WithMany().HasForeignKey(x => x.ItemModelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ItemBrand).WithMany().HasForeignKey(x => x.ItemBrandId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductLeatherType).WithMany().HasForeignKey(x => x.ProductLeatherTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductColor).WithMany().HasForeignKey(x => x.ProductColorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductToeCap).WithMany().HasForeignKey(x => x.ProductToeCapId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductSole).WithMany().HasForeignKey(x => x.ProductSoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductSoleColor).WithMany().HasForeignKey(x => x.ProductSoleColorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductFolioPattern).WithMany().HasForeignKey(x => x.ProductFolioPatternId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductSizeRun).WithMany().HasForeignKey(x => x.ProductSizeRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductLine).WithMany().HasForeignKey(x => x.ProductLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductLast).WithMany().HasForeignKey(x => x.ProductLastId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.MainMaterialItem).WithMany().HasForeignKey(x => x.MainMaterialItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductComponentConfiguration : IEntityTypeConfiguration<ProductComponent>
{
    public void Configure(EntityTypeBuilder<ProductComponent> builder)
    {
        builder.ToTable("product_components");
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        builder.Property(x => x.DefaultConsumption).HasPrecision(18, 4);
        builder.HasOne(x => x.ConsumptionUnit).WithMany().HasForeignKey(x => x.ConsumptionUnitId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FinishedProductMaterialConfiguration : IEntityTypeConfiguration<FinishedProductMaterial>
{
    public void Configure(EntityTypeBuilder<FinishedProductMaterial> builder)
    {
        builder.ToTable("finished_product_materials");
        builder.HasIndex(x => new { x.FinishedProductId, x.ProductComponentId, x.MaterialItemId, x.SizeCode }).IsUnique();
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.HasOne(x => x.FinishedProduct).WithMany().HasForeignKey(x => x.FinishedProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ProductComponent).WithMany().HasForeignKey(x => x.ProductComponentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.MaterialItem).WithMany().HasForeignKey(x => x.MaterialItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductConsumptionProfileConfiguration : IEntityTypeConfiguration<ProductConsumptionProfile>
{
    public void Configure(EntityTypeBuilder<ProductConsumptionProfile> builder)
    {
        builder.ToTable("product_consumption_profiles");
        builder.HasIndex(x => new { x.FinishedProductId, x.ProductComponentId, x.SizeCode }).IsUnique();
        builder.Property(x => x.Consumption).HasPrecision(18, 4);
        builder.HasOne(x => x.FinishedProduct).WithMany().HasForeignKey(x => x.FinishedProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ProductComponent).WithMany().HasForeignKey(x => x.ProductComponentId).OnDelete(DeleteBehavior.Restrict);
    }
}

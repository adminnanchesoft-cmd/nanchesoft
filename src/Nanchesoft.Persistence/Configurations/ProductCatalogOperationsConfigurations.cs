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

public sealed class MaterialCharacteristicConfiguration : IEntityTypeConfiguration<MaterialCharacteristic>
{
    public void Configure(EntityTypeBuilder<MaterialCharacteristic> builder)
    {
        builder.ToTable("material_characteristics");
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(140).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(600);
    }
}

public sealed class MaterialSizeConfiguration : IEntityTypeConfiguration<MaterialSize>
{
    public void Configure(EntityTypeBuilder<MaterialSize> builder)
    {
        builder.ToTable("material_sizes");
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(600);
    }
}

public sealed class MaterialItemConfiguration : IEntityTypeConfiguration<MaterialItem>
{
    public void Configure(EntityTypeBuilder<MaterialItem> builder)
    {
        builder.ToTable("material_items");
        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.CompanyId, x.Name })
            .IsUnique()
            .HasDatabaseName("ix_material_items_company_name");
        builder.HasIndex(x => new { x.CompanyId, x.MaterialCharacteristicId, x.MaterialSizeId })
            .IsUnique()
            .HasFilter("material_characteristic_id IS NOT NULL AND material_size_id IS NOT NULL")
            .HasDatabaseName("ix_material_items_characteristic_size");
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
        builder.HasOne(x => x.MaterialCharacteristic).WithMany().HasForeignKey(x => x.MaterialCharacteristicId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.MaterialSize).WithMany().HasForeignKey(x => x.MaterialSizeId).OnDelete(DeleteBehavior.Restrict);
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

public sealed class ProductionPhaseConfiguration : IEntityTypeConfiguration<ProductionPhase>
{
    public void Configure(EntityTypeBuilder<ProductionPhase> builder)
    {
        builder.ToTable("production_phases");
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(140).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(600);
        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
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
        builder.HasOne(x => x.ProductionPhase).WithMany().HasForeignKey(x => x.ProductionPhaseId).OnDelete(DeleteBehavior.Restrict);
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

public sealed class ConsumptionTemplateConfiguration : IEntityTypeConfiguration<ConsumptionTemplate>
{
    public void Configure(EntityTypeBuilder<ConsumptionTemplate> builder)
    {
        builder.ToTable("consumption_templates");
        builder.HasIndex(x => new { x.CompanyId, x.ProductStyleId, x.ProductSizeRunId })
            .IsUnique()
            .HasFilter("is_active = true")
            .HasDatabaseName("ix_consumption_templates_active_style_run");
        builder.Property(x => x.Notes).HasMaxLength(1200);
        builder.Property(x => x.AuthorizedBy).HasMaxLength(120);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductStyle).WithMany().HasForeignKey(x => x.ProductStyleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductSizeRun).WithMany().HasForeignKey(x => x.ProductSizeRunId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ConsumptionTemplateDetailConfiguration : IEntityTypeConfiguration<ConsumptionTemplateDetail>
{
    public void Configure(EntityTypeBuilder<ConsumptionTemplateDetail> builder)
    {
        builder.ToTable("consumption_template_details");
        builder.HasIndex(x => x.ConsumptionTemplateId);
        builder.HasIndex(x => x.ProductComponentId);
        builder.HasIndex(x => new { x.ConsumptionTemplateId, x.ProductComponentId }).IsUnique();
        builder.Property(x => x.Notes).HasMaxLength(1200);
        builder.HasOne(x => x.ConsumptionTemplate).WithMany(x => x.Details).HasForeignKey(x => x.ConsumptionTemplateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ProductComponent).WithMany().HasForeignKey(x => x.ProductComponentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ConsumptionTemplateSizeConfiguration : IEntityTypeConfiguration<ConsumptionTemplateSize>
{
    public void Configure(EntityTypeBuilder<ConsumptionTemplateSize> builder)
    {
        builder.ToTable("consumption_template_sizes");
        builder.HasIndex(x => x.ConsumptionTemplateDetailId);
        builder.HasIndex(x => x.ProductSizeRunSizeId);
        builder.HasIndex(x => new { x.ConsumptionTemplateDetailId, x.ProductSizeRunSizeId }).IsUnique();
        builder.Property(x => x.Consumption).HasPrecision(18, 4);
        builder.HasOne(x => x.ConsumptionTemplateDetail).WithMany(x => x.Sizes).HasForeignKey(x => x.ConsumptionTemplateDetailId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ProductSizeRunSize).WithMany().HasForeignKey(x => x.ProductSizeRunSizeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FinishedProductSupplyConfiguration : IEntityTypeConfiguration<FinishedProductSupply>
{
    public void Configure(EntityTypeBuilder<FinishedProductSupply> builder)
    {
        builder.ToTable("finished_product_supplies");
        builder.HasIndex(x => new { x.FinishedProductId, x.ProductComponentId }).IsUnique();
        builder.Property(x => x.AuthorizedBy).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(1200);
        builder.HasOne(x => x.FinishedProduct).WithMany().HasForeignKey(x => x.FinishedProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ProductComponent).WithMany().HasForeignKey(x => x.ProductComponentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FinishedProductSupplySizeConfiguration : IEntityTypeConfiguration<FinishedProductSupplySize>
{
    public void Configure(EntityTypeBuilder<FinishedProductSupplySize> builder)
    {
        builder.ToTable("finished_product_supply_sizes");
        builder.HasIndex(x => new { x.FinishedProductSupplyId, x.ProductSizeRunSizeId }).IsUnique();
        builder.Property(x => x.Notes).HasMaxLength(1200);
        builder.HasOne(x => x.Supply).WithMany(x => x.Sizes).HasForeignKey(x => x.FinishedProductSupplyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SizeRunSize).WithMany().HasForeignKey(x => x.ProductSizeRunSizeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.MaterialItem).WithMany().HasForeignKey(x => x.MaterialItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MaterialSizeDistributionConfiguration : IEntityTypeConfiguration<MaterialSizeDistribution>
{
    public void Configure(EntityTypeBuilder<MaterialSizeDistribution> builder)
    {
        builder.ToTable("material_size_distributions");
        builder.HasIndex(x => new { x.CompanyId, x.MaterialSubfamilyId, x.ProductSizeRunId, x.ProductLastId })
            .IsUnique()
            .HasDatabaseName("ix_material_size_dist_company_subfamily_run_last");
        builder.Property(x => x.Notes).HasMaxLength(1200);
        builder.HasOne(x => x.MaterialSubfamily).WithMany().HasForeignKey(x => x.MaterialSubfamilyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductSizeRun).WithMany().HasForeignKey(x => x.ProductSizeRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductLast).WithMany().HasForeignKey(x => x.ProductLastId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MaterialSizeDistributionDetailConfiguration : IEntityTypeConfiguration<MaterialSizeDistributionDetail>
{
    public void Configure(EntityTypeBuilder<MaterialSizeDistributionDetail> builder)
    {
        builder.ToTable("material_size_distribution_details");
        builder.HasIndex(x => new { x.MaterialSizeDistributionId, x.ProductSizeRunSizeId }).IsUnique();
        builder.Property(x => x.Notes).HasMaxLength(1200);
        builder.HasOne(x => x.Distribution).WithMany(x => x.Details).HasForeignKey(x => x.MaterialSizeDistributionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SizeRunSize).WithMany().HasForeignKey(x => x.ProductSizeRunSizeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.MaterialItem).WithMany().HasForeignKey(x => x.MaterialItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

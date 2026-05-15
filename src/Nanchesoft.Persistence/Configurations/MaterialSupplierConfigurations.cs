using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class MaterialSupplierAssignmentConfiguration : IEntityTypeConfiguration<MaterialSupplierAssignment>
{
    public void Configure(EntityTypeBuilder<MaterialSupplierAssignment> builder)
    {
        builder.ToTable("material_supplier_assignments");
        builder.HasIndex(x => new { x.CompanyId, x.MaterialItemId, x.SupplierId }).IsUnique();
        builder.HasIndex(x => new { x.CompanyId, x.MaterialItemId, x.IsPreferred });
        builder.Property(x => x.SupplierItemCode).HasMaxLength(80);
        builder.Property(x => x.SupplierItemName).HasMaxLength(220);
        builder.Property(x => x.ConversionFactor).HasPrecision(18, 6).HasDefaultValue(1m);
        builder.Property(x => x.AuthorizedCost).HasPrecision(18, 4);
        builder.Property(x => x.LastCost).HasPrecision(18, 4);
        builder.Property(x => x.MinimumOrderQuantity).HasPrecision(18, 4);
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasOne(x => x.MaterialItem).WithMany().HasForeignKey(x => x.MaterialItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PurchaseUnit).WithMany().HasForeignKey(x => x.PurchaseUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MaterialSupplierCostHistoryConfiguration : IEntityTypeConfiguration<MaterialSupplierCostHistory>
{
    public void Configure(EntityTypeBuilder<MaterialSupplierCostHistory> builder)
    {
        builder.ToTable("material_supplier_cost_history");
        builder.HasIndex(x => new { x.MaterialSupplierAssignmentId, x.CostDate });
        builder.Property(x => x.Cost).HasPrecision(18, 4);
        builder.Property(x => x.ExchangeRate).HasPrecision(18, 6).HasDefaultValue(1m);
        builder.Property(x => x.SourceDocumentType).HasMaxLength(40);
        builder.Property(x => x.SourceDocumentNumber).HasMaxLength(80);
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasOne(x => x.MaterialSupplierAssignment)
            .WithMany(x => x.CostHistory)
            .HasForeignKey(x => x.MaterialSupplierAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
    }
}

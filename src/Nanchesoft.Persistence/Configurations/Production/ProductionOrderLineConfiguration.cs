using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductionOrderLineConfiguration : IEntityTypeConfiguration<ProductionOrderLine>
{
    public void Configure(EntityTypeBuilder<ProductionOrderLine> builder)
    {
        builder.ToTable("production_order_lines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.Property(x => x.QuantitiesPerSize)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(x => x.ProductionOrderId);
        builder.HasIndex(x => x.FinishedProductId);
        builder.HasIndex(x => x.SalesOrderId);

        builder.HasOne(x => x.ProductionOrder).WithMany(x => x.Lines).HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SalesOrder).WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SalesOrderLine).WithMany().HasForeignKey(x => x.SalesOrderLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FinishedProduct).WithMany().HasForeignKey(x => x.FinishedProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductStyle).WithMany().HasForeignKey(x => x.ProductStyleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductSizeRun).WithMany().HasForeignKey(x => x.ProductSizeRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductLast).WithMany().HasForeignKey(x => x.ProductLastId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductColor).WithMany().HasForeignKey(x => x.ProductColorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductSole).WithMany().HasForeignKey(x => x.ProductSoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductManufacturingType).WithMany().HasForeignKey(x => x.ProductManufacturingTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}

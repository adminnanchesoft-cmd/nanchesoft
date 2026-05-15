using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class SalesShipmentLineConfiguration : IEntityTypeConfiguration<SalesShipmentLine>
{
    public void Configure(EntityTypeBuilder<SalesShipmentLine> builder)
    {
        builder.ToTable("sales_shipment_lines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description).HasMaxLength(240).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.HasOne(x => x.SalesOrderLine).WithMany().HasForeignKey(x => x.SalesOrderLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}

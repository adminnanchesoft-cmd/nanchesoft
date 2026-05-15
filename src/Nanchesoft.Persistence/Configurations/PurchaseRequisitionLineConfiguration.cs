using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PurchaseRequisitionLineConfiguration : IEntityTypeConfiguration<PurchaseRequisitionLine>
{
    public void Configure(EntityTypeBuilder<PurchaseRequisitionLine> builder)
    {
        builder.ToTable("purchase_requisition_lines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description).HasMaxLength(240).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(300);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PurchaseReceiptDiffConfiguration : IEntityTypeConfiguration<PurchaseReceiptDiff>
{
    public void Configure(EntityTypeBuilder<PurchaseReceiptDiff> builder)
    {
        builder.ToTable("purchase_receipt_diffs", "purchase");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AuthorizationNotes).HasMaxLength(500);

        builder.HasOne(x => x.PurchaseReceipt).WithMany()
            .HasForeignKey(x => x.PurchaseReceiptId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PurchaseOrder).WithMany()
            .HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne(l => l.Diff)
            .HasForeignKey(l => l.PurchaseReceiptDiffId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PurchaseReceiptDiffLineConfiguration : IEntityTypeConfiguration<PurchaseReceiptDiffLine>
{
    public void Configure(EntityTypeBuilder<PurchaseReceiptDiffLine> builder)
    {
        builder.ToTable("purchase_receipt_diff_lines", "purchase");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MaterialName).HasMaxLength(200);
        builder.Property(x => x.DiffType).HasMaxLength(40);
        builder.Property(x => x.OrderedQuantity).HasPrecision(18, 4);
        builder.Property(x => x.ReceivedQuantity).HasPrecision(18, 4);
        builder.Property(x => x.OrderedUnitPrice).HasPrecision(18, 4);
        builder.Property(x => x.ReceivedUnitPrice).HasPrecision(18, 4);

        builder.HasOne(x => x.MaterialItem).WithMany()
            .HasForeignKey(x => x.MaterialItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

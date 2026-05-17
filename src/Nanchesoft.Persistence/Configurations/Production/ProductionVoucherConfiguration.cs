using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductionVoucherConfiguration : IEntityTypeConfiguration<ProductionVoucher>
{
    public void Configure(EntityTypeBuilder<ProductionVoucher> builder)
    {
        builder.ToTable("production_vouchers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Folio).HasMaxLength(30).IsRequired();
        builder.Property(x => x.LotNumber).HasMaxLength(30);
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.IssuedBy).HasMaxLength(120);
        builder.Property(x => x.CompletedBy).HasMaxLength(120);
        builder.Property(x => x.CancelledReason).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.Folio }).IsUnique();
        builder.HasIndex(x => x.ProductionOrderId);
        builder.HasIndex(x => new { x.ProductionPhaseId, x.IssuedDate });

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionOrder).WithMany(x => x.Vouchers).HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionOrderLine).WithMany().HasForeignKey(x => x.ProductionOrderLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionPhase).WithMany().HasForeignKey(x => x.ProductionPhaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionCell).WithMany().HasForeignKey(x => x.ProductionCellId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Details).WithOne(x => x.ProductionVoucher).HasForeignKey(x => x.ProductionVoucherId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.PieceWorkRecords).WithOne(x => x.ProductionVoucher).HasForeignKey(x => x.ProductionVoucherId).OnDelete(DeleteBehavior.Restrict);
    }
}

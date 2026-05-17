using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductionVoucherDetailConfiguration : IEntityTypeConfiguration<ProductionVoucherDetail>
{
    public void Configure(EntityTypeBuilder<ProductionVoucherDetail> builder)
    {
        builder.ToTable("production_voucher_details");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OperationCode).HasMaxLength(30);
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasIndex(x => x.ProductionVoucherId);
        builder.HasIndex(x => x.EmployeeId);

        builder.HasOne(x => x.ProductionVoucher).WithMany(x => x.Details).HasForeignKey(x => x.ProductionVoucherId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SizeRunSize).WithMany().HasForeignKey(x => x.SizeRunSizeId).OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ReceiptApplicationConfiguration : IEntityTypeConfiguration<ReceiptApplication>
{
    public void Configure(EntityTypeBuilder<ReceiptApplication> builder)
    {
        builder.ToTable("receipt_applications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AppliedAmount).HasPrecision(18, 2);
        builder.Property(x => x.Reference).HasMaxLength(160);
        builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => new { x.ReceiptId, x.SalesInvoiceId, x.Id });

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Receipt).WithMany().HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SalesInvoice).WithMany().HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AccountsReceivableMovement).WithMany().HasForeignKey(x => x.AccountsReceivableMovementId).OnDelete(DeleteBehavior.SetNull);
    }
}

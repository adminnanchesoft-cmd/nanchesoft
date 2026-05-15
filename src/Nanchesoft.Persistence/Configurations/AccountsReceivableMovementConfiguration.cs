using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class AccountsReceivableMovementConfiguration : IEntityTypeConfiguration<AccountsReceivableMovement>
{
    public void Configure(EntityTypeBuilder<AccountsReceivableMovement> builder)
    {
        builder.ToTable("accounts_receivable_movements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MovementType).HasMaxLength(40).IsRequired();
        builder.Property(x => x.DocumentType).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(160);
        builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ChargeAmount).HasPrecision(18, 2);
        builder.Property(x => x.CreditAmount).HasPrecision(18, 2);
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.AccountsReceivableAccountId, x.MovementDate, x.Id });

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SalesInvoice).WithMany().HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CreditNote).WithMany().HasForeignKey(x => x.CreditNoteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Receipt).WithMany().HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.Restrict);
    }
}

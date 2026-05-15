using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class TreasuryExpenseLineConfiguration : IEntityTypeConfiguration<TreasuryExpenseLine>
{
    public void Configure(EntityTypeBuilder<TreasuryExpenseLine> builder)
    {
        builder.ToTable("treasury_expense_lines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(240).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PurchaseInvoice).WithMany().HasForeignKey(x => x.PurchaseInvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

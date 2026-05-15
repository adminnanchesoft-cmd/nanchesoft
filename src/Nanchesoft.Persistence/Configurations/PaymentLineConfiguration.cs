using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PaymentLineConfiguration : IEntityTypeConfiguration<PaymentLine>
{
    public void Configure(EntityTypeBuilder<PaymentLine> builder)
    {
        builder.ToTable("payment_lines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(240).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasOne(x => x.PurchaseInvoice).WithMany().HasForeignKey(x => x.PurchaseInvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

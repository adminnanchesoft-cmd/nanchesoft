using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class TreasuryIncomeLineConfiguration : IEntityTypeConfiguration<TreasuryIncomeLine>
{
    public void Configure(EntityTypeBuilder<TreasuryIncomeLine> builder)
    {
        builder.ToTable("treasury_income_lines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(240).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SalesInvoice).WithMany().HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class AccountsReceivableAccountConfiguration : IEntityTypeConfiguration<AccountsReceivableAccount>
{
    public void Configure(EntityTypeBuilder<AccountsReceivableAccount> builder)
    {
        builder.ToTable("accounts_receivable_accounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
        builder.Property(x => x.TotalCharges).HasPrecision(18, 2);
        builder.Property(x => x.TotalCredits).HasPrecision(18, 2);
        builder.Property(x => x.CurrentBalance).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CompanyId, x.CustomerId }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Movements).WithOne(x => x.AccountsReceivableAccount).HasForeignKey(x => x.AccountsReceivableAccountId).OnDelete(DeleteBehavior.Cascade);
    }
}

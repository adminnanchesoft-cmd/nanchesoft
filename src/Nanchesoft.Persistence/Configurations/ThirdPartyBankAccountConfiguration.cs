using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ThirdPartyBankAccountConfiguration : IEntityTypeConfiguration<ThirdPartyBankAccount>
{
    public void Configure(EntityTypeBuilder<ThirdPartyBankAccount> builder)
    {
        builder.ToTable("third_party_bank_accounts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ThirdPartyType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.AccountHolder).HasMaxLength(160).IsRequired();
        builder.Property(x => x.AccountNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Clabe).HasMaxLength(30);

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Bank).WithMany().HasForeignKey(x => x.BankId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class AccountingLedgerSnapshotConfiguration : IEntityTypeConfiguration<AccountingLedgerSnapshot>
{
    public void Configure(EntityTypeBuilder<AccountingLedgerSnapshot> builder)
    {
        builder.ToTable("accounting_ledger_snapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OpeningBalance).HasPrecision(18, 2);
        builder.Property(x => x.Debit).HasPrecision(18, 2);
        builder.Property(x => x.Credit).HasPrecision(18, 2);
        builder.Property(x => x.ClosingBalance).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CompanyId, x.AccountId, x.SnapshotDate });
    }
}

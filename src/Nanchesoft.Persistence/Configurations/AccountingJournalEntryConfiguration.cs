using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class AccountingJournalEntryConfiguration : IEntityTypeConfiguration<AccountingJournalEntry>
{
    public void Configure(EntityTypeBuilder<AccountingJournalEntry> builder)
    {
        builder.ToTable("accounting_journal_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Folio).HasMaxLength(32).IsRequired();
        builder.Property(x => x.EntryType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(128);
        builder.Property(x => x.Concept).HasMaxLength(512);
        builder.Property(x => x.TotalDebit).HasPrecision(18, 2);
        builder.Property(x => x.TotalCredit).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CompanyId, x.Folio }).IsUnique();
    }
}

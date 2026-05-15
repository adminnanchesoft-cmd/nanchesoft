using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class AccountingJournalEntryLineConfiguration : IEntityTypeConfiguration<AccountingJournalEntryLine>
{
    public void Configure(EntityTypeBuilder<AccountingJournalEntryLine> builder)
    {
        builder.ToTable("accounting_journal_entry_lines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.Property(x => x.Debit).HasPrecision(18, 2);
        builder.Property(x => x.Credit).HasPrecision(18, 2);
    }
}

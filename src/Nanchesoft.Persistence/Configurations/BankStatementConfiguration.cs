using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class BankStatementConfiguration : IEntityTypeConfiguration<BankStatement>
{
    public void Configure(EntityTypeBuilder<BankStatement> builder)
    {
        builder.ToTable("bank_statements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Source).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(160);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.OpeningBalance).HasPrecision(18, 2);
        builder.Property(x => x.ClosingBalance).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CompanyId, x.BankAccountId, x.StatementDate });
        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.BankAccount).WithMany().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Entries).WithOne(x => x.BankStatement).HasForeignKey(x => x.BankStatementId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class BankStatementEntryConfiguration : IEntityTypeConfiguration<BankStatementEntry>
{
    public void Configure(EntityTypeBuilder<BankStatementEntry> builder)
    {
        builder.ToTable("bank_statement_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(255);
        builder.Property(x => x.Reference).HasMaxLength(160);
        builder.Property(x => x.AmountIn).HasPrecision(18, 2);
        builder.Property(x => x.AmountOut).HasPrecision(18, 2);
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.BankAccountId, x.EntryDate });
        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.BankAccount).WithMany().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

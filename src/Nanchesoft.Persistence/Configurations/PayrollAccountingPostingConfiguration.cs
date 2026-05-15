using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PayrollAccountingPostingConfiguration : IEntityTypeConfiguration<PayrollAccountingPosting>
{
    public void Configure(EntityTypeBuilder<PayrollAccountingPosting> builder)
    {
        builder.ToTable("payroll_accounting_postings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PostingCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.LedgerBook).HasMaxLength(40).IsRequired();
        builder.Property(x => x.JournalNumber).HasMaxLength(40);
        builder.Property(x => x.DebitAmount).HasPrecision(18, 2);
        builder.Property(x => x.CreditAmount).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ExportReference).HasMaxLength(180);
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasIndex(x => new { x.PayrollRunId, x.PostingCode }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollRun).WithMany().HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Restrict);
    }
}

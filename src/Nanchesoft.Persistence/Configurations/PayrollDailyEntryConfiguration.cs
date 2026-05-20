using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PayrollDailyEntryConfiguration : IEntityTypeConfiguration<PayrollDailyEntry>
{
    public void Configure(EntityTypeBuilder<PayrollDailyEntry> builder)
    {
        builder.ToTable("payroll_daily_entries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Notes).HasMaxLength(400);
        builder.Property(x => x.Status).HasMaxLength(30);

        builder.Property(x => x.Units).HasPrecision(18, 4);
        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.CompanyId, x.PayrollPeriodId, x.EmployeeId, x.WorkDate });
        builder.HasIndex(x => new { x.CompanyId, x.PayrollPeriodId, x.EmployeeId, x.WorkDate, x.PayrollDayMnemonicId });

        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollPeriod).WithMany().HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollDayMnemonic).WithMany().HasForeignKey(x => x.PayrollDayMnemonicId).OnDelete(DeleteBehavior.Restrict);
    }
}

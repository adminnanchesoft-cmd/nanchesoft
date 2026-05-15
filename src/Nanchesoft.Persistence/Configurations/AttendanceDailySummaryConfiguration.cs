using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class AttendanceDailySummaryConfiguration : IEntityTypeConfiguration<AttendanceDailySummary>
{
    public void Configure(EntityTypeBuilder<AttendanceDailySummary> builder)
    {
        builder.ToTable("payroll_attendance_daily_summaries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.WorkedHours).HasPrecision(18, 4);
        builder.Property(x => x.OvertimeHours).HasPrecision(18, 4);
        builder.Property(x => x.AbsenceUnits).HasPrecision(18, 4);
        builder.Property(x => x.DayType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(800);

        builder.HasIndex(x => new { x.CompanyId, x.EmployeeId, x.WorkDate }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollPeriod).WithMany().HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Restrict);
    }
}

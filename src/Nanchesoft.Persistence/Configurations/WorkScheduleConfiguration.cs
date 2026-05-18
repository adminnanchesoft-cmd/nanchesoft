using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class WorkScheduleConfiguration : IEntityTypeConfiguration<WorkSchedule>
{
    public void Configure(EntityTypeBuilder<WorkSchedule> builder)
    {
        builder.ToTable("hr_work_schedules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(600);
        builder.Property(x => x.WeeklyHours).HasPrecision(18, 2);

        // Per-day time fields (HH:mm format)
        builder.Property(x => x.MonEntryTime).HasMaxLength(8);
        builder.Property(x => x.MonLunchStartTime).HasMaxLength(8);
        builder.Property(x => x.MonLunchEndTime).HasMaxLength(8);
        builder.Property(x => x.MonExitTime).HasMaxLength(8);

        builder.Property(x => x.TueEntryTime).HasMaxLength(8);
        builder.Property(x => x.TueLunchStartTime).HasMaxLength(8);
        builder.Property(x => x.TueLunchEndTime).HasMaxLength(8);
        builder.Property(x => x.TueExitTime).HasMaxLength(8);

        builder.Property(x => x.WedEntryTime).HasMaxLength(8);
        builder.Property(x => x.WedLunchStartTime).HasMaxLength(8);
        builder.Property(x => x.WedLunchEndTime).HasMaxLength(8);
        builder.Property(x => x.WedExitTime).HasMaxLength(8);

        builder.Property(x => x.ThuEntryTime).HasMaxLength(8);
        builder.Property(x => x.ThuLunchStartTime).HasMaxLength(8);
        builder.Property(x => x.ThuLunchEndTime).HasMaxLength(8);
        builder.Property(x => x.ThuExitTime).HasMaxLength(8);

        builder.Property(x => x.FriEntryTime).HasMaxLength(8);
        builder.Property(x => x.FriLunchStartTime).HasMaxLength(8);
        builder.Property(x => x.FriLunchEndTime).HasMaxLength(8);
        builder.Property(x => x.FriExitTime).HasMaxLength(8);

        builder.Property(x => x.SatEntryTime).HasMaxLength(8);
        builder.Property(x => x.SatLunchStartTime).HasMaxLength(8);
        builder.Property(x => x.SatLunchEndTime).HasMaxLength(8);
        builder.Property(x => x.SatExitTime).HasMaxLength(8);

        builder.Property(x => x.SunEntryTime).HasMaxLength(8);
        builder.Property(x => x.SunLunchStartTime).HasMaxLength(8);
        builder.Property(x => x.SunLunchEndTime).HasMaxLength(8);
        builder.Property(x => x.SunExitTime).HasMaxLength(8);

        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.WorkShift).WithMany().HasForeignKey(x => x.WorkShiftId).OnDelete(DeleteBehavior.Restrict);
    }
}

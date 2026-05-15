using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class AttendancePunchConfiguration : IEntityTypeConfiguration<AttendancePunch>
{
    public void Configure(EntityTypeBuilder<AttendancePunch> builder)
    {
        builder.ToTable("hr_attendance_punches");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PunchType).HasMaxLength(20);
        builder.Property(x => x.Source).HasMaxLength(30);
        builder.Property(x => x.DeviceName).HasMaxLength(120);
        builder.Property(x => x.DeviceSerial).HasMaxLength(120);
        builder.Property(x => x.ExternalReference).HasMaxLength(120);
        builder.Property(x => x.Status).HasMaxLength(30);
        builder.Property(x => x.Notes).HasMaxLength(600);

        builder.HasIndex(x => new { x.CompanyId, x.EmployeeId, x.PunchDateTime });

        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

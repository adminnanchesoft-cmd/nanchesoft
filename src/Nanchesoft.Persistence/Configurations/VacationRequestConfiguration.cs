using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class VacationRequestConfiguration : IEntityTypeConfiguration<VacationRequest>
{
    public void Configure(EntityTypeBuilder<VacationRequest> builder)
    {
        builder.ToTable("hr_vacation_requests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Folio).HasMaxLength(40).IsRequired();
        builder.Property(x => x.RequestedDays).HasPrecision(18, 2);
        builder.Property(x => x.ApprovedDays).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ApprovedBy).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(600);

        builder.HasIndex(x => new { x.CompanyId, x.Folio }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LeaveType).WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

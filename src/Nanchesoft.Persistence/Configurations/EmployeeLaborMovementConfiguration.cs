using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class EmployeeLaborMovementConfiguration : IEntityTypeConfiguration<EmployeeLaborMovement>
{
    public void Configure(EntityTypeBuilder<EmployeeLaborMovement> builder)
    {
        builder.ToTable("hr_employee_movements");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MovementCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.MovementType).HasMaxLength(40).IsRequired();
        builder.Property(x => x.PreviousValue).HasMaxLength(180);
        builder.Property(x => x.NewValue).HasMaxLength(180);
        builder.Property(x => x.SalaryBefore).HasPrecision(18,2);
        builder.Property(x => x.SalaryAfter).HasPrecision(18,2);
        builder.Property(x => x.AuthorizedBy).HasMaxLength(120);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(800);

        builder.HasIndex(x => new { x.CompanyId, x.MovementCode }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Position).WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);
    }
}

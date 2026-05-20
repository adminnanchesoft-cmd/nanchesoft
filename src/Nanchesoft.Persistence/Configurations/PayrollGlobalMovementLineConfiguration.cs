using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PayrollGlobalMovementLineConfiguration : IEntityTypeConfiguration<PayrollGlobalMovementLine>
{
    public void Configure(EntityTypeBuilder<PayrollGlobalMovementLine> builder)
    {
        builder.ToTable("payroll_global_movement_lines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasMaxLength(30);
        builder.Property(x => x.AppliedBy).HasMaxLength(160);
        builder.Property(x => x.Notes).HasMaxLength(600);

        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);

        builder.HasIndex(x => new { x.PayrollGlobalMovementId, x.EmployeeId, x.PayrollPeriodId }).IsUnique();
        builder.HasIndex(x => new { x.CompanyId, x.EmployeeId });

        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollGlobalMovement).WithMany().HasForeignKey(x => x.PayrollGlobalMovementId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollPeriod).WithMany().HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Restrict);
    }
}
